namespace Enemies
{
    public interface IEnemyStatModifier
    {
        void ModifyStats(EnemyAgent enemy, ref EnemyResolvedStats stats);
    }
}
