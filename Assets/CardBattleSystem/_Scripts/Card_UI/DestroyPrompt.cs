using System;
using UnityEngine;
using UnityEngine.UI;

public class DestroyPrompt : MonoBehaviour
{
    [SerializeField] private Button destroyBtn;
    [SerializeField] private Button cancelBtn;
    [SerializeField] private CanvasGroup canvasGroup;

    private DraggableItem _item;

    public static event Action<DraggableItem> OnDestroyConfirmed;
    public static event Action<DraggableItem> OnDestroyCancelled;


    private void Awake()
    {
        ReorderableList.OnItemRemoveRequested += CallForItemDestruction;
        destroyBtn.onClick.AddListener(RemoveEntry);
        cancelBtn.onClick.AddListener(CancelRemove);
    }

    private void CallForItemDestruction(DraggableItem item)
    {
        _item = item;
        ToggleCanvasTo(true);
    }

    private void RemoveEntry()
    {
        ToggleCanvasTo(false);
        OnDestroyConfirmed?.Invoke(_item);
    }

    private void CancelRemove()
    {
        ToggleCanvasTo(false);
        OnDestroyCancelled?.Invoke(_item);
    }

    void ToggleCanvasTo(bool isVisible)
    {
        canvasGroup.alpha = isVisible ? 1 : 0;
        canvasGroup.blocksRaycasts = isVisible;
        canvasGroup.interactable = isVisible;
    }
    private void OnDestroy()
    {
        ReorderableList.OnItemRemoveRequested -= CallForItemDestruction;
        destroyBtn.onClick.RemoveListener(RemoveEntry);
        cancelBtn.onClick.RemoveListener(CancelRemove);
    }
}
