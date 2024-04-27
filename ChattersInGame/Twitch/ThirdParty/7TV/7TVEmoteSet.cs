using Newtonsoft.Json;

namespace ChattersInGame.Twitch.ThirdParty._7TV
{
    public class _7TVEmoteSet
    {
        [JsonProperty("id")]
        public string SetId { get; set; }

        [JsonProperty("name")]
        public string SetName { get; set; }

        [JsonProperty("flags")]
        public int Flags { get; set; }

        [JsonProperty("tags")]
        public object[] Tags { get; set; }

        [JsonProperty("immutable")]
        public bool Immutable { get; set; }

        [JsonProperty("privileged")]
        public bool Priveleged { get; set; }

        [JsonProperty("emotes")]
        public _7TVEmote[] Emotes { get; set; }

        [JsonProperty("emote_count")]
        public int EmoteCount { get; set; }

        [JsonProperty("capacity")]
        public int Capacity { get; set; }

        [JsonProperty("owner")]
        public _7TVUserData Owner { get; set; }
    }
}
