using Codice.Client.BaseCommands.Merge.Xml;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ReorderableList : MonoBehaviour
{

    enum CollapseStyle { Smooth, Discrete }

    [Header("Setup")]
    [SerializeField] private Canvas _rootCanvas;
    [SerializeField] private RectTransform thisContainer;

    [Space]
    [SerializeField] private List<DraggableItem> items = new();

    [Header("Other Lists")]
    [SerializeField] private bool elementsCanMoveToOtherLists;

    [Header("Swap Settings")]
    [SerializeField] private float swapDuration;
    [SerializeField] private AnimationCurve swapEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Removal Settings")]
    [SerializeField] private bool draggingOutRemovesItem;
    [SerializeField] private float distanceUntilRemoveItem = 100f;
    [SerializeField] bool showDestroyPrompt = true;

    [Header("Removal Animation")]
    [SerializeField] private float removalDuration = 0.2f;
    [SerializeField] private AnimationCurve removalEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

   
    public Canvas RootCanvas => _rootCanvas;
    public IReadOnlyList<DraggableItem> Items => items;

    RectTransform _draggedItem;
    int _currentIndex;
    bool _removalPending;
    CollapseStyle _collapseStyle;

    public static event Action<IReadOnlyList<DraggableItem>> OnOrderChanged;
    public static event Action<DraggableItem> OnItemRemoveRequested;


    private void Reset()
    {
        thisContainer = GetComponent<RectTransform>();
        _rootCanvas = GetComponent<Canvas>();
        items = GetComponentsInChildren<DraggableItem>().ToList();
    }

    private void Awake()
    {
        _collapseStyle = thisContainer.GetComponent<GridLayoutGroup>() != null ? CollapseStyle.Discrete: CollapseStyle.Smooth;

        DestroyPrompt.OnDestroyCancelled += CancelRemove;
        DestroyPrompt.OnDestroyConfirmed += ConfirmRemove;

        foreach (var item in items)
        {
            item.Initialize(swapEase, swapDuration, this, _collapseStyle == CollapseStyle.Discrete, elementsCanMoveToOtherLists);
        }
    }

    public void AddItem(DraggableItem item, int index = -1)
    {
        item.RectTransform.SetParent(thisContainer, false);
        item.Initialize(swapEase, swapDuration, this, _collapseStyle == CollapseStyle.Discrete, elementsCanMoveToOtherLists);

        if (index < 0)
        {
            items.Add(item);
            item.RectTransform.SetAsLastSibling();
        }
        else
        {
            index = Mathf.Clamp(index, 0, items.Count);
            items.Insert(index, item);
            item.RectTransform.SetSiblingIndex(index);
        }

        OnOrderChanged?.Invoke(items);
    }

    public void RemoveItem(DraggableItem item)
    {
        if (!items.Contains(item)) return;
        if (_draggedItem == item.RectTransform) _draggedItem = null;

        int removedIndex = items.IndexOf(item);
        items.Remove(item);
        OnOrderChanged?.Invoke(items);

        item.AnimateRemoval(removalEase, removalDuration);

        if (_collapseStyle == CollapseStyle.Discrete)
        {
            AnimateNeighborsFrom(removedIndex);
        }
    }

    private void AnimateNeighborsFrom(int startIndex)
    {
        for (int i = Mathf.Max(startIndex, 0); i  < items.Count; i ++)
        {
            items[i].AnimateItemToContainer(_rootCanvas);
        }
    }

    public void OnItemDragStarted(DraggableItem draggableItem)
    {
        _draggedItem = draggableItem.RectTransform;
        _currentIndex = items.IndexOf(draggableItem);
    }

    internal void OnItemBeingDragged(Vector2 position)
    {
        if (_draggedItem == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(thisContainer, position, _rootCanvas.worldCamera, out Vector2 localPoint);

        bool wantsRemoval = draggingOutRemovesItem && IsOutsideRemovalThreshold(localPoint);

        if (wantsRemoval != _removalPending)
        {
            _removalPending = wantsRemoval;
            if (wantsRemoval)
            {
                items[_currentIndex].CollapsePlaceholder();
            }
            else
            {
                items[_currentIndex].ExpandPlaceholder();
            }

            if (_collapseStyle == CollapseStyle.Discrete)
            {
                AnimateNeighborsFrom(_currentIndex + 1);
            }
        }

        if (_removalPending) return;

        int targetIndex = GetTargetIndex(position, _currentIndex);
        if (targetIndex != _currentIndex) 
        {
            MoveItem(_currentIndex, targetIndex);
        }

    }

    public bool IsOutsideRemovalThreshold(Vector2 localPoint)
    {
        if(thisContainer.rect.Contains(localPoint)) return false;

        float clampedX = Mathf.Clamp(localPoint.x, thisContainer.rect.xMin, thisContainer.rect.xMax);
        float clampedY = Mathf.Clamp(localPoint.y, thisContainer.rect.yMin, thisContainer.rect.yMax);
        Vector2 closestPoint = new Vector2(clampedX, clampedY);

        return Vector2.Distance(closestPoint, localPoint) > distanceUntilRemoveItem;
    }

    public void OnItemDragEnded(DraggableItem draggableItem, ReorderableList destinationList, Vector2 position)
    {
        if(_draggedItem == null) return;
        _draggedItem = null;

        if (destinationList != null && destinationList != this)
        {
            _removalPending = false;
            TransferItemTo(draggableItem, destinationList, position);
            return;
        }

        if (_removalPending) 
        {
            _removalPending = false;
            RequestRemoval(draggableItem);
            return;
        }

        draggableItem.ReturnItemToContainer();
        OnOrderChanged?.Invoke(items);
    }

    

    private void TransferItemTo(DraggableItem draggableItem, ReorderableList destinationList, Vector2 position)
    {
        if(!items.Contains(draggableItem)) return;

        draggableItem.ExpandPlaceholder();
        items.Remove(draggableItem);
        OnOrderChanged?.Invoke(items);

        int destinationIndex = destinationList.GetTargetIndexForScreenPosition(position);

        destinationList.AddItem(draggableItem, destinationIndex);

        draggableItem.ReturnItemToContainer();

        if (_collapseStyle == CollapseStyle.Discrete)
        {
            AnimateNeighborsFrom(_currentIndex);
        }

    }

    private int GetTargetIndexForScreenPosition(Vector2 position)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(thisContainer, position, _rootCanvas.worldCamera, out Vector2 localPoint);

        return GetTargetIndex(localPoint, 0);
    }

    private void MoveItem(int currentIndex, int targetIndex)
    {
       if(currentIndex == targetIndex) return;

        DraggableItem moved = items[currentIndex];

        items.RemoveAt(currentIndex);
        items.Insert(targetIndex, moved);

        int low = Mathf.Min(currentIndex, targetIndex);
        int high = Mathf.Max(currentIndex, targetIndex);

        for (int i = low; i < high; i++)
        {
            items[i].RectTransform.SetSiblingIndex(i);
            if (items[i] != moved)
                items[i].AnimateItemToContainer(_rootCanvas);
        }

        _currentIndex = targetIndex;
    }

    private int GetTargetIndex(Vector2 position, int fallback)
    {
        if(items.Count == 0) return 0;
        int nearest = fallback;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < items.Count; i++)
        {
            if (RectContainsLocalPoint(items[i].RectTransform, position))
                return i;
        }

        for (int i = 0; i < items.Count; i++)
        {
            float sqrDistance = ((Vector2)items[i].RectTransform.localPosition - position).sqrMagnitude;
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                nearest = i;
            }
        }

            return nearest;

    }

    private bool RectContainsLocalPoint(RectTransform rectTransform, Vector2 position)
    {
        Vector2 offset = position - (Vector2)rectTransform.localPosition;
        return Mathf.Abs(offset.x) <= rectTransform.rect.width * 0.5f &&
            Mathf.Abs(offset.y) <= rectTransform.rect.height * 0.5f;
    }

    private void RequestRemoval(DraggableItem draggableItem)
    {
        if(_draggedItem == draggableItem.RectTransform) 
            _draggedItem = null;

        if(showDestroyPrompt)
        {
            OnItemRemoveRequested?.Invoke(draggableItem);
        }
        else
        {
            ConfirmRemove(draggableItem);
        }
    }

    private void ConfirmRemove(DraggableItem item)
    {
        _removalPending = false;
        RemoveItem(item);
    }

    private void CancelRemove(DraggableItem item)
    {
        if (!items.Contains(item)) return;
        _removalPending = false;

        item.ExpandPlaceholder();
        item.ReturnItemToContainer();

        if(_collapseStyle == CollapseStyle.Discrete)
        {
            AnimateNeighborsFrom(items.IndexOf(item) + 1);
        }
    }

    private void OnDestroy()
    {
        DestroyPrompt.OnDestroyCancelled -= CancelRemove;
        DestroyPrompt.OnDestroyConfirmed -= ConfirmRemove;
    }

    [ContextMenu("Grab List Entries")]
    void GrabListItems()
    {
        items = GetComponentsInChildren<DraggableItem>().ToList();
    }
}
