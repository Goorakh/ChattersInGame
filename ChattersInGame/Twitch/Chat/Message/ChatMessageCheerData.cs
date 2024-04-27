using Newtonsoft.Json;

namespace ChattersInGame.Twitch.Chat.Message
{
    public class ChatMessageCheerData
    {
        [JsonProperty("bits")]
        public int TotalBits { get; set; }
    }
}
