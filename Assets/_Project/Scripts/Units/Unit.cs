using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RPG.Data;

namespace RPG.Units
{
    public enum Team      { Player, Enemy }
    public enum UnitState { Idle, Moving, Acting, Dead }

    // ─────────────────────────────────────────────────────────────────────────
    //  Runtime Stats  (mutable layer over UnitStatsSO — buffs/synergies go here)
    // ─────────────────────────────────────────────────────────────────────────
    public class RuntimeStats
    {
        public int   MaxHP, MaxMP, Attack, Defense, MagicAttack, MagicDefense, Speed, Movement;
        public float CritChance;   // 0–1, e.g. 0.10 = 10%
        public float CritMultiplier;  // e.g. 1.6 = 60% bonus damage

        /// <summary>Parameterless constructor — for explicit field initialisation.</summary>
        public RuntimeStats() { }

        public RuntimeStats(UnitStatsSO src)
        {
            MaxHP          = src.MaxHP;
            MaxMP          = src.MaxMP;
            Attack         = src.Attack;
            Defense        = src.Defense;
            MagicAttack    = src.MagicAttack;
            MagicDefense   = src.MagicDefense;
            Speed          = src.Speed;
            Movement       = src.Movement;
            CritChance     = src.BaseCritChance;
            CritMultiplier = src.BaseCritMultiplier;
        }

        /// <summary>Deep copy constructor — use when persisting to GameSession.</summary>
        public RuntimeStats(RuntimeStats src)
        {
            MaxHP          = src.MaxHP;
            MaxMP          = src.MaxMP;
            Attack         = src.Attack;
            Defense        = src.Defense;
            MagicAttack    = src.MagicAttack;
            MagicDefense   = src.MagicDefense;
            Speed          = src.Speed;
            Movement       = src.Movement;
            CritChance     = src.CritChance;
            CritMultiplier = src.CritMultiplier;
        }

        public void ApplyBuff(BuffSO buff)
        {
            MaxHP          += buff.BonusMaxHP;
            MaxMP          += buff.BonusMaxMP;
            Attack         += buff.BonusAttack;
            Defense        += buff.BonusDefense;
            MagicAttack    += buff.BonusMagicAttack;
            MagicDefense   += buff.BonusMagicDefense;
            Speed          += buff.BonusSpeed;
            Movement       += buff.BonusMovement;
            CritChance     += buff.BonusCritChance;
            CritMultiplier += buff.BonusCritMultiplier;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Unit  (base class for HeroUnit, EnemyUnit, and all future unit types)
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Base unit for TFT-style autobattle.
    ///
    /// Key design changes from turn-based version:
    ///   • UnitAI component drives ALL decision-making (no separate EnemyAI).
    ///   • RuntimeStats is publicly mutable so TraitSystem can inject bonuses.
    ///   • No HasMoved / HasActed logic blocking input — autobattle owns the loop.
    ///   • Idle VFX glow + idle breathing animation hook.
    /// </summary>
    public abstract class Unit : MonoBehaviour
    {
        [Header("Data")]
        public UnitStatsSO Stats;

        [Header("Visual")]
        public Renderer  UnitRenderer;
        public Animator  UnitAnimator;
        public Light     UnitGlow;           // optional — idle point light for visual flair

        // ── Sub-components ────────────────────────────────────────────────────
        public UnitAI AI { get; private set; }

        // ── Runtime Stats ─────────────────────────────────────────────────────
        public RuntimeStats RT { get; set; }   // public set so TraitSystem can write it

        // ── Runtime State ─────────────────────────────────────────────────────
        public int        CurrentHP    { get; protected set; }
        public int        CurrentMP    { get; protected set; }
        public Team       Team         { get; set; }
        public UnitState  State        { get; protected set; } = UnitState.Idle;
        public Vector2Int GridPosition { get; set; }
        public bool       IsDefending  { get; set; }

        public List<AbilitySO> Abilities { get; protected set; } = new();

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<int, bool> OnDamaged;   // (amount, isCrit)
        public event Action<int>       OnHealed;
        public event Action            OnDeath;

        public bool IsAlive => State != UnitState.Dead;

        // ── Idle glow animation ───────────────────────────────────────────────
        private float _glowBaseIntensity;
        private Color _glowBaseColor;

        // ── Initialization ────────────────────────────────────────────────────
        public virtual void Initialize(Team team)
        {
            Team = team;

            if (team == Team.Player)
            {
                var session = Core.GameSession.Instance;
                if (session?.HeroRT != null)
                {
                    // Restore deep copy of last fight's stats (preserves all buffs/bonuses)
                    RT = new RuntimeStats(session.HeroRT);
                }
                else
                {
                    // First fight of a run: build from base SO + any earned buffs
                    RT = new RuntimeStats(Stats);
                    if (session != null)
                        foreach (var buff in session.EarnedBuffs)
                            RT.ApplyBuff(buff);
                }

                // Morale modifier: each point above/below 5 adds/removes 2% to Attack & Defense
                if (session != null)
                {
                    float moraleBonus = 1f + (session.Resources.Morale - 5) * 0.02f;
                    RT.Attack  = Mathf.RoundToInt(RT.Attack  * moraleBonus);
                    RT.Defense = Mathf.RoundToInt(RT.Defense * moraleBonus);
                }

                // Restore HP (carry damage forward; -1 = start full)
                int savedHP = session?.HeroCurrentHP ?? -1;
                CurrentHP = (savedHP > 0 && savedHP <= RT.MaxHP) ? savedHP : RT.MaxHP;
            }
            else
            {
                RT        = new RuntimeStats(Stats);
                CurrentHP = RT.MaxHP;
            }
            CurrentMP   = RT.MaxMP;
            State       = UnitState.Idle;
            IsDefending = false;

            // Ensure AI component exists
            AI = GetComponent<UnitAI>() ?? gameObject.AddComponent<UnitAI>();

            // Cache glow defaults
            if (UnitGlow != null)
            {
                _glowBaseIntensity = UnitGlow.intensity;
                _glowBaseColor     = UnitGlow.color;
            }

            SetupAbilities();

            // Spawn pop VFX
            RPG.VFX.CombatVFXManager.Instance?.PlaySpawnEffect(transform.position);
        }

        protected abstract void SetupAbilities();

        // ── Combat ────────────────────────────────────────────────────────────
        public virtual void TakeDamage(int rawDamage, bool ignoreDefense = false)
        {
            if (!IsAlive) return;

            int def    = ignoreDefense ? 0 : (IsDefending ? RT.Defense * 2 : RT.Defense);
            int damage = Mathf.Max(1, rawDamage - def);

            bool isCrit = UnityEngine.Random.value < RT.CritChance;
            if (isCrit) damage = Mathf.RoundToInt(damage * RT.CritMultiplier);

            CurrentHP = Mathf.Max(0, CurrentHP - damage);
            OnDamaged?.Invoke(damage, isCrit);

            if (UnitAnimator != null) UnitAnimator.SetTrigger("Hit");

            if (CurrentHP <= 0) TriggerDeath();
        }

        public virtual void Heal(int amount)
        {
            if (!IsAlive) return;
            CurrentHP = Mathf.Min(RT.MaxHP, CurrentHP + amount);
            OnHealed?.Invoke(amount);
        }

        private void TriggerDeath()
        {
            if (State == UnitState.Dead) return;
            State = UnitState.Dead;
            // Run the death animation on a persistent host so the coroutine
            // survives even if this GameObject gets deactivated mid-sequence.
            var host = RPG.Combat.CombatManager.Instance as MonoBehaviour
                    ?? this as MonoBehaviour;
            host.StartCoroutine(DieRoutine());
        }

        protected virtual IEnumerator DieRoutine()
        {
            if (UnitAnimator != null) UnitAnimator.SetTrigger("Die");

            // Death VFX explosion
            RPG.VFX.CombatVFXManager.Instance?.PlayDeathEffect(
                transform.position,
                Stats != null ? Stats.UnitColor : Color.white);

            // Extinguish glow
            if (UnitGlow != null)
            {
                float elapsed = 0f;
                float startIntensity = UnitGlow.intensity;
                while (elapsed < 0.5f)
                {
                    elapsed += Time.deltaTime;
                    UnitGlow.intensity = Mathf.Lerp(startIntensity, 0f, elapsed / 0.5f);
                    yield return null;
                }
                UnitGlow.enabled = false;
            }

            OnDeath?.Invoke();

            yield return new WaitForSeconds(0.6f);

            // Sink into ground
            float sink = 0f;
            Vector3 start = transform.position;
            while (sink < 0.8f)
            {
                sink += Time.deltaTime;
                transform.position = Vector3.Lerp(start, start - Vector3.up * 2f, sink / 0.8f);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        // ── Movement ──────────────────────────────────────────────────────────
        public IEnumerator MoveToTile(RPG.Grid.GridTile tile, float speed = 4.5f)
        {
            State = UnitState.Moving;

            Vector3 startPos = transform.position;
            Vector3 endPos   = tile.WorldPosition + Vector3.up * UnitHeightOffset;
            float   distance = Vector3.Distance(startPos, endPos);
            float   duration = Mathf.Max(0.3f, distance / speed);

            if (UnitAnimator != null) UnitAnimator.SetBool("IsMoving", true);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t   = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                float arc = Mathf.Sin(t * Mathf.PI) * 0.35f;
                transform.position = Vector3.Lerp(startPos, endPos, t) + Vector3.up * arc;

                Vector3 dir = (endPos - startPos).normalized;
                dir.y = 0;
                if (dir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.LookRotation(dir), elapsed / duration);

                yield return null;
            }

            transform.position = endPos;
            GridPosition       = tile.GridPos;
            if (UnitAnimator != null) UnitAnimator.SetBool("IsMoving", false);
            State = UnitState.Idle;
        }

        // ── Attack Animation ──────────────────────────────────────────────────
        public IEnumerator AttackAnimation(Vector3 targetPos)
        {
            State = UnitState.Acting;
            if (UnitAnimator != null) UnitAnimator.SetTrigger("Attack");

            Vector3 startPos = transform.position;
            Vector3 dir      = (targetPos - startPos).normalized;
            dir.y = 0;
            Vector3 lungeTarget = startPos + dir * 0.65f;

            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(dir);

            // Lunge forward
            float elapsed = 0f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, lungeTarget, elapsed / 0.18f);
                yield return null;
            }
            // Return
            elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(lungeTarget, startPos, elapsed / 0.25f);
                yield return null;
            }
            transform.position = startPos;
            State = UnitState.Idle;
        }

        // ── Idle Glow Pulse (driven by Update) ───────────────────────────────
        private void Update()
        {
            if (!IsAlive || UnitGlow == null) return;

            // Gentle breathing pulse on the idle glow
            float pulse = (Mathf.Sin(Time.time * 1.8f) + 1f) * 0.5f;
            UnitGlow.intensity = Mathf.Lerp(_glowBaseIntensity * 0.6f, _glowBaseIntensity, pulse);
        }

        // ── Utility ───────────────────────────────────────────────────────────
        protected virtual float UnitHeightOffset => 0.65f;

        public int CalculateDamage(AbilitySO ability)
        {
            float stat = ability.Type == AbilityType.Magical ? RT.MagicAttack : RT.Attack;
            return Mathf.RoundToInt(stat * ability.DamageMultiplier) + ability.FlatDamage;
        }

        public bool CanUseAbility(AbilitySO ability)
            => ability != null && CurrentMP >= ability.MPCost;

        public void ConsumeMP(int amount)
            => CurrentMP = Mathf.Max(0, CurrentMP - amount);
    }
}
