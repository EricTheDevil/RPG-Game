using System.Collections;
using UnityEngine;
using TMPro;

namespace RPG.VFX
{
    /// <summary>
    /// World-space floating damage / heal number that rises and fades.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        [Header("References")]
        public TextMeshProUGUI Label;
        public CanvasGroup Group;

        [Header("Animation")]
        public float RiseSpeed   = 1.2f;
        public float Duration    = 1.4f;
        public float FadeDelay   = 0.7f;

        private Camera _cam;
        private Vector3 _worldOrigin;
        private RectTransform _rect;

        private void Awake()
        {
            _cam  = Camera.main;
            _rect = GetComponent<RectTransform>();
        }

        public void Setup(int value, Vector3 worldPos, bool isCrit, bool isHeal)
        {
            _worldOrigin = worldPos + Vector3.up * 0.5f;

            if (isHeal)
            {
                Label.text      = $"+{value}";
                Label.color     = new Color(0.25f, 1f, 0.35f);
                Label.fontSize  = 34f;
            }
            else if (isCrit)
            {
                Label.text      = $"CRIT {value}!";
                Label.color     = new Color(1f, 0.55f, 0f);
                Label.fontSize  = 52f;
                transform.localScale = Vector3.one * 1.2f;
            }
            else
            {
                Label.text      = value.ToString();
                Label.color     = Color.white;
                Label.fontSize  = 40f;
            }

            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            float elapsed  = 0f;
            float xDrift   = Random.Range(-22f, 22f);

            while (elapsed < Duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / Duration;

                // Bill-board position tracking
                Vector3 screenPos = _cam.WorldToScreenPoint(
                    _worldOrigin + Vector3.up * (elapsed * RiseSpeed));
                _rect.position = screenPos + new Vector3(xDrift * t, 0f, 0f);

                // Fade
                if (elapsed >= FadeDelay && Group != null)
                {
                    float fadeT = (elapsed - FadeDelay) / (Duration - FadeDelay);
                    Group.alpha = Mathf.Lerp(1f, 0f, fadeT);
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
