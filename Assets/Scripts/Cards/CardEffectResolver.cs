using Combat;
using Towers;
using UnityEngine;

namespace Cards
{
    public class CardEffectResolver
    {
        private readonly CombatCardState cardState;
        private readonly HandController handController;
        private readonly TowerManager towerManager;

        public CardEffectResolver(
            CombatCardState cardState,
            HandController handController,
            TowerManager towerManager)
        {
            this.cardState = cardState;
            this.handController = handController;
            this.towerManager = towerManager;
        }

        public void ResolveOnPlay(CardInstance card, PlayerState playerState, CardPlayContext playContext)
        {
            if (card == null || card.Definition is null)
            {
                Debug.LogWarning("Tried to resolve effects for a null card.");
                return;
            }

            // First: resolve generic card effects from the effect list
            foreach (CardEffectData effect in card.Definition.effects)
            {
                ResolveEffect(effect, playerState);
            }

            // Then: resolve world interaction for field cards
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
                    // Debug.Log($"Resolved GainMana: +{effect.amount}");
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
                    // No spawned object by default
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
            towerManager.PlaceTower(card.Definition, worldPosition);
            // Debug.Log($"Placed tower {card.DisplayName} at {worldPosition}");
        }

        private void ResolveSpell(CardInstance card, Vector3 worldPosition)
        {
            if (card.Definition.spawnableObject is null || card.Definition.spawnableObject.prefab is null)
            {
                // Debug.Log($"Spell {card.DisplayName} used at {worldPosition} (no spawnable prefab assigned).");
                return;
            }

            Object.Instantiate(
                card.Definition.spawnableObject.prefab,
                worldPosition,
                Quaternion.identity
            );

            // Debug.Log($"Cast spell {card.DisplayName} at {worldPosition}");
        }
    }
}