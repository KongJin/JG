using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Shared.Logging
{
    public static class Log
    {
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string tag, object message, Object context = null)
        {
            if (UnityEngine.Debug.unityLogger.IsLogTypeAllowed(LogType.Log))
                UnityEngine.Debug.unityLogger.Log(tag, message, context);
        }

        public static void Warn(string tag, object message, Object context = null)
        {
            UnityEngine.Debug.unityLogger.LogWarning(tag, message, context);
        }

        public static void Error(string tag, object message, Object context = null)
        {
            UnityEngine.Debug.unityLogger.LogError(tag, message, context);
        }

        public static void Exception(System.Exception exception, Object context = null)
        {
            UnityEngine.Debug.unityLogger.LogException(exception, context);
        }
    }
}
