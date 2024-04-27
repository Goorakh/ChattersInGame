using Newtonsoft.Json;

namespace ChattersInGame.Twitch.Chat.Notification
{
    public class ChannelChatAnnouncementNotificationData
    {
        [JsonProperty("color")]
        public string Color { get; set; }
    }
}
