using Newtonsoft.Json;

namespace ChattersInGame.Twitch.ThirdParty.FFZ
{
    public class FFZEmoteSetResponse
    {
        [JsonProperty("set")]
        public FFZEmoteSet EmoteSet { get; set; }

        [JsonProperty("users")]
        public object Users { get; set; }
    }
}
