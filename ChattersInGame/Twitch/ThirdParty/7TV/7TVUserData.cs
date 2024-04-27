using Newtonsoft.Json;

namespace ChattersInGame.Twitch.ThirdParty._7TV
{
    public class _7TVUserData
    {
        [JsonProperty("id")]
        public string UserId { get; set; }

        [JsonProperty("username")]
        public string UserLoginName { get; set; }

        [JsonProperty("display_name")]
        public string UserDisplayName { get; set; }

        [JsonProperty("avatar_url")]
        public string UserAvatarUrl { get; set; }

        [JsonProperty("style")]
        public _7TVUserStyle Style { get; set; }

        [JsonProperty("roles")]
        public string[] Roles { get; set; }

        [JsonProperty("connections")]
        public _7TVPlatformUserData[] Connections { get; set; }
    }
}
