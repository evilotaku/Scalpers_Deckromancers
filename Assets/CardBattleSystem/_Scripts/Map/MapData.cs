using System.Collections.Generic;
using System;

namespace Assets._Scripts.Map
{
    [Serializable]
    public class MapData
    {
        public List<MapNode> nodes = new List<MapNode>();
        public string playerCurrentNodeId;
        public int currentRing;
        
        // Configuration used to generate this map
        public int ringCount;
        public uint seed;
    }
}
