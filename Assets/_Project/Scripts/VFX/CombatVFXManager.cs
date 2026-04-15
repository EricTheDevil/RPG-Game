using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using TMPro;

namespace RPG.VFX
{
    /// <summary>
    /// Central VFX orchestrator for TFT-style autobattle.
    ///
    /// Responsibilities:
    ///   • Particle burst pool (attack / magic / special / heal / defend / death)
    ///   • Floating damage / heal numbers (world-space billboard)
    ///   • Full-screen flash on hit (crit = intense red, normal = soft white)
    ///   • Hit-ring flash on the target unit (world-space decal)
    ///   • HDRP post-process crit pulse: Bloom + Chromatic Aberration + Vignette
    ///   • Static singleton so any system can call without a reference
    /// </summary>
    public class CombatVFXManager : MonoBehaviour
    {
        public static CombatVFXManager Instance { get; private set; }

        // ── Floating Text ─────────────────────────────────────────────────────
        [Header("Floating Text")]
        public FloatingText FloatingTextPrefab;
        public Canvas       WorldCanvas;

        // ── Particle Prefabs ──────────────────────────────────────────────────
        [Header("Particle VFX Prefabs")]
        public ParticleSystem AttackVFXPrefab;
        public ParticleSystem MagicVFXPrefab;
        public ParticleSystem SpecialVFXPrefab;
        public ParticleSystem HealVFXPrefab;
        public ParticleSystem DefendVFXPrefab;
        public ParticleSystem DeathVFXPrefab;
        public ParticleSystem CritBurstPrefab;
        public ParticleSystem SpawnVFXPrefab;

        // ── Screen FX ─────────────────────────────────────────────────────────
        [Header("Screen Flash")]
        public UnityEngine.UI.Image FlashPanel;
        [Range(0f, 1f)] public float FlashMaxAlpha = 0.45f;

        // ── Hit Ring ──────────────────────────────────────────────────────────
        [Header("Hit Ring")]
        public ParticleSystem HitRingPrefab;

        // ── HDRP Post-Process ─────────────────────────────────────────────────
        [Header("HDRP Post-Process Volume (assign the in-scene Volume)")]
        [Tooltip("Should have Bloom, ChromaticAberration, Vignette, and LensDistortion overrides.")]
        public Volume PostProcessVolume;

        [Header("Crit Post-Process Settings")]
        public float CritBloomPeak            = 3.5f;
        public float CritChromAberrationPeak  = 1.0f;
        public float CritVignettePeak         = 0.55f;
        public float CritLensDistortionPeak   = -0.25f;
        public float CritPulseDuration        = 0.35f;

        // ── Cached PP overrides ───────────────────────────────────────────────
        private Bloom              _bloom;
        private ChromaticAberration _chrom;
        private Vignette           _vignette;
        private LensDistortion     _lensDistortion;

        private float _bloomBase;
        private float _chromBase;
        private float _vignetteBase;
        private float _lensDistBase;

        private Coroutine _critPulseRoutine;

        // ── Particle Pool ─────────────────────────────────────────────────────
        private readonly Dictionary<ParticleSystem, Queue<ParticleSystem>> _pool = new();
        private const int PoolSize = 4;

        // ── Lifecycle ─────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            CachePostProcess();
        }

        private void CachePostProcess()
        {
            if (PostProcessVolume == null) return;
            var profile = PostProcessVolume.profile;

            if (profile.TryGet(out _bloom))
                _bloomBase = _bloom.intensity.value;

            if (profile.TryGet(out _chrom))
                _chromBase = _chrom.intensity.value;

            if (profile.TryGet(out _vignette))
                _vignetteBase = _vignette.intensity.value;

            if (profile.TryGet(out _lensDistortion))
                _lensDistBase = _lensDistortion.intensity.value;
        }

        // ── HDRP Crit Pulse ───────────────────────────────────────────────────

        /// <summary>
        /// Triggers a dramatic HDRP post-process punch on crit:
        /// Bloom surge + Chromatic Aberration + Vignette + Lens Distortion.
        /// Safe to call even when no Volume is assigned.
        /// </summary>
        public void PulseCritPostProcess()
        {
            if (PostProcessVolume == null) return;
            if (_critPulseRoutine != null) StopCoroutine(_critPulseRoutine);
            _critPulseRoutine = StartCoroutine(CritPulseRoutine());
        }

        private IEnumerator CritPulseRoutine()
        {
            float half = CritPulseDuration * 0.4f;
            float tail = CritPulseDuration * 0.6f;

            // Attack: ramp up
            float t = 0f;
            while (t < half)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.SmoothStep(0f, 1f, t / half);
                ApplyPostProcess(p);
                yield return null;
            }

            // Release: ease back to base
            t = 0f;
            while (t < tail)
            {
                t += Time.unscaledDeltaTime;
                float p = 1f - Mathf.SmoothStep(0f, 1f, t / tail);
                ApplyPostProcess(p);
                yield return null;
            }

            ApplyPostProcess(0f);
            _critPulseRoutine = null;
        }

        private void ApplyPostProcess(float strength)
        {
            if (_bloom != null)
                _bloom.intensity.Override(Mathf.Lerp(_bloomBase, CritBloomPeak, strength));

            if (_chrom != null)
                _chrom.intensity.Override(Mathf.Lerp(_chromBase, CritChromAberrationPeak, strength));

            if (_vignette != null)
                _vignette.intensity.Override(Mathf.Lerp(_vignetteBase, CritVignettePeak, strength));

            if (_lensDistortion != null)
                _lensDistortion.intensity.Override(Mathf.Lerp(_lensDistBase, CritLensDistortionPeak, strength));
        }

        // ── Particle VFX ─────────────────────────────────────────────────────

        public void PlayEffect(string key, Vector3 worldPos, Color tint = default)
        {
            if (tint == default) tint = Color.white;
            var prefab = ResolvePrefab(key);
            if (prefab == null) return;

            var vfx = GetFromPool(prefab);
            vfx.transform.position = worldPos;
            TintParticles(vfx, tint);
            vfx.gameObject.SetActive(true);
            vfx.Play(withChildren: true);

            float lifetime = vfx.main.duration + vfx.main.startLifetime.constantMax + 0.5f;
            StartCoroutine(ReturnToPool(vfx, prefab, lifetime));
        }

        public void PlayDeathEffect(Vector3 worldPos, Color unitColor)
        {
            if (DeathVFXPrefab == null) return;
            var vfx = Instantiate(DeathVFXPrefab, worldPos, Quaternion.identity);
            TintParticles(vfx, unitColor);
            vfx.Play(withChildren: true);
            Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax + 1f);
        }

        public void PlaySpawnEffect(Vector3 worldPos)
        {
            if (SpawnVFXPrefab == null) return;
            var vfx = Instantiate(SpawnVFXPrefab, worldPos, Quaternion.identity);
            vfx.Play(withChildren: true);
            Destroy(vfx.gameObject, 2f);
        }

        // ── Floating Numbers ─────────────────────────────────────────────────

        public void ShowDamageNumber(int amount, Vector3 worldPos, bool isCrit, bool isHeal)
        {
            if (FloatingTextPrefab == null || WorldCanvas == null) return;

            var ft = Instantiate(FloatingTextPrefab, WorldCanvas.transform);
            ft.Setup(amount, worldPos, isCrit, isHeal);

            if (isCrit && CritBurstPrefab != null)
            {
                var burst = GetFromPool(CritBurstPrefab);
                burst.transform.position = worldPos;
                burst.gameObject.SetActive(true);
                burst.Play(withChildren: true);
                float lt = burst.main.duration + burst.main.startLifetime.constantMax + 0.3f;
                StartCoroutine(ReturnToPool(burst, CritBurstPrefab, lt));
            }

            if (HitRingPrefab != null)
            {
                var ring = GetFromPool(HitRingPrefab);
                ring.transform.position = worldPos - Vector3.up * 0.5f;
                ring.gameObject.SetActive(true);
                ring.Play(withChildren: true);
                float lt = ring.main.duration + ring.main.startLifetime.constantMax + 0.2f;
                StartCoroutine(ReturnToPool(ring, HitRingPrefab, lt));
            }
        }

        // ── Screen Flash ─────────────────────────────────────────────────────

        public void FlashScreen(Color color, float duration = 0.18f)
        {
            if (FlashPanel != null)
                StartCoroutine(DoFlash(color, duration));
        }

        private IEnumerator DoFlash(Color color, float duration)
        {
            Color c = color;
            c.a = Mathf.Min(color.a, FlashMaxAlpha);
            FlashPanel.gameObject.SetActive(true);
            FlashPanel.color = c;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Lerp(FlashMaxAlpha, 0f, elapsed / duration);
                FlashPanel.color = c;
                yield return null;
            }
            FlashPanel.color = Color.clear;
            FlashPanel.gameObject.SetActive(false);
        }

        // ── Pool Helpers ──────────────────────────────────────────────────────

        private ParticleSystem GetFromPool(ParticleSystem prefab)
        {
            if (!_pool.TryGetValue(prefab, out var queue))
            {
                queue = new Queue<ParticleSystem>();
                _pool[prefab] = queue;
            }

            while (queue.Count > 0)
            {
                var existing = queue.Dequeue();
                if (existing != null)
                {
                    existing.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    return existing;
                }
            }

            return Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
        }

        private IEnumerator ReturnToPool(ParticleSystem vfx, ParticleSystem prefab, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (vfx != null)
            {
                vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                vfx.gameObject.SetActive(false);
                if (_pool.TryGetValue(prefab, out var q) && q.Count < PoolSize)
                    q.Enqueue(vfx);
                else
                    Destroy(vfx.gameObject);
            }
        }

        // ── Tint Helpers ──────────────────────────────────────────────────────

        private static void TintParticles(ParticleSystem ps, Color color)
        {
            SetParticleColor(ps, color);
            foreach (var child in ps.GetComponentsInChildren<ParticleSystem>())
                if (child != ps) SetParticleColor(child, color * 0.85f);
        }

        private static void SetParticleColor(ParticleSystem ps, Color color)
        {
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color);
        }

        private ParticleSystem ResolvePrefab(string key) => key?.ToLower() switch
        {
            "attack"  => AttackVFXPrefab,
            "magic"   => MagicVFXPrefab,
            "special" => SpecialVFXPrefab,
            "heal"    => HealVFXPrefab,
            "defend"  => DefendVFXPrefab,
            "death"   => DeathVFXPrefab,
            _         => AttackVFXPrefab,
        };
    }
}
