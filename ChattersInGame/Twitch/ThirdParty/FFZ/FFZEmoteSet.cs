using Newtonsoft.Json;

namespace ChattersInGame.Twitch.ThirdParty.FFZ
{
    public class FFZEmoteSet
    {
        [JsonProperty("id")]
        public int SetId { get; set; }

        [JsonProperty("_type")]
        public int SetType { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("title")]
        public string Name { get; set; }

        [JsonProperty("css")]
        public string CSS { get; set; }

        [JsonProperty("emoticons")]
        public FFZEmote[] Emotes { get; set; }
    }
}
