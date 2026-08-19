using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using Assets._Scripts;
using BattleCardGameFramework;
using Unity.Services.Authentication;

namespace Assets._Scripts.Map
{
    public class MapManager : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private CardBattleManager multiplayerGameManager;
        [SerializeField] private StyleSheet mapStyleSheet;
        [SerializeField] private MapLayoutType layoutType = MapLayoutType.Circular;
        [SerializeField] private int layerCount = 5;
        [SerializeField] private int nodesPerLayer = 8;
        
        private MapData currentMap;
        private MapScreen mapScreen;

        private void OnEnable()
        {
            if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
            if (multiplayerGameManager == null) multiplayerGameManager = Object.FindAnyObjectByType<CardBattleManager>();

            mapScreen = new MapScreen();
            if (mapStyleSheet != null) mapScreen.styleSheets.Add(mapStyleSheet);
            uiDocument.rootVisualElement.Add(mapScreen);
            
            // Wait for layout to be calculated before initializing
            mapScreen.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            if (multiplayerGameManager != null)
            {
                multiplayerGameManager.OnGameStateUpdated += HandleGameStateUpdated;
            }
            mapScreen.onRegenerateRequested += ResetMap;
        }

        private void OnDisable()
        {
            if (multiplayerGameManager != null)
            {
                multiplayerGameManager.OnGameStateUpdated -= HandleGameStateUpdated;
            }
            mapScreen.onRegenerateRequested -= ResetMap;
        }

        private void HandleGameStateUpdated(BaseGameClientStateDTO state)
        {
            if (state == null) return;

            if (state.IsGameOver)

            {
                // Game finished, show map again
                ShowMap();
            }
            else
            {
                // Game started or in progress, hide map
                HideMap();
            }
        }

        private void ShowMap()
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        private void HideMap()
        {
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            mapScreen.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            _ = InitializeMap();
        }

        public async Task InitializeMap()
        {
            if (!AuthenticationService.Instance.IsSignedIn) return;
            currentMap = await MapSaveService.LoadMapAsync();
            
            if (currentMap == null)
            {
                currentMap = MapGenerator.Generate(layerCount, nodesPerLayer, (uint)Random.Range(0, 100000));
                await MapSaveService.SaveMapAsync(currentMap);
            }
            
            mapScreen.Initialize(currentMap, layoutType, OnNodeSelected);
        }

        private void OnNodeSelected(MapNode node)
        {
            Debug.Log($"Node selected: {node.type} at {node.id}");
            
            currentMap.playerCurrentNodeId = node.id;
            currentMap.currentRing = node.ringIndex;
            
            mapScreen.UpdateNodeAccessibility(node.id);
            _ = MapSaveService.SaveMapAsync(currentMap);
            
            TriggerEncounter(node);
        }

        private void TriggerEncounter(MapNode node)
        {
            switch (node.type)
            {
                case NodeType.Battle:
                    Debug.Log("Triggering Battle Encounter...");
                    if (multiplayerGameManager != null)
                    {
                        _ = multiplayerGameManager.StartGameAsync();
                    }
                    break;
                case NodeType.Boss:
                    Debug.Log("Triggering Boss Encounter...");
                    if (multiplayerGameManager != null)
                    {
                        _ = multiplayerGameManager.StartGameAsync();
                    }
                    break;
                case NodeType.Shop:
                    Debug.Log("Triggering Shop Encounter...");
                    // Implement Shop logic
                    break;
                case NodeType.Upgrade:
                    Debug.Log("Triggering Upgrade Encounter...");
                    // Implement Upgrade logic
                    break;
                case NodeType.Rest:
                    Debug.Log("Triggering Rest Encounter...");
                    // Implement Rest logic
                    break;
                case NodeType.Heal:
                    Debug.Log("Triggering Heal Encounter...");
                    // Implement Heal logic
                    break;
            }
        }
        
        [ContextMenu("Reset Map")]
        public async void ResetMap()
        {
            await MapSaveService.DeleteMapAsync();
            _ = InitializeMap();
        }
    }
}
