using Combat;
using Enemies;
using Towers;
using UnityEngine;

namespace Cards
{
    public class CardEffectResolver
    {
        private readonly CombatCardState cardState;
        private readonly HandController handController;
        private readonly TowerManager towerManager;
        private readonly SpellResolver spellResolver;

        public CardEffectResolver(
            CombatCardState cardState,
            HandController handController,
            TowerManager towerManager,
            EnemyManager enemyManager,
            IPlayerEffects playerEffects)
        {
            this.cardState = cardState;
            this.handController = handController;
            this.towerManager = towerManager;
            spellResolver = new SpellResolver(towerManager, enemyManager, playerEffects);
        }

        public void ResolveOnPlay(CardInstance card, PlayerState playerState, CardPlayContext playContext)
        {
            if (card == null || card.ResolvedData == null)
            {
                Debug.LogWarning("Tried to resolve effects for a null card.");
                return;
            }

            foreach (CardEffectData effect in card.ResolvedData.Effects)
                ResolveEffect(effect, playerState);

            ResolveWorldUse(card, playContext);
        }

        private void ResolveEffect(CardEffectData effect, PlayerState playerState)
        {
            switch (effect.effectType)
            {
                case CardEffectType.None:
                    break;

                case CardEffectType.DrawCards:
                    handController.DrawCards(effect.amount);
                    break;

                case CardEffectType.GainMana:
                    playerState.GainBurstMana(effect.amount);
                    break;
            }
        }

        private void ResolveWorldUse(CardInstance card, CardPlayContext playContext)
        {
            if (card == null || card.ResolvedData == null || playContext == null)
                return;

            if (!playContext.HasWorldPosition)
                return;

            switch (card.Type)
            {
                case CardType.Mod:
                    break;

                case CardType.Spell:
                    ResolveSpell(card, playContext.WorldPosition);
                    break;

                case CardType.Tower:
                    ResolveTower(card, playContext.WorldPosition);
                    break;
            }
        }

        private void ResolveTower(CardInstance card, Vector3 worldPosition)
        {
            if (card.ResolvedData.TowerDefinition != null)
            {
                towerManager.PlaceTower(card.ResolvedData, worldPosition);
                return;
            }

            SpawnableObjectDef spawnable = card.ResolvedData.SpawnableObject;
            if (spawnable == null || spawnable.prefab == null)
                return;

            Object.Instantiate(
                spawnable.prefab,
                worldPosition,
                Quaternion.identity
            );
        }

        private void ResolveSpell(CardInstance card, Vector3 worldPosition)
        {
            if (card.ResolvedData.SpellDefinition is SpellDef spellDef)
            {
                GameObject spawnedSpellObject = null;
                if (spellDef.prefab != null)
                {
                    spawnedSpellObject = Object.Instantiate(
                        spellDef.prefab,
                        worldPosition,
                        Quaternion.identity
                    );
                }

                spellResolver.Resolve(spellDef, spawnedSpellObject);
                return;
            }

            if (card.ResolvedData.SpawnableObject is null || card.ResolvedData.SpawnableObject.prefab is null)
                return;

            Object.Instantiate(
                card.ResolvedData.SpawnableObject.prefab,
                worldPosition,
                Quaternion.identity
            );
        }
    }
}
