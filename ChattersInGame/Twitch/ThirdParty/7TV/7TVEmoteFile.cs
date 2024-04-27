using Newtonsoft.Json;

namespace ChattersInGame.Twitch.ThirdParty._7TV
{
    public class _7TVEmoteFile
    {
        string _name;

        [JsonProperty("name")]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;

                int dotIndex = _name.IndexOf('.');
                Quality = dotIndex != -1 ? _name.Remove(dotIndex) : _name;
            }
        }

        [JsonProperty("static_name")]
        public string StaticName { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("frame_count")]
        public int FrameCount { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonIgnore]
        public string Quality { get; private set; }
    }
}
