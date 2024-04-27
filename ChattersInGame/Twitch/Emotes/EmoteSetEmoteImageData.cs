using Newtonsoft.Json;

namespace ChattersInGame.Twitch.Emotes
{
    public class EmoteSetEmoteImageData
    {
        [JsonProperty("url_1x")]
        public string SmallUrl { get; set; }

        [JsonProperty("url_2x")]
        public string MediumUrl { get; set; }

        [JsonProperty("url_4x")]
        public string LargeUrl { get; set; }
    }
}
