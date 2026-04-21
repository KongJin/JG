import fs from "node:fs/promises";
import path from "node:path";
import process from "node:process";

const REPO_ROOT = process.cwd();
const DEFAULT_OUTPUT_DIR = path.join("artifacts", "stitch", "generated-drafts");

const OVERLAY_TARGETS_BY_SURFACE = {
  "login-loading-overlay": {
    prefabPath: "Assets/Prefabs/Features/Lobby/Independent/LoginLoadingOverlay.prefab",
    sceneRoots: ["/Canvas/LoginLoadingOverlay"],
    primaryContractPaths: [
      "/Canvas/LoginLoadingOverlay",
      "/Canvas/LoginLoadingOverlay/OverlayCard",
    ],
    serializedOwners: ["/Canvas/LoginLoadingOverlay::LoginLoadingView"],
    ctaTargets: {
      "wait-or-cancel": "/Canvas/LoginLoadingOverlay/OverlayCard/SecondaryActionButton",
    },
    smokeScripts: [],
  },
  "common-error-dialog": {
    prefabPath: "Assets/Prefabs/Features/Shared/Ui/Independent/CommonErrorDialog.prefab",
    sceneRoots: ["/Canvas/CommonErrorDialog"],
    primaryContractPaths: [
      "/Canvas/CommonErrorDialog",
      "/Canvas/CommonErrorDialog/OverlayCard",
    ],
    serializedOwners: ["/Canvas/CommonErrorDialog::SceneErrorPresenter"],
    ctaTargets: {
      retry: "/Canvas/CommonErrorDialog/OverlayCard/RetryButton",
      dismiss: "/Canvas/CommonErrorDialog/OverlayCard/DismissButton",
    },
    smokeScripts: [],
  },
  "create-room-modal": {
    prefabPath: "Assets/Prefabs/Features/Lobby/Independent/LobbyCreateRoomModal.prefab",
    sceneRoots: ["/Canvas/LobbyCreateRoomModal"],
    primaryContractPaths: [
      "/Canvas/LobbyCreateRoomModal",
      "/Canvas/LobbyCreateRoomModal/OverlayCard",
    ],
    serializedOwners: ["/Canvas/LobbyCreateRoomModal::CreateRoomModalView"],
    ctaTargets: {
      "confirm-create-room": "/Canvas/LobbyCreateRoomModal/OverlayCard/CreateButton",
      "cancel-create-room": "/Canvas/LobbyCreateRoomModal/OverlayCard/CancelButton",
    },
    smokeScripts: [],
  },
  "account-delete-confirm": {
    prefabPath: "Assets/Prefabs/Features/Garage/Independent/AccountDeleteConfirmDialog.prefab",
    sceneRoots: ["/Canvas/AccountDeleteConfirmDialog"],
    primaryContractPaths: [
      "/Canvas/AccountDeleteConfirmDialog",
      "/Canvas/AccountDeleteConfirmDialog/OverlayCard",
    ],
    serializedOwners: ["/Canvas/AccountDeleteConfirmDialog::AccountSettingsView"],
    ctaTargets: {
      "confirm-delete-account": "/Canvas/AccountDeleteConfirmDialog/OverlayCard/DeleteButton",
      "cancel-delete-account": "/Canvas/AccountDeleteConfirmDialog/OverlayCard/CancelButton",
    },
    smokeScripts: [],
  },
  "room-detail-panel": {
    prefabPath: "Assets/Prefabs/Features/Lobby/Independent/LobbyRoomDetailPanel.prefab",
    sceneRoots: ["/Canvas/LobbyRoomDetailPanel"],
    primaryContractPaths: [
      "/Canvas/LobbyRoomDetailPanel",
      "/Canvas/LobbyRoomDetailPanel/OverlayCard",
    ],
    serializedOwners: ["/Canvas/LobbyRoomDetailPanel::RoomDetailView"],
    ctaTargets: {
      "join-room": "/Canvas/LobbyRoomDetailPanel/OverlayCard/PrimaryActionButton",
      "close-room-detail": "/Canvas/LobbyRoomDetailPanel/OverlayCard/SecondaryActionButton",
    },
    smokeScripts: [],
  },
};

function fail(message) {
  console.error(`Overlay draft generator error: ${message}`);
  process.exit(1);
}

function getArg(flag) {
  const index = process.argv.indexOf(flag);
  if (index === -1 || index + 1 >= process.argv.length) {
    return null;
  }

  return process.argv[index + 1];
}

function hasFlag(flag) {
  return process.argv.includes(flag);
}

function dedupe(values) {
  return [...new Set(values)];
}

function ensureArray(value) {
  return Array.isArray(value) ? value : [];
}

function normalizeFirstReadOrder(intake) {
  const intakePrimaryExists = ensureArray(intake.ctaPriority).some((cta) => cta.priority === "primary");
  const firstReadOrder = ["overlay-card"];
  if (intakePrimaryExists) {
    firstReadOrder.push("primary-cta");
  }

  return firstReadOrder;
}

function buildOverlayCardNotes(intake) {
  const overlayCard = ensureArray(intake.blocks).find((block) => block.blockId === "overlay-card");
  return ensureArray(overlayCard?.notes);
}

function buildPrimaryCtaOverride(intake, mapping) {
  const primaryCta = ensureArray(intake.ctaPriority).find((cta) => cta.priority === "primary");
  if (!primaryCta) {
    return {
      enabled: false,
      notes: [
        "Intake did not confirm a visible primary action for this accepted screen.",
        "Do not invent a CTA until the accepted screen or repo mapping proves one.",
      ],
    };
  }

  const unityTargetPath = mapping.ctaTargets[primaryCta.id];
  if (!unityTargetPath) {
    fail(`Missing CTA mapping for primary action '${primaryCta.id}' on surface '${intake.surfaceId}'.`);
  }

  return {
    unityTargetPath,
    enabled: true,
    notes: [
      `Primary CTA '${primaryCta.id}' confirmed from intake and mapped to Unity target.`,
    ],
  };
}

function buildManifestFromIntake(intake, mapping) {
  const ctaPriority = ensureArray(intake.ctaPriority).map((cta) => {
    const unityTargetPath = mapping.ctaTargets[cta.id];
    if (!unityTargetPath) {
      fail(`Missing CTA mapping for '${cta.id}' on surface '${intake.surfaceId}'.`);
    }

    return {
      id: cta.id,
      priority: cta.priority,
      unityTargetPath,
      outcome: cta.outcome,
    };
  });

  const requiredChecks = dedupe(["contract", ...ensureArray(intake.validation?.requiredChecks)]);
  const notes = [
    "Generated from screen-intake with repo mapping attached.",
    ...ensureArray(intake.notes),
  ];

  return {
    schemaVersion: "1.1.0",
    contractKind: "screen-manifest",
    setId: intake.setId,
    surfaceId: intake.surfaceId,
    surfaceRole: intake.surfaceRole,
    status: "draft",
    extends: "overlay-modal",
    targets: {
      prefabPath: mapping.prefabPath,
      sceneRoots: mapping.sceneRoots,
      primaryContractPaths: mapping.primaryContractPaths,
      serializedOwners: mapping.serializedOwners,
    },
    ctaPriority,
    states: intake.states,
    validation: {
      frame: intake.validation.frame,
      firstReadOrder: normalizeFirstReadOrder(intake),
      requiredChecks,
      smokeScripts: mapping.smokeScripts,
    },
    blockOverrides: {
      "overlay-card": {
        notes: buildOverlayCardNotes(intake),
      },
      "primary-cta": buildPrimaryCtaOverride(intake, mapping),
    },
    appendBlocks: [],
    notes,
  };
}

function summarizeManifestComparison(generatedManifest, acceptedManifest) {
  return {
    status: {
      generated: generatedManifest.status,
      accepted: acceptedManifest.status,
    },
    ctaCount: {
      generated: ensureArray(generatedManifest.ctaPriority).length,
      accepted: ensureArray(acceptedManifest.ctaPriority).length,
    },
    ctaIds: {
      generated: ensureArray(generatedManifest.ctaPriority).map((cta) => cta.id),
      accepted: ensureArray(acceptedManifest.ctaPriority).map((cta) => cta.id),
    },
    requiredChecks: {
      generated: ensureArray(generatedManifest.validation?.requiredChecks),
      accepted: ensureArray(acceptedManifest.validation?.requiredChecks),
    },
    notes: {
      generated: ensureArray(generatedManifest.notes),
      accepted: ensureArray(acceptedManifest.notes),
    },
    firstReadOrder: {
      generated: ensureArray(generatedManifest.validation?.firstReadOrder),
      accepted: ensureArray(acceptedManifest.validation?.firstReadOrder),
    },
  };
}

function validateOverlayIntake(intake) {
  if (intake.contractKind !== "screen-intake") {
    fail(`Expected contractKind 'screen-intake', got '${intake.contractKind}'.`);
  }

  if (intake.familyHints?.suggestedBlueprintId !== "overlay-modal") {
    fail(
      `Only overlay-modal intake files are supported. Current suggested blueprint is '${intake.familyHints?.suggestedBlueprintId ?? ""}'.`,
    );
  }

  if (ensureArray(intake.openQuestions).length > 0) {
    fail(
      `Intake '${intake.surfaceId}' still has open questions. Resolve them before generating a draft manifest.`,
    );
  }

  if (!OVERLAY_TARGETS_BY_SURFACE[intake.surfaceId]) {
    fail(`No overlay mapping registered for surface '${intake.surfaceId}'.`);
  }
}

async function main() {
  const inputArg = getArg("--input");
  if (!inputArg) {
    fail("Missing --input <path-to-screen-intake.json>.");
  }

  const outputArg = getArg("--output");
  const compareArg = getArg("--compare");
  const inputPath = path.resolve(REPO_ROOT, inputArg);
  const raw = await fs.readFile(inputPath, "utf8");
  const intake = JSON.parse(raw);

  validateOverlayIntake(intake);
  const mapping = OVERLAY_TARGETS_BY_SURFACE[intake.surfaceId];
  const manifest = buildManifestFromIntake(intake, mapping);

  const outputPath =
    outputArg != null
      ? path.resolve(REPO_ROOT, outputArg)
      : path.resolve(
          REPO_ROOT,
          DEFAULT_OUTPUT_DIR,
          `${path.basename(inputPath).replace(/\.intake\.json$/u, "")}.generated-draft.screen.json`,
        );

  await fs.mkdir(path.dirname(outputPath), { recursive: true });
  await fs.writeFile(outputPath, `${JSON.stringify(manifest, null, 2)}\n`, "utf8");

  const summary = {
    success: true,
    inputPath: path.relative(REPO_ROOT, inputPath),
    outputPath: path.relative(REPO_ROOT, outputPath),
    surfaceId: intake.surfaceId,
    extends: manifest.extends,
    ctaCount: manifest.ctaPriority.length,
  };

  if (compareArg != null) {
    const comparePath = path.resolve(REPO_ROOT, compareArg);
    const compareRaw = await fs.readFile(comparePath, "utf8");
    const acceptedManifest = JSON.parse(compareRaw);
    summary.comparePath = path.relative(REPO_ROOT, comparePath);
    summary.comparison = summarizeManifestComparison(manifest, acceptedManifest);
  }

  if (hasFlag("--stdout")) {
    console.log(JSON.stringify(manifest, null, 2));
    return;
  }

  console.log(JSON.stringify(summary, null, 2));
}

await main();
