using Newtonsoft.Json;

namespace ChattersInGame.Twitch.Chat.Message
{
    public class ChatMessageMentionData
    {
        [JsonProperty("user_id")]
        public string MentionedUserID { get; set; }

        [JsonProperty("user_name")]
        public string MentionedUserDisplayName { get; set; }

        [JsonProperty("user_login")]
        public string MentionedUserLoginName { get; set; }
    }
}
