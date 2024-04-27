using Newtonsoft.Json;
using System;
using System.Linq;

namespace ChattersInGame.Twitch.ThirdParty._7TV
{
    public class _7TVEmoteHost
    {
        [JsonProperty("url")]
        public string BaseUrl { get; set; }

        [JsonProperty("files")]
        public _7TVEmoteFile[] Files { get; set; }

        public string FindBestUrl(string[] qualityPreferences, string[] formatPreferences, out string selectedQuality, out string selectedFormat)
        {
            foreach (string formatPreference in formatPreferences)
            {
                _7TVEmoteFile[] formatMatchingFiles = Files.Where(file => string.Equals(file.Format, formatPreference, StringComparison.OrdinalIgnoreCase)).ToArray();
                if (formatMatchingFiles.Length == 0)
                    continue;

                selectedFormat = formatPreference;

                foreach (string qualityPreference in qualityPreferences)
                {
                    selectedQuality = qualityPreference;

                    foreach (_7TVEmoteFile emoteFile in formatMatchingFiles)
                    {
                        if (string.Equals(emoteFile.Quality, qualityPreference, StringComparison.OrdinalIgnoreCase))
                        {
                            return $"https:{BaseUrl}/{emoteFile.Name}";
                        }
                    }
                }
            }

            selectedQuality = default;
            selectedFormat = default;
            return null;
        }
    }
}
