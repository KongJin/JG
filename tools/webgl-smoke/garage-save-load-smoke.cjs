const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const DEFAULT_URL = 'https://projectsd-51439--qa-362a4g3j.web.app';
const OUTPUT_DIR = path.resolve(process.cwd(), 'artifacts', 'webgl');
const BEFORE_PATH = path.join(OUTPUT_DIR, 'garage-save-load-before.png');
const AFTER_SAVE_PATH = path.join(OUTPUT_DIR, 'garage-save-load-after-save.png');
const AFTER_RELOAD_PATH = path.join(OUTPUT_DIR, 'garage-save-load-after-reload.png');
const RESULT_PATH = path.join(OUTPUT_DIR, 'garage-save-load-smoke-result.json');

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function ensureDir(dirPath) {
  fs.mkdirSync(dirPath, { recursive: true });
}

function extractGarageJsonFromFirestoreDocument(raw) {
  if (!raw || typeof raw !== 'string') {
    return null;
  }

  const match = raw.match(/"json"\s*:\s*\{\s*"stringValue"\s*:\s*"((?:\\.|[^"\\])*)"/);
  if (!match) {
    return null;
  }

  return JSON.parse(`"${match[1]}"`);
}

function findLatestGarageEvent(events, method, kind) {
  for (let i = events.length - 1; i >= 0; i -= 1) {
    const event = events[i];
    if (event.url.includes('/garage/roster') && event.method === method && event.kind === kind) {
      return event;
    }
  }

  return null;
}

async function waitFor(predicate, timeoutMs, pollMs = 250) {
  const startedAt = Date.now();
  while (Date.now() - startedAt < timeoutMs) {
    const value = predicate();
    if (value) {
      return value;
    }

    await sleep(pollMs);
  }

  return null;
}

async function invokeUnity(page, gameObjectName, methodName, value) {
  await page.evaluate(
    ({ target, method, argument }) => {
      if (!window.unityInstance || typeof window.unityInstance.SendMessage !== 'function') {
        throw new Error('Unity instance is not ready.');
      }

      window.unityInstance.SendMessage(target, method, argument ?? '');
    },
    {
      target: gameObjectName,
      method: methodName,
      argument: value ?? '',
    }
  );
}

async function buildCompleteSlot(page, slotIndex) {
  await invokeUnity(page, 'GaragePageRoot', 'WebglSmokeSelectSlot', String(slotIndex));
  await sleep(300);
  await invokeUnity(page, 'GaragePageRoot', 'WebglSmokeCycleFrame', '1');
  await sleep(300);
  await invokeUnity(page, 'GaragePageRoot', 'WebglSmokeCycleFirepower', '1');
  await sleep(300);
  await invokeUnity(page, 'GaragePageRoot', 'WebglSmokeCycleMobility', '1');
  await sleep(500);
}

async function main() {
  const targetUrl = process.argv[2] || DEFAULT_URL;
  ensureDir(OUTPUT_DIR);

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1280, height: 720 },
    deviceScaleFactor: 2,
  });
  const page = await context.newPage();

  const events = [];
  const consoleMessages = [];
  const pageErrors = [];

  page.on('console', async (msg) => {
    consoleMessages.push({
      type: msg.type(),
      text: msg.text(),
    });
  });

  page.on('pageerror', (error) => {
    pageErrors.push({
      message: error.message,
      stack: error.stack,
    });
  });

  page.on('response', async (response) => {
    const url = response.url();
    if (!url.includes('firestore.googleapis.com') && !url.includes('identitytoolkit.googleapis.com')) {
      return;
    }

    let bodyText = null;
    try {
      bodyText = await response.text();
    } catch (error) {
      bodyText = `<unavailable: ${error.message}>`;
    }

    events.push({
      kind: 'response',
      url,
      method: response.request().method(),
      status: response.status(),
      bodyText,
      timestamp: new Date().toISOString(),
    });
  });

  page.on('request', (request) => {
    const url = request.url();
    if (!url.includes('firestore.googleapis.com') && !url.includes('identitytoolkit.googleapis.com')) {
      return;
    }

    events.push({
      kind: 'request',
      url,
      method: request.method(),
      postData: request.postData(),
      timestamp: new Date().toISOString(),
    });
  });

  await page.goto(targetUrl, { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.waitForSelector('#unity-canvas', { timeout: 120000 });
  await sleep(20000);

  await page.screenshot({ path: BEFORE_PATH, fullPage: true });

  const initialGarageGet = await waitFor(
    () => findLatestGarageEvent(events, 'GET', 'response'),
    15000
  );

  await buildCompleteSlot(page, 0);
  await buildCompleteSlot(page, 1);
  await buildCompleteSlot(page, 2);
  await invokeUnity(page, 'GaragePageRoot', 'WebglSmokeSaveDraft', '');

  const savePatchResponse = await waitFor(
    () => {
      const latest = findLatestGarageEvent(events, 'PATCH', 'response');
      return latest && latest.status >= 200 && latest.status < 300 ? latest : null;
    },
    30000
  );

  await sleep(3000);
  await page.screenshot({ path: AFTER_SAVE_PATH, fullPage: true });

  await page.reload({ waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.waitForSelector('#unity-canvas', { timeout: 120000 });
  await sleep(20000);

  const reloadedGarageGet = await waitFor(
    () => {
      const latest = findLatestGarageEvent(events, 'GET', 'response');
      if (!latest || latest === initialGarageGet) {
        return null;
      }

      return latest.status >= 200 && latest.status < 300 ? latest : null;
    },
    30000
  );

  await page.screenshot({ path: AFTER_RELOAD_PATH, fullPage: true });

  const saveRequest = findLatestGarageEvent(events, 'PATCH', 'request');
  const savePatch = findLatestGarageEvent(events, 'PATCH', 'response');
  const uidMatch = events
    .map((event) => event.url.match(/\/documents\/accounts\/([^/]+)\//))
    .find(Boolean);
  const savedGarageJson = saveRequest && saveRequest.postData
    ? extractGarageJsonFromFirestoreDocument(saveRequest.postData)
    : savePatch
      ? extractGarageJsonFromFirestoreDocument(savePatch.bodyText)
      : null;
  const reloadedGarageJson = reloadedGarageGet
    ? extractGarageJsonFromFirestoreDocument(reloadedGarageGet.bodyText)
    : null;

  const result = {
    url: targetUrl,
    uid: uidMatch ? uidMatch[1] : null,
    initialGarageGetStatus: initialGarageGet ? initialGarageGet.status : null,
    savePatchStatus: savePatchResponse ? savePatchResponse.status : null,
    reloadGarageGetStatus: reloadedGarageGet ? reloadedGarageGet.status : null,
    saveRequestGarageJson: savedGarageJson,
    reloadGarageJson: reloadedGarageJson,
    saveAndReloadMatch:
      savedGarageJson !== null &&
      reloadedGarageJson !== null &&
      savedGarageJson === reloadedGarageJson,
    screenshots: {
      before: BEFORE_PATH,
      afterSave: AFTER_SAVE_PATH,
      afterReload: AFTER_RELOAD_PATH,
    },
    consoleMessages,
    pageErrors,
    garageEvents: events.filter((event) => event.url.includes('/garage/roster')),
  };

  fs.writeFileSync(RESULT_PATH, JSON.stringify(result, null, 2), 'utf8');
  await browser.close();

  console.log(JSON.stringify(result, null, 2));
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
