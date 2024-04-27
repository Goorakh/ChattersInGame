using Newtonsoft.Json;

namespace ChattersInGame.Twitch.ThirdParty.FFZ
{
    public class FFZEmoteOwner
    {
        [JsonProperty("_id")]
        public int UserId { get; set; }

        [JsonProperty("name")]
        public string Username { get; set; }

        [JsonProperty("display_name")]
        public string UserDisplayName { get; set; }
    }
}
