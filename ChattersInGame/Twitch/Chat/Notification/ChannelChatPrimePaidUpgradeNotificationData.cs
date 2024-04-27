using Newtonsoft.Json;

namespace ChattersInGame.Twitch.Chat.Notification
{
    public class ChannelChatPrimePaidUpgradeNotificationData
    {
        [JsonProperty("sub_tier")]
        public string Tier { get; set; }
    }
}
