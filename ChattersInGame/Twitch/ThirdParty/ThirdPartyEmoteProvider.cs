using System;

namespace ChattersInGame.Twitch.ThirdParty
{
    public enum ThirdPartyEmoteProvider
    {
        BTTV,
        _7TV,
        FFZ
    }

    public static class ThirdPartyEmoteProviderExtensions
    {
        public static string FormatUniqueId(this ThirdPartyEmoteProvider emoteProvider, string id)
        {
            return emoteProvider switch
            {
                ThirdPartyEmoteProvider.BTTV => $"bttv_{id}",
                ThirdPartyEmoteProvider._7TV => $"7tv_{id}",
                ThirdPartyEmoteProvider.FFZ => $"ffz_{id}",
                _ => throw new NotImplementedException($"Emote provider {emoteProvider} is not implemented")
            };
        }
    }
}
