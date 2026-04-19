import fs from "node:fs/promises";
import path from "node:path";
import process from "node:process";

import { StitchToolClient, stitch } from "@google/stitch-sdk";

const DEFAULT_PROJECT_TITLE = "JG UI Refresh - Lobby Garage GameScene";

const DEFAULT_DESIGN_SYSTEM = {
  displayName: "JG Tactical UI",
  theme: {
    colorMode: "DARK",
    headlineFont: "SPACE_GROTESK",
    bodyFont: "IBM_PLEX_SANS",
    roundness: "ROUND_EIGHT",
    customColor: "#F59E0B",
    colorVariant: "EXPRESSIVE",
    overrideSecondaryColor: "#5EB6FF",
    overrideNeutralColor: "#111827",
    designMd: `mobile-first tactical sci-fi dashboard
dark hangar atmosphere
orange primary CTA and blue secondary emphasis
bold but practical
compact cards with low wasted space
avoid marketing-site layouts
avoid generic SaaS/admin card grids
no desktop or web-only structure
empty states should feel finished, not blank
390x844 mobile-first baseline`,
  },
};

const JG_SCREEN_SPECS = [
  {
    name: "Lobby",
    prompt: `Create a mobile-first tactical sci-fi game lobby screen for a co-op defense game.

Purpose:
- this is the matchmaking home screen
- the room list must be the first thing the player reads

Required hierarchy:
- header card first
- rooms section second
- create room card third
- garage summary card fourth

Behavior and emphasis:
- create room is secondary, not hero
- garage entry is visible but quieter than room actions
- empty room state must still feel like a finished card, not a blank panel
- avoid decorative filler panels

Style:
- dark tactical sci-fi dashboard
- compact, readable, bold but practical
- mobile-first only
- no desktop/web-only patterns
- no generic SaaS/admin layout`,
  },
  {
    name: "Garage",
    prompt: `Create a mobile-first tactical sci-fi game garage screen for repeated short play sessions.

Purpose:
- this is a unit roster editing workspace
- slot selection must be the first structure the player reads

Required structure:
- slot selection first
- single continuous scroll body
- focused part editor for the selected part
- preview and summary should feel like finished cards
- fixed persistent Save Roster dock
- settings is only an auxiliary overlay trigger

Behavior and emphasis:
- the selected slot must be immediately obvious
- avoid cluttered editors
- avoid blank preview placeholders
- save roster must remain the clearest persistent action

Style:
- dark hangar atmosphere
- compact game dashboard
- bold but practical
- mobile-first only
- no desktop/web-only patterns
- no generic SaaS/admin layout`,
  },
  {
    name: "GameScene HUD",
    prompt: `Create a mobile-first tactical sci-fi battle HUD for a co-op defense game.

Purpose:
- this must read like a field-command HUD, not a character action HUD

Required hierarchy:
- top left: Wave, Countdown, short status
- top right: Core HP objective card
- bottom: summon command bar as the most important interaction zone

Behavior and emphasis:
- placement feedback and error state must read inside the same command language
- the player should quickly understand wave status, core status, and summon controls
- do not include a result or end-of-battle overlay in this version
- avoid decorative filler panels

Style:
- dark tactical command surface
- compact, readable, practical
- mobile-first only
- no desktop/web-only patterns
- no generic SaaS/admin layout`,
  },
];

function printHelp() {
  console.log(`Stitch SDK helper for JG

Usage:
  npm run stitch:help
  npm run stitch:list:projects
  npm run stitch:create:project -- --title <projectTitle>
  npm run stitch:create:design-system -- --project <projectId> [--design-md <markdown>]
  npm run stitch:generate:screen -- --project <projectId> --name <screenName> --prompt <prompt>
  npm run stitch:bootstrap:jg [-- --title <projectTitle>]
  npm run stitch:list:screens -- --project <projectId>
  npm run stitch:fetch:screen -- --url <stitchUrl>
  npm run stitch:fetch:screen -- --project <projectId> --screen <screenId>

Environment:
  STITCH_API_KEY=<your-api-key>

Examples:
  npm run stitch:create:project -- --title "${DEFAULT_PROJECT_TITLE}"
  npm run stitch:create:design-system -- --project 15511739434163767886
  npm run stitch:generate:screen -- --project 15511739434163767886 --name "Lobby" --prompt "A mobile-first tactical sci-fi lobby"
  npm run stitch:bootstrap:jg
  npm run stitch:list:screens -- --project 15511739434163767886
  npm run stitch:fetch:screen -- --url "https://stitch.withgoogle.com/projects/15511739434163767886?node-id=2225f2733de747d298f1e0c445fbb47c"
`);
}

function fail(message) {
  console.error(`Stitch error: ${message}`);
  process.exitCode = 1;
}

function requireApiKey() {
  if (!process.env.STITCH_API_KEY) {
    fail("STITCH_API_KEY is not set. Set it, then rerun the command.");
    return false;
  }

  return true;
}

function getArg(flag) {
  const index = process.argv.indexOf(flag);
  if (index === -1 || index + 1 >= process.argv.length) {
    return null;
  }

  return process.argv[index + 1];
}

function sanitize(value) {
  return String(value).replace(/[^a-zA-Z0-9._-]+/g, "_");
}

function parseStitchUrl(urlString) {
  const url = new URL(urlString);
  const match = url.pathname.match(/\/projects\/([^/]+)/);
  const screenId = url.searchParams.get("node-id");
  if (!match || !screenId) {
    throw new Error("Expected a Stitch project URL with /projects/<id>?node-id=<screenId>.");
  }

  return {
    projectId: match[1],
    screenId,
  };
}

async function ensureDir(dirPath) {
  await fs.mkdir(dirPath, { recursive: true });
}

async function downloadToFile(url, filePath) {
  const response = await fetch(url);
  if (!response.ok) {
    throw new Error(`Download failed: ${response.status} ${response.statusText}`);
  }

  const buffer = Buffer.from(await response.arrayBuffer());
  await fs.writeFile(filePath, buffer);
  return response.headers.get("content-type") || "application/octet-stream";
}

function extractGeneratedScreen(raw) {
  const screen = (raw?.outputComponents || [])
    .map((component) => component?.design?.screens?.[0])
    .find(Boolean);

  if (!screen) {
    throw new Error("Incomplete API response from generate_screen_from_text: expected at least one generated screen.");
  }

  return screen;
}

async function generateScreenData(projectId, prompt, deviceType = "MOBILE") {
  const client = new StitchToolClient({ apiKey: process.env.STITCH_API_KEY });

  try {
    const raw = await client.callTool("generate_screen_from_text", {
      projectId,
      prompt,
      deviceType,
    });

    return extractGeneratedScreen(raw);
  } finally {
    await client.close();
  }
}

async function listProjects() {
  if (!requireApiKey()) {
    return;
  }

  const projects = await stitch.projects();
  if (!projects.length) {
    console.log("No Stitch projects found.");
    return;
  }

  for (const project of projects) {
    console.log(project.projectId);
  }
}

async function createProject() {
  if (!requireApiKey()) {
    return;
  }

  const title = getArg("--title") || DEFAULT_PROJECT_TITLE;
  const project = await stitch.createProject(title);
  console.log(project.projectId);
}

async function createDesignSystem() {
  if (!requireApiKey()) {
    return;
  }

  const projectId = getArg("--project");
  if (!projectId) {
    fail("Missing --project <projectId>.");
    return;
  }

  const designMd = getArg("--design-md");
  const designSystem = designMd
    ? {
        ...DEFAULT_DESIGN_SYSTEM,
        theme: {
          ...DEFAULT_DESIGN_SYSTEM.theme,
          designMd,
        },
      }
    : DEFAULT_DESIGN_SYSTEM;
  const project = stitch.project(projectId);
  const created = await project.createDesignSystem(designSystem);
  await created.update(designSystem);
  console.log(created.assetId);
}

async function generateScreen() {
  if (!requireApiKey()) {
    return;
  }

  const projectId = getArg("--project");
  const prompt = getArg("--prompt");
  const screenName = getArg("--name") || "Untitled Screen";
  const deviceType = getArg("--device") || "MOBILE";

  if (!projectId) {
    fail("Missing --project <projectId>.");
    return;
  }

  if (!prompt) {
    fail("Missing --prompt <prompt>.");
    return;
  }

  const screen = await generateScreenData(projectId, `${screenName}\n\n${prompt}`, deviceType);
  console.log((screen.name || "").replace(/^projects\/[^/]+\/screens\//, "") || screen.id);
}

async function listScreens() {
  if (!requireApiKey()) {
    return;
  }

  const projectId = getArg("--project");
  if (!projectId) {
    fail("Missing --project <projectId>.");
    return;
  }

  const project = stitch.project(projectId);
  const screens = await project.screens();
  if (!screens.length) {
    console.log(`No screens found for project ${projectId}.`);
    return;
  }

  for (const screen of screens) {
    console.log(`${screen.screenId}`);
  }
}

async function fetchScreen() {
  if (!requireApiKey()) {
    return;
  }

  const url = getArg("--url");
  const projectArg = getArg("--project");
  const screenArg = getArg("--screen");

  let projectId = projectArg;
  let screenId = screenArg;

  if (url) {
    const parsed = parseStitchUrl(url);
    projectId = parsed.projectId;
    screenId = parsed.screenId;
  }

  if (!projectId || !screenId) {
    fail("Provide either --url <stitchUrl> or both --project <projectId> and --screen <screenId>.");
    return;
  }

  const project = stitch.project(projectId);
  const screen = await project.getScreen(screenId);
  const htmlUrl = await screen.getHtml();
  const imageUrl = await screen.getImage();

  const outDir = path.resolve("artifacts", "stitch", sanitize(projectId), sanitize(screenId));
  await ensureDir(outDir);

  const htmlPath = path.join(outDir, "screen.html");
  const imagePath = path.join(outDir, "screen.png");
  const metaPath = path.join(outDir, "meta.json");

  await downloadToFile(htmlUrl, htmlPath);
  const imageContentType = await downloadToFile(imageUrl, imagePath);

  const metadata = {
    fetchedAt: new Date().toISOString(),
    projectId,
    screenId,
    htmlUrl,
    imageUrl,
    htmlPath,
    imagePath,
    imageContentType,
  };

  await fs.writeFile(metaPath, `${JSON.stringify(metadata, null, 2)}\n`, "utf8");

  console.log(`Saved Stitch screen to ${outDir}`);
  console.log(`- HTML: ${htmlPath}`);
  console.log(`- Image: ${imagePath}`);
  console.log(`- Meta: ${metaPath}`);
}

async function bootstrapJg() {
  if (!requireApiKey()) {
    return;
  }

  const title = getArg("--title") || DEFAULT_PROJECT_TITLE;
  const project = await stitch.createProject(title);
  const designSystem = await project.createDesignSystem(DEFAULT_DESIGN_SYSTEM);
  await designSystem.update(DEFAULT_DESIGN_SYSTEM);

  const summary = {
    createdAt: new Date().toISOString(),
    projectTitle: title,
    projectId: project.projectId,
    designSystemId: designSystem.assetId,
    screens: [],
  };

  for (const spec of JG_SCREEN_SPECS) {
    const generatedScreen = await generateScreenData(project.projectId, `${spec.name}\n\n${spec.prompt}`, "MOBILE");
    const screenId = (generatedScreen.name || "").replace(/^projects\/[^/]+\/screens\//, "") || generatedScreen.id;
    const screen = await project.getScreen(screenId);
    const htmlUrl = await screen.getHtml();
    const imageUrl = await screen.getImage();

    const outDir = path.resolve("artifacts", "stitch", sanitize(project.projectId), sanitize(screen.screenId));
    await ensureDir(outDir);

    const htmlPath = path.join(outDir, "screen.html");
    const imagePath = path.join(outDir, "screen.png");
    const metaPath = path.join(outDir, "meta.json");

    await downloadToFile(htmlUrl, htmlPath);
    const imageContentType = await downloadToFile(imageUrl, imagePath);

    const metadata = {
      fetchedAt: new Date().toISOString(),
      projectId: project.projectId,
      screenId,
      screenName: spec.name,
      prompt: spec.prompt,
      htmlUrl,
      imageUrl,
      htmlPath,
      imagePath,
      imageContentType,
    };

    await fs.writeFile(metaPath, `${JSON.stringify(metadata, null, 2)}\n`, "utf8");

    summary.screens.push({
      name: spec.name,
      screenId,
      htmlPath,
      imagePath,
      metaPath,
    });
  }

  const summaryDir = path.resolve("artifacts", "stitch", sanitize(project.projectId));
  await ensureDir(summaryDir);
  const summaryPath = path.join(summaryDir, "jg-bootstrap-summary.json");
  await fs.writeFile(summaryPath, `${JSON.stringify(summary, null, 2)}\n`, "utf8");

  console.log(`Project: ${project.projectId}`);
  console.log(`Design system: ${designSystem.assetId}`);
  console.log(`Summary: ${summaryPath}`);
  for (const screen of summary.screens) {
    console.log(`${screen.name}: ${screen.screenId}`);
  }
}

async function main() {
  const command = process.argv[2] || "help";

  try {
    switch (command) {
      case "help":
        printHelp();
        break;
      case "list-projects":
        await listProjects();
        break;
      case "create-project":
        await createProject();
        break;
      case "create-design-system":
        await createDesignSystem();
        break;
      case "generate-screen":
        await generateScreen();
        break;
      case "bootstrap-jg":
        await bootstrapJg();
        break;
      case "list-screens":
        await listScreens();
        break;
      case "fetch-screen":
        await fetchScreen();
        break;
      default:
        fail(`Unknown command: ${command}`);
        printHelp();
        break;
    }
  } catch (error) {
    fail(error instanceof Error ? error.message : String(error));
  }
}

await main();
