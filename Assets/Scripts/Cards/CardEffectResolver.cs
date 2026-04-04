using UnityEngine;
using Combat;

namespace Cards
{
    public class CardEffectResolver
    {
        private readonly CombatCardState cardState;
        private readonly HandController handController;

        public CardEffectResolver(CombatCardState cardState, HandController handController)
        {
            this.cardState = cardState;
            this.handController = handController;
        }

        public void ResolveOnPlay(CardInstance card, PlayerState playerState)
        {
            if (card == null || card.Definition == null)
            {
                Debug.LogWarning("Tried to resolve effects for a null card.");
                return;
            }

            foreach (CardEffectData effect in card.Definition.effects)
            {
                ResolveEffect(effect, playerState);
            }
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
                    Debug.Log($"Resolved GainMana: +{effect.amount}");
                    break;

                default:
                    Debug.LogWarning($"Unhandled effect type: {effect.effectType}");
                    break;
            }
        }
    }
}