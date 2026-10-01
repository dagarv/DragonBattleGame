using System;
using DragonBattle.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DragonBattle.UI
{
    public class MenuItem : Selectable, IPointerClickHandler, ISubmitHandler, ICancelHandler
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Image glow;
        [SerializeField] private RectTransform marker;
        [SerializeField] private RectTransform underline;
        [SerializeField] private RectTransform valueArea;
        [SerializeField] private CanvasGroup group;

        [Header("Look")]
        [SerializeField] private Color normalColor = new Color(0.93f, 0.88f, 0.8f, 0.55f);
        [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.75f, 1f);
        [SerializeField, Min(0f)] private float highlightShift = 18f;
        [SerializeField, Min(0f)] private float revealShift = 60f;
        [SerializeField, Min(0.01f)] private float highlightSpeed = 7f;
        [SerializeField, Min(0f)] private float pressPunch = 0.06f;

        private bool isHighlighted;
        private float highlight;
        private float reveal = 1f;
        private float pressTime = float.NegativeInfinity;
        private float glowAlpha;

        public event Action Submitted;
        public event Action<int> Adjusted;
        public event Action Cancelled;
        public event Action Highlighted;

        public bool IsAdjustable => Adjusted != null;

        public void Setup(RectTransform contentRoot, TMP_Text labelText, Image glowImage, RectTransform markerRect,
            RectTransform underlineRect, RectTransform valueRect, CanvasGroup canvasGroup, Color normal, Color highlighted)
        {
            content = contentRoot;
            label = labelText;
            glow = glowImage;
            marker = markerRect;
            underline = underlineRect;
            valueArea = valueRect;
            group = canvasGroup;
            normalColor = normal;
            highlightColor = highlighted;
            glowAlpha = glow != null ? glow.color.a : 0f;
            transition = Transition.None;
            Apply();
        }

        public void SetReveal(float value)
        {
            reveal = Mathf.Clamp01(value);
            Apply();
        }

        public void SetLabel(string text)
        {
            label.text = text;
        }

        public void Press()
        {
            pressTime = Time.unscaledTime;
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            isHighlighted = true;
            Highlighted?.Invoke();
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            isHighlighted = false;
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            EventSystem system = EventSystem.current;
            if (IsInteractable() && system != null && system.currentSelectedGameObject != gameObject)
            {
                Select();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !IsInteractable())
            {
                return;
            }

            if (IsAdjustable && valueArea != null)
            {
                Vector2 local;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(valueArea, eventData.position,
                    eventData.pressEventCamera, out local);
                Adjusted?.Invoke(local.x < valueArea.rect.center.x ? -1 : 1);
                Press();
                return;
            }

            Activate();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!IsInteractable())
            {
                return;
            }

            if (IsAdjustable)
            {
                Adjusted?.Invoke(1);
                Press();
                return;
            }

            Activate();
        }

        public void OnCancel(BaseEventData eventData)
        {
            Cancelled?.Invoke();
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (IsAdjustable && IsInteractable() &&
                (eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right))
            {
                Adjusted?.Invoke(eventData.moveDir == MoveDirection.Right ? 1 : -1);
                Press();
                return;
            }
            base.OnMove(eventData);
        }

        private void Activate()
        {
            Press();
            Submitted?.Invoke();
        }

        private void Update()
        {
            if (content == null)
            {
                return;
            }

            float target = isHighlighted ? 1f : 0f;
            highlight = Mathf.MoveTowards(highlight, target, highlightSpeed * Time.unscaledDeltaTime);
            Apply();
        }

        private void Apply()
        {
            if (content == null)
            {
                return;
            }

            float h = Ease.OutCubic(highlight);
            float press = Mathf.Clamp01((Time.unscaledTime - pressTime) / 0.35f);
            float punch = press < 1f ? Ease.Bell(press) * pressPunch : 0f;

            if (group != null)
            {
                group.alpha = reveal;
            }
            content.anchoredPosition = new Vector2(-revealShift * (1f - Ease.OutCubic(reveal)), 0f);
            content.localScale = Vector3.one * (1f + punch);

            label.color = Color.Lerp(normalColor, highlightColor, h);
            label.rectTransform.anchoredPosition = new Vector2(highlightShift * h, label.rectTransform.anchoredPosition.y);

            if (glow != null)
            {
                Color color = glow.color;
                color.a = glowAlpha * h;
                glow.color = color;
                glow.rectTransform.localScale = new Vector3(Mathf.Lerp(0.4f, 1f, h), 1f, 1f);
            }
            if (marker != null)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.12f * h;
                marker.localScale = Vector3.one * (Ease.OutBack(h) * pulse);
            }
            if (underline != null)
            {
                underline.localScale = new Vector3(h, 1f, 1f);
            }
        }
    }
}
