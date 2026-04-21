#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Features.Lobby.Presentation;
using ProjectSD.EditorTools.UnityMcp;
using UnityEditor;
using UnityEngine;

namespace ProjectSD.EditorTools.SceneTools
{
    internal static class LobbySceneContract
    {
        internal const string ScenePath = "Assets/Scenes/LobbyScene.unity";

        private static readonly string[] SentinelPaths =
        {
            "/Canvas/LobbyPageRoot",
            "/Canvas/GaragePageRoot",
            "/Canvas/LobbyGarageNavBar",
            "/Canvas/LobbyGarageNavBar/LobbyTabButton",
            "/Canvas/LobbyGarageNavBar/GarageTabButton",
            "/Canvas/GaragePageRoot/GarageMobileStackRoot",
            "/Canvas/GaragePageRoot/GarageMobileStackRoot/GarageMobileTabBar",
            "/Canvas/GaragePageRoot/GarageMobileStackRoot/MobileBodyHost",
            "/Canvas/GaragePageRoot/GarageMobileStackRoot/MobileBodyHost/MobileBodyScrollContent",
            "/Canvas/GaragePageRoot/GarageHeaderRow/SettingsButton",
            "/Canvas/GaragePageRoot/GarageSettingsOverlay",
            "/Canvas/GaragePageRoot/GarageSettingsOverlay/AccountCard",
            "/Canvas/GaragePageRoot/MobileSaveDock",
            "/Canvas/GaragePageRoot/MobileSaveDock/MobileSaveButton",
            "/Canvas/GaragePageRoot/GarageMobileStackRoot/MobileBodyHost/MobileBodyScrollContent/RosterListPane/SlotStripRow",
            "/Canvas/LobbyPageRoot/RoomListPanel",
            "/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/ListHeaderRow",
            "/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/RoomListSurface",
            "/Canvas/LobbyPageRoot/RoomListPanel/RoomsSectionCard/RoomListSurface/EmptyStateText",
            "/Canvas/LobbyPageRoot/RoomListPanel/GarageSummaryCard",
        };

        private static readonly (string path, Type componentType, string[] fields)[] ReferenceChecks =
        {
            (
                "/Canvas/GaragePageRoot",
                typeof(Features.Garage.Presentation.GaragePageController),
                new[]
                {
                    "_rosterListView",
                    "_unitEditorView",
                    "_resultPanelView",
                    "_unitPreviewView",
                    "_mobileContentRoot",
                    "_mobileBodyHost",
                    "_mobileSlotHost",
                    "_rightRailRoot",
                    "_mobileTabBar",
                    "_mobileEditTabButton",
                    "_mobileEditTabLabel",
                    "_mobilePreviewTabButton",
                    "_mobilePreviewTabLabel",
                    "_mobileSummaryTabButton",
                    "_mobileSummaryTabLabel",
                    "_garageHeaderSummaryText",
                    "_settingsOpenButton",
                    "_settingsOpenButtonLabel",
                    "_settingsOverlayRoot",
                    "_settingsCloseButton",
                    "_settingsCloseButtonLabel",
                    "_mobileSaveDockRoot",
                    "_mobileSaveButton",
                    "_mobileSaveButtonLabel",
                    "_mobileSaveStateText",
                }
            ),
            (
                "/LobbyView",
                typeof(LobbyView),
                new[]
                {
                    "_lobbyPageRoot",
                    "_garagePageRoot",
                    "_roomListPanel",
                    "_roomDetailPanel",
                    "_roomListView",
                    "_roomDetailView",
                    "_garageSummaryView",
                    "_lobbyPageCanvasGroup",
                    "_garagePageCanvasGroup",
                    "_navigationBar",
                }
            ),
            (
                "/Canvas/LobbyGarageNavBar",
                typeof(LobbyGarageNavBarView),
                new[]
                {
                    "_lobbyTabButton",
                    "_garageTabButton",
                    "_lobbyTabText",
                    "_garageTabText",
                    "_lobbyTabBorder",
                    "_garageTabBorder",
                }
            ),
            (
                "/Canvas/LobbyPageRoot/RoomListPanel",
                typeof(RoomListView),
                new[]
                {
                    "_roomListContent",
                    "_roomItemPrefab",
                    "_roomListCountText",
                    "_roomListEmptyStateText",
                }
            ),
            (
                "/Canvas/LobbyPageRoot/RoomListPanel/GarageSummaryCard",
                typeof(LobbyGarageSummaryView),
                new[]
                {
                    "_statusPillText",
                    "_headlineText",
                    "_bodyText",
                }
            ),
        };

        internal static LobbySceneContractReport VerifyActiveScene()
        {
            var report = new LobbySceneContractReport
            {
                scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                verifiedSentinels = new List<string>(),
                missingSentinels = new List<string>(),
                verifiedReferences = new List<string>(),
                missingReferences = new List<string>(),
            };

            foreach (var sentinelPath in SentinelPaths)
            {
                var go = McpSharedHelpers.FindGameObjectByPath(sentinelPath);
                if (go == null)
                    report.missingSentinels.Add(sentinelPath);
                else
                    report.verifiedSentinels.Add(sentinelPath);
            }

            foreach (var check in ReferenceChecks)
            {
                var go = McpSharedHelpers.FindGameObjectByPath(check.path);
                if (go == null)
                {
                    foreach (var fieldName in check.fields)
                        report.missingReferences.Add($"{check.path}::{check.componentType.Name}.{fieldName}");
                    continue;
                }

                var component = go.GetComponent(check.componentType);
                if (component == null)
                {
                    foreach (var fieldName in check.fields)
                        report.missingReferences.Add($"{check.path}::{check.componentType.Name}.{fieldName}");
                    continue;
                }

                var serializedObject = new SerializedObject(component);
                foreach (var fieldName in check.fields)
                {
                    var property = serializedObject.FindProperty(fieldName);
                    if (property == null || property.propertyType != SerializedPropertyType.ObjectReference)
                    {
                        report.missingReferences.Add($"{check.path}::{check.componentType.Name}.{fieldName}");
                        continue;
                    }

                    if (property.objectReferenceValue == null)
                        report.missingReferences.Add($"{check.path}::{check.componentType.Name}.{fieldName}");
                    else
                        report.verifiedReferences.Add($"{check.path}::{check.componentType.Name}.{fieldName}");
                }
            }

            report.ok = report.missingSentinels.Count == 0 && report.missingReferences.Count == 0;
            report.summary =
                report.ok
                    ? "LobbyScene contract verified."
                    : $"LobbyScene contract missing {report.missingSentinels.Count} sentinel(s) and {report.missingReferences.Count} reference(s).";
            return report;
        }
    }

    [Serializable]
    internal sealed class LobbySceneContractReport
    {
        public bool ok;
        public string scenePath;
        public List<string> verifiedSentinels;
        public List<string> missingSentinels;
        public List<string> verifiedReferences;
        public List<string> missingReferences;
        public string summary;
    }
}
#endif
