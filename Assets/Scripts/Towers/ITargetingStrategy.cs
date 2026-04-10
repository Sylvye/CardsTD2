using Enemies;

namespace Towers
{
    public interface ITargetingStrategy
    {
        EnemyAgent SelectTarget(TargetingContext context);
    }
}
