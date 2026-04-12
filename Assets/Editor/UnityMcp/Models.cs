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
    }

    [Serializable]
    internal sealed class CreateResponse
    {
        public string name;
        public string path;
        public int instanceId;
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
    }

    [Serializable]
    internal sealed class GameObjectSetActiveRequest
    {
        public string path;
        public bool active;
    }

    [Serializable]
    internal sealed class ComponentSetRequest
    {
        public string gameObjectPath;
        public string componentType;
        public string propertyName;
        public string value;
        public string assetPath;
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
    }

    [Serializable]
    internal sealed class UiCreatePanelResponse
    {
        public bool success;
        public string message;
        public string path;
        public string name;
    }

    [Serializable]
    internal sealed class UiCreateRawImageRequest
    {
        public string name;
        public string parent;
        public float width;
        public float height;
    }

    [Serializable]
    internal sealed class UiCreateRawImageResponse
    {
        public bool success;
        public string message;
        public string path;
        public string name;
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
    }

    [Serializable]
    internal sealed class UiSetRectResponse
    {
        public bool success;
        public string message;
        public string path;
    }

    [Serializable]
    internal sealed class GameObjectSetSiblingRequest
    {
        public string path;
        public int siblingIndex;
    }

    [Serializable]
    internal sealed class GameObjectSetSiblingResponse
    {
        public bool success;
        public string message;
        public string path;
        public int siblingIndex;
    }

    [Serializable]
    internal sealed class UiCreateButtonRequest
    {
        public string name;
        public string parent;
        public string buttonText;
    }

    [Serializable]
    internal sealed class UiCreateButtonResponse
    {
        public bool success;
        public string message;
        public string path;
        public string name;
    }

    [Serializable]
    internal sealed class ComponentSetSerializedFieldRequest
    {
        public string componentPath;
        public string componentTypeName;
        public string fieldName;
        public string targetPath;
    }

    [Serializable]
    internal sealed class ComponentSetSerializedFieldResponse
    {
        public bool success;
        public string message;
        public string componentPath;
        public string fieldName;
        public string targetPath;
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
    }

    internal sealed class GameViewInputContext
    {
        public EditorWindow gameView;
        public float width;
        public float height;
    }
}
#endif
