using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;

namespace ChattersInGame.Twitch
{
    public class EmoteData
    {
        public EmoteMetadata Metadata;
        public byte[] ImageBytes;

        public EmoteData()
        {
        }

        public EmoteData(EmoteMetadata metadata, byte[] imageBytes)
        {
            Metadata = metadata;
            ImageBytes = imageBytes;
        }

        public static async Task<EmoteData> ReadFromStreamAsync(Stream stream, bool animated, CancellationToken cancellationToken = default)
        {
            EmoteImageConverter.EmoteConversionResult conversionResult = await EmoteImageConverter.ConvertEmoteImageAsync(stream, animated, cancellationToken);

            if (animated)
            {
                if (conversionResult.SpriteSheetData.HasValue)
                {
                    EmoteImageConverter.EmoteSpriteSheetData spriteSheet = conversionResult.SpriteSheetData.Value;
                    EmoteMetadata metadata = EmoteMetadata.Animated(spriteSheet.FrameCounts, spriteSheet.FrameDelays, spriteSheet.EmptyFrames);
                    return new EmoteData(metadata, conversionResult.PngBytes);
                }
                else
                {
                    Log.Warning("Attempted to create animated emote without any frames");
                    return new EmoteData(EmoteMetadata.Static(), conversionResult.PngBytes);
                }
            }
            else
            {
                if (conversionResult.SpriteSheetData.HasValue)
                {
                    Log.Warning("Creating static emote from several frames");
                }

                return new EmoteData(EmoteMetadata.Static(), conversionResult.PngBytes);
            }
        }
    }
}
