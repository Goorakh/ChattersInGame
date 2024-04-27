using System;
using System.Threading;
using UnityEngine;

namespace ChattersInGame.Twitch
{
    public class EmoteImage : IDisposable
    {
        readonly CancellationTokenSource _objectDisposedTokenSource = new CancellationTokenSource();

        public Texture2D ImageTexture { get; private set; }

        EmoteFrame[] _emoteFrames = [];

        public int FrameCount { get; private set; }

        public bool IsAnimated { get; private set; }

        public float TotalDuration { get; private set; }

        bool _isLoaded;
        public bool IsLoaded
        {
            get
            {
                return _isLoaded;
            }
            private set
            {
                if (_isLoaded == value)
                    return;

                _isLoaded = value;

                if (_isLoaded)
                {
                    OnLoaded?.Invoke();
                }
            }
        }

        public event Action OnLoaded;

        bool _isDisposed;

        public EmoteImage(EmoteData emoteData)
        {
            _ = AsyncUtils.RunNextUnityUpdate(() =>
            {
                ImageTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };

                if (ImageTexture.LoadImage(emoteData.ImageBytes))
                {
                    ImageTexture.Apply(true);
                }
                else
                {
                    GameObject.Destroy(ImageTexture);
                    ImageTexture = GameObject.Instantiate(Texture2D.redTexture);
                }

                int horizontalCount;
                int verticalCount;

                if (emoteData.Metadata.Type == EmoteType.Animated)
                {
                    horizontalCount = emoteData.Metadata.FrameCounts.Value.x;
                    verticalCount = emoteData.Metadata.FrameCounts.Value.y;

                    _emoteFrames = new EmoteFrame[(horizontalCount * verticalCount) - emoteData.Metadata.EmptyFrames.Value];
                }
                else
                {
                    horizontalCount = 1;
                    verticalCount = 1;

                    _emoteFrames = new EmoteFrame[1];
                }

                Vector2 frameSize = new Vector2(ImageTexture.width / (float)horizontalCount, ImageTexture.height / (float)verticalCount);

                for (int y = 0; y < verticalCount; y++)
                {
                    for (int x = 0; x < horizontalCount; x++)
                    {
                        int frameIndex = (y * horizontalCount) + x;

                        // The only remaining frames are empty, skip them all.
                        // This should only ever happen when y = verticalCount - 1,
                        // so a break; should leave both loops
                        if (frameIndex >= _emoteFrames.Length)
                            break;

                        Rect frameRect = new Rect(x * frameSize.x, (verticalCount - 1 - y) * frameSize.y, frameSize.x, frameSize.y);
                        Vector2 framePivot = new Vector2((x + 0.5f) / horizontalCount, (verticalCount - y - (1 - 0.5f)) / verticalCount);

                        _emoteFrames[frameIndex] = new EmoteFrame(this, framePivot, frameRect, TotalDuration);

                        if (emoteData.Metadata.Type == EmoteType.Animated)
                        {
                            TotalDuration += emoteData.Metadata.FrameDelays[frameIndex];
                        }
                    }
                }

                FrameCount = _emoteFrames.Length;
                IsAnimated = FrameCount > 1;

                IsLoaded = true;
            }, _objectDisposedTokenSource.Token);
        }

        ~EmoteImage()
        {
            dispose();
        }

        public void Dispose()
        {
            dispose();
            GC.SuppressFinalize(this);
        }

        protected virtual void dispose()
        {
            if (!_isDisposed)
            {
                _objectDisposedTokenSource.Cancel();
                _objectDisposedTokenSource.Dispose();

                AsyncUtils.RunNextUnityUpdate(() =>
                {
                    foreach (EmoteFrame frame in _emoteFrames)
                    {
                        frame.Dispose();
                    }

                    _emoteFrames = [];

                    GameObject.Destroy(ImageTexture);
                });

                _isDisposed = true;
            }
        }

        public EmoteFrame GetCurrentFrame()
        {
            return GetFrame(Time.unscaledTime % TotalDuration);
        }

        public EmoteFrame GetFrame(float time)
        {
            if (!IsAnimated)
                return _emoteFrames[0];

            int frameIndex = Array.BinarySearch(_emoteFrames, time, EmoteFrame.FrameTimeComparer);
            if (frameIndex >= 0)
            {
                return _emoteFrames[frameIndex];
            }
            else
            {
                return _emoteFrames[~frameIndex - 1];
            }
        }

        public void CallWhenLoaded(Action action)
        {
            if (IsLoaded)
            {
                action();
            }
            else
            {
                OnLoaded += action;
            }
        }
    }
}
