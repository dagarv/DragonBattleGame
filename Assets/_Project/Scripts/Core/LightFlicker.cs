using UnityEngine;

namespace DragonBattle.Core
{
    [RequireComponent(typeof(Light))]
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float intensityVariation = 0.25f;
        [SerializeField, Min(0f)] private float rangeVariation = 0.6f;
        [SerializeField, Min(0.01f)] private float speed = 7f;

        private Light target;
        private float baseIntensity;
        private float baseRange;
        private float seed;

        private void Awake()
        {
            target = GetComponent<Light>();
            baseIntensity = target.intensity;
            baseRange = target.range;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            float time = Time.time * speed;
            float noise = Mathf.PerlinNoise(seed, time) * 0.7f + Mathf.PerlinNoise(seed + 5f, time * 2.7f) * 0.3f;
            float offset = noise * 2f - 1f;
            target.intensity = baseIntensity * (1f + offset * intensityVariation);
            target.range = baseRange + offset * rangeVariation;
        }
    }
}
