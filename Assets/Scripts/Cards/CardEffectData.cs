using System;

namespace Cards
{
    [Serializable]
    public class CardEffectData
    {
        public CardEffectType effectType = CardEffectType.None;
        public int amount = 0;

        public CardEffectData Clone()
        {
            return new CardEffectData
            {
                effectType = effectType,
                amount = amount
            };
        }
    }
}
