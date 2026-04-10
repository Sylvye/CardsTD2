namespace Towers
{
    public interface IStatModifier
    {
        void ModifyStats(TowerAgent tower, ref TowerResolvedStats stats);
    }
}
