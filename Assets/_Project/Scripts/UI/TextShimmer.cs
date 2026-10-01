using TMPro;
using UnityEngine;

namespace DragonBattle.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class TextShimmer : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private Color shineColor = new Color(1f, 0.97f, 0.88f, 1f);
        [SerializeField, Range(0f, 1f)] private float strength = 0.75f;
        [SerializeField, Min(0.01f)] private float bandWidth = 0.14f;
        [SerializeField, Min(0f)] private float slant = 0.25f;
        [SerializeField, Min(0.1f)] private float sweepDuration = 1.6f;
        [SerializeField, Min(0f)] private float interval = 4.5f;
        [SerializeField, Min(0f)] private float startDelay = 2.2f;

        private float enabledTime;
        private bool wasShining;

        private void Reset()
        {
            text = GetComponent<TMP_Text>();
        }

        private void Awake()
        {
            if (text == null)
            {
                text = GetComponent<TMP_Text>();
            }
        }

        private void OnEnable()
        {
            enabledTime = Time.unscaledTime;
        }

        private void LateUpdate()
        {
            float elapsed = Time.unscaledTime - enabledTime - startDelay;
            if (elapsed < 0f)
            {
                return;
            }

            float cycle = elapsed % (sweepDuration + interval);
            bool shining = cycle <= sweepDuration;
            if (!shining && !wasShining)
            {
                return;
            }

            wasShining = shining;
            text.ForceMeshUpdate();
            if (!shining)
            {
                return;
            }

            TMP_TextInfo info = text.textInfo;
            Bounds bounds = text.textBounds;
            float width = Mathf.Max(1f, bounds.size.x);
            float height = Mathf.Max(1f, bounds.size.y);
            float phase = Mathf.Lerp(-bandWidth - slant, 1f + bandWidth, cycle / sweepDuration);

            for (int i = 0; i < info.characterCount; i++)
            {
                TMP_CharacterInfo character = info.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                Color32[] colors = info.meshInfo[character.materialReferenceIndex].colors32;
                Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
                for (int v = 0; v < 4; v++)
                {
                    int index = character.vertexIndex + v;
                    Vector3 vertex = vertices[index];
                    float x = (vertex.x - bounds.min.x) / width;
                    float y = (vertex.y - bounds.min.y) / height;
                    float distance = Mathf.Abs(x + y * slant - phase);
                    float amount = (1f - Mathf.SmoothStep(0f, 1f, distance / bandWidth)) * strength;
                    Color32 original = colors[index];
                    Color32 shine = shineColor;
                    shine.a = original.a;
                    colors[index] = Color32.Lerp(original, shine, amount);
                }
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
