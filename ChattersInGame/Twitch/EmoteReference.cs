using ChattersInGame.Twitch.Emotes;
using ChattersInGame.Twitch.ThirdParty;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ChattersInGame.Twitch
{
    public class EmoteReference : IDisposable
    {
        static readonly ConcurrentDictionary<string, EmoteReference> _cachedEmotes = [];

        readonly CancellationTokenSource _objectDisposedTokenSource = new CancellationTokenSource();

        public string EmoteSetId { get; }

        public string EmoteId { get; }

        public ThirdPartyEmoteProvider? EmoteProvider { get; }

        public EmoteImage Image { get; private set; }

        public LoadState LoadState { get; private set; } = LoadState.Loading;

        bool _isDisposed;

        EmoteReference(string emoteSetId, string emoteId, ThirdPartyEmoteProvider? thirdPartyEmoteProvider)
        {
            EmoteSetId = emoteSetId;
            EmoteId = emoteId;
            EmoteProvider = thirdPartyEmoteProvider;
        }

        ~EmoteReference()
        {
            dispose();
        }

        public void Dispose()
        {
            dispose();
            GC.SuppressFinalize(this);
        }

        protected virtual void dispose()
        {
            if (!_isDisposed)
            {
                _objectDisposedTokenSource.Cancel();
                _objectDisposedTokenSource.Dispose();

                Image?.Dispose();

                _isDisposed = true;
            }
        }

        void loadImageData()
        {
            LoadState = LoadState.Loading;

            string emoteStorageId;
            if (EmoteProvider.HasValue)
            {
                emoteStorageId = EmoteProvider.Value.FormatUniqueId(EmoteId);
            }
            else
            {
                emoteStorageId = EmoteId;
            }

            if (TwitchDataStorage.TryGetEmoteData(emoteStorageId, out EmoteData emoteData))
            {
                Image = new EmoteImage(emoteData);
                LoadState = LoadState.Complete;
                return;
            }

            Task.Run(async () =>
            {
                EmoteData loadedData;
                try
                {
                    loadedData = await downloadEmoteAsync(_objectDisposedTokenSource.Token);
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix($"Failed to load emote data: {e}");
                    LoadState = LoadState.Failed;
                    return;
                }

                if (loadedData != null)
                {
                    Image = new EmoteImage(loadedData);
                    Image.CallWhenLoaded(() =>
                    {
                        TwitchDataStorage.StoreEmoteData(emoteStorageId, loadedData);
                        LoadState = LoadState.Complete;
                    });
                }
                else
                {
                    LoadState = LoadState.Failed;
                }
            }, _objectDisposedTokenSource.Token);
        }

        async Task<EmoteData> downloadEmoteAsync(CancellationToken cancellationToken)
        {
            if (EmoteProvider.HasValue)
            {
                return await ThirdPartyEmoteManager.DownloadEmoteAsync(EmoteProvider.Value, EmoteId, cancellationToken);
            }

            GetEmoteSetResponse getEmoteSetResponse = await TwitchAPI.GetEmoteSets([EmoteSetId], _objectDisposedTokenSource.Token);
            if (!getEmoteSetResponse.TryFindEmote(EmoteId, out EmoteSetEmoteData emoteSetData))
            {
                Log.Warning($"Emote {EmoteId} does not exist in set {EmoteSetId}");
                return null;
            }

            if (emoteSetData.EmoteFormats.Length == 0)
            {
                Log.Error("No emote formats specified");
                return null;
            }

            if (emoteSetData.ThemeModes.Length == 0)
            {
                Log.Error("No emote themes specified");
                return null;
            }

            if (emoteSetData.Scales.Length == 0)
            {
                Log.Error("No emote scales specified");
                return null;
            }

            string format = emoteSetData.EmoteFormats.SelectMin(format => format switch
            {
                "animated" => 0,
                "static" => 1,
                _ => throw new NotImplementedException($"Format {format} is not implemented")
            });

            string theme = emoteSetData.ThemeModes.SelectMin(theme => theme switch
            {
                "dark" => 0,
                "light" => 1,
                _ => throw new NotImplementedException($"Theme {theme} is not implemented")
            });

            string scale = emoteSetData.Scales.SelectMin(scale => scale switch
            {
                "2.0" => 0,
                "1.0" => 1,
                "3.0" => 2,
                _ => throw new NotImplementedException($"Scale {scale} is not implemented")
            });

            string emoteFetchUrl = getEmoteSetResponse.GetEmoteFetchUrl(emoteSetData, format, theme, scale);

            using HttpClient client = new HttpClient();

            using HttpResponseMessage fetchEmoteResponseMessage = await client.GetAsync(emoteFetchUrl, cancellationToken);
            if (!fetchEmoteResponseMessage.IsSuccessStatusCode)
            {
                Log.Error($"Fetch emote request returned {fetchEmoteResponseMessage.StatusCode}");
                return null;
            }

            bool isAnimated = format switch
            {
                "animated" => true,
                "static" => false,
                _ => throw new NotImplementedException($"Format {format} is not implemented")
            };

#if DEBUG
            Log.Debug($"Downloading Twitch emote: {emoteSetData.EmoteName}");
#endif

            return await EmoteData.ReadFromStreamAsync(await fetchEmoteResponseMessage.Content.ReadAsStreamAsync(), isAnimated, cancellationToken);
        }

        public static EmoteReference GetEmote(string emoteSetId, string emoteId)
        {
            if (!_cachedEmotes.TryGetValue(emoteId, out EmoteReference emote))
            {
                emote = new EmoteReference(emoteSetId, emoteId, null);
                _cachedEmotes[emoteId] = emote;
                emote.loadImageData();
            }

            return emote;
        }

        public static EmoteReference GetThirdPartyEmote(ThirdPartyEmoteProvider emoteProvider, string emoteId)
        {
            string key = emoteProvider.FormatUniqueId(emoteId);

            if (!_cachedEmotes.TryGetValue(key, out EmoteReference emote))
            {
                emote = new EmoteReference(string.Empty, emoteId, emoteProvider);
                _cachedEmotes[key] = emote;
                emote.loadImageData();
            }

            return emote;
        }
    }
}
