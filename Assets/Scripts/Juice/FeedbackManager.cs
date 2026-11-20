using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace NightCast.Juice
{
    public class FeedbackManager : MonoBehaviour
    {
        private RectTransform _tensionBarRect;
        private Image _tensionFillImage;
        private Vector2 _tensionBarOriginalPos;

        private RectTransform _starliteRect;
        private Vector2 _starliteBasePos;

        private RectTransform _fishingScreen;
        private Font _defaultFont;

        private class FloatingText
        {
            public RectTransform rect;
            public Text text;
            public float lifeTime;
            public float maxLife;
        }
        private List<FloatingText> _floatingTexts = new List<FloatingText>();

        public void Init(RectTransform tensionBar, Image tensionFill, RectTransform fishingScreen, Font font, RectTransform rodTip = null)
        {
            _tensionBarRect = tensionBar;
            _tensionFillImage = tensionFill;
            _tensionBarOriginalPos = tensionBar.anchoredPosition;
            _fishingScreen = fishingScreen;
            _defaultFont = font;

            GameObject slObj = new GameObject("Starlite");
            _starliteRect = slObj.AddComponent<RectTransform>();

            if (rodTip != null)
            {
                _starliteRect.SetParent(rodTip, false);
                _starliteBasePos = Vector2.zero;
            }
            else
            {
                _starliteRect.SetParent(_fishingScreen, false);
                _starliteBasePos = new Vector2(0, -100);
            }

            _starliteRect.anchoredPosition = _starliteBasePos;
            _starliteRect.sizeDelta = new Vector2(16, 16);

            Image slImg = slObj.AddComponent<Image>();
            slImg.color = new Color(0.2f, 1f, 0.35f, 1f);
            slImg.raycastTarget = false;
            _starliteRect.gameObject.SetActive(false);
        }

        public void SetStarliteActive(bool active)
        {
            if (_starliteRect != null)
            {
                _starliteRect.gameObject.SetActive(active);
                if (!active) _starliteRect.anchoredPosition = _starliteBasePos;
            }
        }

        public void UpdateTensionFeedback(float tensionPct)
        {
            if (_tensionBarRect == null || _tensionFillImage == null) return;

            if (tensionPct >= 0.8f)
            {
                float intensity = Mathf.Clamp((tensionPct - 0.8f) * 35f, 2f, 18f);
                _tensionBarRect.anchoredPosition = _tensionBarOriginalPos + Random.insideUnitCircle * intensity;
                _tensionFillImage.color = Color.Lerp(new Color(1f, 0.45f, 0f), Color.red, Mathf.PingPong(Time.time * 16f, 1f));
            }
            else
            {
                _tensionBarRect.anchoredPosition = _tensionBarOriginalPos;
                _tensionFillImage.color = tensionPct < 0.75f ? new Color(0.2f, 0.85f, 0.35f) : new Color(1f, 0.55f, 0f);
            }
        }

        public void ResetTensionFeedback()
        {
            if (_tensionBarRect != null) _tensionBarRect.anchoredPosition = _tensionBarOriginalPos;
            if (_tensionFillImage != null) _tensionFillImage.color = new Color(0.2f, 0.85f, 0.35f);
        }

        public void UpdateStarlite(bool isBiting)
        {
            if (_starliteRect == null || !_starliteRect.gameObject.activeInHierarchy) return;

            if (isBiting)
            {
                _starliteRect.anchoredPosition = _starliteBasePos + new Vector2(Random.Range(-8f, 8f), Random.Range(-18f, 6f));
            }
            else
            {
                _starliteRect.anchoredPosition = _starliteBasePos + new Vector2(0f, Mathf.Sin(Time.time * 2.5f) * 4f);
            }
        }

        public void SpawnFloatingText(string message, Color color, Vector2 startPos)
        {
            GameObject obj = new GameObject("FloatingText");
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.SetParent(_fishingScreen, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = startPos;
            rect.sizeDelta = new Vector2(900, 100);

            Text txt = obj.AddComponent<Text>();
            txt.text = message;
            txt.font = _defaultFont;
            txt.fontSize = 48;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(3, -3);

            _floatingTexts.Add(new FloatingText { rect = rect, text = txt, lifeTime = 0f, maxLife = 2.4f });
        }

        private void Update()
        {
            for (int i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                var ft = _floatingTexts[i];
                ft.lifeTime += Time.deltaTime;
                if (ft.lifeTime > ft.maxLife)
                {
                    if (ft.rect != null) Destroy(ft.rect.gameObject);
                    _floatingTexts.RemoveAt(i);
                }
                else if (ft.rect != null && ft.text != null)
                {
                    ft.rect.anchoredPosition += Vector2.up * 55f * Time.deltaTime;
                    float alpha = 1f - (ft.lifeTime / ft.maxLife);
                    ft.text.color = new Color(ft.text.color.r, ft.text.color.g, ft.text.color.b, alpha);
                }
            }
        }
    }
}
