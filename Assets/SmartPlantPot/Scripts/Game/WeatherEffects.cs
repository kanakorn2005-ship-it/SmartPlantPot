using UnityEngine;

namespace SmartPlantPot
{
    /// <summary>Turns the rain particle systems on/off and scales them with the current weather.</summary>
    [AddComponentMenu("Smart Plant Pot/Weather Effects")]
    public class WeatherEffects : MonoBehaviour
    {
        public ParticleSystem[] rain;
        public float drizzleRate = 250f;
        public float rainRate = 900f;
        public float stormRate = 1800f;

        WeatherKind shown = (WeatherKind)(-1);

        void Update()
        {
            if (EnvironmentState.Weather == shown) return;
            shown = EnvironmentState.Weather;
            float rate = shown == WeatherKind.Drizzle ? drizzleRate
                : shown == WeatherKind.Rain ? rainRate
                : shown == WeatherKind.Storm ? stormRate : 0f;
            if (rain == null) return;
            foreach (var ps in rain)
            {
                if (ps == null) continue;
                var em = ps.emission;
                em.rateOverTime = rate;
                if (rate > 0f && !ps.isPlaying) ps.Play();
                if (rate <= 0f && ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
