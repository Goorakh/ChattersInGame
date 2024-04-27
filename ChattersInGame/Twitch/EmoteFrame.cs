using System;
using System.Collections;
using UnityEngine;

namespace ChattersInGame.Twitch
{
    public class EmoteFrame : IDisposable
    {
        public static IComparer FrameTimeComparer { get; } = new FrameTimeComparerImpl();

        public EmoteImage Image { get; }

        public Vector2 SpritePivot { get; }

        public Rect SpriteSheetRect { get; }

        Sprite _sprite;
        public Sprite Sprite
        {
            get
            {
                if (!_sprite && !_isDisposed)
                {
                    _sprite = Sprite.Create(Image.ImageTexture, SpriteSheetRect, SpritePivot);
                }

                return _sprite;
            }
        }

        public float StartTime { get; }

        bool _isDisposed;

        public EmoteFrame(EmoteImage image, Vector2 spritePivot, Rect spriteSheetRect, float startTime)
        {
            Image = image;
            SpritePivot = spritePivot;
            SpriteSheetRect = spriteSheetRect;
            StartTime = startTime;
        }

        ~EmoteFrame()
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
                if (Sprite)
                {
                    GameObject.Destroy(Sprite);
                }

                _isDisposed = true;
            }
        }

        sealed class FrameTimeComparerImpl : IComparer
        {
            public int Compare(object x, object y)
            {
                if (x is EmoteFrame emoteFrame && y is float frameStartTime)
                {
                    return emoteFrame.StartTime.CompareTo(frameStartTime);
                }
                else
                {
                    Log.Warning("Unsupported types");
                    return Comparer.Default.Compare(x, y);
                }
            }
        }
    }
}
