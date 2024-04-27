using ChattersInGame.Twitch.ThirdParty._7TV;
using ChattersInGame.Twitch.ThirdParty.BTTV;
using ChattersInGame.Twitch.ThirdParty.FFZ;
using System.Collections.Generic;
using System.Linq;

namespace ChattersInGame.Twitch.ThirdParty
{
    public class ThirdPartyEmoteData
    {
        public ThirdPartyEmoteProvider Provider { get; }

        public string Code { get; }

        public string Id { get; }

        public bool IsAnimated { get; }

        public string ImageFormat { get; }

        public string ImageFetchUrl { get; }

        public ThirdPartyEmoteData(BTTVEmoteData bttvEmote)
        {
            Provider = ThirdPartyEmoteProvider.BTTV;
            Code = bttvEmote.EmoteCode;
            Id = bttvEmote.EmoteID;
            IsAnimated = bttvEmote.IsAnimated;
            ImageFormat = bttvEmote.ImageFormat;

            ImageFetchUrl = $"https://cdn.betterttv.net/emote/{Id}/2x";
        }

        public ThirdPartyEmoteData(_7TVEmote emote)
        {
            Provider = ThirdPartyEmoteProvider._7TV;
            Code = emote.EmoteName;
            Id = emote.EmoteId;
            IsAnimated = emote.Data.Animated;

            ImageFetchUrl = emote.Data.Host.FindBestUrl(["2x", "1x", "3x", "4x"], ["WEBP", "AVIF"], out _, out string imageFormat);
            ImageFormat = imageFormat;
        }

        public ThirdPartyEmoteData(FFZEmote emote)
        {
            Provider = ThirdPartyEmoteProvider.FFZ;
            Code = emote.EmoteName;
            Id = emote.EmoteId.ToString();
            IsAnimated = emote.AnimatedUrls != null;

            static string getBestUrl(Dictionary<string, string> urlLookup)
            {
                if (urlLookup.TryGetValue("2", out string url2x))
                    return url2x;

                if (urlLookup.TryGetValue("1", out string url1x))
                    return url1x;

                return urlLookup.Values.First();
            }

            if (IsAnimated)
            {
                ImageFetchUrl = getBestUrl(emote.AnimatedUrls) + ".gif";
                ImageFormat = "gif";
            }
            else
            {
                ImageFetchUrl = getBestUrl(emote.Urls);
                ImageFormat = "png";
            }
        }
    }
}
