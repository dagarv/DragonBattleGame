using UnityEngine;

namespace DragonBattle.Core
{
    public static class EffectSpawner
    {
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
        {
            if (prefab == null)
            {
                return null;
            }

            GameObject instance = Object.Instantiate(prefab, position, rotation);
            Object.Destroy(instance, lifetime);
            return instance;
        }

        public static void Release(GameObject instance, float linger)
        {
            if (instance == null)
            {
                return;
            }

            foreach (ParticleSystem system in instance.GetComponentsInChildren<ParticleSystem>())
            {
                system.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
            Object.Destroy(instance, linger);
        }
    }
}
