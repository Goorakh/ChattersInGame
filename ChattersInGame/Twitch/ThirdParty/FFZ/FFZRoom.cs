using Newtonsoft.Json;
using System.Collections.Generic;

namespace ChattersInGame.Twitch.ThirdParty.FFZ
{
    public class FFZRoom
    {
        [JsonProperty("_id")]
        public int RoomId { get; set; }

        [JsonProperty("twitch_id")]
        public int TwitchId { get; set; }

        [JsonProperty("youtube_id")]
        public int? YoutubeId { get; set; }

        [JsonProperty("id")]
        public string RoomIdentifier { get; set; }

        [JsonProperty("is_group")]
        public bool IsGroup { get; set; }

        [JsonProperty("display_name")]
        public string RoomDisplayName { get; set; }

        [JsonProperty("set")]
        public int EmoteSetId { get; set; }

        [JsonProperty("moderator_badge")]
        public string ModeratorBadge { get; set; }

        [JsonProperty("vip_badge")]
        public Dictionary<string, string> VipBadgeUrls { get; set; }

        [JsonProperty("mod_urls")]
        public Dictionary<string, string> ModUrls { get; set; }

        [JsonProperty("user_badges")]
        public object UserBadges { get; set; }

        [JsonProperty("user_badge_ids")]
        public object UserBadgeIds { get; set; }

        [JsonProperty("css")]
        public string CSS { get; set; }
    }
}
