using System;
using UnityEngine;
using UnityEngine.UIElements;

public class CardDragManipulator : PointerManipulator
{
    private Vector2 _pointerDownPosition;
    private Vector2 _lastPointerPosition;
    private bool _isDragging;
private VisualElement _originalParent; // This is the .card-slot element
    private VisualElement _rootPanel;
    private bool _removalPending;
    private float _distanceUntilRemoveItem = 100f;
    private bool _draggingOutRemovesItem = false;
    private bool _elementsCanMoveToOtherLists = true;

    // Events
    public static event Action<VisualElement, VisualElement> OnItemDropped; // (cardContent, targetSlot)
    public static event Action<VisualElement> OnItemRemoveRequested; // (cardContent)
    public static event Action<VisualElement, Vector2> OnWorldDropped; // (cardContent, screenPosition)

    public CardDragManipulator(bool elementsCanMoveToOtherLists, bool draggingOutRemovesItem, float distanceUntilRemoveItem)
    {
        _elementsCanMoveToOtherLists = elementsCanMoveToOtherLists;
        _draggingOutRemovesItem = draggingOutRemovesItem;
        _distanceUntilRemoveItem = distanceUntilRemoveItem;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (_isDragging) return;

        _pointerDownPosition = evt.position;
        _isDragging = true;
        _removalPending = false;
        _originalParent = target.parent;

        var root = target.panel.visualTree;
        _rootPanel = root.Q<VisualElement>("root") ?? root;

        // Remember dimensions
        float width = target.resolvedStyle.width;
        float height = target.resolvedStyle.height;

        // Parent to root panel to render on top of everything
        _rootPanel.Add(target);
        target.AddToClassList("card-content-dragging");
        target.style.width = width;
        target.style.height = height;
        target.pickingMode = PickingMode.Ignore;
        foreach (var child in target.Children())
        {
            child.pickingMode = PickingMode.Ignore;
        }

        // Set initial translation
        UpdatePosition(evt.position);

        target.CapturePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!_isDragging || !target.HasPointerCapture(evt.pointerId)) return;

        _lastPointerPosition = evt.position;
        UpdatePosition(evt.position);

        // Find what element is under the pointer
        VisualElement hovered = target.panel.Pick(evt.position);
        
        // Analyze the target
        VisualElement hoveredSlot = FindAncestorWithClass(hovered, "card-slot");
        VisualElement hoveredList = FindAncestorWithClass(hovered, "reorderable-list");

        bool insideValidList = hoveredList != null;

        // Prevent dragging into opponent-owned areas
        if (insideValidList && (hoveredList.ClassListContains("opponent-hand") || hoveredList.ClassListContains("opponent-board")))
        {
            insideValidList = false;
        }

        // If elements can't move to other lists, check if hoveredList is the same as the original list
        if (insideValidList && !_elementsCanMoveToOtherLists)
        {
            VisualElement originalList = FindAncestorWithClass(_originalParent, "reorderable-list");
            if (hoveredList != originalList)
            {
                insideValidList = false;
            }
        }

        // Drag out removal logic
        if (_draggingOutRemovesItem)
        {
            bool wantsRemoval = !insideValidList;
            if (!insideValidList && hoveredList == null)
            {
                // Let's compute distance to nearest list
                float minDistance = float.MaxValue;
                var lists = _rootPanel.Query<VisualElement>(className: "reorderable-list").ToList();
                foreach (var list in lists)
                {
                    Vector2 closestPoint = ClampPointToRect(evt.position, list.worldBound);
                    float dist = Vector2.Distance(evt.position, closestPoint);
                    if (dist < minDistance) minDistance = dist;
                }

                wantsRemoval = minDistance > _distanceUntilRemoveItem;
            }

            if (wantsRemoval != _removalPending)
            {
                _removalPending = wantsRemoval;
                if (_removalPending)
                {
                    _originalParent.AddToClassList("card-slot-collapsed");
                }
                else
                {
                    _originalParent.RemoveFromClassList("card-slot-collapsed");
                }
            }
        }

        // If we are not removing, update slot/placeholder position in real-time!
        if (!_removalPending && insideValidList)
        {
            if (hoveredSlot != null && hoveredSlot != _originalParent)
            {
                VisualElement parentList = hoveredSlot.parent;
                int hoveredIndex = parentList.IndexOf(hoveredSlot);
                int placeholderIndex = parentList.IndexOf(_originalParent);

                if (parentList == _originalParent.parent)
                {
                    // Reorder within the same list
                    if (placeholderIndex < hoveredIndex)
                    {
                        parentList.Insert(hoveredIndex, _originalParent);
                    }
                    else
                    {
                        parentList.Insert(hoveredIndex, _originalParent);
                    }
                }
                else
                {
                    // Move to a different list at the hovered position
                    parentList.Insert(hoveredIndex, _originalParent);
                }
            }
            else if (hoveredList != null && hoveredList != _originalParent.parent)
            {
                // Hovered empty list or list with cards, but not on a specific slot
                if (hoveredList.childCount == 0)
                {
                    hoveredList.Add(_originalParent);
                }
                else
                {
                    // Find closest slot in this list and insert
                    int closestIndex = GetClosestSlotIndex(evt.position, hoveredList);
                    if (closestIndex >= 0 && closestIndex < hoveredList.childCount)
                    {
                        hoveredList.Insert(closestIndex, _originalParent);
                    }
                    else
                    {
                        hoveredList.Add(_originalParent);
                    }
                }
            }
        }

        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!_isDragging || !target.HasPointerCapture(evt.pointerId)) return;

        target.ReleasePointer(evt.pointerId);
        evt.StopPropagation();
    }

    private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        if (!_isDragging) return;
        _isDragging = false;

        target.pickingMode = PickingMode.Position;
        foreach (var child in target.Children())
        {
            child.pickingMode = PickingMode.Position;
        }

        if (_removalPending)
        {
            // Reset collapse so that if cancel, it's ready, but keep overlay prompt
            _originalParent.RemoveFromClassList("card-slot-collapsed");
            
            // Re-parent to original parent for now, but hidden if prompt is shown
            _originalParent.Add(target);
            ResetTargetStyles();

            // Trigger removal request!
            OnItemRemoveRequested?.Invoke(target);
            OnWorldDropped?.Invoke(target, _lastPointerPosition);
        }
else
        {
            // Drop successfully
            _originalParent.Add(target);
            ResetTargetStyles();
            OnItemDropped?.Invoke(target, _originalParent);
        }
    }

    private void UpdatePosition(Vector2 pointerPosition)
    {
        // Compute position relative to root panel
        Vector2 localPos = _rootPanel.WorldToLocal(pointerPosition);
        
        // Offset so pointer is at center of the card
        float halfWidth = target.resolvedStyle.width / 2f;
        if (float.IsNaN(halfWidth) || halfWidth <= 0) halfWidth = 55f; // fallback
        float halfHeight = target.resolvedStyle.height / 2f;
        if (float.IsNaN(halfHeight) || halfHeight <= 0) halfHeight = 85f; // fallback

        target.style.left = localPos.x - halfWidth;
        target.style.top = localPos.y - halfHeight;
    }

    private void ResetTargetStyles()
    {
        target.RemoveFromClassList("card-content-dragging");
        target.style.width = StyleKeyword.Null;
        target.style.height = StyleKeyword.Null;
        target.style.left = StyleKeyword.Null;
        target.style.top = StyleKeyword.Null;
        target.style.translate = StyleKeyword.Null;
    }

    private VisualElement FindAncestorWithClass(VisualElement element, string className)
    {
        while (element != null)
        {
            if (element.ClassListContains(className)) return element;
            element = element.parent;
        }
        return null;
    }

    private int GetClosestSlotIndex(Vector2 position, VisualElement list)
    {
        int nearestIndex = -1;
        float minSqrDistance = float.MaxValue;

        for (int i = 0; i < list.childCount; i++)
        {
            VisualElement slot = list[i];
            if (slot == _originalParent) continue;

            float dist = Vector2.SqrMagnitude(position - (Vector2)slot.worldBound.center);
            if (dist < minSqrDistance)
            {
                minSqrDistance = dist;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private Vector2 ClampPointToRect(Vector2 point, Rect rect)
    {
        float x = Mathf.Clamp(point.x, rect.xMin, rect.xMax);
        float y = Mathf.Clamp(point.y, rect.yMin, rect.yMax);
        return new Vector2(x, y);
    }
}
