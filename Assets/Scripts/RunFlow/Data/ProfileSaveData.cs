using System;
using System.Collections.Generic;

namespace RunFlow
{
    [Serializable]
    public class ProfileSaveData
    {
        public int metaCurrency;
        public List<string> unlockIds = new();
        public string activeRunId;

        public bool HasUnlock(string unlockId)
        {
            return !string.IsNullOrWhiteSpace(unlockId) && unlockIds != null && unlockIds.Contains(unlockId);
        }

        public void AddUnlock(string unlockId)
        {
            if (string.IsNullOrWhiteSpace(unlockId))
                return;

            unlockIds ??= new List<string>();
            if (!unlockIds.Contains(unlockId))
                unlockIds.Add(unlockId);
        }
    }
}
