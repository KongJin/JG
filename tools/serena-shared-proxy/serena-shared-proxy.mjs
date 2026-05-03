#!/usr/bin/env node
import { createHash } from "node:crypto";
import { spawn, spawnSync } from "node:child_process";
import fs from "node:fs";
import net from "node:net";
import os from "node:os";
import path from "node:path";
import process from "node:process";
import { fileURLToPath } from "node:url";

const SERVER_INFO = {
  name: "jg-serena-shared-proxy",
  version: "0.1.0"
};

const DEFAULT_PROTOCOL_VERSION = "2024-11-05";
const DEFAULT_BACKEND_ARGS = [
  "start-mcp-server",
  "--context=codex",
  "--project-from-cwd",
  "--enable-web-dashboard=false",
  "--open-web-dashboard=false",
  "--enable-gui-log-window=false",
  "--log-level=WARNING"
];
const BACKEND_START_TIMEOUT_MS = Number(process.env.SERENA_SHARED_PROXY_BACKEND_START_TIMEOUT_MS || 7 * 60 * 1000);
const BACKEND_TOOL_CALL_TIMEOUT_MS = Number(process.env.SERENA_SHARED_PROXY_BACKEND_TOOL_CALL_TIMEOUT_MS || 7 * 60 * 1000);

const PROXY_STATUS_TOOL = {
  name: "serena_shared_proxy_status",
  description: "Report the shared Serena proxy daemon and backend readiness state.",
  inputSchema: {
    type: "object",
    properties: {},
    additionalProperties: false
  },
  annotations: {
    readOnlyHint: true,
    destructiveHint: false,
    idempotentHint: true,
    openWorldHint: false
  }
};

const FALLBACK_SERENA_TOOLS = [
  "search_for_pattern",
  "get_symbols_overview",
  "find_symbol",
  "find_referencing_symbols",
  "replace_symbol_body",
  "insert_after_symbol",
  "insert_before_symbol",
  "rename_symbol",
  "safe_delete_symbol",
  "write_memory",
  "read_memory",
  "list_memories",
  "delete_memory",
  "rename_memory",
  "edit_memory",
  "activate_project",
  "get_current_config",
  "check_onboarding_performed",
  "onboarding",
  "initial_instructions"
].map((name) => ({
  name,
  description: `Serena backend tool '${name}'. The shared proxy may show this fallback schema while Serena is warming up.`,
  inputSchema: {
    type: "object",
    additionalProperties: true
  }
}));

const args = parseArgs(process.argv.slice(2));
const repoRoot = path.resolve(args["repo-root"] || process.env.SERENA_SHARED_PROXY_REPO_ROOT || process.cwd());
const projectKey = hashText(repoRoot).slice(0, 10);
const port = Number(args.port || process.env.SERENA_SHARED_PROXY_PORT || deterministicPort(repoRoot));
const host = process.env.SERENA_SHARED_PROXY_HOST || "127.0.0.1";
const logDir = process.env.SERENA_SHARED_PROXY_LOG_DIR || path.join(repoRoot, "Temp", "SerenaSharedProxy");
const logPath = path.join(logDir, `serena-shared-proxy-${projectKey}.log`);
const toolsCachePath = path.join(logDir, `serena-tools-${projectKey}.json`);

async function runProxy() {
  log(`proxy start repo=${repoRoot} daemon=${host}:${port}`);
  await ensureDaemon();

  const parser = new FramedJsonParser(process.stdin, async (message) => {
    try {
      await handleProxyMessage(message);
    } catch (error) {
      log("proxy message error", error);
      if (message && Object.prototype.hasOwnProperty.call(message, "id")) {
        sendMcpError(message.id, -32603, errorMessage(error));
      }
    }
  });
  parser.start();
  process.stdin.on("end", () => process.exit(0));
  process.stdin.resume();
}

async function handleProxyMessage(message) {
  if (!message || typeof message !== "object" || typeof message.method !== "string") {
    return;
  }

  const hasId = Object.prototype.hasOwnProperty.call(message, "id");
  const id = hasId ? message.id : undefined;

  if (message.method.startsWith("notifications/")) {
    return;
  }

  switch (message.method) {
    case "initialize":
      if (hasId) {
        sendMcpResult(id, {
          protocolVersion: selectProtocolVersion(message.params),
          capabilities: { tools: {} },
          serverInfo: SERVER_INFO
        });
      }
      return;

    case "ping":
      if (hasId) {
        sendMcpResult(id, {});
      }
      return;

    case "tools/list": {
      if (!hasId) {
        return;
      }
      const response = await requestDaemon({ type: "tools/list" }, 10000);
      sendMcpResult(id, { tools: response.tools || [] });
      return;
    }

    case "tools/call": {
      if (!hasId) {
        return;
      }
      const params = message.params && typeof message.params === "object" ? message.params : {};
      const response = await requestDaemon({
        type: "tools/call",
        name: params.name,
        arguments: params.arguments && typeof params.arguments === "object" ? params.arguments : {}
      }, BACKEND_TOOL_CALL_TIMEOUT_MS + 30000);
      sendMcpResult(id, response.result || { content: [] });
      return;
    }

    case "resources/list":
      if (hasId) {
        sendMcpResult(id, { resources: [] });
      }
      return;

    case "prompts/list":
      if (hasId) {
        sendMcpResult(id, { prompts: [] });
      }
      return;

    case "shutdown":
      if (hasId) {
        sendMcpResult(id, null);
      }
      return;

    default:
      if (hasId) {
        sendMcpError(id, -32601, `Method not found: ${message.method}`);
      }
  }
}

async function ensureDaemon() {
  if (await canReachDaemon()) {
    return;
  }

  fs.mkdirSync(logDir, { recursive: true });
  const child = spawn(process.execPath, [
    fileURLToPath(import.meta.url),
    "--daemon",
    "--repo-root",
    repoRoot,
    "--port",
    String(port)
  ], {
    cwd: repoRoot,
    detached: true,
    stdio: "ignore",
    env: {
      ...process.env,
      SERENA_SHARED_PROXY_REPO_ROOT: repoRoot,
      SERENA_SHARED_PROXY_PORT: String(port),
      SERENA_SHARED_PROXY_LOG_DIR: logDir
    }
  });
  child.unref();
  log(`spawned daemon pid=${child.pid}`);

  const deadline = Date.now() + 30000;
  while (Date.now() < deadline) {
    if (await canReachDaemon()) {
      return;
    }
    await sleep(250);
  }

  throw new Error(`Serena shared daemon did not become reachable at ${host}:${port}`);
}

async function canReachDaemon() {
  try {
    const response = await requestDaemon({ type: "health" }, 1000);
    return response && response.ok === true;
  } catch {
    return false;
  }
}

async function runDaemon() {
  fs.mkdirSync(logDir, { recursive: true });
  log(`daemon start repo=${repoRoot} listen=${host}:${port}`);

  const backend = new SerenaBackend(repoRoot);
  const server = net.createServer((socket) => {
    let buffer = "";
    socket.setEncoding("utf8");
    socket.on("data", (chunk) => {
      buffer += chunk;
      let newline;
      while ((newline = buffer.indexOf("\n")) !== -1) {
        const line = buffer.slice(0, newline).trim();
        buffer = buffer.slice(newline + 1);
        if (line.length === 0) {
          continue;
        }
        handleDaemonRequestLine(socket, backend, line).catch((error) => {
          writeLine(socket, { ok: false, error: errorMessage(error) });
        });
      }
    });
  });

  server.on("error", (error) => {
    if (error && error.code === "EADDRINUSE") {
      log(`daemon port already in use ${host}:${port}; exiting duplicate`);
      process.exit(0);
    }
    throw error;
  });

  server.listen(port, host);

  const idleTimeoutMs = Number(process.env.SERENA_SHARED_PROXY_IDLE_TIMEOUT_MS || 30 * 60 * 1000);
  let lastRequestAt = Date.now();
  setInterval(() => {
    if (Date.now() - lastRequestAt > idleTimeoutMs) {
      log(`daemon idle timeout after ${idleTimeoutMs}ms`);
      backend.stop();
      cleanupRepoLanguageServers({ wait: true });
      process.exit(0);
    }
  }, Math.min(idleTimeoutMs, 60000)).unref();

  async function handleDaemonRequestLine(socket, backend, line) {
    lastRequestAt = Date.now();
    let request;
    try {
      request = JSON.parse(line);
    } catch {
      writeLine(socket, { ok: false, error: "Invalid JSON request." });
      return;
    }

    switch (request.type) {
      case "health":
        writeLine(socket, {
          ok: true,
          pid: process.pid,
          repoRoot,
          backendPid: backend.pid,
          initializing: backend.isInitializing,
          initialized: backend.initialized
        });
        return;

      case "tools/list": {
        const tools = backend.listToolsFast();
        writeLine(socket, { ok: true, tools });
        return;
      }

      case "tools/call": {
        if (typeof request.name !== "string" || request.name.length === 0) {
          writeLine(socket, { ok: false, error: "Missing tool name." });
          return;
        }
        if (request.name === PROXY_STATUS_TOOL.name) {
          writeLine(socket, { ok: true, result: buildStatusToolResult(backend) });
          return;
        }
        const result = await backend.callTool(request.name, request.arguments || {});
        writeLine(socket, { ok: true, result });
        return;
      }

      default:
        writeLine(socket, { ok: false, error: `Unknown daemon request type: ${request.type}` });
    }
  }
}

class SerenaBackend {
  constructor(cwd) {
    this.cwd = cwd;
    this.process = null;
    this.parser = null;
    this.nextId = 1;
    this.pending = new Map();
    this.initializing = null;
    this.initialized = false;
    this.tools = null;
    this.queue = Promise.resolve();
  }

  get pid() {
    return this.process ? this.process.pid : null;
  }

  get isInitializing() {
    return this.initializing !== null;
  }

  stop() {
    if (this.process) {
      killProcessTree(this.process.pid);
      this.process = null;
    }
  }

  async listTools() {
    await this.ensureInitialized();
    return withProxyTools(this.tools || []);
  }

  listToolsFast() {
    if (this.tools) {
      return withProxyTools(this.tools);
    }

    this.ensureInitialized().catch((error) => {
      log("backend warmup failed", error);
    });
    return withProxyTools(loadCachedTools() || FALLBACK_SERENA_TOOLS);
  }

  async callTool(name, args) {
    await this.ensureInitialized();
    return this.enqueue(async () => {
      const response = await this.request("tools/call", { name, arguments: args }, BACKEND_TOOL_CALL_TIMEOUT_MS);
      return response || { content: [] };
    });
  }

  enqueue(fn) {
    const next = this.queue.then(fn, fn);
    this.queue = next.catch(() => {});
    return next;
  }

  async ensureInitialized() {
    if (this.initialized && this.process) {
      return;
    }
    if (this.initializing) {
      return this.initializing;
    }

    this.initializing = this.startAndInitialize().finally(() => {
      this.initializing = null;
    });
    return this.initializing;
  }

  async startAndInitialize() {
    this.startProcess();
    const init = await this.request("initialize", {
      protocolVersion: DEFAULT_PROTOCOL_VERSION,
      capabilities: {},
      clientInfo: {
        name: "jg-serena-shared-daemon",
        version: "0.1.0"
      }
    }, BACKEND_START_TIMEOUT_MS);
    log(`backend initialized protocol=${init?.protocolVersion || "unknown"}`);
    this.sendNotification("notifications/initialized", {});
    const toolsResponse = await this.request("tools/list", {}, BACKEND_START_TIMEOUT_MS);
    this.tools = Array.isArray(toolsResponse?.tools) ? toolsResponse.tools : [];
    this.initialized = true;
    log(`backend tools loaded count=${this.tools.length}`);
    saveCachedTools(this.tools);
  }

  startProcess() {
    if (this.process) {
      return;
    }

    const command = process.env.SERENA_SHARED_PROXY_BACKEND_COMMAND || "C:\\Users\\SOL\\.local\\bin\\serena.exe";
    const backendArgs = parseBackendArgs();
    log(`starting backend command=${command} args=${JSON.stringify(backendArgs)} cwd=${this.cwd}`);
    this.process = spawn(command, backendArgs, {
      cwd: this.cwd,
      stdio: ["pipe", "pipe", "pipe"],
      windowsHide: true,
      env: process.env
    });
    this.process.stderr.on("data", (chunk) => {
      log(`backend stderr: ${chunk.toString("utf8").trimEnd()}`);
    });
    this.process.on("exit", (code, signal) => {
      log(`backend exit code=${code} signal=${signal}`);
      this.process = null;
      this.initialized = false;
      this.tools = null;
      for (const pending of this.pending.values()) {
        pending.reject(new Error(`Serena backend exited code=${code} signal=${signal}`));
      }
      this.pending.clear();
    });
    this.parser = new FramedJsonParser(this.process.stdout, (message) => this.handleBackendMessage(message));
    this.parser.start();
  }

  handleBackendMessage(message) {
    if (!message || typeof message !== "object") {
      return;
    }
    if (!Object.prototype.hasOwnProperty.call(message, "id")) {
      return;
    }
    const key = String(message.id);
    const pending = this.pending.get(key);
    if (!pending) {
      return;
    }
    this.pending.delete(key);
    if (message.error) {
      pending.reject(new Error(message.error.message || JSON.stringify(message.error)));
    } else {
      pending.resolve(message.result);
    }
  }

  request(method, params, timeoutMs) {
    this.startProcess();
    const id = this.nextId++;
    const message = {
      jsonrpc: "2.0",
      id,
      method,
      params
    };
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        this.pending.delete(String(id));
        reject(new Error(`Serena backend request timeout: ${method}`));
      }, timeoutMs);
      this.pending.set(String(id), {
        resolve: (value) => {
          clearTimeout(timer);
          resolve(value);
        },
        reject: (error) => {
          clearTimeout(timer);
          reject(error);
        }
      });
      this.send(message);
    });
  }

  sendNotification(method, params) {
    this.send({
      jsonrpc: "2.0",
      method,
      params
    });
  }

  send(message) {
    if (!this.process || !this.process.stdin.writable) {
      throw new Error("Serena backend stdin is not writable.");
    }
    const body = Buffer.from(JSON.stringify(message), "utf8");
    this.process.stdin.write(`Content-Length: ${body.length}\r\n\r\n`);
    this.process.stdin.write(body);
  }
}

class FramedJsonParser {
  constructor(stream, onMessage) {
    this.stream = stream;
    this.onMessage = onMessage;
    this.buffer = Buffer.alloc(0);
  }

  start() {
    this.stream.on("data", (chunk) => {
      this.buffer = Buffer.concat([this.buffer, chunk]);
      this.process();
    });
  }

  process() {
    while (true) {
      const headerEnd = this.buffer.indexOf("\r\n\r\n");
      if (headerEnd === -1) {
        return;
      }
      const headerText = this.buffer.slice(0, headerEnd).toString("utf8");
      const length = extractContentLength(headerText);
      if (length === null) {
        log(`invalid MCP frame header: ${headerText}`);
        this.buffer = Buffer.alloc(0);
        return;
      }
      const bodyStart = headerEnd + 4;
      const bodyEnd = bodyStart + length;
      if (this.buffer.length < bodyEnd) {
        return;
      }
      const body = this.buffer.slice(bodyStart, bodyEnd);
      this.buffer = this.buffer.slice(bodyEnd);
      try {
        const message = JSON.parse(body.toString("utf8"));
        Promise.resolve(this.onMessage(message)).catch((error) => log("message handler failed", error));
      } catch (error) {
        log("invalid MCP JSON body", error);
      }
    }
  }
}

function requestDaemon(payload, timeoutMs) {
  return new Promise((resolve, reject) => {
    const socket = net.createConnection({ host, port });
    let buffer = "";
    const timer = setTimeout(() => {
      socket.destroy();
      reject(new Error(`Daemon request timeout: ${payload.type}`));
    }, timeoutMs);

    socket.setEncoding("utf8");
    socket.on("connect", () => {
      socket.write(`${JSON.stringify(payload)}\n`);
    });
    socket.on("data", (chunk) => {
      buffer += chunk;
      const newline = buffer.indexOf("\n");
      if (newline === -1) {
        return;
      }
      clearTimeout(timer);
      socket.end();
      const line = buffer.slice(0, newline).trim();
      try {
        const response = JSON.parse(line);
        if (response.ok === false) {
          reject(new Error(response.error || "Daemon request failed."));
        } else {
          resolve(response);
        }
      } catch (error) {
        reject(error);
      }
    });
    socket.on("error", (error) => {
      clearTimeout(timer);
      reject(error);
    });
  });
}

function sendMcpResult(id, result) {
  sendMcpMessage({ jsonrpc: "2.0", id, result });
}

function sendMcpError(id, code, message, data) {
  const error = { code, message };
  if (data !== undefined) {
    error.data = data;
  }
  sendMcpMessage({ jsonrpc: "2.0", id, error });
}

function sendMcpMessage(message) {
  const body = Buffer.from(JSON.stringify(message), "utf8");
  process.stdout.write(`Content-Length: ${body.length}\r\n\r\n`);
  process.stdout.write(body);
}

function writeLine(socket, payload) {
  socket.write(`${JSON.stringify(payload)}\n`);
}

function extractContentLength(headerText) {
  for (const line of headerText.split("\r\n")) {
    const separator = line.indexOf(":");
    if (separator <= 0) {
      continue;
    }
    if (line.slice(0, separator).trim().toLowerCase() === "content-length") {
      const parsed = Number.parseInt(line.slice(separator + 1).trim(), 10);
      return Number.isFinite(parsed) && parsed >= 0 ? parsed : null;
    }
  }
  return null;
}

function selectProtocolVersion(params) {
  const version = params && typeof params === "object" ? params.protocolVersion : "";
  return typeof version === "string" && version.length > 0 ? version : DEFAULT_PROTOCOL_VERSION;
}

function parseArgs(argv) {
  const parsed = {};
  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    if (!arg.startsWith("--")) {
      continue;
    }
    const key = arg.slice(2);
    if (key === "daemon") {
      parsed.daemon = true;
      continue;
    }
    parsed[key] = argv[i + 1];
    i++;
  }
  return parsed;
}

function parseBackendArgs() {
  const raw = process.env.SERENA_SHARED_PROXY_BACKEND_ARGS_JSON;
  if (!raw) {
    return DEFAULT_BACKEND_ARGS;
  }
  try {
    const parsed = JSON.parse(raw);
    if (Array.isArray(parsed) && parsed.every((item) => typeof item === "string")) {
      return parsed;
    }
  } catch {
    // Fall through to default args.
  }
  return DEFAULT_BACKEND_ARGS;
}

function withProxyTools(tools) {
  const backendTools = Array.isArray(tools) ? tools : [];
  return [
    PROXY_STATUS_TOOL,
    ...backendTools.filter((tool) => tool && tool.name !== PROXY_STATUS_TOOL.name)
  ];
}

function buildStatusToolResult(backend) {
  const status = {
    daemonPid: process.pid,
    repoRoot,
    port,
    backendPid: backend.pid,
    backendInitializing: backend.isInitializing,
    backendInitialized: backend.initialized,
    hasLiveToolSchema: Array.isArray(backend.tools),
    logPath,
    toolsCachePath
  };
  return {
    content: [
      {
        type: "text",
        text: JSON.stringify(status, null, 2)
      }
    ],
    structuredContent: status
  };
}

function loadCachedTools() {
  try {
    const parsed = JSON.parse(fs.readFileSync(toolsCachePath, "utf8"));
    if (Array.isArray(parsed.tools) && parsed.tools.every((tool) => tool && typeof tool.name === "string")) {
      return parsed.tools;
    }
  } catch {
    // Cache miss; use fallback schemas.
  }
  return null;
}

function saveCachedTools(tools) {
  try {
    fs.mkdirSync(logDir, { recursive: true });
    fs.writeFileSync(
      toolsCachePath,
      JSON.stringify({ updatedAt: new Date().toISOString(), tools }, null, 2),
      "utf8"
    );
  } catch (error) {
    log("failed to save Serena tools cache", error);
  }
}

function killProcessTree(pid) {
  if (!pid) {
    return;
  }
  if (process.platform === "win32") {
    try {
      spawn("taskkill.exe", ["/PID", String(pid), "/T", "/F"], {
        windowsHide: true,
        detached: true,
        stdio: "ignore"
      }).unref();
      cleanupRepoLanguageServers();
      return;
    } catch (error) {
      log(`taskkill failed for pid=${pid}`, error);
    }
  }
  try {
    process.kill(pid);
  } catch (error) {
    log(`process kill failed for pid=${pid}`, error);
  }
}

function cleanupRepoLanguageServers(options = {}) {
  const escapedRepo = repoRoot.replace(/'/g, "''");
  const command = [
    "$repo = '" + escapedRepo + "';",
    "function Stop-JGSerenaLanguageServers {",
    "$pattern = [regex]::Escape($repo + '\\.serena\\');",
    "$matches = Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -match $pattern -and $_.CommandLine -match 'OmniSharp' };",
    "$ids = @();",
    "$ids += $matches.ProcessId;",
    "$ids += $matches.ParentProcessId;",
    "$ids = $ids | Where-Object { $_ -and $_ -ne $PID } | Select-Object -Unique;",
    "$ids | ForEach-Object { Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue }",
    "}",
    "Stop-JGSerenaLanguageServers;",
    "Start-Sleep -Milliseconds 1500;",
    "Stop-JGSerenaLanguageServers"
  ].join(" ");
  try {
    if (options.wait) {
      spawnSync("powershell.exe", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command], {
        windowsHide: true,
        stdio: "ignore",
        timeout: 8000
      });
    } else {
      spawn("powershell.exe", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", command], {
        windowsHide: true,
        detached: true,
        stdio: "ignore"
      }).unref();
    }
  } catch (error) {
    log("repo language server cleanup failed", error);
  }
}

function deterministicPort(text) {
  const hash = createHash("sha256").update(text.toLowerCase()).digest();
  return 43000 + (hash.readUInt32BE(0) % 2000);
}

function hashText(text) {
  return createHash("sha256").update(text.toLowerCase()).digest("hex");
}

function log(message, error) {
  try {
    fs.mkdirSync(logDir, { recursive: true });
    const suffix = error ? ` ${errorMessage(error)}` : "";
    fs.appendFileSync(logPath, `${new Date().toISOString()} ${message}${suffix}${os.EOL}`, "utf8");
  } catch {
    // Logging must never corrupt MCP stdout.
  }
}

function errorMessage(error) {
  if (error instanceof Error) {
    return error.stack || error.message;
  }
  return String(error);
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

if (args.daemon) {
  runDaemon().catch((error) => {
    log("daemon fatal", error);
    process.exit(1);
  });
} else {
  runProxy().catch((error) => {
    log("proxy fatal", error);
    process.exit(1);
  });
}
