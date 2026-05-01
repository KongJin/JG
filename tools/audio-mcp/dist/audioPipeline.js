import { copyFile, mkdir, readdir, readFile, stat, writeFile } from "node:fs/promises";
import path from "node:path";
export const defaultManifestPath = "artifacts/audio/sfx/sfx-batch-manifest.json";
export const defaultCatalogPath = "Assets/Data/Sound/SoundCatalog.asset";
export const defaultTargetRoot = "Assets/Audio/UI";
export const defaultInboxPath = "artifacts/audio/inbox";
const defaultPrompts = {
    ui_click: "short futuristic game UI button click, 0.2 seconds, dry tactile digital tick, clean sci-fi interface, no melody, no vocals, isolated one-shot",
    ui_select: "short futuristic UI selection sound, 0.25 seconds, soft bright digital tick with tiny energy lift, no melody, no vocals, isolated one-shot",
    ui_confirm: "short futuristic UI confirm sound, 0.45 seconds, compact rising chime with soft mechanical lock, no melody, no vocals, isolated one-shot",
    ui_back: "short UI back or close sound, 0.3 seconds, low soft digital fold, restrained sci-fi interface, no melody, no vocals, isolated one-shot",
    ui_error: "short restrained UI error beep, 0.35 seconds, tactical soft warning, not harsh, no melody, no vocals, isolated one-shot",
    ui_retry: "short network retry UI sound, 0.45 seconds, two clean signal pings, subtle digital scan, no melody, no vocals, isolated one-shot",
    garage_slot_select: "short sci-fi garage unit slot select sound, 0.35 seconds, metallic slot click with soft servo, no melody, no vocals, isolated one-shot",
    garage_part_select: "short sci-fi garage part selection sound, 0.4 seconds, component scan tick with light mechanical texture, no melody, no vocals, isolated one-shot",
    garage_save: "short sci-fi garage save and deploy confirm sound, 0.65 seconds, satisfying mechanical lock-in plus energy pulse, no melody, no vocals, isolated one-shot",
    lobby_ready: "short tactical lobby ready confirm sound, 0.5 seconds, confident digital arming cue, clean and restrained, no melody, no vocals, isolated one-shot",
    battle_slot_select: "short tactical battle HUD slot select sound, 0.25 seconds, crisp immediate interface tick, subtle combat UI energy, no melody, no vocals, isolated one-shot",
    skill_select: "short skill card select sound, 0.4 seconds, clean energy card pickup with light sparkle, no melody, no vocals, isolated one-shot",
};
const defaultVolumes = {
    ui_error: 0.78,
    garage_save: 0.85,
    lobby_ready: 0.82,
    battle_slot_select: 0.72,
};
export function createDefaultBatch(options = {}) {
    const soundKeys = options.soundKeys?.length ? options.soundKeys : Object.keys(defaultPrompts);
    const provider = options.provider ?? "sandraschi-suno-mcp";
    const targetRoot = normalizeRepoPath(options.targetRoot ?? defaultTargetRoot);
    const now = new Date().toISOString();
    return {
        schemaVersion: 1,
        createdAt: now,
        updatedAt: now,
        providerDefault: provider,
        items: soundKeys.map((soundKey) => {
            const prompt = defaultPrompts[soundKey];
            if (!prompt) {
                throw new Error(`No default SFX prompt is registered for soundKey '${soundKey}'.`);
            }
            return {
                soundKey,
                prompt,
                provider,
                targetPath: `${targetRoot}/${soundKey}.mp3`,
                status: "planned",
                volume: defaultVolumes[soundKey] ?? 0.75,
                spatialBlend: 0,
                cooldown: 0.05,
            };
        }),
    };
}
export async function readManifest(manifestPath) {
    const absolutePath = resolveRepoPath(manifestPath);
    const text = await readFile(absolutePath, "utf8");
    const parsed = JSON.parse(text);
    validateManifest(parsed);
    return parsed;
}
export async function writeManifest(manifestPath, manifest) {
    const normalizedPath = normalizeRepoPath(manifestPath);
    const absolutePath = resolveRepoPath(normalizedPath);
    manifest.updatedAt = new Date().toISOString();
    await mkdir(path.dirname(absolutePath), { recursive: true });
    await writeFile(absolutePath, `${JSON.stringify(manifest, null, 2)}\n`, "utf8");
}
export async function importDownloadedAssets(manifest, options = {}) {
    const sourceDir = normalizeRepoPath(options.sourceDir ?? defaultInboxPath);
    const soundKeys = new Set(options.soundKeys ?? []);
    const candidates = manifest.items.filter((item) => item.targetPath &&
        (soundKeys.size === 0 || soundKeys.has(item.soundKey)) &&
        (item.status === "planned" || item.status === "complete" || item.status === "downloaded"));
    const inboxFiles = await findAudioFiles(sourceDir);
    const results = [];
    for (const item of candidates) {
        const sourcePath = findSourceForSoundKey(inboxFiles, item.soundKey);
        if (!sourcePath) {
            results.push({
                soundKey: item.soundKey,
                targetPath: normalizeRepoPath(item.targetPath),
                status: "failed",
                error: `No downloaded audio file found for '${item.soundKey}' under ${sourceDir}.`,
            });
            continue;
        }
        const sourceExtension = path.extname(sourcePath);
        const targetBase = removeExtension(normalizeRepoPath(item.targetPath));
        const normalizedTarget = `${targetBase}${sourceExtension}`;
        if (!isAllowedAudioTarget(normalizedTarget)) {
            results.push({
                soundKey: item.soundKey,
                sourcePath,
                targetPath: normalizedTarget,
                status: "failed",
                error: "Target path must be under Assets/Audio/UI or Assets/Audio/SFX.",
            });
            continue;
        }
        if (options.dryRun) {
            results.push({ soundKey: item.soundKey, sourcePath, targetPath: normalizedTarget, status: item.status });
            continue;
        }
        try {
            if (!options.overwrite && await pathExists(normalizedTarget)) {
                results.push({
                    soundKey: item.soundKey,
                    sourcePath,
                    targetPath: normalizedTarget,
                    status: "failed",
                    error: "Target already exists. Pass overwrite=true to replace it.",
                });
                continue;
            }
            const sourceAbsolutePath = resolveRepoPath(sourcePath);
            await mkdir(path.dirname(resolveRepoPath(normalizedTarget)), { recursive: true });
            await copyFile(sourceAbsolutePath, resolveRepoPath(normalizedTarget));
            const copied = await stat(resolveRepoPath(normalizedTarget));
            item.targetPath = normalizedTarget;
            item.status = "downloaded";
            results.push({
                soundKey: item.soundKey,
                sourcePath,
                targetPath: normalizedTarget,
                status: "downloaded",
                bytes: copied.size,
            });
        }
        catch (error) {
            results.push({
                soundKey: item.soundKey,
                sourcePath,
                targetPath: normalizedTarget,
                status: "failed",
                error: getErrorMessage(error),
            });
        }
    }
    return results;
}
export async function syncSoundCatalog(manifest, options = {}) {
    const catalogPath = normalizeRepoPath(options.catalogPath ?? defaultCatalogPath);
    const catalogText = await readFile(resolveRepoPath(catalogPath), "utf8");
    const catalog = parseSoundCatalog(catalogText);
    const soundKeys = new Set(options.soundKeys ?? []);
    const candidates = manifest.items.filter((item) => item.targetPath &&
        (item.status === "downloaded" || item.status === "catalog-synced") &&
        (soundKeys.size === 0 || soundKeys.has(item.soundKey)));
    const results = [];
    const entryByKey = new Map(catalog.entries.map((entry) => [entry.key, entry]));
    for (const item of candidates) {
        const targetPath = normalizeRepoPath(item.targetPath);
        try {
            const clipGuid = await readMetaGuid(`${targetPath}.meta`);
            const action = entryByKey.has(item.soundKey) ? "update" : "add";
            const entry = {
                key: item.soundKey,
                clipGuid,
                volume: item.volume,
                spatialBlend: item.spatialBlend,
                cooldown: item.cooldown,
                channel: 0,
                loop: 0,
            };
            entryByKey.set(item.soundKey, entry);
            item.status = "catalog-synced";
            results.push({ soundKey: item.soundKey, targetPath, action, clipGuid });
        }
        catch (error) {
            results.push({ soundKey: item.soundKey, targetPath, action: "error", error: getErrorMessage(error) });
        }
    }
    const nextEntries = catalog.entries.map((entry) => entryByKey.get(entry.key) ?? entry);
    for (const [key, entry] of entryByKey.entries()) {
        if (!catalog.entries.some((existing) => existing.key === key)) {
            nextEntries.push(entry);
        }
    }
    const duplicateKeys = getDuplicateKeys(nextEntries);
    if (!options.dryRun && results.some((result) => result.action === "add" || result.action === "update")) {
        const nextText = renderSoundCatalog(catalog.header, nextEntries);
        await writeFile(resolveRepoPath(catalogPath), nextText, "utf8");
    }
    return { results, duplicateKeys };
}
export function validateManifest(manifest) {
    if (manifest.schemaVersion !== 1 || !Array.isArray(manifest.items)) {
        throw new Error("Unsupported SFX manifest schema.");
    }
    const keys = new Set();
    for (const item of manifest.items) {
        if (!item.soundKey || !item.prompt || !item.provider || !item.targetPath || !item.status) {
            throw new Error("Each manifest item must include soundKey, prompt, provider, targetPath, and status.");
        }
        if (keys.has(item.soundKey)) {
            throw new Error(`Duplicate manifest soundKey '${item.soundKey}'.`);
        }
        keys.add(item.soundKey);
    }
}
function parseSoundCatalog(text) {
    const lines = text.replace(/\r\n/g, "\n").split("\n");
    const entriesLine = lines.findIndex((line) => line.trim() === "entries:");
    if (entriesLine < 0) {
        throw new Error("SoundCatalog asset does not contain an entries section.");
    }
    const header = lines.slice(0, entriesLine + 1);
    const entryLines = lines.slice(entriesLine + 1);
    const entries = [];
    let block = [];
    const flush = () => {
        if (block.length === 0) {
            return;
        }
        const key = matchValue(block, /^\s*-\skey:\s*(.+)$/);
        const clipGuid = matchValue(block, /^\s*clip:\s*\{fileID:\s*8300000,\s*guid:\s*([^,]+),\s*type:\s*3\}$/);
        if (!key || !clipGuid) {
            block = [];
            return;
        }
        entries.push({
            key,
            clipGuid,
            volume: parseNumber(matchValue(block, /^\s*volume:\s*(.+)$/), 1),
            spatialBlend: parseNumber(matchValue(block, /^\s*spatialBlend:\s*(.+)$/), 0),
            cooldown: parseNumber(matchValue(block, /^\s*cooldown:\s*(.+)$/), 0.05),
            channel: parseNumber(matchValue(block, /^\s*channel:\s*(.+)$/), 0),
            loop: parseNumber(matchValue(block, /^\s*loop:\s*(.+)$/), 0),
        });
        block = [];
    };
    for (const line of entryLines) {
        if (/^\s*-\skey:\s*/.test(line)) {
            flush();
            block = [line];
            continue;
        }
        if (block.length > 0) {
            block.push(line);
        }
    }
    flush();
    return { header, entries };
}
function renderSoundCatalog(header, entries) {
    const rendered = [...header];
    for (const entry of entries) {
        rendered.push(`  - key: ${entry.key}`);
        rendered.push(`    clip: {fileID: 8300000, guid: ${entry.clipGuid}, type: 3}`);
        rendered.push(`    volume: ${formatNumber(entry.volume)}`);
        rendered.push(`    spatialBlend: ${formatNumber(entry.spatialBlend)}`);
        rendered.push(`    cooldown: ${formatNumber(entry.cooldown)}`);
        rendered.push(`    channel: ${formatNumber(entry.channel)}`);
        rendered.push(`    loop: ${formatNumber(entry.loop)}`);
    }
    return `${rendered.join("\n")}\n`;
}
function matchValue(lines, regex) {
    for (const line of lines) {
        const match = regex.exec(line);
        if (match) {
            return match[1].trim();
        }
    }
    return undefined;
}
function parseNumber(value, fallback) {
    if (value === undefined) {
        return fallback;
    }
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : fallback;
}
function formatNumber(value) {
    return Number.isInteger(value) ? value.toFixed(0) : String(value);
}
function getDuplicateKeys(entries) {
    const counts = new Map();
    for (const entry of entries) {
        counts.set(entry.key, (counts.get(entry.key) ?? 0) + 1);
    }
    return [...counts.entries()].filter(([, count]) => count > 1).map(([key]) => key);
}
async function readMetaGuid(metaPath) {
    const text = await readFile(resolveRepoPath(metaPath), "utf8");
    const match = /^guid:\s*([0-9a-fA-F]+)$/m.exec(text);
    if (!match) {
        throw new Error(`Unity meta file is missing a guid: ${metaPath}`);
    }
    return match[1];
}
export function normalizeRepoPath(value) {
    return value.replace(/\\/g, "/").replace(/^\.?\//, "");
}
export function resolveRepoPath(repoPath) {
    return path.resolve(process.cwd(), normalizeRepoPath(repoPath));
}
export function isAllowedAudioTarget(repoPath) {
    const normalized = normalizeRepoPath(repoPath);
    return normalized.startsWith("Assets/Audio/UI/") || normalized.startsWith("Assets/Audio/SFX/");
}
async function findAudioFiles(sourceDir) {
    const root = resolveRepoPath(sourceDir);
    const entries = await readdir(root, { recursive: true, withFileTypes: true });
    return entries
        .filter((entry) => entry.isFile())
        .map((entry) => normalizeRepoPath(path.relative(process.cwd(), path.join(entry.parentPath, entry.name))))
        .filter((entry) => isSupportedAudioFile(entry))
        .sort((left, right) => left.localeCompare(right));
}
function findSourceForSoundKey(files, soundKey) {
    const expected = soundKey.toLowerCase();
    return files.find((file) => removeExtension(path.basename(file)).toLowerCase() === expected);
}
function isSupportedAudioFile(repoPath) {
    return [".mp3", ".wav", ".ogg", ".aif", ".aiff", ".flac"].includes(path.extname(repoPath).toLowerCase());
}
function removeExtension(repoPath) {
    const extension = path.extname(repoPath);
    return extension ? repoPath.slice(0, -extension.length) : repoPath;
}
export async function pathExists(repoPath) {
    try {
        await stat(resolveRepoPath(repoPath));
        return true;
    }
    catch {
        return false;
    }
}
function getErrorMessage(error) {
    return error instanceof Error ? error.message : String(error);
}
