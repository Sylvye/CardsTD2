using System;

namespace Cards
{
    [Serializable]
    public class CardEffectData
    {
        public CardEffectType effectType = CardEffectType.None;
        public int amount = 0;
    }
}