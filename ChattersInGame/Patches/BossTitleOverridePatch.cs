using RoR2;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace ChattersInGame.Patches
{
    [SuppressMessage("Member Access", "Publicizer001: Accessing a member that was not originally public", Justification = "Patch")]
    static class BossTitleOverridePatch
    {
        public static void Apply()
        {
            On.RoR2.BossGroup.Awake += BossGroup_Awake;
            On.RoR2.BossGroup.RememberBoss += BossGroup_RememberBoss;
        }

        public static void Undo()
        {
            On.RoR2.BossGroup.Awake -= BossGroup_Awake;
            On.RoR2.BossGroup.RememberBoss -= BossGroup_RememberBoss;
        }

        static void BossGroup_Awake(On.RoR2.BossGroup.orig_Awake orig, BossGroup self)
        {
            orig(self);
            self.gameObject.AddComponent<ChatterBossGroupController>();
        }

        static void BossGroup_RememberBoss(On.RoR2.BossGroup.orig_RememberBoss orig, BossGroup self, CharacterMaster master)
        {
            orig(self, master);

            if (self.TryGetComponent(out ChatterBossGroupController chatterBossGroup))
            {
                chatterBossGroup.MarkParticipantsDirty();
            }
        }

        class ChatterBossGroupController : MonoBehaviour
        {
            class ParticipantInfo
            {
                public readonly CharacterMaster Master;
                public readonly ChatterInfo Chatter;

                public string MostRecentNameFormat;

                public ParticipantInfo(CharacterMaster master, ChatterInfo chatter)
                {
                    Master = master;
                    Chatter = chatter;
                }

                public bool Update()
                {
                    if (!Master)
                        return false;

                    CharacterBody body = Master.GetBody();
                    if (!body)
                        return false;

                    ref string nameToken = ref body.baseNameToken;
                    string oldNameToken = Interlocked.Exchange(ref nameToken, "{0}");
                    string oldNameFormat = Interlocked.Exchange(ref MostRecentNameFormat, Util.GetBestBodyName(body.gameObject));
                    nameToken = oldNameToken;

                    return !string.Equals(oldNameFormat, MostRecentNameFormat);
                }

                public string GetDisplayName()
                {
                    if (Chatter.UserDataIsReady)
                    {
                        try
                        {
                            return string.Format(MostRecentNameFormat, Chatter.UserDisplayName);
                        }
                        catch (FormatException)
                        {
                            return Chatter.UserDisplayName;
                        }
                    }
                    else if (Master)
                    {
                        CharacterBody body = Master.GetBody();
                        if (body)
                        {
                            return Util.GetBestBodyName(body.gameObject);
                        }
                        else
                        {
                            return Util.GetBestMasterName(Master);
                        }
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
            }

            readonly struct ChatterNamePair
            {
                public readonly ChatterInfo ChatterInfo;
                public readonly string DisplayName;

                public ChatterNamePair(ChatterInfo chatterInfo, string displayName)
                {
                    ChatterInfo = chatterInfo;
                    DisplayName = displayName;
                }
            }

            readonly List<ParticipantInfo> _participatingChatters = [];

            BossGroup _bossGroup;

            bool _participantsDirty;
            bool _nameDirty;

            void Awake()
            {
                _bossGroup = GetComponent<BossGroup>();
            }

            public void MarkParticipantsDirty()
            {
                _participantsDirty = true;
            }

            public void MarkNameDirty()
            {
                _nameDirty = true;
            }

            void Update()
            {
                if (!_bossGroup)
                {
                    Destroy(this);
                    return;
                }

                bool bossNameEmpty() => string.IsNullOrEmpty(_bossGroup.bestObservedName) && _participatingChatters.Count > 0;

                if (_participantsDirty || bossNameEmpty())
                {
                    if (RefreshParticipants())
                    {
                        MarkNameDirty();
                    }

                    _participantsDirty = false;
                }

                foreach (ParticipantInfo participant in _participatingChatters)
                {
                    if (participant.Update())
                    {
                        MarkNameDirty();
                    }
                }

                if (_nameDirty || bossNameEmpty())
                {
                    RefreshName();

                    _nameDirty = false;
                }
            }

            public bool RefreshParticipants()
            {
                bool requiresBossGroupRefresh = false;

                for (int i = 0; i < _bossGroup.bossMemoryCount; i++)
                {
                    BossGroup.BossMemory bossMemory = _bossGroup.bossMemories[i];
                    if (bossMemory.cachedMaster && bossMemory.cachedMaster.TryGetComponent(out ChatName chatName))
                    {
                        ChatterInfo chatterInfo = chatName.ChatterInfo;
                        if (chatterInfo is null)
                            continue;

                        if (_participatingChatters.Any(p => p.Master == bossMemory.cachedMaster))
                            continue;

                        if (!chatterInfo.UserDataIsReady)
                        {
                            chatterInfo.OnUserDataReady += MarkNameDirty;
                        }

                        _participatingChatters.Add(new ParticipantInfo(bossMemory.cachedMaster, chatterInfo));
                    }
                }

                return requiresBossGroupRefresh;
            }

            public void RefreshName()
            {
                List<ChatterNamePair> names = new List<ChatterNamePair>(_bossGroup.bossMemoryCount);

                foreach (ParticipantInfo participant in _participatingChatters)
                {
                    string participantName = participant.GetDisplayName();
                    if (string.IsNullOrEmpty(participantName))
                        continue;

                    ChatterNamePair namePair = new ChatterNamePair(participant.Chatter, participantName);

                    bool shouldSkipParticipant = false;
                    for (int i = 0; i < names.Count; i++)
                    {
                        if (names[i].ChatterInfo == participant.Chatter)
                        {
                            if (participantName.Length > names[i].DisplayName.Length)
                            {
                                names[i] = namePair;
                            }

                            shouldSkipParticipant = true;
                            break;
                        }
                    }

                    if (!shouldSkipParticipant)
                    {
                        names.Add(namePair);
                    }
                }

                if (names.Count > 0)
                {
                    const int BOSS_NAME_CHARACTER_LIMIT = int.MaxValue;

                    int totalLength = 0;
                    for (int i = 0; i < names.Count; i++)
                    {
                        int nameLength = names[i].DisplayName.Length;
                        if (totalLength + nameLength < BOSS_NAME_CHARACTER_LIMIT)
                        {
                            totalLength += nameLength;
                        }
                        else
                        {
                            names.RemoveRange(i, names.Count - i);
                            break;
                        }
                    }

                    Log.Debug($"boss name length: {totalLength}");

                    StringBuilder bossNameStringBuilder = HG.StringBuilderPool.RentStringBuilder();
                    bossNameStringBuilder.EnsureCapacity(totalLength + (3 * (totalLength - 1)));

                    for (int i = 0; i < names.Count; i++)
                    {
                        if (i > 0)
                        {
                            if (i == names.Count - 1)
                            {
                                bossNameStringBuilder.Append(" & ");
                            }
                            else
                            {
                                bossNameStringBuilder.Append(", ");
                            }
                        }

                        bossNameStringBuilder.Append(names[i].DisplayName);
                    }

                    _bossGroup.bestObservedName = bossNameStringBuilder.Take();
                    HG.StringBuilderPool.ReturnStringBuilder(bossNameStringBuilder);
                }
            }
        }
    }
}
