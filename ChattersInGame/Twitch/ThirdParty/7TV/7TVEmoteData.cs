using Newtonsoft.Json;

namespace ChattersInGame.Twitch.ThirdParty._7TV
{
    public class _7TVEmoteData
    {
        [JsonProperty("id")]
        public string EmoteId { get; set; }

        [JsonProperty("name")]
        public string EmoteName { get; set; }

        [JsonProperty("flags")]
        public int Flags { get; set; }

        [JsonProperty("lifecycle")]
        public int Lifecycle { get; set; }

        [JsonProperty("state")]
        public string[] State { get; set; }

        [JsonProperty("listed")]
        public bool Listed { get; set; }

        [JsonProperty("animated")]
        public bool Animated { get; set; }

        [JsonProperty("owner")]
        public _7TVUserData Owner { get; set; }

        [JsonProperty("host")]
        public _7TVEmoteHost Host { get; set; }
    }
}
