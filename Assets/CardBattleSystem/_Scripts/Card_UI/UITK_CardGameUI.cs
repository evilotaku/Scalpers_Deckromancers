using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AppUI.UI;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UITK_CardGameUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool elementsCanMoveToOtherLists = true;
    [SerializeField] private bool draggingOutRemovesItem = true;
    [SerializeField] private float distanceUntilRemoveItem = 120f;
    [SerializeField] private bool showDestroyPrompt = true;

    [Header("Animations")]
    [SerializeField] private float swapDuration = 0.2f;
    [SerializeField] private AnimationCurve swapEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float removalDuration = 0.2f;
    [SerializeField] private AnimationCurve removalEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private UIDocument _uiDocument;
    private VisualElement _root;
    private VisualElement _destroyPromptOverlay;
    private Unity.AppUI.UI.Button _confirmButton;
    private Unity.AppUI.UI.Button _cancelButton;

    private VisualElement _pendingDestroyCardContent;

    // Events matching original system for compatibility
    public static event Action OnOrderChanged;
    public static event Action<VisualElement> OnItemRemoveRequested;
    public static event Action OnDestroyConfirmed;
    public static event Action OnDestroyCancelled;

    private Dictionary<VisualElement, int> _originalIndices = new Dictionary<VisualElement, int>();

    private void OnEnable()
    {
        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;

        // Query components
        _destroyPromptOverlay = _root.Q<VisualElement>("destroyPrompt");
        _confirmButton = _root.Q<Unity.AppUI.UI.Button>("confirmButton");
        _cancelButton = _root.Q<Unity.AppUI.UI.Button>("cancelButton");

        // Set up click handlers
        if (_confirmButton != null)
        {
            _confirmButton.clicked += ConfirmRemoval;
        }
        if (_cancelButton != null)
        {
            _cancelButton.clicked += CancelRemoval;
        }

        // Initialize drag manipulators on all card contents
        var cardContents = _root.Query<VisualElement>(className: "card-content").ToList();
        foreach (var card in cardContents)
        {
            var manipulator = new CardDragManipulator(elementsCanMoveToOtherLists, draggingOutRemovesItem, distanceUntilRemoveItem);
            card.AddManipulator(manipulator);

            // Register pointer hover callbacks to bring hovered card on top of overlap
            BindHoverEvents(card);
        }

        // Subscribe to drag manipulator static events
        CardDragManipulator.OnItemDropped += HandleItemDropped;
        CardDragManipulator.OnItemRemoveRequested += HandleItemRemoveRequested;

        // Register geometry changed callback for robust layout fanning
        VisualElement horizontalList = _root.Q<VisualElement>("horizontalList");
        if (horizontalList != null)
        {
            horizontalList.RegisterCallback<GeometryChangedEvent>(OnListGeometryChanged);
        }

        // Apply initial card fan with a slight layout delay
        StartCoroutine(InitialFanUpdate());
    }

    private void OnDisable()
    {
        if (_confirmButton != null)
        {
            _confirmButton.clicked -= ConfirmRemoval;
        }
        if (_cancelButton != null)
        {
            _cancelButton.clicked -= CancelRemoval;
        }

        CardDragManipulator.OnItemDropped -= HandleItemDropped;
        CardDragManipulator.OnItemRemoveRequested -= HandleItemRemoveRequested;

        VisualElement horizontalList = _root.Q<VisualElement>("horizontalList") ?? _root.Q<VisualElement>(className: "player-hand");
        if (horizontalList != null)
        {
            horizontalList.UnregisterCallback<GeometryChangedEvent>(OnListGeometryChanged);
        }
}

    private void OnListGeometryChanged(GeometryChangedEvent evt)
    {
        UpdateHorizontalFan();
        ResetSlotsInOtherLists();
    }

    private void BindHoverEvents(VisualElement card)
    {
        card.RegisterCallback<PointerEnterEvent>(evt => HandlePointerEnter(card));
        card.RegisterCallback<PointerLeaveEvent>(evt => HandlePointerLeave(card));
    }

    private void HandlePointerEnter(VisualElement card)
    {
        VisualElement slot = card.parent;
        if (slot == null) return;
        VisualElement parentList = slot.parent;
        if (parentList == null) return;

        // Only apply bringing to front within the horizontal fanned hand
        if (parentList.name != "horizontalList") return;

        // Skip if dragging or slot has no siblings
        if (parentList.childCount <= 1) return;

        // Do not trigger if dragging is active
        if (card.ClassListContains("card-content-dragging")) return;

        if (!_originalIndices.ContainsKey(slot))
        {
            int index = parentList.IndexOf(slot);
            _originalIndices[slot] = index;
            slot.BringToFront();
        }
    }

    private void HandlePointerLeave(VisualElement card)
    {
        VisualElement slot = card.parent;
        if (slot == null) return;
        VisualElement parentList = slot.parent;
        if (parentList == null) return;

        if (parentList.name != "horizontalList") return;

        if (_originalIndices.TryGetValue(slot, out int originalIndex))
        {
            _originalIndices.Remove(slot);
            if (originalIndex < parentList.childCount)
            {
                parentList.Insert(originalIndex, slot);
            }
            else
            {
                parentList.Add(slot);
            }
        }
    }

    private IEnumerator InitialFanUpdate()
    {
        yield return null; // Wait for layout pass
        UpdateHorizontalFan();
        ResetSlotsInOtherLists();
    }

    public void SetupCardManipulators(VisualElement card)
    {
        var manipulator = new CardDragManipulator(elementsCanMoveToOtherLists, draggingOutRemovesItem, distanceUntilRemoveItem);
        card.AddManipulator(manipulator);
        BindHoverEvents(card);
    }

   
    public void UpdateHorizontalFan()
    {
        // Fan player hand (curves upward from bottom)
        UpdateHandFan("player-hand", false);
        // Fan opponent hand (curves downward from top)
        UpdateHandFan("opponent-hand", true);
    }

    private void UpdateHandFan(string handClass, bool invert)
    {
        VisualElement handContainer = _root.Q<VisualElement>(className: handClass);
        if (handContainer == null) return;

        

        // Ensure we target the list itself, not the parent container if they share the class
        VisualElement list = handContainer.ClassListContains("reorderable-list") ? handContainer : handContainer.Q<VisualElement>(className: "reorderable-list");
        if (list == null) list = handContainer;

        var slots = list.Query<VisualElement>(className: "card-slot").ToList();
        int count = slots.Count;
        if (count == 0) return;

        // Save original sorted indices before fanning to maintain stable arc math
        if (_originalIndices.Count == 0)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].userData = i;
            }
        }

        // Sort slots by their semantic index (stored in userData)
        slots.Sort((a, b) =>
        {
            int indexA = a.userData is int ia ? ia : 0;
            int indexB = b.userData is int ib ? ib : 0;
            return indexA.CompareTo(indexB);
        });

        float containerWidth = list.resolvedStyle.width;
        if (float.IsNaN(containerWidth) || containerWidth <= 0) containerWidth = 654f;

        float containerHeight = list.resolvedStyle.height;
        if (float.IsNaN(containerHeight) || containerHeight <= 0) containerHeight = 200f;

        float cardWidth = 110f;
        float cardHeight = 170f;

        float center = (count - 1) / 2f;
        float maxAngle = 12f; // Soft arc rotation limit at extreme edges
        float angleStep = count > 1 ? maxAngle * 2f / (count - 1) : 0f;

        float arcDepth = 15f; // Vertical curvature depth
        float overlapSpacing = 35f; // Squeezed cards

        // Inversion factor
        // sideFactor inverts the Y-offset and rotation
        float sideFactor = invert ? -1f : 1f;

        for (int i = 0; i < count; i++)
        {
            VisualElement slot = slots[i];
            slot.style.position = Position.Absolute;

            float offset = i - center;

            // Rotation: Inverted for opponent so they fan "outward" from their perspective
            float angle = offset * angleStep * sideFactor;

            // Curve: quadratic offset from center
            // For player (sideFactor 1): edges move down (+Y), center stays high. (Upward arc)
            // For opponent (sideFactor -1): edges move up (-Y), center stays low. (Downward arc)
            float normOffset = center > 0f ? offset / center : 0f;
            float yOffset = normOffset * normOffset * arcDepth * sideFactor;

            float xPos = (containerWidth / 2f) - (cardWidth / 2f) + (offset * overlapSpacing);
            float yPos = (containerHeight / 2f) - (cardHeight / 2f) + yOffset;

            slot.style.left = xPos;
            slot.style.top = yPos;
            slot.style.rotate = new Rotate(Angle.Degrees(angle));
            slot.style.translate = StyleKeyword.Null;
        }
    }

    public void ResetSlotsInOtherLists()
    {
        VisualElement gridLayout = _root.Q<VisualElement>("gridLayout");
        VisualElement verticalList = _root.Q<VisualElement>("verticalList");

        if (gridLayout != null)
        {
            var slots = gridLayout.Query<VisualElement>(className: "card-slot").ToList();
            foreach (var slot in slots)
            {
                slot.style.rotate = StyleKeyword.Null;
                slot.style.translate = StyleKeyword.Null;
                slot.style.position = StyleKeyword.Null;
                slot.style.left = StyleKeyword.Null;
                slot.style.top = StyleKeyword.Null;
            }
        }

        if (verticalList != null)
        {
            var slots = verticalList.Query<VisualElement>(className: "card-slot").ToList();
            foreach (var slot in slots)
            {
                slot.style.rotate = StyleKeyword.Null;
                slot.style.translate = StyleKeyword.Null;
                slot.style.position = StyleKeyword.Null;
                slot.style.left = StyleKeyword.Null;
                slot.style.top = StyleKeyword.Null;
            }
        }
    }

    private void HandleItemDropped(VisualElement cardContent, VisualElement targetSlot)
    {
        // Recalculate fanning and clear slots in other lists
        UpdateHorizontalFan();
        ResetSlotsInOtherLists();

        // Item is dropped in a slot. Play a subtle drop snap animation!
        StartCoroutine(AnimateDropSnap(cardContent));
        OnOrderChanged?.Invoke();
    }

    private IEnumerator AnimateDropSnap(VisualElement element)
    {
        // Scale up then snap back
        float elapsed = 0f;
        float duration = swapDuration;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveT = swapEase.Evaluate(t);
            float scale = Mathf.Lerp(1.15f, 1.0f, curveT);
            element.style.scale = new Scale(new Vector3(scale, scale, 1f));
            yield return null;
        }

        element.style.scale = StyleKeyword.Null;
    }

    private void HandleItemRemoveRequested(VisualElement cardContent)
    {
        _pendingDestroyCardContent = cardContent;

        if (showDestroyPrompt)
        {
            if (_destroyPromptOverlay != null)
            {
                _destroyPromptOverlay.RemoveFromClassList("hidden");
            }
            OnItemRemoveRequested?.Invoke(cardContent);
        }
        else
        {
            ConfirmRemoval();
        }
    }

    private void ConfirmRemoval()
    {
        if (_destroyPromptOverlay != null)
        {
            _destroyPromptOverlay.AddToClassList("hidden");
        }

        if (_pendingDestroyCardContent != null)
        {
            VisualElement slot = _pendingDestroyCardContent.parent;
            StartCoroutine(AnimateRemovalAndDestroy(_pendingDestroyCardContent, slot));
        }

        OnDestroyConfirmed?.Invoke();
    }

    private void CancelRemoval()
    {
        if (_destroyPromptOverlay != null)
        {
            _destroyPromptOverlay.AddToClassList("hidden");
        }

        // Snap back to slot
        if (_pendingDestroyCardContent != null)
        {
            StartCoroutine(AnimateDropSnap(_pendingDestroyCardContent));
        }

        _pendingDestroyCardContent = null;
        OnDestroyCancelled?.Invoke();
    }

    private IEnumerator AnimateRemovalAndDestroy(VisualElement element, VisualElement slot)
    {
        float elapsed = 0f;
        float duration = removalDuration;
        Vector3 startScale = Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float curveT = removalEase.Evaluate(t);
            float scale = Mathf.Lerp(1f, 0f, curveT);
            element.style.scale = new Scale(new Vector3(scale, scale, 1f));
            yield return null;
        }

        // Remove slot from list completely
        if (slot != null)
        {
            VisualElement parentList = slot.parent;
            if (parentList != null)
            {
                parentList.Remove(slot);
            }
        }

        // Recalculate fanning
        UpdateHorizontalFan();
        ResetSlotsInOtherLists();

        _pendingDestroyCardContent = null;
        OnOrderChanged?.Invoke();
    }
}
