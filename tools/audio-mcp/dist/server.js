#!/usr/bin/env node
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";
import { createDefaultBatch, defaultCatalogPath, defaultInboxPath, defaultManifestPath, importDownloadedAssets, readManifest, syncSoundCatalog, writeManifest, } from "./audioPipeline.js";
const server = new McpServer({
    name: "jg-audio-mcp",
    version: "0.1.0",
});
const manifestPathSchema = z.string().default(defaultManifestPath);
const soundKeysSchema = z.array(z.string().min(1)).optional();
function ok(message, structuredContent) {
    return {
        content: [{ type: "text", text: message }],
        structuredContent,
    };
}
function fail(message, structuredContent = {}) {
    return {
        isError: true,
        content: [{ type: "text", text: message }],
        structuredContent,
    };
}
server.registerTool("audio_plan_sfx_batch", {
    title: "Plan JG SFX Batch",
    description: "Create or preview a JG UITK SFX manifest and prompt list for direct sandraschi/suno-mcp generation.",
    inputSchema: {
        manifestPath: manifestPathSchema,
        soundKeys: soundKeysSchema,
        targetRoot: z.string().default("Assets/Audio/UI"),
        provider: z.literal("sandraschi-suno-mcp").default("sandraschi-suno-mcp"),
        writeManifest: z.boolean().default(false),
    },
    annotations: {
        destructiveHint: false,
        idempotentHint: true,
        openWorldHint: false,
    },
}, async ({ manifestPath, soundKeys, targetRoot, provider, writeManifest }) => {
    try {
        const manifest = createDefaultBatch({ soundKeys, targetRoot, provider });
        if (writeManifest) {
            await writeManifestFile(manifestPath, manifest);
        }
        return ok(writeManifest
            ? `Planned ${manifest.items.length} SFX item(s) and wrote ${manifestPath}.`
            : `Planned ${manifest.items.length} SFX item(s) without writing a manifest.`, { manifestPath, written: writeManifest, items: manifest.items });
    }
    catch (error) {
        return fail(`audio_plan_sfx_batch failed: ${getErrorMessage(error)}`);
    }
});
server.registerTool("audio_import_downloaded_assets", {
    title: "Import Downloaded SFX Assets",
    description: "Copy audio files downloaded by sandraschi/suno-mcp from an inbox into approved Unity audio folders using manifest sound keys.",
    inputSchema: {
        manifestPath: manifestPathSchema,
        sourceDir: z.string().default(defaultInboxPath),
        soundKeys: soundKeysSchema,
        dryRun: z.boolean().default(false),
        overwrite: z.boolean().default(false),
        writeManifest: z.boolean().default(true),
    },
    annotations: {
        destructiveHint: false,
        idempotentHint: true,
        openWorldHint: false,
    },
}, async ({ manifestPath, sourceDir, soundKeys, dryRun, overwrite, writeManifest }) => {
    try {
        const manifest = await readManifest(manifestPath);
        const results = await importDownloadedAssets(manifest, {
            sourceDir,
            soundKeys,
            dryRun,
            overwrite,
        });
        if (writeManifest && !dryRun) {
            await writeManifestFile(manifestPath, manifest);
        }
        return ok(`Imported ${results.filter((result) => result.status === "downloaded").length} SFX asset(s).`, {
            manifestPath,
            sourceDir,
            dryRun,
            overwrite,
            results,
        });
    }
    catch (error) {
        return fail(`audio_import_downloaded_assets failed: ${getErrorMessage(error)}`);
    }
});
server.registerTool("audio_sync_sound_catalog", {
    title: "Sync Unity SoundCatalog",
    description: "Register downloaded audio files in Assets/Data/Sound/SoundCatalog.asset after Unity has generated .meta GUIDs.",
    inputSchema: {
        manifestPath: manifestPathSchema,
        catalogPath: z.string().default(defaultCatalogPath),
        soundKeys: soundKeysSchema,
        dryRun: z.boolean().default(false),
        writeManifest: z.boolean().default(true),
    },
    annotations: {
        destructiveHint: false,
        idempotentHint: true,
        openWorldHint: false,
    },
}, async ({ manifestPath, catalogPath, soundKeys, dryRun, writeManifest }) => {
    try {
        const manifest = await readManifest(manifestPath);
        const { results, duplicateKeys } = await syncSoundCatalog(manifest, {
            catalogPath,
            soundKeys,
            dryRun,
        });
        if (writeManifest && !dryRun) {
            await writeManifestFile(manifestPath, manifest);
        }
        const errored = results.filter((result) => result.action === "error");
        if (errored.length > 0 || duplicateKeys.length > 0) {
            return fail("SoundCatalog sync finished with issues.", {
                manifestPath,
                catalogPath,
                dryRun,
                duplicateKeys,
                results,
            });
        }
        return ok(`Synced ${results.filter((result) => result.action !== "skip").length} SoundCatalog entry item(s).`, {
            manifestPath,
            catalogPath,
            dryRun,
            duplicateKeys,
            results,
        });
    }
    catch (error) {
        return fail(`audio_sync_sound_catalog failed: ${getErrorMessage(error)}`);
    }
});
async function writeManifestFile(manifestPath, manifest) {
    await writeManifest(manifestPath, manifest);
}
function getErrorMessage(error) {
    return error instanceof Error ? error.message : String(error);
}
const transport = new StdioServerTransport();
await server.connect(transport);
