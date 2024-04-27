using BepInEx.Logging;
using System.Runtime.CompilerServices;

namespace ChattersInGame
{
    internal static class Log
    {
        internal static ManualLogSource _logSource;

        static readonly object _lock = new object();

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        static string getLogPrefix(string callerPath, string callerMemberName, int callerLineNumber)
        {
            const string MOD_NAME = nameof(ChattersInGame) + @"\";

            int modNameLastPathIndex = callerPath.LastIndexOf(MOD_NAME);
            if (modNameLastPathIndex >= 0)
            {
                callerPath = callerPath.Substring(modNameLastPathIndex + MOD_NAME.Length);
            }

            return $"{callerPath}:{callerLineNumber} ({callerMemberName}) ";
        }

#if DEBUG
        internal static void Debug(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            lock (_lock)
            {
                _logSource.LogDebug(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data);
            }
        }
        internal static void Debug_NoCallerPrefix(object data)
        {
            lock (_lock)
            {
                _logSource.LogDebug(data);
            }
        }
#endif

        internal static void Error(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            lock (_lock)
            {
                _logSource.LogError(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data);
            }
        }
        internal static void Error_NoCallerPrefix(object data)
        {
            lock (_lock)
            {
                _logSource.LogError(data);
            }
        }

        internal static void Fatal(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            lock (_lock)
            {
                _logSource.LogFatal(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data);
            }
        }
        internal static void Fatal_NoCallerPrefix(object data)
        {
            lock (_lock)
            {
                _logSource.LogFatal(data);
            }
        }

        internal static void Info(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            lock (_lock)
            {
                _logSource.LogInfo(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data);
            }
        }
        internal static void Info_NoCallerPrefix(object data)
        {
            lock (_lock)
            {
                _logSource.LogInfo(data);
            }
        }

        internal static void Message(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            lock (_lock)
            {
                _logSource.LogMessage(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data);
            }
        }
        internal static void Message_NoCallerPrefix(object data)
        {
            lock (_lock)
            {
                _logSource.LogMessage(data);
            }
        }

        internal static void Warning(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            lock (_lock)
            {
                _logSource.LogWarning(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data);
            }
        }
        internal static void Warning_NoCallerPrefix(object data)
        {
            lock (_lock)
            {
                _logSource.LogWarning(data);
            }
        }
    }
}