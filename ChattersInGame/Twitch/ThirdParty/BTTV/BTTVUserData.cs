using Newtonsoft.Json;

namespace ChattersInGame.Twitch.ThirdParty.BTTV
{
    public class BTTVUserData
    {
        [JsonProperty("id")]
        public string UserID { get; set; }

        [JsonProperty("bots")]
        public string[] BotUsernames { get; set; }

        [JsonProperty("avatar")]
        public string UserAvatarUrl { get; set; }

        [JsonProperty("channelEmotes")]
        public BTTVEmoteData[] ChannelEmotes { get; set; }

        [JsonProperty("sharedEmotes")]
        public BTTVEmoteData[] SharedEmotes { get; set; }
    }
}
