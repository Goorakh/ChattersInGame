using Newtonsoft.Json;

namespace ChattersInGame.Twitch.Chat.Notification
{
    public class ChannelChatBitsBadgeTierNotificationData
    {
        [JsonProperty("tier")]
        public int Tier { get; set; }
    }
}
