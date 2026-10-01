using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DragonBattle.Core
{
    public class EffectWarmup : MonoBehaviour
    {
        private const float StageDepth = 10000f;
        private const float StageDistance = 8f;
        private const int TargetSize = 64;
        private const int MinFrames = 3;
        private const float MinDuration = 0.5f;

        private static readonly HashSet<GameObject> Warmed = new HashSet<GameObject>();
        private static EffectWarmup instance;

        private readonly List<GameObject> pending = new List<GameObject>();

        public static void Request(GameObject prefab)
        {
            if (prefab == null || Warmed.Contains(prefab))
            {
                return;
            }
            if (instance == null)
            {
                instance = new GameObject("Effect Warmup").AddComponent<EffectWarmup>();
            }
            if (!instance.pending.Contains(prefab))
            {
                instance.pending.Add(prefab);
            }
        }

        private IEnumerator Start()
        {
            yield return null;

            Camera source = Camera.main;
            if (source != null)
            {
                while (pending.Count > 0)
                {
                    List<GameObject> batch = new List<GameObject>(pending);
                    pending.Clear();
                    yield return Render(source, batch);
                    Warmed.UnionWith(batch);
                }
            }

            instance = null;
            Destroy(gameObject);
        }

        private IEnumerator Render(Camera source, List<GameObject> prefabs)
        {
            RenderTexture target = new RenderTexture(TargetSize, TargetSize, 24,
                source.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default)
            {
                antiAliasing = source.allowMSAA ? Mathf.Max(1, QualitySettings.antiAliasing) : 1
            };

            Camera stageCamera = new GameObject("Warmup Camera").AddComponent<Camera>();
            stageCamera.CopyFrom(source);
            stageCamera.transform.SetParent(transform, true);
            stageCamera.transform.position = source.transform.position + Vector3.down * StageDepth;
            stageCamera.depth = source.depth - 1f;
            stageCamera.targetTexture = target;

            Vector3 stagePoint = stageCamera.transform.position + stageCamera.transform.forward * StageDistance;
            List<GameObject> instances = new List<GameObject>(prefabs.Count);
            foreach (GameObject prefab in prefabs)
            {
                GameObject effect = Instantiate(prefab, stagePoint, Quaternion.identity, transform);
                foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>())
                {
                    system.Emit(1);
                }
                instances.Add(effect);
            }

            float endTime = Time.unscaledTime + MinDuration;
            for (int frame = 0; frame < MinFrames || Time.unscaledTime < endTime; frame++)
            {
                yield return null;
            }

            foreach (GameObject effect in instances)
            {
                if (effect != null)
                {
                    Destroy(effect);
                }
            }
            stageCamera.targetTexture = null;
            Destroy(stageCamera.gameObject);
            target.Release();
            Destroy(target);
        }
    }
}
