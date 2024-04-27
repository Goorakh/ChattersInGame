using Newtonsoft.Json;

namespace ChattersInGame.Twitch.Chat.Notification
{
    public class ChannelChatCharityDonationNotificationData
    {
        [JsonProperty("charity_name")]
        public string CharityName { get; set; }

        [JsonProperty("amount")]
        public ChannelChatCharityDonationAmountData Amount { get; set; }
    }
}
