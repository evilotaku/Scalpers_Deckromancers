using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Assets._Scripts.Map
{
    public class MapGenerator
    {
        public static MapData Generate(int ringCount, int nodesPerRing, uint seed)
        {
            Random.State oldState = Random.state;
            Random.InitState((int)seed);

            MapData data = new MapData();
            data.ringCount = ringCount;
            data.seed = seed;

            // Generate nodes for each ring
            // Ring 0 is the center (Boss)
            // Ring 1..ringCount-1 are normal rings
            
            // Center Node
            MapNode centerNode = new MapNode
            {
                type = NodeType.Boss,
                position = new Vector2(0, 0),
                ringIndex = 0,
                isDiscovered = false
            };
            data.nodes.Add(centerNode);

            List<List<MapNode>> rings = new List<List<MapNode>>();
            rings.Add(new List<MapNode> { centerNode });

            for (int i = 1; i < ringCount; i++)
            {
                List<MapNode> currentRingNodes = new List<MapNode>();
                int actualNodes = Mathf.Max(2, Mathf.RoundToInt(nodesPerRing * (float)i / (ringCount - 1))); 

                for (int j = 0; j < actualNodes; j++)
                {
                    float baseAngle = (float)j / actualNodes;
                    float angle = baseAngle;
                    
                    // Controlled jitter to prevent nodes from being too close
                    // Max jitter is 40% of the space between nodes
                    float maxJitter = 0.4f / actualNodes;
                    angle += Random.Range(-maxJitter, maxJitter);
                    
                    if (angle < 0) angle += 1f;
                    if (angle >= 1f) angle -= 1f;

                    float radius = (float)i / (ringCount - 1);
                    if (i > 0 && i < ringCount - 1)
                    {
                        // Slight radius jitter for organic look, but limited to 20% of ring spacing
                        float ringSpacing = 1f / (ringCount - 1);
                        radius += Random.Range(-0.2f * ringSpacing, 0.2f * ringSpacing);
                    }

                    MapNode node = new MapNode
                    {
                        type = GetRandomNodeType(i, ringCount),
                        position = new Vector2(angle, radius),
                        ringIndex = i,
                        isDiscovered = false
                    };
                    currentRingNodes.Add(node);
                    data.nodes.Add(node);
                }
                rings.Add(currentRingNodes);
            }

            // Connect nodes: Each node in ring i connects to nodes in ring i-1 (moving inward)
            for (int i = 1; i < ringCount; i++)
            {
                // Sort both rings by angle
                List<MapNode> currentRing = rings[i].OrderBy(n => n.position.x).ToList();
                List<MapNode> innerRing = rings[i - 1].OrderBy(n => n.position.x).ToList();

                int nCur = currentRing.Count;
                int nInner = innerRing.Count;

                for (int j = 0; j < nCur; j++)
                {
                    MapNode node = currentRing[j];
                    
                    // Find the angularly nearest node in the inner ring
                    // This is better than index mapping for preventing long lines
                    MapNode bestTarget = innerRing[0];
                    float minDiff = GetAngleDifference(node.position.x, bestTarget.position.x);
                    
                    int bestK = 0;
                    for(int k = 1; k < nInner; k++)
                    {
                        float diff = GetAngleDifference(node.position.x, innerRing[k].position.x);
                        if(diff < minDiff)
                        {
                            minDiff = diff;
                            bestTarget = innerRing[k];
                            bestK = k;
                        }
                    }

                    AddConnection(node, bestTarget);

                    // Secondary connection to an adjacent inner node for variety, 
                    // but only if it's not too far angularly
                    if (nInner > 1 && Random.value < 0.3f)
                    {
                        // Check neighbors in the sorted inner ring
                        int nextK = (bestK + 1) % nInner;
                        int prevK = (bestK - 1 + nInner) % nInner;
                        
                        float diffNext = GetAngleDifference(node.position.x, innerRing[nextK].position.x);
                        float diffPrev = GetAngleDifference(node.position.x, innerRing[prevK].position.x);
                        
                        int secondaryK = diffNext < diffPrev ? nextK : prevK;
                        float secondaryDiff = Mathf.Min(diffNext, diffPrev);
                        
                        // Limit secondary connection to 0.25 (90 degrees)
                        if (secondaryDiff < 0.25f)
                        {
                            AddConnection(node, innerRing[secondaryK]);
                        }
                    }
                }

                // Ensure all inner nodes have at least one incoming connection
                foreach (var innerNode in innerRing)
                {
                    if (innerNode.incomingConnections.Count == 0)
                    {
                        // Connect to angularly nearest outer node
                        MapNode bestSource = currentRing.OrderBy(n => GetAngleDifference(n.position.x, innerNode.position.x)).First();
                        AddConnection(bestSource, innerNode);
                    }
                }
            }

            Random.state = oldState;
            return data;
        }

        private static float GetAngleDifference(float a, float b)
        {
            float diff = Mathf.Abs(a - b);
            if (diff > 0.5f) diff = 1.0f - diff;
            return diff;
        }

        private static void AddConnection(MapNode source, MapNode target)
        {
            if (!source.outgoingConnections.Contains(target.id))
            {
                source.outgoingConnections.Add(target.id);
                target.incomingConnections.Add(source.id);
            }
        }

        private static NodeType GetRandomNodeType(int ringIndex, int ringCount)
        {
            float r = Random.value;
            if (r < 0.6f) return NodeType.Battle;
            if (r < 0.75f) return NodeType.Shop;
            if (r < 0.85f) return NodeType.Upgrade;
            if (r < 0.95f) return NodeType.Rest;
            return NodeType.Heal;
        }
    }
}
