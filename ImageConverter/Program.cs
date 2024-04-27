using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace ImageConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
                return;

            IPAddress connectIP = IPAddress.Parse(args[0]);
            int connectPort = int.Parse(args[1]);

            using TcpClient client = new TcpClient();

            void log(string log)
            {
                Console.Out.WriteLine($"({connectIP}:{connectPort}): {log}");
            }

            log("Connecting...");

            try
            {
                client.ConnectAsync(connectIP, connectPort, new CancellationTokenSource(1000).Token).AsTask().Wait();
            }
            catch (OperationCanceledException) // connection timeout
            {
                log("Connection timed out");
                return;
            }

            log("Connected!");

            NetworkStream clientStream = client.GetStream();

            while (client.Available < sizeof(long)) { }

            Span<byte> streamLengthBuffer = stackalloc byte[sizeof(long)];
            clientStream.Read(streamLengthBuffer);

            long streamLength = BitConverter.ToInt64(streamLengthBuffer);

            log($"Stream length: {streamLength}");

            byte[] inputBytes = new byte[streamLength];

            const int BUFFER_SIZE = 1024;
            Span<byte> buffer = stackalloc byte[BUFFER_SIZE];

            int totalReadBytes = 0;
            while (totalReadBytes < streamLength)
            {
                int readBytes = clientStream.Read(buffer);
                if (readBytes > 0)
                {
                    buffer.Slice(0, readBytes).CopyTo(new Span<byte>(inputBytes, totalReadBytes, readBytes));

                    totalReadBytes += readBytes;
                }
            }

            using MemoryStream inputStream = new MemoryStream(inputBytes);

            using Image<Rgba32> inputImage = Image.Load<Rgba32>(inputStream);

            /*
            Color backgroundColor;
            if (inputImage.Metadata.TryGetWebpMetadata(out WebpMetadata? webpMetadata))
            {
                backgroundColor = webpMetadata.BackgroundColor;
            }
            else if (inputImage.Metadata.TryGetGifMetadata(out GifMetadata? gifMetadata) && gifMetadata.GlobalColorTable.HasValue)
            {
                backgroundColor = gifMetadata.GlobalColorTable.Value.Span[gifMetadata.BackgroundColorIndex];
            }
            else if (inputImage.Metadata.TryGetPngMetadata(out PngMetadata? pngMetadata) && pngMetadata.TransparentColor.HasValue)
            {
                backgroundColor = pngMetadata.TransparentColor.Value;
            }
            else
            {
                backgroundColor = Color.Transparent;
            }
            */

            using MemoryStream outputStream = new MemoryStream();

            if (inputImage.Frames == null || inputImage.Frames.Count <= 1)
            {
                log("Single frame input, returning image as png");

                using BinaryWriter binaryWriter = new BinaryWriter(outputStream);
                binaryWriter.Write(false);

                inputImage.SaveAsPng(outputStream);
            }
            else
            {
                ImageFrameCollection<Rgba32> imageFrames = inputImage.Frames;
                int frameCount = imageFrames.Count;

                log($"{frameCount} frame input, generating spritesheet");

                int frameWidth = imageFrames[0].Width;
                int frameHeight = imageFrames[0].Height;
                float frameWidthPerHeight = frameWidth / (float)frameHeight;

                int frameCountHorizontal = (int)Math.Ceiling(Math.Sqrt(frameCount * frameWidthPerHeight) / frameWidthPerHeight);
                int frameCountVertical = MathUtil.IntDivisionCeil(frameCount, frameCountHorizontal);

                int numEmptyFrames = (frameCountHorizontal * frameCountVertical) - frameCount;
                if (numEmptyFrames >= frameCountHorizontal)
                {
                    frameCountVertical -= numEmptyFrames / frameCountHorizontal;
                    numEmptyFrames %= frameCountHorizontal;
                }

                float[] frameDelays = new float[frameCount];

                using Image<Rgba32> outputImage = new Image<Rgba32>(frameWidth * frameCountHorizontal, frameHeight * frameCountVertical, new Rgba32(0, 0, 0, 0));

                for (int frameY = 0; frameY < frameCountVertical; frameY++)
                {
                    bool reachedEnd = false;

                    for (int frameX = 0; frameX < frameCountHorizontal; frameX++)
                    {
                        int frameIndex = (frameY * frameCountHorizontal) + frameX;
                        if (frameIndex >= frameCount)
                        {
                            reachedEnd = true;
                            break;
                        }

                        ImageFrame<Rgba32> frameImage = imageFrames[frameIndex];

                        float frameDelay;

                        bool checkWhiteArtifacts;
                        if (frameImage.Metadata.TryGetWebpFrameMetadata(out WebpFrameMetadata? wepbFrameMetadata))
                        {
                            checkWhiteArtifacts = true;

                            frameDelay = wepbFrameMetadata.FrameDelay / 1000f;
                        }
                        else if (frameImage.Metadata.TryGetGifMetadata(out GifFrameMetadata? gifFrameMetadata))
                        {
                            checkWhiteArtifacts = false;
                            frameDelay = gifFrameMetadata.FrameDelay / 100f;
                        }
                        else if (frameImage.Metadata.TryGetPngMetadata(out PngFrameMetadata? pngFrameMetadata))
                        {
                            checkWhiteArtifacts = false;
                            Rational frameDelayRatio = pngFrameMetadata.FrameDelay;
                            frameDelay = frameDelayRatio.Numerator / (frameDelayRatio.Denominator * 100f);
                        }
                        else
                        {
                            checkWhiteArtifacts = false;
                            log("METADATA FORMAT NOT SUPPORTED AAAAAAAAAAAAAAAAAA");
                            frameDelay = 1f / 15f;
                        }

                        if (frameDelay <= 0f)
                        {
                            log("0 FRAME DELAY?????? WHY");
                            frameDelays[frameIndex] = 1f / 30f;
                        }
                        else
                        {
                            frameDelays[frameIndex] = frameDelay;
                        }

                        int[,] whiteArtifactSizeLookupMap = new int[frameWidth, frameHeight];
                        if (checkWhiteArtifacts)
                        {
                            const uint ARTIFACT_COLOR = (255U << (8 * 3))  // R
                                                      | (255U << (8 * 2))  // G
                                                      | (255U << (8 * 1))  // B
                                                      | (255U);            // A

                            int[] topEdgePossibleArtifactSize = new int[frameWidth];
                            int[] bottomEdgePossibleArtifactSize = new int[frameWidth];
                            for (int x = 0; x < frameWidth; x++)
                            {
                                for (int y = 0; y < frameHeight && frameImage[x, y].Rgba == ARTIFACT_COLOR; y++)
                                {
                                    topEdgePossibleArtifactSize[x]++;
                                }

                                for (int y = frameHeight - 1; y >= 0 && frameImage[x, y].Rgba == ARTIFACT_COLOR; y--)
                                {
                                    bottomEdgePossibleArtifactSize[x]++;
                                }
                            }

                            int[] leftEdgePossibleArtifactSize = new int[frameHeight];
                            int[] rightEdgePossibleArtifactSize = new int[frameHeight];
                            for (int y = 0; y < frameHeight; y++)
                            {
                                for (int x = 0; x < frameWidth && frameImage[x, y].Rgba == ARTIFACT_COLOR; x++)
                                {
                                    leftEdgePossibleArtifactSize[y]++;
                                }

                                for (int x = frameWidth - 1; x >= 0 && frameImage[x, y].Rgba == ARTIFACT_COLOR; x--)
                                {
                                    rightEdgePossibleArtifactSize[y]++;
                                }
                            }

                            for (int x = 0; x < frameWidth; x++)
                            {
                                for (int y = 0; y < frameHeight; y++)
                                {
                                    List<int> possibleArtifactSizes = [];

                                    if (y < topEdgePossibleArtifactSize[x])
                                        possibleArtifactSizes.Add(topEdgePossibleArtifactSize[x]);

                                    if (frameHeight - y <= bottomEdgePossibleArtifactSize[x])
                                        possibleArtifactSizes.Add(bottomEdgePossibleArtifactSize[x]);

                                    if (x < leftEdgePossibleArtifactSize[y])
                                        possibleArtifactSizes.Add(leftEdgePossibleArtifactSize[y]);

                                    if (frameWidth - x <= rightEdgePossibleArtifactSize[y])
                                        possibleArtifactSizes.Add(rightEdgePossibleArtifactSize[y]);

                                    if (possibleArtifactSizes.Count > 0)
                                    {
                                        whiteArtifactSizeLookupMap[x, y] = possibleArtifactSizes.Max();
                                    }
                                }
                            }
                        }

                        for (int frameImageY = 0; frameImageY < frameHeight; frameImageY++)
                        {
                            for (int frameImageX = 0; frameImageX < frameWidth; frameImageX++)
                            {
                                Rgba32 pixelColor = frameImage[frameImageX, frameImageY];
                                if (checkWhiteArtifacts)
                                {
                                    int artifactSize = whiteArtifactSizeLookupMap[frameImageX, frameImageY];
                                    if (artifactSize > 10)
                                    {
                                        pixelColor = new Rgba32(0, 0, 0, 0);
                                    }
                                }

                                outputImage[(frameX * frameWidth) + frameImageX, (frameY * frameHeight) + frameImageY] = pixelColor;
                            }
                        }
                    }

                    if (reachedEnd)
                        break;
                }

                using BinaryWriter binaryWriter = new BinaryWriter(outputStream);

                binaryWriter.Write(true);
                binaryWriter.Write(frameCountHorizontal);
                binaryWriter.Write(frameCountVertical);

                binaryWriter.Write(numEmptyFrames);

                foreach (float frameDelay in frameDelays)
                {
                    binaryWriter.Write(frameDelay);
                }

                outputImage.SaveAsPng(outputStream);
            }

            byte[] outputBytes = outputStream.ToArray();

            clientStream.Write(BitConverter.GetBytes(outputBytes.Length));
            clientStream.Write(outputBytes);
            clientStream.Flush();

            client.Close();
        }
    }
}
