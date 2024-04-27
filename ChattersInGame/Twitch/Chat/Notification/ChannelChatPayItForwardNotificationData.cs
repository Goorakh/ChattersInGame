﻿using Newtonsoft.Json;

namespace ChattersInGame.Twitch.Chat.Notification
{
    public class ChannelChatPayItForwardNotificationData
    {
        [JsonProperty("gifter_is_anonymous")]
        public bool GifterIsAnonymous { get; set; }

        [JsonProperty("gifter_user_id")]
        public string GifterUserId { get; set; }

        [JsonProperty("gifter_user_name")]
        public string GifterDisplayName { get; set; }

        [JsonProperty("gifter_user_login")]
        public string GifterLoginName { get; set; }
    }
}
