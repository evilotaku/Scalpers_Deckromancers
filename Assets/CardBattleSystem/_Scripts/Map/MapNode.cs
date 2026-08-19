using System.Collections.Generic;
using UnityEngine;
using System;

namespace Assets._Scripts.Map
{
    [Serializable]
    public class MapNode
    {
        public string id;
        public NodeType type;
        public Vector2 position; // Normalized polar coordinates: x = angle (0-1), y = ring radius (0-1)
        public int ringIndex;
        public List<string> outgoingConnections = new List<string>();
        public List<string> incomingConnections = new List<string>();
        
        public bool isDiscovered;
        public bool isCompleted;

        public MapNode()
        {
            id = Guid.NewGuid().ToString();
        }
    }
}
