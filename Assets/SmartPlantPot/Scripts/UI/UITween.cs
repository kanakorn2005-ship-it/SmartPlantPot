using System;
using System.Collections;
using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>Tiny coroutine tween helper (no external packages needed).</summary>
    public static class UITween
    {
        public static Coroutine Run(MonoBehaviour host, float duration, Action<float> step, Action done = null, float delay = 0f)
        {
            if (host == null || !host.isActiveAndEnabled)
            {
                step(1f);
                if (done != null) done();
                return null;
            }
            return host.StartCoroutine(Routine(duration, step, done, delay));
        }

        static IEnumerator Routine(float duration, Action<float> step, Action done, float delay)
        {
            if (delay > 0f)
            {
                step(0f);
                float w = 0f;
                while (w < delay)
                {
                    w += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                step(Mathf.Clamp01(t / Mathf.Max(0.0001f, duration)));
                yield return null;
            }
            step(1f);
            if (done != null) done();
        }

        public static float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        public static float EaseInQuad(float t)
        {
            return t * t;
        }

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
