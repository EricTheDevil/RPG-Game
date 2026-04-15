using System.Collections;
using UnityEngine;

namespace RPG.VFX
{
    /// <summary>
    /// Add to the MainCamera. Call Shake() from anywhere via CameraShake.Instance.
    /// Unit.OnDamaged hooks into this automatically from CombatManager.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [Header("Defaults")]
        public float NormalDuration  = 0.18f;
        public float NormalMagnitude = 0.06f;
        public float CritDuration    = 0.28f;
        public float CritMagnitude   = 0.14f;

        private Vector3 _originPos;
        private Coroutine _shakeRoutine;

        private void Awake()
        {
            Instance = this;
            _originPos = transform.localPosition;
        }

        /// <summary>Trigger a screen shake. isCrit uses bigger values.</summary>
        public void Shake(bool isCrit = false)
        {
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            float dur = isCrit ? CritDuration  : NormalDuration;
            float mag = isCrit ? CritMagnitude : NormalMagnitude;
            _shakeRoutine = StartCoroutine(DoShake(dur, mag));
        }

        private IEnumerator DoShake(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;   // unscaled — works during hit-pause
                float t = elapsed / duration;
                float envelope = 1f - t;             // fade out
                float ox = Random.Range(-1f, 1f) * magnitude * envelope;
                float oy = Random.Range(-1f, 1f) * magnitude * envelope;
                transform.localPosition = _originPos + new Vector3(ox, oy, 0f);
                yield return null;
            }
            transform.localPosition = _originPos;
        }
    }
}
