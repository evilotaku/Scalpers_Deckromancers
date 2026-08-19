using UnityEngine.UIElements;
using UnityEngine;
using System;

namespace Assets._Scripts.Map
{
    [UxmlElement]
    public partial class MapNodeView : VisualElement
    {
        public MapNode nodeData;
        public Action<MapNodeView> onNodeClicked;

        private Label iconLabel;

        public MapNodeView()
        {
            AddToClassList("map-node");

            iconLabel = new Label();
            iconLabel.AddToClassList("map-node__label");
            Add(iconLabel);

            RegisterCallback<ClickEvent>(evt => onNodeClicked?.Invoke(this));
        }

        public void Setup(MapNode node)
        {
            nodeData = node;
            iconLabel.text = node.type.ToString().Substring(0, 1);
            
            // Set color based on type
            Color color = Color.gray;
            switch (node.type)
            {
                case NodeType.Battle: color = new Color(0.8f, 0.2f, 0.2f); break; // Red
                case NodeType.Shop: color = new Color(0.8f, 0.8f, 0.2f); break; // Yellow
                case NodeType.Upgrade: color = new Color(0.2f, 0.2f, 0.8f); break; // Blue
                case NodeType.Rest: color = new Color(0.2f, 0.8f, 0.2f); break; // Green
                case NodeType.Heal: color = new Color(0.2f, 0.8f, 0.8f); break; // Cyan
                case NodeType.Boss: color = new Color(0.5f, 0.0f, 0.5f); break; // Purple
            }
            style.backgroundColor = color;
        }
        
        public void SetAccessible(bool accessible)
        {
            if (accessible)
            {
                AddToClassList("map-node--accessible");
                RemoveFromClassList("map-node--inaccessible");
            }
            else
            {
                AddToClassList("map-node--inaccessible");
                RemoveFromClassList("map-node--accessible");
            }
            pickingMode = accessible ? PickingMode.Position : PickingMode.Ignore;
        }

        public void SetHighlight(bool highlight)
        {
            if (highlight)
                AddToClassList("map-node--highlight");
            else
                RemoveFromClassList("map-node--highlight");
        }
    }
}
