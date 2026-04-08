namespace Combat
{
    public interface IPlayerEffects
    {
        void LoseLives(int amount);
        void GainMana(int amount);
    }
}
