using Newtonsoft.Json;

namespace ChattersInGame.Twitch.ThirdParty.FFZ
{
    public class FFZRoomResponse
    {
        [JsonProperty("room")]
        public FFZRoom Room { get; set; }
    }
}
