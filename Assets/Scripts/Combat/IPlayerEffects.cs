namespace Combat
{
    public interface IPlayerEffects
    {
        void LoseHealth(int amount);
        void GainMana(int amount);
    }
}
