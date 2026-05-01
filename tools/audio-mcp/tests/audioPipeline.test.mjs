import test from "node:test";
import assert from "node:assert/strict";
import { mkdtemp, mkdir, readFile, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import path from "node:path";

import {
  createDefaultBatch,
  importDownloadedAssets,
  syncSoundCatalog,
} from "../dist/audioPipeline.js";

test("createDefaultBatch creates the default UITK SFX manifest", () => {
  const manifest = createDefaultBatch();

  assert.equal(manifest.schemaVersion, 1);
  assert.equal(manifest.items.length, 12);
  assert.ok(manifest.items.every((item) => item.provider === "sandraschi-suno-mcp"));
  assert.ok(manifest.items.some((item) => item.soundKey === "garage_save"));
});

test("importDownloadedAssets rejects targets outside approved Unity audio roots", async () => {
  const repoRoot = await mkdtemp(path.join(tmpdir(), "jg-audio-mcp-"));
  const previousCwd = process.cwd();
  process.chdir(repoRoot);

  try {
    await mkdir("artifacts/audio/inbox", { recursive: true });
    await writeFile("artifacts/audio/inbox/ui_click.mp3", "fake-audio", "utf8");

    const manifest = createDefaultBatch({ soundKeys: ["ui_click"] });
    manifest.items[0].targetPath = "Assets/Resources/ui_click.mp3";

    const results = await importDownloadedAssets(manifest, { dryRun: true });

    assert.equal(results.length, 1);
    assert.equal(results[0].status, "failed");
    assert.match(results[0].error ?? "", /Assets\/Audio\/UI/);
  } finally {
    process.chdir(previousCwd);
  }
});

test("importDownloadedAssets copies matching downloaded files into Unity audio roots", async () => {
  const repoRoot = await mkdtemp(path.join(tmpdir(), "jg-audio-mcp-"));
  const previousCwd = process.cwd();
  process.chdir(repoRoot);

  try {
    await mkdir("artifacts/audio/inbox", { recursive: true });
    await writeFile("artifacts/audio/inbox/ui_click.wav", "fake-audio", "utf8");

    const manifest = createDefaultBatch({ soundKeys: ["ui_click"] });

    const results = await importDownloadedAssets(manifest);
    const copied = await readFile("Assets/Audio/UI/ui_click.wav", "utf8");

    assert.equal(results.length, 1);
    assert.equal(results[0].status, "downloaded");
    assert.equal(results[0].targetPath, "Assets/Audio/UI/ui_click.wav");
    assert.equal(manifest.items[0].targetPath, "Assets/Audio/UI/ui_click.wav");
    assert.equal(copied, "fake-audio");
  } finally {
    process.chdir(previousCwd);
  }
});

test("syncSoundCatalog adds and updates entries using Unity meta GUIDs", async () => {
  const repoRoot = await mkdtemp(path.join(tmpdir(), "jg-audio-mcp-"));
  const previousCwd = process.cwd();
  process.chdir(repoRoot);

  try {
    await mkdir("Assets/Data/Sound", { recursive: true });
    await mkdir("Assets/Audio/UI", { recursive: true });
    await writeFile(
      "Assets/Data/Sound/SoundCatalog.asset",
      [
        "%YAML 1.1",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  entries:",
        "  - key: ui_click",
        "    clip: {fileID: 8300000, guid: oldguid, type: 3}",
        "    volume: 0.8",
        "    spatialBlend: 0",
        "    cooldown: 0.05",
        "    channel: 0",
        "    loop: 0",
        "",
      ].join("\n"),
      "utf8",
    );
    await writeFile("Assets/Audio/UI/ui_click.mp3.meta", "fileFormatVersion: 2\nguid: 11112222333344445555666677778888\n", "utf8");
    await writeFile("Assets/Audio/UI/garage_save.mp3.meta", "fileFormatVersion: 2\nguid: aaaabbbbccccddddeeeeffff00001111\n", "utf8");

    const manifest = createDefaultBatch({ soundKeys: ["ui_click", "garage_save"] });
    for (const item of manifest.items) {
      item.status = "downloaded";
    }

    const result = await syncSoundCatalog(manifest);
    const catalog = await readFile("Assets/Data/Sound/SoundCatalog.asset", "utf8");

    assert.deepEqual(result.duplicateKeys, []);
    assert.equal(result.results.find((entry) => entry.soundKey === "ui_click")?.action, "update");
    assert.equal(result.results.find((entry) => entry.soundKey === "garage_save")?.action, "add");
    assert.match(catalog, /guid: 11112222333344445555666677778888/);
    assert.match(catalog, /- key: garage_save/);
  } finally {
    process.chdir(previousCwd);
  }
});
