using UnityEngine;

namespace DragonBattle.Core
{
    public static class Ease
    {
        public static float OutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            float inverse = 1f - t;
            return 1f - inverse * inverse * inverse;
        }

        public static float InCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        public static float InOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.5f)
            {
                return 4f * t * t * t;
            }
            float inverse = -2f * t + 2f;
            return 1f - inverse * inverse * inverse * 0.5f;
        }

        public static float OutBack(float t, float overshoot = 1.4f)
        {
            t = Mathf.Clamp01(t) - 1f;
            return 1f + t * t * ((overshoot + 1f) * t + overshoot);
        }

        public static float Smooth(float from, float to, float t)
        {
            float x = Mathf.Clamp01((t - from) / Mathf.Max(0.0001f, to - from));
            return x * x * x * (x * (x * 6f - 15f) + 10f);
        }

        public static float Bell(float t)
        {
            return Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
        }
    }
}
