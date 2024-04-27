using Newtonsoft.Json;
using System.Collections.Generic;

namespace ChattersInGame.Twitch.ThirdParty.FFZ
{
    public class FFZEmote
    {
        [JsonProperty("id")]
        public int EmoteId { get; set; }

        [JsonProperty("name")]
        public string EmoteName { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("public")]
        public bool IsPublic { get; set; }

        [JsonProperty("hidden")]
        public bool IsHidden { get; set; }

        [JsonProperty("modifier")]
        public bool IsModifier { get; set; }

        [JsonProperty("modifier_flags")]
        public int ModifierFlags { get; set; }

        [JsonProperty("offset")]
        public string Offset { get; set; }

        [JsonProperty("margins")]
        public string Margins { get; set; }

        [JsonProperty("css")]
        public string CSS { get; set; }

        [JsonProperty("owner")]
        public FFZEmoteOwner Owner { get; set; }

        [JsonProperty("artist")]
        public FFZEmoteOwner Artist { get; set; }

        [JsonProperty("urls")]
        public Dictionary<string, string> Urls { get; set; }

        [JsonProperty("animated")]
        public Dictionary<string, string> AnimatedUrls { get; set; }

        [JsonProperty("masks")]
        public Dictionary<string, string> MaskUrls { get; set; }

        [JsonProperty("masks_animated")]
        public Dictionary<string, string> AnimatedMaskUrls { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("usage_count")]
        public int UsageCount { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }

        [JsonProperty("last_updated")]
        public string LastUpdatedAt { get; set; }
    }
}
