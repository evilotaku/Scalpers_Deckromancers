using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Assets._Scripts.Map
{
    [UxmlElement]
    public partial class MapScreen : VisualElement
    {
        private VisualElement nodeLayer;
        private VisualElement lineLayer;
        private MapData mapData;
        public System.Action onRegenerateRequested;
        
        public MapScreen()
        {
            style.flexGrow = 1;
            style.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            
            lineLayer = new VisualElement();
            lineLayer.style.position = Position.Absolute;
            lineLayer.style.width = Length.Percent(100);
            lineLayer.style.height = Length.Percent(100);
            lineLayer.pickingMode = PickingMode.Ignore;
            lineLayer.generateVisualContent += OnGenerateVisualContent;
            Add(lineLayer);

            nodeLayer = new VisualElement();
            nodeLayer.style.position = Position.Absolute;
            nodeLayer.style.width = Length.Percent(100);
            nodeLayer.style.height = Length.Percent(100);
            nodeLayer.pickingMode = PickingMode.Ignore;
            Add(nodeLayer);

            var regenerateButton = new Button { text = "Regenerate Map" };
            regenerateButton.style.position = Position.Absolute;
            regenerateButton.style.top = 20;
            regenerateButton.style.right = 20;
            regenerateButton.clicked += () => onRegenerateRequested?.Invoke();
            Add(regenerateButton);

            RegisterCallback<GeometryChangedEvent>(evt => lineLayer.MarkDirtyRepaint());
        }

        private MapLayoutType layoutType;

        private Dictionary<int, int> nodesPerRingCount = new Dictionary<int, int>();
        private Dictionary<string, int> nodeIndexInRing = new Dictionary<string, int>();

        public void Initialize(MapData data, MapLayoutType layout, System.Action<MapNode> onNodeSelected)
        {
            mapData = data;
            layoutType = layout;
            nodeLayer.Clear();

            // Pre-calculate ring node counts and indices for centering
            nodesPerRingCount.Clear();
            nodeIndexInRing.Clear();
            var sortedNodes = data.nodes.OrderBy(n => n.ringIndex).ThenBy(n => n.position.x).ToList();
            foreach (var node in sortedNodes)
            {
                if (!nodesPerRingCount.ContainsKey(node.ringIndex)) nodesPerRingCount[node.ringIndex] = 0;
                nodeIndexInRing[node.id] = nodesPerRingCount[node.ringIndex];
                nodesPerRingCount[node.ringIndex]++;
            }
            
            // Layout nodes based on current size
            float width = resolvedStyle.width > 0 ? resolvedStyle.width : 800;
            float height = resolvedStyle.height > 0 ? resolvedStyle.height : 600;

            foreach (var node in data.nodes)
            {
                MapNodeView nodeView = new MapNodeView();
                nodeView.Setup(node);
                nodeView.name = node.id;
                
                Vector2 pos = GetScreenPosition(node, width, height);
                
                nodeView.style.left = pos.x - 20;
                nodeView.style.top = pos.y - 20;
                
                nodeView.onNodeClicked = (view) => onNodeSelected?.Invoke(view.nodeData);
                
                nodeLayer.Add(nodeView);
            }
            
            UpdateNodeAccessibility(data.playerCurrentNodeId);
            lineLayer.MarkDirtyRepaint();
        }

        public void UpdateNodeAccessibility(string currentNodeId)
        {
            if (mapData == null) return;
            MapNode currentNode = mapData.nodes.FirstOrDefault(n => n.id == currentNodeId);
            
            foreach (var nodeView in nodeLayer.Children().OfType<MapNodeView>())
            {
                bool isAccessible = false;
                if (currentNode == null)
                {
                    isAccessible = nodeView.nodeData.ringIndex == mapData.ringCount - 1;
                }
                else
                {
                    isAccessible = currentNode.outgoingConnections.Contains(nodeView.nodeData.id);
                }
                
                nodeView.SetAccessible(isAccessible);
                nodeView.SetHighlight(nodeView.nodeData.id == currentNodeId);
            }
        }

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (mapData == null) return;

            var painter = mgc.painter2D;
            float width = resolvedStyle.width;
            float height = resolvedStyle.height;

            if (layoutType == MapLayoutType.Circular)
            {
                float centerX = width / 2;
                float centerY = height / 2;
                float maxRadius = Mathf.Min(centerX, centerY) * 0.8f;

                // Draw concentric rings
                painter.strokeColor = new Color(1, 1, 1, 0.1f);
                painter.lineWidth = 1f;
                for (int i = 0; i < mapData.ringCount; i++)
                {
                    float radius = ((float)i / (mapData.ringCount - 1)) * maxRadius;
                    if (radius <= 0) continue;

                    painter.BeginPath();
                    painter.Arc(new Vector2(centerX, centerY), radius, 0, 360);
                    painter.Stroke();
                }
            }

            // Draw connections
            painter.strokeColor = new Color(1, 1, 1, 0.3f);
            painter.lineWidth = 2f;

            foreach (var node in mapData.nodes)
            {
                Vector2 startPos = GetScreenPosition(node, width, height);
                
                foreach (var targetId in node.outgoingConnections)
                {
                    var targetNode = mapData.nodes.FirstOrDefault(n => n.id == targetId);
                    if (targetNode != null)
                    {
                        Vector2 endPos = GetScreenPosition(targetNode, width, height);
                        painter.BeginPath();
                        painter.MoveTo(startPos);
                        painter.LineTo(endPos);
                        painter.Stroke();
                    }
                }
            }
        }

        private Vector2 GetScreenPosition(MapNode node, float width, float height)
        {
            if (layoutType == MapLayoutType.Circular)
            {
                float centerX = width / 2;
                float centerY = height / 2;
                float maxRadius = Mathf.Min(centerX, centerY) * 0.8f;
                float angleRad = node.position.x * Mathf.PI * 2;
                float radius = node.position.y * maxRadius;
                return new Vector2(centerX + Mathf.Cos(angleRad) * radius, centerY + Mathf.Sin(angleRad) * radius);
            }
            else
            {
                // Vertical Top-to-Bottom
                float padding = 80f;
                float centerX = width / 2;
                
                int countInRing = nodesPerRingCount[node.ringIndex];
                int indexInRing = nodeIndexInRing[node.id];

                // Horizontal centering logic
                float spacing = Mathf.Min(width - 2 * padding, width / 2); // Cap the spread
                float x = centerX + (indexInRing - (countInRing - 1) / 2f) * (spacing / (countInRing > 1 ? countInRing - 1 : 1));
                
                // If only 1 node, it's at centerX. If more, they spread.
                if (countInRing == 1) x = centerX;
                else x = centerX + (indexInRing - (countInRing - 1) / 2f) * (spacing / Mathf.Max(1, countInRing - 1));

                // Add variance to X
                x += (node.position.x - (float)indexInRing/countInRing) * 40f; 

                float y = padding + (1f - node.position.y) * (height - 2 * padding);
                return new Vector2(x, y);
            }
        }
    }
}

