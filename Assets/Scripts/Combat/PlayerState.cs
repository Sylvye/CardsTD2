namespace Combat
{
    public class PlayerState
    {
        public int CurrentMana { get; private set; }
        public int MaxMana { get; private set; }
        public float ManaRegenPerSecond { get; private set; }
        public int Lives { get; private set; }

        private float manaRegenProgress;

        public PlayerState(int startingMana = 0, int maxMana = 20, float manaRegenPerSecond = 1f, int startingLives = 20)
        {
            CurrentMana = startingMana;
            MaxMana = maxMana;
            ManaRegenPerSecond = manaRegenPerSecond;
            Lives = startingLives;
            manaRegenProgress = 0f;
        }

        public void UpdateMana(float deltaTime)
        {
            if (CurrentMana >= MaxMana)
                return;

            manaRegenProgress += ManaRegenPerSecond * deltaTime;

            while (manaRegenProgress >= 1f)
            {
                if (CurrentMana >= MaxMana)
                {
                    manaRegenProgress = 0f;
                    return;
                }

                CurrentMana++;
                manaRegenProgress -= 1f;
            }
        }

        public bool SpendMana(int amount)
        {
            if (amount < 0 || CurrentMana < amount)
                return false;

            CurrentMana -= amount;
            return true;
        }

        public void GainBurstMana(int amount)
        {
            if (amount <= 0)
                return;

            CurrentMana += amount;
        }

        public void LoseLives(int amount)
        {
            if (amount <= 0)
                return;

            Lives -= amount;
            if (Lives < 0)
                Lives = 0;
        }
    }
}