import process from "node:process";

import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { StitchProxy } from "@google/stitch-sdk";

async function main() {
  const proxy = new StitchProxy({
    apiKey: process.env.STITCH_API_KEY,
    name: "jg-stitch-proxy",
    version: "0.1.0",
  });

  const transport = new StdioServerTransport();
  await proxy.start(transport);
}

main().catch((error) => {
  const message = error instanceof Error ? error.stack || error.message : String(error);
  console.error(`[jg-stitch-proxy] ${message}`);
  process.exit(1);
});
