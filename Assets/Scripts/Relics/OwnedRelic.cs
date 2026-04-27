using System;
using UnityEngine;

namespace Relics
{
    [Serializable]
    public class OwnedRelic
    {
        [SerializeField] private RelicDef definition;

        public RelicDef Definition => definition;

        public OwnedRelic()
        {
        }

        public OwnedRelic(RelicDef relicDefinition)
        {
            definition = relicDefinition;
        }
    }
}
