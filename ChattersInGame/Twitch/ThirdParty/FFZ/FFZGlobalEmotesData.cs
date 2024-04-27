using Newtonsoft.Json;

namespace ChattersInGame.Twitch.ThirdParty.FFZ
{
    public class FFZGlobalEmotesData
    {
        [JsonProperty("default_sets")]
        public int[] DefaultSets { get; set; }
    }
}
