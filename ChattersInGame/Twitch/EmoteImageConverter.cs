using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ChattersInGame.Twitch
{
    public static class EmoteImageConverter
    {
        class QueuedImageConversion
        {
            public readonly Stream ImageStream;
            public readonly bool IsAnimated;
            public readonly CancellationToken CancellationToken;

            public Task<EmoteConversionResult> ConversionTask;

            public QueuedImageConversion(Stream imageStream, bool isAnimated, CancellationToken cancellationToken)
            {
                ImageStream = imageStream;
                IsAnimated = isAnimated;
                CancellationToken = cancellationToken;
            }

            public async Task<EmoteConversionResult> PerformConversion(CancellationToken cancellationToken)
            {
                await Task.Yield();

                using CancellationTokenSource externalProcessErrorTokenSource = new CancellationTokenSource();
                using CancellationTokenSource cancelledOrErrorTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, externalProcessErrorTokenSource.Token);

                CancellationToken cancelledOrErrorToken = cancelledOrErrorTokenSource.Token;

                TcpListener tcpListener = null;
                Process imageConverterProcess = null;
                TcpClient client = null;
                try
                {
                    IPAddress ipAddress = IPAddress.Loopback;
                    const int port = 4006;

                    tcpListener = new TcpListener(ipAddress, port);
                    tcpListener.Start();

                    imageConverterProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.Combine(Path.GetDirectoryName(Main.Instance.Info.Location), "lib/ImageConverter/ImageConverter.exe"),
                        UseShellExecute = false,
                        Arguments = $"{ipAddress} {port}",
                        RedirectStandardError = true,
#if DEBUG
                        RedirectStandardOutput = true,
#endif
                        WorkingDirectory = Path.Combine(Path.GetDirectoryName(Main.Instance.Info.Location), "lib/ImageConverter/"),
                        CreateNoWindow = true
                    });

#if DEBUG
                    imageConverterProcess.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Log.Debug_NoCallerPrefix($"{e.Data} ({s})");
                        }
                    };

                    imageConverterProcess.BeginOutputReadLine();
#endif

                    imageConverterProcess.ErrorDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Log.Error_NoCallerPrefix($"Error converting emote data: {e.Data} ({s})");
                            externalProcessErrorTokenSource.Cancel();
                        }
                    };

                    imageConverterProcess.BeginErrorReadLine();

                    client = await tcpListener.AcceptTcpClientAsync();
                    cancelledOrErrorToken.ThrowIfCancellationRequested();

                    NetworkStream clientStream = client.GetStream();

                    byte[] streamLengthBytes = BitConverter.GetBytes(ImageStream.Length);
                    await clientStream.WriteAsync(streamLengthBytes, 0, streamLengthBytes.Length, cancelledOrErrorToken);

                    const int BUFFER_SIZE = 1024;
                    byte[] buffer = new byte[BUFFER_SIZE];

                    {
                        int readBytes;
                        while ((readBytes = await ImageStream.ReadAsync(buffer, 0, BUFFER_SIZE, cancelledOrErrorToken)) > 0)
                        {
                            await clientStream.WriteAsync(buffer, 0, readBytes, cancelledOrErrorToken);
                        }
                    }

                    await clientStream.FlushAsync(cancelledOrErrorToken);

                    while (client.Available < sizeof(int)) { }

                    byte[] outputLengthBytes = new byte[sizeof(int)];
                    await clientStream.ReadAsync(outputLengthBytes, 0, sizeof(int), cancelledOrErrorToken);

                    int outputLength = BitConverter.ToInt32(outputLengthBytes, 0);
                    byte[] outputBytes = new byte[outputLength];

#if DEBUG
                    Log.Debug($"Converted emote bytes: {outputLength}");
#endif

                    {
                        int totalReadBytes = 0;
                        while (totalReadBytes < outputLength)
                        {
                            int readBytes = await clientStream.ReadAsync(buffer, 0, BUFFER_SIZE, cancelledOrErrorToken);
                            if (readBytes > 0)
                            {
                                Array.Copy(buffer, 0, outputBytes, totalReadBytes, readBytes);
                                totalReadBytes += readBytes;
                            }
                        }
                    }

#if DEBUG
                    Log.Debug("Received converted emote");
#endif

                    using MemoryStream outputStream = new MemoryStream(outputBytes);
                    using BinaryReader outputReader = new BinaryReader(outputStream);

                    EmoteSpriteSheetData? spriteSheetData;
                    if (outputReader.ReadBoolean())
                    {
                        int frameCountHorizontal = outputReader.ReadInt32();
                        int frameCountVertical = outputReader.ReadInt32();

                        int emptyFrames = outputReader.ReadInt32();

                        int frameCount = (frameCountHorizontal * frameCountVertical) - emptyFrames;

                        float[] frameDelays = new float[frameCount];
                        for (int i = 0; i < frameCount; i++)
                        {
                            frameDelays[i] = outputReader.ReadSingle();
                        }

#if DEBUG
                        Log.Debug($"Emote sprite sheet data: ({frameCountHorizontal}x{frameCountVertical}-{emptyFrames})");
#endif

                        spriteSheetData = new EmoteSpriteSheetData(new Vector2Int(frameCountHorizontal, frameCountVertical), frameDelays, emptyFrames);
                    }
                    else
                    {
                        spriteSheetData = null;
                    }

                    int imageBytesLength = (int)(outputLength - outputReader.BaseStream.Position);
                    byte[] imageBytes = outputReader.ReadBytes(imageBytesLength);

                    return new EmoteConversionResult(imageBytes, spriteSheetData);
                }
                finally
                {
                    if (imageConverterProcess != null)
                    {
                        if (!imageConverterProcess.HasExited)
                        {
                            imageConverterProcess.Close();
                        }

                        imageConverterProcess.Dispose();
                    }

                    if (client != null)
                    {
                        if (client.Connected)
                            client.Close();

                        client.Dispose();
                    }

                    tcpListener?.Stop();
                }
            }
        }

        public readonly struct EmoteSpriteSheetData
        {
            public readonly Vector2Int FrameCounts;
            public readonly float[] FrameDelays;
            public readonly int EmptyFrames;

            public EmoteSpriteSheetData(Vector2Int frameCounts, float[] frameDelays, int emptyFrames)
            {
                FrameCounts = frameCounts;
                FrameDelays = frameDelays;
                EmptyFrames = emptyFrames;
            }
        }

        public readonly struct EmoteConversionResult
        {
            public readonly byte[] PngBytes;

            public readonly EmoteSpriteSheetData? SpriteSheetData;

            public EmoteConversionResult(byte[] pngBytes, EmoteSpriteSheetData? spriteSheetData)
            {
                PngBytes = pngBytes;
                SpriteSheetData = spriteSheetData;
            }
        }

        static readonly ConcurrentQueue<QueuedImageConversion> _conversionQueue = new ConcurrentQueue<QueuedImageConversion>();

        static bool _isRunningConversionThread = false;

        public static async Task<EmoteConversionResult> ConvertEmoteImageAsync(Stream inputImageStream, bool isAnimated, CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            QueuedImageConversion queuedConversion = new QueuedImageConversion(inputImageStream, isAnimated, cancellationToken);

            _conversionQueue.Enqueue(queuedConversion);

            if (!_isRunningConversionThread)
            {
                _isRunningConversionThread = true;
                _ = Task.Run(conversionLoop);
            }

            while (queuedConversion.ConversionTask == null || queuedConversion.ConversionTask.Status < TaskStatus.RanToCompletion)
            {
                await Task.Delay(10);
            }

            if (queuedConversion.ConversionTask.IsCanceled)
            {
                throw new TaskCanceledException(queuedConversion.ConversionTask);
            }
            else if (queuedConversion.ConversionTask.Exception != null)
            {
                throw queuedConversion.ConversionTask.Exception;
            }
            else
            {
                return queuedConversion.ConversionTask.Result;
            }
        }

        static async Task conversionLoop()
        {
            while (true)
            {
                if (_conversionQueue.TryDequeue(out QueuedImageConversion queuedImageConversion))
                {
                    try
                    {
                        queuedImageConversion.ConversionTask = queuedImageConversion.PerformConversion(queuedImageConversion.CancellationToken);
                        await queuedImageConversion.ConversionTask;
                    }
                    catch (Exception e)
                    {
                        queuedImageConversion.ConversionTask = Task.FromException<EmoteConversionResult>(e);
                    }

                    await Task.Delay(500);
                }
                else
                {
                    await Task.Delay(10);
                }
            }
        }
    }
}
