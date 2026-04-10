using Enemies;

namespace Towers
{
    public interface ITargetFilter
    {
        bool IsTargetValid(TowerAgent tower, EnemyAgent enemy);
    }
}
