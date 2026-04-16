#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectSD.EditorTools.UnityMcp
{
    [Serializable]
    internal class SceneOpenRequest
    {
        public string scenePath;
        public bool saveCurrentSceneIfDirty;
    }

    [Serializable]
    internal sealed class CompileStatusResponse
    {
        public bool isCompiling;
    }

    [Serializable]
    internal sealed class CompileRequestBody
    {
        public bool cleanBuildCache;
    }

    [Serializable]
    internal sealed class CompileRequestResponse
    {
        public bool ok;
        public bool cleanBuildCacheRequested;
        public bool isCompiling;
        public string message;
    }

    [Serializable]
    internal sealed class CompileWaitBody
    {
        public int timeoutMs;
        public int pollIntervalMs;
        public bool requestFirst;
        public bool cleanBuildCache;
    }

    [Serializable]
    internal sealed class CompileWaitResponse
    {
        public bool ok;
        public bool timedOut;
        public bool requestedCompilation;
        public int waitedMs;
        public bool isCompiling;
    }

    [Serializable]
    internal sealed class MenuExecuteRequest
    {
        public string menuPath;
    }

    [Serializable]
    internal sealed class BuildWebGLRequest
    {
        public string outputPath;
        public bool fastBuild;
    }

    [Serializable]
    internal sealed class HealthResponse
    {
        public bool ok;
        public bool bridgeRunning;
        public int port;
        public bool isPlaying;
        public bool isPlayingOrWillChange;
        public bool isPlayModeChanging;
        public bool rawIsPlayingOrWillChange;
        public bool isCompiling;
        public bool isWaitingForPlayMode;
        public string projectKey;
        public string projectRootPath;
        public string activeScene;
        public string activeScenePath;
    }

    [Serializable]
    internal sealed class SceneResponse
    {
        public string name;
        public string path;
        public int buildIndex;
        public bool isLoaded;
        public bool isDirty;
        public bool isPlaying;
        public bool isPlayingOrWillChange;
        public bool isPlayModeChanging;
        public bool rawIsPlayingOrWillChange;
    }

    [Serializable]
    internal sealed class PlayResponse
    {
        public string action;
        public bool isPlaying;
        public bool isPlayingOrWillChange;
        public bool isPlayModeChanging;
        public bool rawIsPlayingOrWillChange;
    }

    [Serializable]
    internal sealed class ErrorResponse
    {
        public string error;
        public string detail;
        public string stackTrace;
        public string hint;
    }

    [Serializable]
    internal sealed class ConsoleLogsResponse
    {
        public int count;
        public ConsoleLogEntry[] items;
    }

    [Serializable]
    internal sealed class ConsoleLogEntry
    {
        public string timestampUtc;
        public string type;
        public string message;
        public string stackTrace;
    }

    internal sealed class HierarchyResponse
    {
        public string sceneName;
        public HierarchyNode[] nodes;
    }

    internal sealed class HierarchyNode
    {
        public string name;
        public string path;
        public bool activeSelf;
        public string[] components;
        public int childCount;
        public HierarchyNode[] children;
    }

    [Serializable]
    internal sealed class FindRequest
    {
        public string name;
        public string path;
        public bool lightweight;
        public string[] componentFilter;
    }

    [Serializable]
    internal sealed class GameObjectResponse
    {
        public bool found;
        public string name;
        public string path;
        public bool activeSelf;
        public string layer;
        public string tag;
        public ComponentInfo[] components;
    }

    [Serializable]
    internal sealed class ComponentInfo
    {
        public string typeName;
        public string fullTypeName;
        public PropertyInfo[] properties;
    }

    [Serializable]
    internal sealed class PropertyInfo
    {
        public string name;
        public string type;
        public string value;
    }

    [Serializable]
    internal sealed class CreateRequest
    {
        public string name;
        public string parent;
        public string[] components;
        public string uiPreset;
        public float? width;
        public float? height;
        public bool autoSave;
    }

    [Serializable]
    internal sealed class CreateResponse
    {
        public string name;
        public string path;
        public int instanceId;
        public bool queued;
        public int pendingCount;
        public bool autoSaved;
    }

    [Serializable]
    internal sealed class CreatePrimitiveRequest
    {
        public string name;
        public string primitiveType;
        public string[] components;
    }

    [Serializable]
    internal sealed class ComponentAddRequest
    {
        public string gameObjectPath;
        public string componentType;
        public bool autoSave;
    }

    [Serializable]
    internal sealed class GameObjectSetActiveRequest
    {
        public string path;
        public bool active;
        public bool autoSave;
    }

    [Serializable]
    internal sealed class ComponentSetRequest
    {
        public string gameObjectPath;
        public string componentType;
        public string propertyName;
        public string value;
        public string assetPath;
        public bool autoSave;
    }

    [Serializable]
    internal sealed class ComponentGetRequest
    {
        public string gameObjectPath;
        public string componentType;
        public string[] propertyNames;
    }

    [Serializable]
    internal sealed class ComponentGetResponse
    {
        public string gameObjectPath;
        public string componentType;
        public PropertyInfo[] properties;
    }

    [Serializable]
    internal sealed class PrefabSaveRequest
    {
        public string gameObjectPath;
        public string savePath;
        public bool destroySceneObject;
    }

    [Serializable]
    internal sealed class PrefabGetRequest
    {
        public string assetPath;
        public string childPath;
        public bool lightweight;
        public string[] componentFilter;
    }

    [Serializable]
    internal sealed class PrefabSetRequest
    {
        public string assetPath;
        public string childPath;
        public string componentType;
        public string propertyName;
        public string value;
        public string assetReferencePath;
        public string autoWireType;
    }

    [Serializable]
    internal sealed class PrefabAddComponentRequest
    {
        public string assetPath;
        public string childPath;
        public string componentType;
    }

    [Serializable]
    internal sealed class GenericResponse
    {
        public bool success;
        public string message;
        public bool queued;
        public int pendingCount;
        public bool autoSaved;
    }

    [Serializable]
    internal sealed class ScreenshotCaptureRequest
    {
        public string outputPath;
        public int superSize;
        public bool overwrite;
    }

    [Serializable]
    internal sealed class ScreenshotCaptureResponse
    {
        public bool success;
        public string message;
        public string relativePath;
        public string absolutePath;
        public long fileSizeBytes;
    }

    internal sealed class ScreenshotCapturePlan
    {
        public string relativePath;
        public string absolutePath;
        public int superSize;
    }

    [Serializable]
    internal sealed class InputClickRequest
    {
        public float x;
        public float y;
        public bool normalized;
        public int button;
        public int clickCount;
    }

    [Serializable]
    internal sealed class InputClickResponse
    {
        public bool success;
        public string message;
        public float x;
        public float y;
        public int button;
        public int clickCount;
        public float gameViewWidth;
        public float gameViewHeight;
        public bool normalized;
    }

    [Serializable]
    internal sealed class InputMoveRequest
    {
        public float x;
        public float y;
        public bool normalized;
    }

    [Serializable]
    internal sealed class InputMoveResponse
    {
        public bool success;
        public string message;
        public float x;
        public float y;
        public float gameViewWidth;
        public float gameViewHeight;
        public bool normalized;
    }

    [Serializable]
    internal sealed class InputDragRequest
    {
        public float startX;
        public float startY;
        public float endX;
        public float endY;
        public bool normalized;
        public int button;
        public int steps;
    }

    [Serializable]
    internal sealed class InputDragResponse
    {
        public bool success;
        public string message;
        public float startX;
        public float startY;
        public float endX;
        public float endY;
        public int button;
        public int steps;
        public float gameViewWidth;
        public float gameViewHeight;
        public bool normalized;
    }

    [Serializable]
    internal sealed class InputKeyRequest
    {
        public string keyCode;
        public string character;
        public string phase;
        public bool shift;
        public bool control;
        public bool alt;
        public bool command;
    }

    [Serializable]
    internal sealed class InputKeyResponse
    {
        public bool success;
        public string message;
        public string phase;
        public string keyCode;
        public string character;
        public string modifiers;
    }

    [Serializable]
    internal sealed class InputTextRequest
    {
        public string text;
        public bool appendReturn;
    }

    [Serializable]
    internal sealed class InputTextResponse
    {
        public bool success;
        public string message;
        public int charactersSubmitted;
        public bool appendReturn;
    }

    [Serializable]
    internal sealed class InputScrollRequest
    {
        public float x;
        public float y;
        public bool normalized;
        public float delta;
        public float deltaX;
        public float deltaY;
    }

    [Serializable]
    internal sealed class InputScrollResponse
    {
        public bool success;
        public string message;
        public float x;
        public float y;
        public float deltaX;
        public float deltaY;
        public float gameViewWidth;
        public float gameViewHeight;
        public bool normalized;
    }

    [Serializable]
    internal sealed class InputKeyComboRequest
    {
        public string preset;
        public int repeat;
    }

    [Serializable]
    internal sealed class InputKeyComboResponse
    {
        public bool success;
        public string message;
        public string preset;
        public string keyCode;
        public string character;
        public string modifiers;
        public int repeat;
    }

    [Serializable]
    internal sealed class UiButtonInvokeRequest
    {
        public string path;
    }

    [Serializable]
    internal sealed class UiButtonInvokeResponse
    {
        public bool success;
        public string message;
        public string path;
        public string name;
        public bool activeSelf;
        public bool activeInHierarchy;
        public bool interactable;
        public int persistentListenerCount;
    }

    [Serializable]
    internal sealed class UiCreatePanelRequest
    {
        public string name;
        public string parent;
        public float width;
        public float height;
        public bool autoSave;
    }

    [Serializable]
    internal sealed class UiCreatePanelResponse
    {
        public bool success;
        public string message;
        public string path;
        public string name;
        public bool queued;
        public int pendingCount;
        public bool autoSaved;
    }

    [Serializable]
    internal sealed class UiCreateRawImageRequest
    {
        public string name;
        public string parent;
        public float width;
        public float height;
        public bool autoSave;
    }

    [Serializable]
    internal sealed class UiCreateRawImageResponse
    {
        public bool success;
        public string message;
        public string path;
        public string name;
        public bool queued;
        public int pendingCount;
        public bool autoSaved;
    }

    [Serializable]
    internal sealed class UiSetRectRequest
    {
        public string path;
        public float? anchoredPositionX;
        public float? anchoredPositionY;
        public float? sizeDeltaX;
        public float? sizeDeltaY;
        public float? anchorMinX;
        public float? anchorMinY;
        public float? anchorMaxX;
        public float? anchorMaxY;
        public float? pivotX;
        public float? pivotY;
        public bool autoSave;
    }

    [Serializable]
    internal sealed class UiSetRectResponse
    {
        public bool success;
        public string message;
        public string path;
        public bool queued;
        public int pendingCount;
        public bool autoSaved;
    }

    [Serializable]
    internal sealed class GameObjectSetSiblingRequest
    {
        public string path;
        public int siblingIndex;
        public bool autoSave;
    }

    [Serializable]
    internal sealed class GameObjectSetSiblingResponse
    {
        public bool success;
        public string message;
        public string path;
        public int siblingIndex;
        public bool queued;
        public int pendingCount;
        public bool autoSaved;
    }

    [Serializable]
    internal sealed class GameObjectSetParentRequest
    {
        public string path;
        public string parentPath;
        public bool autoSave;
    }

    [Serializable]
    internal sealed class GameObjectSetParentResponse
    {
        public bool success;
        public string message;
        public string path;
        public string parentPath;
        public bool queued;
        public int pendingCount;
        public bool autoSaved;
    }

    [Serializable]
    internal sealed class UiCreateButtonRequest
    {
        public string name;
        public string parent;
        public string buttonText;
        public bool autoSave;
    }

    [Serializable]
    internal sealed class UiCreateButtonResponse
    {
        public bool success;
        public string message;
        public string path;
        public string name;
        public bool queued;
        public int pendingCount;
        public bool autoSaved;
    }

    [Serializable]
    internal sealed class ComponentSetSerializedFieldRequest
    {
        public string componentPath;
        public string componentTypeName;
        public string fieldName;
        public string targetPath;
        public bool autoSave;
    }

    [Serializable]
    internal sealed class ComponentSetSerializedFieldResponse
    {
        public bool success;
        public string message;
        public string componentPath;
        public string fieldName;
        public string targetPath;
        public bool queued;
        public int pendingCount;
        public bool autoSaved;
    }

    [Serializable]
    internal sealed class ComponentAutoConnectFieldsRequest
    {
        public string componentPath;
        public string componentTypeName;
        public string searchScope;
    }

    [Serializable]
    internal sealed class ComponentAutoConnectFieldsResponse
    {
        public bool success;
        public string message;
        public string componentPath;
        public int connectedCount;
        public string[] connectedFields;
        public bool queued;
        public int pendingCount;
        public bool autoSaved;
    }

    internal sealed class GameViewInputContext
    {
        public EditorWindow gameView;
        public float width;
        public float height;
    }

    // =====================================================================
    // Locator / Selector (Phase 1)
    // =====================================================================

    [Serializable]
    internal sealed class LocatorRequest
    {
        public string selector;
        public string scope;
        public bool activeOnly;
    }

    [Serializable]
    internal sealed class LocatorResponse
    {
        public bool found;
        public int count;
        public LocatorItem[] items;
    }

    [Serializable]
    internal sealed class LocatorItem
    {
        public string path;
        public string name;
        public bool activeSelf;
        public bool activeInHierarchy;
        public string[] components;
    }

    [Serializable]
    internal sealed class LocatorCountResponse
    {
        public int count;
    }

    [Serializable]
    internal sealed class ErrorResponseEnvelope
    {
        public string code;
        public string message;
        public string detail;
    }

    // =====================================================================
    // Auto-wait (Phase 2)
    // =====================================================================

    [Serializable]
    internal sealed class WaitRequest
    {
        public string selector;
        public string path;
        public string componentType;
        public string scope;
        public int timeoutMs;
        public int pollIntervalMs;
    }

    [Serializable]
    internal sealed class WaitResponse
    {
        public bool success;
        public bool timedOut;
        public int waitedMs;
        public string condition;
        public LocatorItem result;
        public string hint;
    }

    // =====================================================================
    // Evaluate / Public State (Phase 3)
    // =====================================================================

    [Serializable]
    internal sealed class EvalFindComponentRequest
    {
        public string path;
        public string componentType;
        public string[] fields;
    }

    [Serializable]
    internal sealed class EvalFindComponentResponse
    {
        public bool success;
        public string componentPath;
        public string componentType;
        public string fieldsJson; // JSON string of Dictionary<string, string>

        public static EvalFindComponentResponse PlayModeRequired()
        {
            return new EvalFindComponentResponse
            {
                success = false,
                componentType = "PLAY_MODE_REQUIRED"
            };
        }
    }

    [Serializable]
    internal sealed class EvalGetPublicStateRequest
    {
        public string path;
        public string componentType;
        public string[] fields;
    }

    [Serializable]
    internal sealed class EvalGetPublicStateResponse
    {
        public bool success;
        public string componentPath;
        public string componentType;
        public string fieldsJson;

        public static EvalGetPublicStateResponse PlayModeRequired()
        {
            return new EvalGetPublicStateResponse
            {
                success = false,
                componentType = "PLAY_MODE_REQUIRED"
            };
        }
    }

    // =====================================================================
    // Snapshot (Phase 4)
    // =====================================================================

    [Serializable]
    internal sealed class SnapshotUiRequest
    {
        public int maxDepth;
        public string canvasPath;
    }

    [Serializable]
    internal sealed class SnapshotUiResponse
    {
        public string scene;
        public string canvasPath;
        public SnapshotUiNode[] uiNodes;
        public int totalUiNodes;
        public int interactiveElements;
    }

    [Serializable]
    internal sealed class SnapshotUiNode
    {
        public string path;
        public string name;
        public bool activeSelf;
        public int childCount;
        public string[] views;
    }

    [Serializable]
    internal sealed class SnapshotDiffRequest
    {
        public string beforeJson;
        public string afterJson;
    }

    [Serializable]
    internal sealed class SnapshotDiffResponse
    {
        public string[] added;
        public string[] removed;
        public string[] changed;
    }

    [Serializable]
    internal sealed class SnapshotComponentsResponse
    {
        public string scene;
        public RootComponentInfo[] rootComponents;
    }

    [Serializable]
    internal sealed class RootComponentInfo
    {
        public string path;
        public string name;
        public string[] customComponents;
    }

    // =====================================================================
    // Explore (Phase 5)
    // =====================================================================

    [Serializable]
    internal sealed class ExploreInteractiveResponse
    {
        public string scene;
        public InteractiveElement[] interactiveElements;
        public int totalInteractive;
        public string byTypeJson;
    }

    [Serializable]
    internal sealed class InteractiveElement
    {
        public string path;
        public string name;
        public string type;
        public string text;
        public bool interactable;
        public bool activeInHierarchy;
    }

    // =====================================================================
    // Log Streaming (Improved Phase 6)
    // =====================================================================

    [Serializable]
    internal sealed class LogStreamSubscribeRequest
    {
        public string[] filters;    // Log 타입 필터 (Log, Warning, Error, Exception)
        public string[] tags;       // 태그 필터 ([Account], [FirebaseAuth] 등)
        public string contains;     // 메시지에 포함된 텍스트
        public int sinceId;         // 이 ID 이후의 로그만 받기
    }

    [Serializable]
    internal sealed class LogStreamResponse
    {
        public int id;
        public string timestampUtc;
        public string type;
        public string message;
        public string stackTrace;
        public string[] tags;       // 로그에서 추출한 태그
    }

    [Serializable]
    internal sealed class LogStreamStatsResponse
    {
        public int totalLogs;
        public int errorCount;
        public int warningCount;
        public int lastLogId;
        public long oldestLogTimestampMs;
        public long newestLogTimestampMs;
    }

    // =====================================================================
    // UI Direct Invoke (Improved Phase 7)
    // =====================================================================

    [Serializable]
    internal sealed class UiInvokeRequest
    {
        public string path;
        public string method;       // "click", "submit", "value", "custom"
        public string customMethod; // 커스텀 메서드 이름 (method="custom"일 때)
        public object[] args;       // 메서드에 전달할 인자
    }

    [Serializable]
    internal sealed class UiInvokeResponse
    {
        public bool success;
        public string message;
        public string path;
        public string invokedMethod;
        public string result;       // 메서드 반환 값 (문자열)
        public float durationMs;
    }

    // =====================================================================
    // GameObject with Component Fields (Improved Phase 8)
    // =====================================================================

    [Serializable]
    internal sealed class GameObjectWithFieldsResponse
    {
        public bool found;
        public string name;
        public string path;
        public bool activeSelf;
        public string layer;
        public string tag;
        public ComponentWithFields[] components;
    }

    [Serializable]
    internal sealed class ComponentWithFields
    {
        public string typeName;
        public string fullTypeName;
        public SerializedFieldInfo[] fields;
        public MethodInfo[] methods;
    }

    [Serializable]
    internal sealed class SerializedFieldInfo
    {
        public string name;
        public string type;
        public string value;
        public bool isNull;
        public bool isObjectReference;
        public string objectPath;    // ObjectReference일 때 대상 경로
    }

    [Serializable]
    internal sealed class MethodInfo
    {
        public string name;
        public string returnType;
        public string[] parameterTypes;
        public bool isPublic;
    }
}
#endif
