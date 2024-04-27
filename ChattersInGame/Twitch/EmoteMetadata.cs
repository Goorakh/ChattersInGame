using System.IO;
using UnityEngine;

namespace ChattersInGame.Twitch
{
    public class EmoteMetadata
    {
        public EmoteType Type;

        public Vector2Int? FrameCounts;
        public int? EmptyFrames;

        public float[] FrameDelays;

        public EmoteMetadata()
        {
        }

        EmoteMetadata(EmoteType emoteType, Vector2Int? frameCounts, int? emptyFrames, float[] frameDelays)
        {
            Type = emoteType;
            FrameCounts = frameCounts;
            EmptyFrames = emptyFrames;
            FrameDelays = frameDelays;
        }

        public void Deserialize(BinaryReader reader)
        {
            Type = (EmoteType)reader.ReadByte();

            if (Type == EmoteType.Animated)
            {
                int frameCountHorizontal = reader.ReadInt32();
                int frameCountVertical = reader.ReadInt32();

                FrameCounts = new Vector2Int(frameCountHorizontal, frameCountVertical);

                int emptyFrames = reader.ReadInt32();
                EmptyFrames = emptyFrames;

                int usedFrameCount = (frameCountHorizontal * frameCountVertical) - emptyFrames;
                FrameDelays = new float[usedFrameCount];

                for (int i = 0; i < usedFrameCount; i++)
                {
                    FrameDelays[i] = reader.ReadSingle();
                }
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);

            if (Type == EmoteType.Animated)
            {
                writer.Write(FrameCounts.Value.x);
                writer.Write(FrameCounts.Value.y);

                writer.Write(EmptyFrames.Value);

                for (int i = 0; i < FrameDelays.Length; i++)
                {
                    writer.Write(FrameDelays[i]);
                }
            }
        }

        public static EmoteMetadata Static()
        {
            return new EmoteMetadata(EmoteType.Static, null, null, null);
        }

        public static EmoteMetadata Animated(Vector2Int frameCounts, float[] frameDelays, int emptyFrames)
        {
            return new EmoteMetadata(EmoteType.Animated, frameCounts, emptyFrames, frameDelays);
        }
    }
}
