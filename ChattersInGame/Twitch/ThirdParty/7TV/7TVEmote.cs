using Newtonsoft.Json;

namespace ChattersInGame.Twitch.ThirdParty._7TV
{
    public class _7TVEmote
    {
        [JsonProperty("id")]
        public string EmoteId { get; set; }

        [JsonProperty("name")]
        public string EmoteName { get; set; }

        [JsonProperty("flags")]
        public int Flags { get; set; }

        [JsonProperty("timestamp")]
        public ulong Timestamp { get; set; }

        [JsonProperty("actor_id")]
        public string ActorId { get; set; }

        [JsonProperty("data")]
        public _7TVEmoteData Data { get; set; }
    }
}
