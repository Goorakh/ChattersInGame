using ChattersInGame.Twitch;
using ChattersInGame.Twitch.Chat.Message;
using ChattersInGame.Twitch.ThirdParty;
using ChattersInGame.Twitch.User;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ChattersInGame
{
    public class ChatterInfo : IEquatable<ChatterInfo>
    {
        public readonly string UserId;

        public TimeStamp LastActivity;

        public int ReferenceCount;

        bool _userDataIsReady;
        public bool UserDataIsReady
        {
            get
            {
                return _userDataIsReady;
            }
            private set
            {
                if (_userDataIsReady == value)
                    return;

                _userDataIsReady = value;

                if (_userDataIsReady)
                {
                    OnUserDataReady?.Invoke();
                }
            }
        }

        bool _userDataRetrieveFailed;
        public bool DataRetrieveFailed
        {
            get
            {
                return _userDataRetrieveFailed;
            }
            private set
            {
                if (_userDataRetrieveFailed == value)
                    return;

                _userDataRetrieveFailed = value;

                if (_userDataRetrieveFailed)
                {
                    OnUserDataRetrieveFailed?.Invoke();
                }
            }
        }

        public event Action OnUserDataReady;
        public event Action OnUserDataRetrieveFailed;

        public string UserDisplayName { get; private set; }

        public Texture2D ProfileImage { get; private set; }

        public Color? NameColor;

        public string ColorCode
        {
            get
            {
                if (!NameColor.HasValue)
                    return string.Empty;

                Color32 color = NameColor.Value;
                return $"#{color.r:X2}{color.g:X2}{color.b:X2}";
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    NameColor = null;
                    return;
                }

                if (value[0] == '#')
                    value = value.Substring(1);

                if (value.Length != 2 * 3)
                    throw new ArgumentException("Value is not a valid rgb hex code");

                if (byte.TryParse(value.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r)
                    && byte.TryParse(value.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g)
                    && byte.TryParse(value.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
                {
                    NameColor = new Color32(r, g, b, byte.MaxValue);
                }
                else
                {
                    NameColor = null;
                }
            }
        }

        public EmoteReference LastUsedEmote;
        public TimeStamp? LastUsedEmoteTime;

        public ChatterInfo(string userId)
        {
            UserId = userId;

            Task.Run(retrieveUserDataAsync).ContinueWith((retrieveUserDataTask) =>
            {
                AsyncUtils.RunNextUnityUpdate(() =>
                {
                    bool wasSuccess;
                    if (retrieveUserDataTask.Exception != null)
                    {
                        wasSuccess = false;
                        Log.Error_NoCallerPrefix($"Failed to retrieve user data for {UserId}: {retrieveUserDataTask.Exception}");
                    }
                    else
                    {
                        wasSuccess = retrieveUserDataTask.Result;
                    }

                    UserDataIsReady = wasSuccess;
                    DataRetrieveFailed = !wasSuccess;
                });
            });
        }

        async Task<bool> retrieveUserDataAsync()
        {
            if (!TwitchDataStorage.HasAccessToken)
            {
                Log.Error($"{UserId}: Cannot retrieve user data: no access token");
                return false;
            }

            GetUsersResponse getUsersResponse = await TwitchAPI.GetUsers([UserId], []);

            if (getUsersResponse == null || getUsersResponse.Users.Length <= 0)
            {
                Log.Error($"{UserId}: No user data was returned");
                return false;
            }

            TwitchUserData userData = getUsersResponse.Users[0];

            string userName = userData.UserDisplayName;

            // If any character is not ascii, use login name instead
            if (userName.Any(c => c > 0x7F))
            {
                userName = userData.UserLoginName;
            }

            UserDisplayName = userName;

            _ = userData.ProfileImageURL;

            return true;
        }

        void recordEmoteUsage(EmoteReference emote)
        {
            LastUsedEmote = emote;
            LastUsedEmoteTime = TimeStamp.Now;
        }

        public void OnUserMessage(ChannelChatMessageData messageData)
        {
            foreach (ChatMessageFragment fragment in messageData.Fragments)
            {
                if (fragment.EmoteData != null)
                {
                    recordEmoteUsage(EmoteReference.GetEmote(fragment.EmoteData.EmoteSetID, fragment.EmoteData.BaseEmoteID));
                }
                else
                {
                    string[] words = fragment.FragmentText.Split([' '], StringSplitOptions.RemoveEmptyEntries);

                    foreach (string word in words)
                    {
                        if (ThirdPartyEmoteManager.EmotesByCode.TryGetValue(word, out ThirdPartyEmoteData thirdPartyEmoteData))
                        {
                            recordEmoteUsage(EmoteReference.GetThirdPartyEmote(thirdPartyEmoteData.Provider, thirdPartyEmoteData.Id));
                        }
                    }
                }
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ChatterInfo);
        }

        public bool Equals(ChatterInfo other)
        {
            return other is not null &&
                   UserId == other.UserId;
        }

        public override int GetHashCode()
        {
            return UserId.GetHashCode();
        }

        public static bool operator ==(ChatterInfo left, ChatterInfo right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(ChatterInfo left, ChatterInfo right)
        {
            return !(left == right);
        }
    }
}
