const fs = require('fs');
const path = require('path');
const { chromium } = require('playwright');

const DEFAULT_URL = 'https://projectsd-51439--qa-362a4g3j.web.app';
const OUTPUT_DIR = path.resolve(process.cwd(), 'artifacts', 'webgl');
const BEFORE_PATH = path.join(OUTPUT_DIR, 'account-delete-before.png');
const AFTER_DELETE_PATH = path.join(OUTPUT_DIR, 'account-delete-after-delete.png');
const AFTER_RELOAD_PATH = path.join(OUTPUT_DIR, 'account-delete-after-reload.png');
const RESULT_PATH = path.join(OUTPUT_DIR, 'account-delete-smoke-result.json');
const ACCOUNT_CARD_OBJECT = 'AccountCard';

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

function ensureDir(dirPath) {
  fs.mkdirSync(dirPath, { recursive: true });
}

async function waitFor(predicate, timeoutMs, pollMs = 250) {
  const startedAt = Date.now();
  while (Date.now() - startedAt < timeoutMs) {
    const value = await predicate();
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

async function getStoredUid(page) {
  return page.evaluate(() => window.localStorage.getItem('account.auth.uid'));
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

  page.on('console', (msg) => {
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

  const initialUid = await waitFor(async () => {
    const uid = await getStoredUid(page);
    return uid && uid.trim().length > 0 ? uid : null;
  }, 20000);

  await page.screenshot({ path: BEFORE_PATH, fullPage: true });

  await invokeUnity(page, ACCOUNT_CARD_OBJECT, 'WebglSmokeDeleteAccountClick', '');
  await sleep(500);
  await invokeUnity(page, ACCOUNT_CARD_OBJECT, 'WebglSmokeDeleteAccountConfirm', '');
  const deleteTriggeredAt = Date.now();

  const authDeleteResponse = await waitFor(() => {
    for (let i = events.length - 1; i >= 0; i -= 1) {
      const event = events[i];
      if (event.kind !== 'response') {
        continue;
      }

      if (event.url.includes('identitytoolkit.googleapis.com') &&
          event.url.includes('accounts:delete') &&
          event.method === 'POST' &&
          event.status >= 200 &&
          event.status < 300) {
        return event;
      }
    }

    return null;
  }, 30000);

  const deletedFirestoreResponses = await waitFor(() => {
    if (!initialUid) {
      return [];
    }

    const deletes = events.filter((event) =>
      event.kind === 'response' &&
      event.method === 'DELETE' &&
      event.url.includes(`/documents/accounts/${initialUid}/`) &&
      event.status >= 200 &&
      event.status < 300
    );

    return deletes.length > 0 ? deletes : null;
  }, 30000);

  const clearedUid = await waitFor(async () => {
    const uid = await getStoredUid(page);
    return !uid ? true : null;
  }, 15000);

  await sleep(3000);
  await page.screenshot({ path: AFTER_DELETE_PATH, fullPage: true });

  await page.reload({ waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.waitForSelector('#unity-canvas', { timeout: 120000 });
  await sleep(20000);

  const reloadedUid = await waitFor(async () => {
    const uid = await getStoredUid(page);
    return uid && uid.trim().length > 0 ? uid : null;
  }, 20000);

  await page.screenshot({ path: AFTER_RELOAD_PATH, fullPage: true });

  const recreatedProfileResponse = events.find((event) =>
    event.kind === 'response' &&
    event.method === 'PATCH' &&
    event.url.includes('/documents/accounts/') &&
    event.url.includes('/profile/profile') &&
    event.status >= 200 &&
    event.status < 300 &&
    Date.parse(event.timestamp) >= deleteTriggeredAt &&
    (!initialUid || !event.url.includes(`/documents/accounts/${initialUid}/`))
  );

  const initialUidWriteObservedAfterDelete = initialUid
    ? events.some((event) =>
        event.kind === 'response' &&
        event.method === 'PATCH' &&
        event.url.includes(`/documents/accounts/${initialUid}/`) &&
        Date.parse(event.timestamp) >= deleteTriggeredAt
      )
    : null;

  const result = {
    url: targetUrl,
    initialUid,
    authDeleteStatus: authDeleteResponse ? authDeleteResponse.status : null,
    firestoreDeleteCount: deletedFirestoreResponses ? deletedFirestoreResponses.length : 0,
    storageClearedAfterDelete: Boolean(clearedUid),
    reloadedUid,
    uidChangedAfterReload:
      Boolean(initialUid) &&
      Boolean(reloadedUid) &&
      initialUid !== reloadedUid,
    recreatedProfileStatus: recreatedProfileResponse ? recreatedProfileResponse.status : null,
    initialUidWriteObservedAfterDelete,
    screenshots: {
      before: BEFORE_PATH,
      afterDelete: AFTER_DELETE_PATH,
      afterReload: AFTER_RELOAD_PATH,
    },
    consoleMessages,
    pageErrors,
    authEvents: events.filter((event) => event.url.includes('identitytoolkit.googleapis.com')),
    firestoreAccountEvents: events.filter((event) => event.url.includes('/documents/accounts/')),
  };

  fs.writeFileSync(RESULT_PATH, JSON.stringify(result, null, 2), 'utf8');
  await browser.close();

  console.log(JSON.stringify({
    url: result.url,
    initialUid: result.initialUid,
    authDeleteStatus: result.authDeleteStatus,
    firestoreDeleteCount: result.firestoreDeleteCount,
    storageClearedAfterDelete: result.storageClearedAfterDelete,
    reloadedUid: result.reloadedUid,
    uidChangedAfterReload: result.uidChangedAfterReload,
    recreatedProfileStatus: result.recreatedProfileStatus,
    initialUidWriteObservedAfterDelete: result.initialUidWriteObservedAfterDelete,
    resultPath: RESULT_PATH,
    screenshots: result.screenshots,
  }, null, 2));
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
