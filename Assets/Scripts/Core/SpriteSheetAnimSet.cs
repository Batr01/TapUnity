using System;
using UnityEngine;

namespace TapBrawl.Core
{
    [Serializable]
    public struct SpriteSheetAnimSet
    {
        [Tooltip("Кадры по порядку (00, 01, 02…). Пусто — анимация отключена.")]
        public Sprite[] frames;

        [Tooltip("Скорость проигрывания, кадров в секунду.")]
        [Min(1f)] public float fps;

        [Tooltip("Множитель размера относительно круга (1 = как круг).")]
        [Range(0.5f, 4f)] public float scale;

        public const int MinTapFxFrames = 2;

        public int ValidFrameCount
        {
            get
            {
                if (frames == null || frames.Length == 0)
                    return 0;
                var count = 0;
                for (var i = 0; i < frames.Length; i++)
                {
                    if (frames[i] != null)
                        count++;
                }

                return count;
            }
        }

        public bool HasFrames => ValidFrameCount > 0;

        public bool HasPlayableTapFx => ValidFrameCount >= MinTapFxFrames;

        public SpriteSheetAnimSet(Sprite[] sprites, float framesPerSecond, float animScale)
        {
            frames = sprites;
            fps = framesPerSecond;
            scale = animScale;
        }

        public SpriteSheetAnimSet WithValidFramesOnly()
        {
            if (frames == null || frames.Length == 0)
                return default;

            var valid = new Sprite[ValidFrameCount];
            var write = 0;
            for (var i = 0; i < frames.Length; i++)
            {
                if (frames[i] == null)
                    continue;
                valid[write++] = frames[i];
            }

            return new SpriteSheetAnimSet(valid, fps, scale);
        }
    }
}
