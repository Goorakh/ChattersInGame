using ChattersInGame.Twitch.ThirdParty._7TV;
using ChattersInGame.Twitch.ThirdParty.BTTV;
using ChattersInGame.Twitch.ThirdParty.FFZ;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ChattersInGame.Twitch.ThirdParty
{
    public static class ThirdPartyEmoteManager
    {
        static readonly ConcurrentDictionary<string, ThirdPartyEmoteData> _emotesByCode = [];
        public static readonly ReadOnlyDictionary<string, ThirdPartyEmoteData> EmotesByCode;

        static ThirdPartyEmoteManager()
        {
            EmotesByCode = new ReadOnlyDictionary<string, ThirdPartyEmoteData>(_emotesByCode);
        }

        [RoR2.ConCommand(commandName = "refresh_third_party_emotes", helpText = "refresh_third_party_emotes [clear cache:true/false]")]
        static void CCRefreshThirdPartyEmotes(RoR2.ConCommandArgs args)
        {
            if (!TwitchDataStorage.HasAccessToken)
            {
                Debug.LogError("Cannot refresh emotes: Not authenticated");
                return;
            }

            if (args.Count > 0)
            {
                bool? clearCache = args.TryGetArgBool(0);
                if (clearCache == true)
                {
                    ClearThirdPartyEmotesCache();
                }
            }

            Task.Run(async () =>
            {
                AuthenticationTokenValidationResponse accesTokenValidation = await AuthenticationAPI.GetAccessTokenValidationAsync(TwitchDataStorage.AccessToken);
                if (accesTokenValidation == null)
                {
                    Log.Error("Authentication token is not valid");
                    return;
                }

                _emotesByCode.Clear();
                await RefreshThirdPartyEmotes(accesTokenValidation.UserID);
            });
        }

        public static void ClearThirdPartyEmotesCache()
        {
            _emotesByCode.Clear();

            foreach (DirectoryInfo directory in new DirectoryInfo(TwitchDataStorage.EmotesCachePath).EnumerateDirectories())
            {
                if (directory.Name.StartsWith("7tv_") || directory.Name.StartsWith("bttv_") || directory.Name.StartsWith("ffz_"))
                {
                    directory.Delete(true);
                }
            }
        }

        public static async Task RefreshThirdPartyEmotes(string channelUserId, CancellationToken cancellationToken = default)
        {
            await Task.WhenAll([
                refreshBTTVGlobalEmotes(cancellationToken),
                refreshBTTVChannelEmotes(channelUserId, cancellationToken),
                refresh7TVGlobalEmotes(cancellationToken),
                refresh7TVChannelEmotes(channelUserId, cancellationToken),
                refreshFFZGlobalEmotes(cancellationToken),
                refreshFFZChannelEmotes(channelUserId, cancellationToken)
            ]);
        }

        static async Task refreshBTTVGlobalEmotes(CancellationToken cancellationToken)
        {
            await Task.Yield();

            using HttpClient client = new HttpClient();

            HttpResponseMessage httpResponseMessage = await client.GetAsync("https://api.betterttv.net/3/cached/emotes/global", cancellationToken);
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                Log.Error($"BTTV global emotes responded with {httpResponseMessage.StatusCode} ({httpResponseMessage.ReasonPhrase})");
                return;
            }

            BTTVEmoteData[] bttvGlobalEmotes;
            try
            {
                bttvGlobalEmotes = JsonConvert.DeserializeObject<BTTVEmoteData[]>(await httpResponseMessage.Content.ReadAsStringAsync());
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize BTTV global emotes response: {e}");
                return;
            }

            foreach (BTTVEmoteData emoteData in bttvGlobalEmotes)
            {
                ThirdPartyEmoteData emote = new ThirdPartyEmoteData(emoteData);
                _emotesByCode.TryAdd(emoteData.EmoteCode, emote);
            }
        }

        static async Task refreshBTTVChannelEmotes(string channelUserId, CancellationToken cancellationToken)
        {
            await Task.Yield();

            using HttpClient client = new HttpClient();

            HttpResponseMessage httpResponseMessage = await client.GetAsync($"https://api.betterttv.net/3/cached/users/twitch/{channelUserId}", cancellationToken);
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                Log.Error($"BTTV channel emotes responded with {httpResponseMessage.StatusCode} ({httpResponseMessage.ReasonPhrase})");
                return;
            }

            BTTVUserData userData;
            try
            {
                userData = JsonConvert.DeserializeObject<BTTVUserData>(await httpResponseMessage.Content.ReadAsStringAsync());
            }
            catch (JsonException e)
            {
                Log.Error($"Failed to deserialize BTTV channel emotes response: {e}");
                return;
            }

            if (userData.ChannelEmotes != null)
            {
                foreach (BTTVEmoteData channelEmote in userData.ChannelEmotes)
                {
                    ThirdPartyEmoteData emote = new ThirdPartyEmoteData(channelEmote);
                    _emotesByCode.TryAdd(channelEmote.EmoteCode, emote);
                }
            }

            if (userData.SharedEmotes != null)
            {
                foreach (BTTVEmoteData sharedEmote in userData.SharedEmotes)
                {
                    ThirdPartyEmoteData emote = new ThirdPartyEmoteData(sharedEmote);
                    _emotesByCode.TryAdd(sharedEmote.EmoteCode, emote);
                }
            }
        }

        static async Task refresh7TVGlobalEmotes(CancellationToken cancellationToken)
        {
            await Task.Yield();

            using HttpClient client = new HttpClient();

            HttpResponseMessage httpResponseMessage = await client.GetAsync("https://7tv.io/v3/emote-sets/global", cancellationToken);
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                Log.Error($"7TV global emotes responded with {httpResponseMessage.StatusCode} ({httpResponseMessage.ReasonPhrase})");
                return;
            }

            _7TVEmoteSet globalEmoteSet;
            try
            {
                globalEmoteSet = JsonConvert.DeserializeObject<_7TVEmoteSet>(await httpResponseMessage.Content.ReadAsStringAsync());
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize 7TV global emotes response: {e}");
                return;
            }

            foreach (_7TVEmote emote in globalEmoteSet.Emotes)
            {
                _emotesByCode.TryAdd(emote.EmoteName, new ThirdPartyEmoteData(emote));
            }
        }

        static async Task refresh7TVChannelEmotes(string channelUserId, CancellationToken cancellationToken)
        {
            await Task.Yield();

            using HttpClient client = new HttpClient();

            HttpResponseMessage httpResponseMessage = await client.GetAsync($"https://7tv.io/v3/users/twitch/{channelUserId}", cancellationToken);
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                Log.Error($"7TV channel emotes responded with {httpResponseMessage.StatusCode} ({httpResponseMessage.ReasonPhrase})");
                return;
            }

            _7TVPlatformUserData platformUserData;
            try
            {
                platformUserData = JsonConvert.DeserializeObject<_7TVPlatformUserData>(await httpResponseMessage.Content.ReadAsStringAsync());
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize 7TV channel emotes response: {e}");
                return;
            }

            if (platformUserData.EmoteSet?.Emotes == null)
                return;

            foreach (_7TVEmote emote in platformUserData.EmoteSet.Emotes)
            {
                _emotesByCode.TryAdd(emote.EmoteName, new ThirdPartyEmoteData(emote));
            }
        }

        static async Task refreshFFZGlobalEmotes(CancellationToken cancellationToken)
        {
            await Task.Yield();

            using HttpClient client = new HttpClient();

            HttpResponseMessage httpResponseMessage = await client.GetAsync("https://api.frankerfacez.com/v1/_set/global", cancellationToken);
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                Log.Error($"FFZ global emotes responded with {httpResponseMessage.StatusCode} ({httpResponseMessage.ReasonPhrase})");
                return;
            }

            FFZGlobalEmotesData globalEmotes;
            try
            {
                globalEmotes = JsonConvert.DeserializeObject<FFZGlobalEmotesData>(await httpResponseMessage.Content.ReadAsStringAsync());
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize FFZ global emotes response: {e}");
                return;
            }

            await Task.WhenAll(Array.ConvertAll(globalEmotes.DefaultSets, setId => refreshFFZEmoteSet(setId, cancellationToken)));
        }

        static async Task refreshFFZChannelEmotes(string channelUserId, CancellationToken cancellationToken)
        {
            await Task.Yield();

            using HttpClient client = new HttpClient();

            HttpResponseMessage httpResponseMessage = await client.GetAsync($"https://api.frankerfacez.com/v1/_room/id/{channelUserId}", cancellationToken);
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                Log.Error($"FFZ room emotes responded with {httpResponseMessage.StatusCode} ({httpResponseMessage.ReasonPhrase})");
                return;
            }

            FFZRoomResponse roomResponse;
            try
            {
                roomResponse = JsonConvert.DeserializeObject<FFZRoomResponse>(await httpResponseMessage.Content.ReadAsStringAsync());
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize FFZ room response: {e}");
                return;
            }

            if (roomResponse.Room == null)
                return;

            await refreshFFZEmoteSet(roomResponse.Room.EmoteSetId, cancellationToken);
        }

        static async Task refreshFFZEmoteSet(int setId, CancellationToken cancellationToken)
        {
            await Task.Yield();

            FFZEmoteSet emoteSet = await getFFZEmoteSet(setId, cancellationToken);
            if (emoteSet == null)
                return;

            foreach (FFZEmote emote in emoteSet.Emotes)
            {
                if (emote.IsModifier && (emote.ModifierFlags & FFZEmoteModifierFlags.HIDDEN) != 0)
                    continue;

                _emotesByCode.TryAdd(emote.EmoteName, new ThirdPartyEmoteData(emote));
            }
        }

        static async Task<FFZEmoteSet> getFFZEmoteSet(int setId, CancellationToken cancellationToken)
        {
            using HttpClient client = new HttpClient();

            HttpResponseMessage httpResponseMessage = await client.GetAsync($"https://api.frankerfacez.com/v1/set/{setId}", cancellationToken);
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                Log.Error($"FFZ set emotes ({setId}) responded with {httpResponseMessage.StatusCode} ({httpResponseMessage.ReasonPhrase})");
                return null;
            }

            FFZEmoteSetResponse emoteSetResponse;
            try
            {
                emoteSetResponse = JsonConvert.DeserializeObject<FFZEmoteSetResponse>(await httpResponseMessage.Content.ReadAsStringAsync());
            }
            catch (JsonException e)
            {
                Log.Error_NoCallerPrefix($"Failed to deserialize FFZ set emotes {setId} response: {e}");
                return null;
            }

            return emoteSetResponse.EmoteSet;
        }

        public static async Task<EmoteData> DownloadEmoteAsync(ThirdPartyEmoteProvider emoteProvider, string emoteId, CancellationToken cancellationToken = default)
        {
            ThirdPartyEmoteData thirdPartyEmoteData = null;
            foreach (ThirdPartyEmoteData emote in _emotesByCode.Values)
            {
                if (emote.Provider == emoteProvider && emote.Id == emoteId)
                {
                    thirdPartyEmoteData = emote;
                    break;
                }
            }

            if (thirdPartyEmoteData == null)
            {
                Log.Warning($"Emote {emoteId} from {emoteProvider} is not registered in cache");
                return null;
            }

            if (string.IsNullOrEmpty(thirdPartyEmoteData.ImageFetchUrl))
            {
                Log.Warning($"Emote {emoteId} from {emoteProvider} is missing fetch url");
                return null;
            }

            using HttpClient client = new HttpClient();

            HttpResponseMessage httpResponseMessage = await client.GetAsync(thirdPartyEmoteData.ImageFetchUrl, cancellationToken);
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                Log.Error($"{emoteProvider} emote fetch ({thirdPartyEmoteData.Code}) responded with {httpResponseMessage.StatusCode} ({httpResponseMessage.ReasonPhrase})");
                return null;
            }

#if DEBUG
            Log.Debug($"Downloading third party emote ({thirdPartyEmoteData.Provider}): {thirdPartyEmoteData.Code}");
#endif

            return await EmoteData.ReadFromStreamAsync(await httpResponseMessage.Content.ReadAsStreamAsync(), thirdPartyEmoteData.IsAnimated, cancellationToken);
        }
    }
}
