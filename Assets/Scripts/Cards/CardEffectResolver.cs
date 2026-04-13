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
            EnemyManager enemyManager)
        {
            this.cardState = cardState;
            this.handController = handController;
            this.towerManager = towerManager;
            spellResolver = new SpellResolver(towerManager, enemyManager);
        }

        public void ResolveOnPlay(CardInstance card, PlayerState playerState, CardPlayContext playContext)
        {
            if (card == null || card.Definition is null)
            {
                Debug.LogWarning("Tried to resolve effects for a null card.");
                return;
            }

            foreach (CardEffectData effect in card.Definition.effects)
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
            if (card == null || card.Definition is null || playContext == null)
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
            SpawnableObjectDef spawnable = card.Definition.spawnableObject;
            if (spawnable is TowerDef towerDef)
            {
                towerManager.PlaceTower(towerDef, worldPosition);
                return;
            }

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
            if (card.Definition.spawnableObject is SpellDef spellDef)
            {
                Debug.Log("Resolving spell " + spellDef.name);
                spellResolver.Resolve(spellDef, worldPosition);
                if (spellDef.prefab != null)
                {
                    Object.Instantiate(
                        spellDef.prefab,
                        worldPosition,
                        Quaternion.identity
                    );
                }
                return;
            }

            if (card.Definition.spawnableObject is null || card.Definition.spawnableObject.prefab is null)
                return;

            Object.Instantiate(
                card.Definition.spawnableObject.prefab,
                worldPosition,
                Quaternion.identity
            );
        }
    }
}
