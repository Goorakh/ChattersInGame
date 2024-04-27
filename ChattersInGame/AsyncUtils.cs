using RoR2;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ChattersInGame
{
    public static class AsyncUtils
    {
        public static Thread UnityMainThread { get; private set; }

        internal static void RecordMainThread()
        {
            UnityMainThread = Thread.CurrentThread;

#if DEBUG
            Log.Debug($"Unity main thread: '{UnityMainThread.Name}' ({UnityMainThread.ManagedThreadId})");
#endif
        }

        public static Task RunNextUnityUpdate(Action action, CancellationToken cancellationToken = default)
        {
            if (UnityMainThread != null && Thread.CurrentThread.ManagedThreadId == UnityMainThread.ManagedThreadId)
            {
                action();
                return Task.CompletedTask;
            }

            bool completed = false;

            RoR2Application.onNextUpdate += () =>
            {
                action();
                completed = true;
            };

            return Task.Run(() =>
            {
                while (!completed)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }, cancellationToken);
        }

        public static Task<T> RunNextUnityUpdate<T>(Func<T> action, CancellationToken cancellationToken = default)
        {
            if (UnityMainThread != null && Thread.CurrentThread.ManagedThreadId == UnityMainThread.ManagedThreadId)
            {
                return Task.FromResult(action());
            }

            bool completed = false;
            T result = default;

            RoR2Application.onNextUpdate += () =>
            {
                result = action();
                completed = true;
            };

            return Task.Run(() =>
            {
                while (!completed)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return result;
            }, cancellationToken);
        }
    }
}
