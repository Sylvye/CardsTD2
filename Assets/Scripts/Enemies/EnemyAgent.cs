using System.Collections.Generic;
using Combat;
using UnityEngine;

namespace Enemies
{
    [RequireComponent(typeof(PathFollower))]
    public class EnemyAgent : MonoBehaviour
    {
        private readonly List<IEnemyStatModifier> runtimeModifiers = new();
        private readonly List<ActiveEnemyStatusEffect> activeStatusEffects = new();
        private PathFollower pathFollower;
        private SpriteRenderer[] spriteRenderers;
        private Color[] spriteBaseColors;
        private EnemyManager enemyManager;
        private EnemySpawner enemySpawner;
        private IPlayerEffects playerEffects;
        private EnemyDef enemyDef;
        private EnemyEffectResolver effectResolver;

        private float maxHealth;
        private float currentHealth;
        private int lifeDamage;
        private float damageFlashTimeRemaining;
        private float damageFlashDuration;
        private Color damageFlashColor;
        private Color resistedDamageFlashColor;
        private Color weaknessDamageFlashColor;
        private Color activeDamageFlashColor;

        private bool isInitialized;
        private bool isDeadOrEscaped;

        public event System.Action<EnemyAgent> Died;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public int LifeDamage => lifeDamage;
        public bool IsDeadOrEscaped => isDeadOrEscaped;
        public EnemyDef Definition => enemyDef;
        public float TrackDistance => pathFollower != null ? pathFollower.DistanceTravelled : 0f;

        private void Awake()
        {
            EnsureRuntimeDependencies();
        }

        public void Initialize(
            EnemyManager manager,
            EnemySpawner spawner,
            IPlayerEffects effects,
            EnemyPath path,
            EnemyDef def,
            float startingTrackDistance = 0f)
        {
            EnsureRuntimeDependencies();

            enemyManager = manager;
            enemySpawner = spawner;
            playerEffects = effects;
            enemyDef = def;

            isDeadOrEscaped = false;
            isInitialized = true;

            maxHealth = def.maxHealth;
            currentHealth = maxHealth;
            lifeDamage = def.lifeDamage;
            damageFlashColor = def.damageFlashColor;
            resistedDamageFlashColor = def.resistedDamageFlashColor;
            weaknessDamageFlashColor = def.weaknessDamageFlashColor;
            activeDamageFlashColor = damageFlashColor;
            damageFlashDuration = Mathf.Max(0f, def.damageFlashDuration);
            damageFlashTimeRemaining = 0f;

            CacheSpriteBaseColors();
            ApplyBaseSpriteColors();

            pathFollower.SetPath(path, startingTrackDistance);
            runtimeModifiers.Clear();
            activeStatusEffects.Clear();
            ApplyResolvedStats();

            enemyManager?.RegisterEnemy(this);

            FireTrigger(EnemyTriggerType.OnSpawn);
        }

        private void Update()
        {
            if (!isInitialized || isDeadOrEscaped || pathFollower == null)
                return;

            UpdateDamageFlash(Time.deltaTime);
            UpdateStatusEffects(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!isInitialized || isDeadOrEscaped || pathFollower == null)
                return;

            if (pathFollower.ReachedEnd)
            {
                Escape();
            }
        }

        public EnemyDamageResult TakeDamage(float amount, DamageTypeDef damageType = null)
        {
            if (isDeadOrEscaped || amount <= 0f)
                return new EnemyDamageResult(0f, EnemyDamageResponseType.Normal, false);

            EnemyResolvedStats stats = GetResolvedStats();
            EnemyDamageResponse damageResponse = enemyDef != null
                ? enemyDef.ResolveDamageTypeResponse(amount, damageType)
                : new EnemyDamageResponse(amount, EnemyDamageResponseType.Normal);
            float damageAmount = damageResponse.Amount;
            damageAmount *= stats.DamageTakenMultiplier;
            if (damageAmount <= 0f)
            {
                if (damageResponse.ResponseType == EnemyDamageResponseType.Resistance)
                    TriggerDamageFlash(GetDamageFlashColor(damageResponse.ResponseType));

                return new EnemyDamageResult(0f, damageResponse.ResponseType, false);
            }

            currentHealth -= damageAmount;
            TriggerDamageFlash(GetDamageFlashColor(damageResponse.ResponseType));

            FireTrigger(EnemyTriggerType.OnHit);

            if (currentHealth <= 0f)
            {
                Die();
            }

            return new EnemyDamageResult(damageAmount, damageResponse.ResponseType, isDeadOrEscaped);
        }

        public void ApplyStatusEffect(EnemyStatusEffectApplication application)
        {
            if (!isInitialized || isDeadOrEscaped || application == null || !application.IsValid)
                return;

            ActiveEnemyStatusEffect incoming = ActiveEnemyStatusEffect.Create(application);
            if (incoming == null)
                return;

            bool statsDirty = incoming.AffectsStats;
            switch (incoming.StackingMode)
            {
                case EnemyStatusStackingMode.StackingInstances:
                    activeStatusEffects.Add(incoming);
                    break;

                case EnemyStatusStackingMode.RefreshLongest:
                    RefreshLongest(incoming);
                    break;

                case EnemyStatusStackingMode.NonStackingStrongest:
                default:
                    ApplyNonStackingStrongest(incoming);
                    break;
            }

            if (statsDirty)
                ApplyResolvedStats();
        }

        public void AddModifier(IEnemyStatModifier modifier)
        {
            if (modifier == null)
                return;

            runtimeModifiers.Add(modifier);
            ApplyResolvedStats();
        }

        public void RemoveModifier(IEnemyStatModifier modifier)
        {
            if (modifier == null)
                return;

            runtimeModifiers.Remove(modifier);
            ApplyResolvedStats();
        }

        internal void TickStatusEffectsForTest(float deltaTime)
        {
            UpdateStatusEffects(deltaTime);
        }

        internal int ActiveStatusEffectCount => activeStatusEffects.Count;

        private void Die()
        {
            if (isDeadOrEscaped)
                return;

            isDeadOrEscaped = true;
            FireTrigger(EnemyTriggerType.OnDeath);
            Died?.Invoke(this);
            enemyManager?.UnregisterEnemy(this);
            Destroy(gameObject);
        }

        private void Escape()
        {
            if (isDeadOrEscaped)
                return;

            FireTrigger(EnemyTriggerType.OnExit);

            isDeadOrEscaped = true;
            enemyManager?.HandleEnemyEscaped(this);
            enemyManager?.UnregisterEnemy(this);
            Destroy(gameObject);
        }

        private Color GetDamageFlashColor(EnemyDamageResponseType damageResponseType)
        {
            switch (damageResponseType)
            {
                case EnemyDamageResponseType.Weakness:
                    return weaknessDamageFlashColor;
                case EnemyDamageResponseType.Resistance:
                    return resistedDamageFlashColor;
                case EnemyDamageResponseType.Normal:
                default:
                    return damageFlashColor;
            }
        }

        private void TriggerDamageFlash(Color flashColor)
        {
            if (spriteRenderers == null || spriteRenderers.Length == 0 || damageFlashDuration <= 0f)
                return;

            activeDamageFlashColor = flashColor;
            damageFlashTimeRemaining = damageFlashDuration;
            ApplyFlashColor(1f);
        }

        private void UpdateDamageFlash(float deltaTime)
        {
            if (damageFlashTimeRemaining <= 0f)
                return;

            damageFlashTimeRemaining = Mathf.Max(0f, damageFlashTimeRemaining - deltaTime);

            if (damageFlashTimeRemaining <= 0f)
            {
                ApplyBaseSpriteColors();
                return;
            }

            float normalized = damageFlashTimeRemaining / damageFlashDuration;
            ApplyFlashColor(normalized);
        }

        private void UpdateStatusEffects(float deltaTime)
        {
            if (activeStatusEffects.Count == 0 || deltaTime <= 0f)
                return;

            bool statsDirty = false;
            for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
            {
                ActiveEnemyStatusEffect statusEffect = activeStatusEffects[i];
                statusEffect.Tick(this, deltaTime);
                if (isDeadOrEscaped)
                    return;

                if (statusEffect.DurationRemaining > 0f)
                    continue;

                statsDirty |= statusEffect.AffectsStats;
                activeStatusEffects.RemoveAt(i);
            }

            if (statsDirty)
                ApplyResolvedStats();
        }

        private void ApplyNonStackingStrongest(ActiveEnemyStatusEffect incoming)
        {
            ActiveEnemyStatusEffect existing = FindRefreshableStatusEffect(incoming);
            if (existing == null)
            {
                activeStatusEffects.Add(incoming);
                return;
            }

            existing.Strength = Mathf.Max(existing.Strength, incoming.Strength);
            existing.DurationRemaining = Mathf.Max(existing.DurationRemaining, incoming.DurationRemaining);
            existing.TickInterval = incoming.TickInterval;
            existing.TickDamage = Mathf.Max(existing.TickDamage, incoming.TickDamage);
            existing.DamageType = incoming.DamageType != null ? incoming.DamageType : existing.DamageType;
        }

        private void RefreshLongest(ActiveEnemyStatusEffect incoming)
        {
            ActiveEnemyStatusEffect existing = FindRefreshableStatusEffect(incoming);
            if (existing == null)
            {
                activeStatusEffects.Add(incoming);
                return;
            }

            existing.DurationRemaining = Mathf.Max(existing.DurationRemaining, incoming.DurationRemaining);
            existing.Strength = Mathf.Max(existing.Strength, incoming.Strength);
        }

        private ActiveEnemyStatusEffect FindRefreshableStatusEffect(ActiveEnemyStatusEffect incoming)
        {
            for (int i = 0; i < activeStatusEffects.Count; i++)
            {
                ActiveEnemyStatusEffect activeStatusEffect = activeStatusEffects[i];
                if (activeStatusEffect.HasSameStack(incoming))
                    return activeStatusEffect;
            }

            return null;
        }

        private void CacheSpriteBaseColors()
        {
            if (spriteRenderers == null)
                return;

            if (spriteBaseColors == null || spriteBaseColors.Length != spriteRenderers.Length)
                spriteBaseColors = new Color[spriteRenderers.Length];

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];
                spriteBaseColors[i] = spriteRenderer != null ? spriteRenderer.color : Color.white;
            }
        }

        private void ApplyBaseSpriteColors()
        {
            if (spriteRenderers == null || spriteBaseColors == null)
                return;

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];
                if (spriteRenderer == null)
                    continue;

                spriteRenderer.color = spriteBaseColors[i];
            }
        }

        private void ApplyFlashColor(float normalizedIntensity)
        {
            if (spriteRenderers == null || spriteBaseColors == null)
                return;

            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                SpriteRenderer spriteRenderer = spriteRenderers[i];
                if (spriteRenderer == null)
                    continue;

                spriteRenderer.color = Color.Lerp(spriteBaseColors[i], activeDamageFlashColor, normalizedIntensity);
            }
        }

        private void EnsureRuntimeDependencies()
        {
            if (pathFollower == null)
                pathFollower = GetComponent<PathFollower>();

            if (spriteRenderers == null || spriteBaseColors == null)
            {
                spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
                spriteBaseColors = new Color[spriteRenderers.Length];
                CacheSpriteBaseColors();
            }

            effectResolver ??= new EnemyEffectResolver();
        }

        private void FireTrigger(EnemyTriggerType trigger)
        {
            if (enemyDef == null || effectResolver == null)
                return;

            EnemyEffectContext context = new(
                this,
                enemyManager,
                enemySpawner,
                playerEffects,
                transform.position,
                TrackDistance
            );

            effectResolver.ResolveEffectsForTrigger(enemyDef, trigger, context);
        }

        private EnemyResolvedStats GetResolvedStats()
        {
            EnemyResolvedStats stats = new(enemyDef != null ? enemyDef.moveSpeed : 0f, 1f);
            for (int i = 0; i < runtimeModifiers.Count; i++)
                runtimeModifiers[i].ModifyStats(this, ref stats);

            for (int i = 0; i < activeStatusEffects.Count; i++)
                activeStatusEffects[i].ModifyStats(ref stats);

            stats.Clamp();
            return stats;
        }

        private void ApplyResolvedStats()
        {
            if (pathFollower == null)
                return;

            EnemyResolvedStats stats = GetResolvedStats();
            pathFollower.SetSpeed(stats.MoveSpeed);
        }

        private sealed class ActiveEnemyStatusEffect
        {
            private const int MaxDotTicksPerFrame = 32;

            private readonly EnemyStatusEffectDef effect;
            private readonly string stackKey;

            private ActiveEnemyStatusEffect(
                EnemyStatusEffectDef effect,
                string stackKey,
                float duration,
                float strength,
                float tickInterval,
                float tickDamage,
                DamageTypeDef damageType)
            {
                this.effect = effect;
                this.stackKey = stackKey;
                DurationRemaining = duration;
                Strength = strength;
                TickInterval = tickInterval;
                TickDamage = tickDamage;
                DamageType = damageType;
                TickTimer = tickInterval;
            }

            public EnemyStatusStackingMode StackingMode => effect.stackingMode;
            public EnemyStatusEffectBehaviorType BehaviorType => effect.behaviorType;
            public float DurationRemaining { get; set; }
            public float Strength { get; set; }
            public float TickInterval { get; set; }
            public float TickDamage { get; set; }
            public DamageTypeDef DamageType { get; set; }
            private float TickTimer { get; set; }

            public bool AffectsStats =>
                BehaviorType == EnemyStatusEffectBehaviorType.Slow ||
                BehaviorType == EnemyStatusEffectBehaviorType.Stun;

            public static ActiveEnemyStatusEffect Create(EnemyStatusEffectApplication application)
            {
                EnemyStatusEffectDef effect = application.effect;
                if (effect == null)
                    return null;

                string resolvedStackKey = effect.ResolvedStackKey;
                if (string.IsNullOrWhiteSpace(resolvedStackKey))
                    resolvedStackKey = effect.name;

                return new ActiveEnemyStatusEffect(
                    effect,
                    resolvedStackKey,
                    application.ResolveDuration(),
                    application.ResolveStrength(),
                    application.ResolveTickInterval(),
                    application.ResolveTickDamage(),
                    application.ResolveDamageType()
                );
            }

            public bool HasSameStack(ActiveEnemyStatusEffect other)
            {
                return other != null &&
                    BehaviorType == other.BehaviorType &&
                    string.Equals(stackKey, other.stackKey, System.StringComparison.Ordinal);
            }

            public void Tick(EnemyAgent enemy, float deltaTime)
            {
                DurationRemaining -= deltaTime;

                if (BehaviorType != EnemyStatusEffectBehaviorType.DamageOverTime || TickDamage <= 0f)
                    return;

                TickTimer -= deltaTime;
                int ticksApplied = 0;
                while (TickTimer <= 0f && ticksApplied < MaxDotTicksPerFrame && !enemy.IsDeadOrEscaped)
                {
                    enemy.TakeDamage(TickDamage, DamageType);
                    TickTimer += TickInterval;
                    ticksApplied++;
                }
            }

            public void ModifyStats(ref EnemyResolvedStats stats)
            {
                switch (BehaviorType)
                {
                    case EnemyStatusEffectBehaviorType.Slow:
                        stats.MoveSpeed *= Mathf.Max(0f, 1f - Mathf.Clamp01(Strength));
                        break;

                    case EnemyStatusEffectBehaviorType.Stun:
                        stats.MoveSpeed = 0f;
                        break;
                }

                stats.Clamp();
            }
        }
    }
}
