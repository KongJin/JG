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
    internal static class CodexLobbySceneContract
    {
        internal const string ScenePath = "Assets/Scenes/CodexLobbyScene.unity";
        internal const string BuilderMethodName =
            "ProjectSD.EditorTools.SceneTools.CodexLobbySceneBuilder.BuildCodexLobbySceneForAutomation";

        private static readonly string[] SentinelPaths =
        {
            "/Canvas/LobbyPageRoot",
            "/Canvas/GaragePageRoot",
            "/Canvas/LobbyPageRoot/RoomListPanel",
            "/Canvas/LobbyPageRoot/RoomListPanel/ListHeaderRow",
            "/Canvas/LobbyPageRoot/RoomListPanel/RoomListSurface",
            "/Canvas/LobbyPageRoot/RoomListPanel/RoomListSurface/EmptyStateText",
            "/Canvas/LobbyPageRoot/GarageTabButton",
            "/Canvas/GaragePageRoot/GarageHeaderRow/LobbyTabButton",
        };

        private static readonly (string path, Type componentType, string[] fields)[] ReferenceChecks =
        {
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
                    "_lobbyPageCanvasGroup",
                    "_garagePageCanvasGroup",
                    "_lobbyTabButton",
                    "_garageTabButton",
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
        };

        internal static CodexLobbySceneContractReport VerifyActiveScene()
        {
            var report = new CodexLobbySceneContractReport
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
                    ? "CodexLobbyScene contract verified."
                    : $"CodexLobbyScene contract missing {report.missingSentinels.Count} sentinel(s) and {report.missingReferences.Count} reference(s).";
            return report;
        }
    }

    [Serializable]
    internal sealed class CodexLobbySceneContractReport
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
