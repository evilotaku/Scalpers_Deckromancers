using UnityEngine;
using UnityEngine.UIElements;
using Unity.Netcode;
using BattleCardGameFramework;
using Assets._Scripts;
using System.Collections.Generic;

public class PlayerHandCountDisplay : NetworkBehaviour
{
    private PanelRenderer panelRenderer;
    private Label countLabel;
    private VisualElement cardContainer;
    private PlayerData playerData;
    private CardBattleManager cardBattleManager;

    [SerializeField] private float maxFanAngle = 45f;
    [SerializeField] private float spreadRadius = 50f;

    private void Awake()
    {
        playerData = GetComponentInParent<PlayerData>();
        panelRenderer = GetComponent<PanelRenderer>();
        panelRenderer.RegisterUIReloadCallback(OnUIReload);
    }

    private void Start()
    {
        cardBattleManager = Object.FindAnyObjectByType<CardBattleManager>();
        if (cardBattleManager != null)
        {
            cardBattleManager.OnGameStateUpdated += UpdateHandDisplay;
            if (cardBattleManager.m_CurrentState != null)
            {
                UpdateHandDisplay(cardBattleManager.m_CurrentState);
            }
        }
    }

    public override void OnDestroy()
    {
        if (cardBattleManager != null)
        {
            cardBattleManager.OnGameStateUpdated -= UpdateHandDisplay;
        }
        base.OnDestroy();
    }

    private void OnUIReload(PanelRenderer renderer, VisualElement rootElement)
    {
        countLabel = rootElement.Q<Label>("CountLabel");
        cardContainer = rootElement.Q<VisualElement>("CardContainer");
        if (cardBattleManager != null && cardBattleManager.m_CurrentState != null)
        {
            UpdateHandDisplay(cardBattleManager.m_CurrentState);
        }
    }

    private void UpdateHandDisplay(BaseGameClientStateDTO baseState)
    {
        var state = baseState.AsHearthstoneState();
        if (state == null || cardContainer == null || playerData == null) return;


        int handSize = 0;
        string myId = playerData.PlayerId.Value.ToString();

        if (state.YourState != null && state.YourState.PlayerId == myId)
        {
            handSize = state.YourState.Hand != null ? state.YourState.Hand.Count : 0;
        }
        else if (state.OpponentState != null && state.OpponentState.PlayerId == myId)
        {
            handSize = state.OpponentState.Hand != null ? state.OpponentState.Hand.Count : 0;
        }
        else
        {
            return;
        }

        if (countLabel != null)
            countLabel.text = handSize.ToString();

        UpdateCardFan(handSize);
    }

    private void UpdateCardFan(int count)
    {
        cardContainer.Clear();
        if (count <= 0) return;

        float angleStep = count > 1 ? maxFanAngle / (count - 1) : 0;
        float startAngle = -maxFanAngle / 2f;

        for (int i = 0; i < count; i++)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("card-back");
            
            float angle = startAngle + (angleStep * i);
            
            // Apply rotation and slight vertical offset for the fan effect
            card.style.rotate = new Rotate(angle);
            
            // To make it look like a fan, we can also offset them slightly on X/Y
            // but transform-origin: bottom center in USS handles most of it.
            // We can add a bit of radial displacement if desired:
            float rad = angle * Mathf.Deg2Rad;
            float xOffset = Mathf.Sin(rad) * spreadRadius;
            float yOffset = (1f - Mathf.Cos(rad)) * spreadRadius;
            
            // Centering: Parent width (300) / 2 - Card width (60) / 2 = 120
            card.style.left = 120f + xOffset;
            card.style.bottom = yOffset;

            cardContainer.Add(card);
        }
    }

    private void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
        }
    }
}
