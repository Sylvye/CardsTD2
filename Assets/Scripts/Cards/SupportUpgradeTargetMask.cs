using System;

namespace Cards
{
    [Flags]
    public enum SupportUpgradeTargetMask
    {
        None = 0,
        Beacon = 1 << 0,
        Conduit = 1 << 1
    }
}
