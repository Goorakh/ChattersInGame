using ChattersInGame.Twitch.Chat.Message;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ChattersInGame
{
    public static class ChatterManager
    {
        static readonly ConcurrentDictionary<string, ChatterInfo> _chatters = [];

        static ChatterInfo getOrAddChatter(string userId)
        {
            return _chatters.GetOrAdd(userId, key =>
            {
                ChatterInfo newChatterInfo = new ChatterInfo(key);

                newChatterInfo.OnUserDataRetrieveFailed += () =>
                {
                    _chatters.TryRemove(key, out _);
                };

                return newChatterInfo;
            });
        }

        public static ChatterInfo BumpChatter(string chatterUserId)
        {
            ChatterInfo chatterInfo = getOrAddChatter(chatterUserId);
            chatterInfo.LastActivity = TimeStamp.Now;

            return chatterInfo;
        }

        public static bool RemoveChatter(string chatterUserId, out ChatterInfo removedChatter)
        {
            return _chatters.TryRemove(chatterUserId, out removedChatter);
        }

        public static bool RemoveChatter(string chatterUserId)
        {
            return RemoveChatter(chatterUserId, out _);
        }

        public static ChatterInfo GetRandomChatter(Xoroshiro128Plus rng)
        {
            ChatterInfo[] allChatters = new ChatterInfo[_chatters.Count];
            _chatters.Values.CopyTo(allChatters, 0);

            int minReferenceCount = int.MaxValue;
            List<ChatterInfo> activeChatters = new List<ChatterInfo>(allChatters.Length);

            foreach (ChatterInfo chatter in allChatters)
            {
                if (chatter.LastActivity.TimeSince.TotalMinutes > Main.ChatterMaxInactivityTime.Value)
                    continue;

                if (chatter.ReferenceCount > minReferenceCount)
                    continue;

                if (chatter.ReferenceCount < minReferenceCount)
                {
                    minReferenceCount = chatter.ReferenceCount;
                    activeChatters.Clear();
                }

                activeChatters.Add(chatter);
            }

            if (activeChatters.Count == 0)
                return null;

            return rng.NextElementUniform(activeChatters);
        }
    }
}
