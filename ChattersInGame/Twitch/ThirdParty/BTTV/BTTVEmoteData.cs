using Newtonsoft.Json;

namespace ChattersInGame.Twitch.ThirdParty.BTTV
{
    public class BTTVEmoteData
    {
        [JsonProperty("id")]
        public string EmoteID { get; set; }

        [JsonProperty("code")]
        public string EmoteCode { get; set; }

        [JsonProperty("imageType")]
        public string ImageFormat { get; set; }

        [JsonProperty("animated")]
        public bool IsAnimated { get; set; }
    }
}
