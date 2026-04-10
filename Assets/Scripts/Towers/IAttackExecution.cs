namespace Towers
{
    public interface IAttackExecution
    {
        void Tick(float deltaTime);
        void Shutdown();
    }
}
