using UnityEngine;

namespace TapBrawl.Core
{
    [CreateAssetMenu(fileName = "CircleSpawnConfig", menuName = "TapBrawl/Circle Spawn Config")]
    public sealed class CircleSpawnConfig : ScriptableObject
    {
        [Min(0.05f)] public float minSpawnInterval = 0.35f;
        [Min(0.05f)] public float maxSpawnInterval = 0.65f;

        [Min(0.5f)] public float minLifetime = 0.8f;
        [Min(0.5f)] public float maxLifetime = 1.2f;

        [Tooltip("Веса спавна: Normal, Gold, Bomb, Phantom")]
        public float[] weights = { 70f, 10f, 10f, 10f };

        [Tooltip("Отступ от краёв поля, где могут появиться круги")]
        [Range(0.02f, 0.2f)] public float margin = 0.08f;

        [Tooltip("Интервал мигания фантома (видимый / полупрозрачный)")]
        [Min(0.05f)] public float phantomBlinkInterval = 0.15f;

        [Header("Покой — sprite sheet (loop)")]
        [Tooltip("Синий круг. Кадры blue_ball_00… из blue_ball_sheet.png")]
        public SpriteSheetAnimSet normalIdleAnim;

        [Tooltip("Золотой круг. Кадры gold_ball_00…")]
        public SpriteSheetAnimSet goldIdleAnim;

        [Tooltip("Бомба. Кадры bomb_ball_00…")]
        public SpriteSheetAnimSet bombIdleAnim;

        [Tooltip("Фантом. Кадры phantom_ball_00…")]
        public SpriteSheetAnimSet phantomIdleAnim;

        [Header("Покой — запасной вариант (без sprite sheet)")]
        [Tooltip("Лёгкое «дыхание» масштабом, если кадры покоя не заданы")]
        public bool enableIdlePulse = true;

        [Range(0f, 0.08f)] public float idlePulseAmplitude = 0.025f;
        [Range(0.1f, 4f)] public float idlePulseFrequency = 1.35f;

        [Header("Иконки целей (статичный спрайт)")]
        [Tooltip("Обычный (синий) — point.png. Используется, если нет normalIdleAnim")]
        public Sprite? normalTargetSprite;

        [Tooltip("Золотой — goold.png")]
        public Sprite? goldTargetSprite;

        [Tooltip("Бомба — boom.png")]
        public Sprite? bombTargetSprite;

        [Tooltip("Фантом — purplePoint.png")]
        public Sprite? phantomTargetSprite;

        [Header("Нажатие — спрайты")]
        [Tooltip("Если пусто — остаётся idle-кадр + сжатие")]
        public Sprite? normalPressedSprite;

        public Sprite? goldPressedSprite;
        public Sprite? bombPressedSprite;
        public Sprite? phantomPressedSprite;

        [Range(0.65f, 1f)] public float pressedScaleMultiplier = 0.86f;

        [Header("Тап — VFX sprite sheet (один проход)")]
        [Tooltip("Успешный тап по синему кругу (+1). hit_explosion_00…")]
        public SpriteSheetAnimSet normalHitAnim;

        [Tooltip("Успешный тап по золотому (+3). gold_hit_00…")]
        public SpriteSheetAnimSet goldHitAnim;

        [Tooltip("Успешный тап по фантому в видимой фазе (+1)")]
        public SpriteSheetAnimSet phantomHitAnim;

        [Tooltip("Идеальный тап (последняя треть жизни цели). Приоритетнее обычного hit")]
        public SpriteSheetAnimSet perfectHitAnim;

        [Tooltip("Удар цепной молнии (авто-тап в разряде)")]
        public SpriteSheetAnimSet chainHitAnim;

        [Tooltip("Тап по бомбе (−1 очко)")]
        public SpriteSheetAnimSet bombTapAnim;

        [Tooltip("Тап по фантому в невидимой фазе (0 очков)")]
        public SpriteSheetAnimSet phantomMissAnim;

        [Header("Тап — запасной juice (без sprite sheet)")]
        [Tooltip("Процедурный pop + частицы, если для события нет кадров выше")]
        [Min(0.08f)] public float tapPopLifetimeSec = 0.34f;

        [Range(1.1f, 3f)] public float tapPopScale = 1.75f;
        [Range(2f, 8f)] public float bombExplosionScale = 4.8f;

        public SpriteSheetAnimSet GetIdleAnim(CircleKind kind) =>
            kind switch
            {
                CircleKind.Gold => goldIdleAnim,
                CircleKind.Bomb => bombIdleAnim,
                CircleKind.Phantom => phantomIdleAnim,
                _ => normalIdleAnim,
            };

        public bool HasIdleAnim(CircleKind kind) => GetIdleAnim(kind).HasFrames;

        public SpriteSheetAnimSet ResolveTapAnim(CircleKind kind, int scoreDelta, bool isPerfectTap, bool chainLightningStrike)
        {
            if (scoreDelta > 0)
            {
                if (chainLightningStrike && TryPickTapAnim(chainHitAnim, out var chainAnim))
                    return chainAnim;
                if (isPerfectTap && TryPickTapAnim(perfectHitAnim, out var perfectAnim))
                    return perfectAnim;
                if (kind == CircleKind.Gold && TryPickTapAnim(goldHitAnim, out var goldAnim))
                    return goldAnim;
                if (kind == CircleKind.Phantom && TryPickTapAnim(phantomHitAnim, out var phantomAnim))
                    return phantomAnim;
                if (TryPickTapAnim(normalHitAnim, out var normalAnim))
                    return normalAnim;
                return default;
            }

            if (scoreDelta < 0 && kind == CircleKind.Bomb && TryPickTapAnim(bombTapAnim, out var bombAnim))
                return bombAnim;

            if (scoreDelta == 0 && kind == CircleKind.Phantom && TryPickTapAnim(phantomMissAnim, out var missAnim))
                return missAnim;

            return default;
        }

        private static bool TryPickTapAnim(SpriteSheetAnimSet candidate, out SpriteSheetAnimSet result)
        {
            if (!candidate.HasPlayableTapFx)
            {
                result = default;
                return false;
            }

            result = candidate.WithValidFramesOnly();
            return result.HasPlayableTapFx;
        }
    }
}
