using System.Collections.Generic;
using Combat;
using UnityEngine;

namespace Enemies
{
    [RequireComponent(typeof(PathFollower))]
    public class EnemyAgent : MonoBehaviour
    {
        private readonly List<IEnemyStatModifier> runtimeModifiers = new();
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
            pathFollower = GetComponent<PathFollower>();
            spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            spriteBaseColors = new Color[spriteRenderers.Length];
            CacheSpriteBaseColors();
            effectResolver = new EnemyEffectResolver();
        }

        public void Initialize(
            EnemyManager manager,
            EnemySpawner spawner,
            IPlayerEffects effects,
            EnemyPath path,
            EnemyDef def,
            float startingTrackDistance = 0f)
        {
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
            damageFlashDuration = Mathf.Max(0f, def.damageFlashDuration);
            damageFlashTimeRemaining = 0f;

            CacheSpriteBaseColors();
            ApplyBaseSpriteColors();

            if (pathFollower == null)
                pathFollower = GetComponent<PathFollower>();

            pathFollower.SetPath(path, startingTrackDistance);
            runtimeModifiers.Clear();
            ApplyResolvedStats();

            enemyManager?.RegisterEnemy(this);

            FireTrigger(EnemyTriggerType.OnSpawn);
        }

        private void Update()
        {
            if (!isInitialized || isDeadOrEscaped || pathFollower == null)
                return;

            UpdateDamageFlash(Time.deltaTime);

            if (pathFollower.ReachedEnd)
            {
                Escape();
            }
        }

        public void TakeDamage(float amount)
        {
            if (isDeadOrEscaped || amount <= 0f)
                return;

            EnemyResolvedStats stats = GetResolvedStats();
            currentHealth -= amount * stats.DamageTakenMultiplier;
            TriggerDamageFlash();

            FireTrigger(EnemyTriggerType.OnHit);

            if (currentHealth <= 0f)
            {
                Die();
            }
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

        private void TriggerDamageFlash()
        {
            if (spriteRenderers == null || spriteRenderers.Length == 0 || damageFlashDuration <= 0f)
                return;

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

                spriteRenderer.color = Color.Lerp(spriteBaseColors[i], damageFlashColor, normalizedIntensity);
            }
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
    }
}
