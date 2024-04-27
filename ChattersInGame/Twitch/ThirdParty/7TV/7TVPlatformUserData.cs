using Newtonsoft.Json;

namespace ChattersInGame.Twitch.ThirdParty._7TV
{
    public class _7TVPlatformUserData
    {
        [JsonProperty("id")]
        public string PlatformUserId { get; set; }

        [JsonProperty("platform")]
        public string PlatformName { get; set; }

        [JsonProperty("username")]
        public string UserLoginName { get; set; }

        [JsonProperty("display_name")]
        public string UserDisplayName { get; set; }

        [JsonProperty("linked_at")]
        public ulong LinkedAt { get; set; }

        [JsonProperty("emote_capacity")]
        public int EmoteCapacity { get; set; }

        [JsonProperty("emote_set_id")]
        public string EmoteSetId { get; set; }

        [JsonProperty("emote_set")]
        public _7TVEmoteSet EmoteSet { get; set; }

        [JsonProperty("user")]
        public _7TVUserData UserData { get; set; }
    }
}
