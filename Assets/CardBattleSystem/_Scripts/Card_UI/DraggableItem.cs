using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private float dragThreshold;

    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private RectTransform contents;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private LayoutElement tempLayout;

    public RectTransform RectTransform => rectTransform;

    AnimationCurve _swapEase = AnimationCurve.EaseInOut(0, 0, 1, 1);
    float _swapDuration = 0.15f;
    ReorderableList _List;
    bool _moveableToOtherContainers;
    Canvas _rootcanvas => _List.RootCanvas;

    Coroutine _swapCoroutine;
    Vector2 _pointerDownPosition;
    Vector2 _dragOffset;
    bool _isDragging;
    Canvas _dragCanvas;

    //Placeholder for Collapsing Lists
    Coroutine _resizeCoroutine;
    Vector2 _naturalSize;
    Vector2 _offsetMin;
    Vector2 _offsetMax;
    bool _discretePlaceholder;

    public static event Action<DraggableItem> OnBeingRemoved;

    private void Reset()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        tempLayout = GetComponent<LayoutElement>();
        if(transform.childCount > 0)
        {
            contents = transform.GetChild(0).GetComponent<RectTransform>();
        }
    }

    private void Awake()
    {
        _offsetMin = contents.offsetMin;
        _offsetMax = contents.offsetMax;
    }

    public void Initialize(AnimationCurve swapEase, float swapDuration, ReorderableList list, bool discretePlaceholder, bool moveableToOtherContainers)
    {
        _swapEase = swapEase;
        _swapDuration = swapDuration;
        _List = list;
        _moveableToOtherContainers = moveableToOtherContainers;
        _discretePlaceholder = discretePlaceholder;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerDownPosition = eventData.position;
    }

    private void BeginDrag(PointerEventData eventData)
    {        
        _isDragging = true;
        _List.OnItemDragStarted(this);
        _naturalSize = rectTransform.rect.size;

        _dragOffset = (Vector2)contents.position - eventData.position;
        contents.SetParent(_rootcanvas.transform, true);

        canvasGroup.blocksRaycasts = false;

        _dragCanvas = contents.gameObject.AddComponent<Canvas>();
        _dragCanvas.overrideSorting = true;
        _dragCanvas.sortingOrder = 999;
    }

    public void OnDrag(PointerEventData eventData)
    {
        
        if (!_isDragging && Vector2.Distance(eventData.position, _pointerDownPosition) > dragThreshold)
        {            
            BeginDrag(eventData);
        }

        contents.position = eventData.position + _dragOffset;
        _List.OnItemBeingDragged(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
       
        if(!_isDragging) return;
        _isDragging = false;

        ReorderableList sourceList = _List;
        ReorderableList destinationList = sourceList;

        if(_moveableToOtherContainers)
        {
            var hoveringOverObject = eventData.pointerCurrentRaycast.gameObject;
            destinationList = hoveringOverObject != null ? hoveringOverObject.GetComponentInParent<ReorderableList>() : null;
        }

        canvasGroup.blocksRaycasts = true;

        if(_dragCanvas != null) 
        {
            Destroy(_dragCanvas);
            _dragCanvas = null;
        }

        sourceList.OnItemDragEnded(this, destinationList, eventData.position);
    }


    public void ReturnItemToContainer()
    {
        if(_swapCoroutine != null)
        {
            StopCoroutine(_swapCoroutine);
            _swapCoroutine = null;
        }

        contents.SetParent(rectTransform, false);
        contents.offsetMin = _offsetMin;
        contents.offsetMax = _offsetMax;
    }

    public void AnimateItemToContainer(Canvas root)
    {
        if (_swapCoroutine != null)
        {
            StopCoroutine(_swapCoroutine);
        }
        _swapCoroutine = StartCoroutine(LerpItemToContainer());
    }

    IEnumerator LerpItemToContainer()
    {
       var startPos = contents.position;
        float elapsedTime = 0f;

        while(elapsedTime < _swapDuration)
        {
            elapsedTime += Time.deltaTime;
            float easedT = _swapEase.Evaluate(Mathf.Clamp01(elapsedTime / _swapDuration));
            contents.position = Vector2.LerpUnclamped(startPos, rectTransform.position, easedT);
            yield return null;
        }

        ReturnItemToContainer();
    }

    public void CollapsePlaceholder()
    {
        if (_discretePlaceholder)
        {
            tempLayout.ignoreLayout = true;
            return;
        }

        StartPlaceholderAnimation(0f);
    }

    public void ExpandPlaceholder()
    {
        if (_discretePlaceholder)
        {
            tempLayout.ignoreLayout = false;
            return;
        }
        StartPlaceholderAnimation(1f);
    }

    private void StartPlaceholderAnimation(float targetScale)
    {
        if(_resizeCoroutine != null)
        {
            StopCoroutine(_resizeCoroutine);
        }
        _resizeCoroutine = StartCoroutine(AnimateContainer(targetScale));
    }

    IEnumerator AnimateContainer(float targetScale)
    {      
       
        float startWidth = tempLayout.preferredWidth < 0 ? _naturalSize.x : tempLayout.preferredWidth;
        float startHeight = tempLayout.preferredHeight < 0 ? _naturalSize.y : tempLayout.preferredHeight;

        float targetHeight = _naturalSize.x * targetScale;
        float targetWidth = _naturalSize.y * targetScale;

        float elapsedTime = 0f;
        while (elapsedTime < _swapDuration)
        {
            elapsedTime += Time.deltaTime;
            float easedT = _swapEase.Evaluate(Mathf.Clamp01(elapsedTime / _swapDuration));
            
            float width = Mathf.LerpUnclamped(startWidth, targetWidth, easedT);
            float height = Mathf.LerpUnclamped(startHeight, targetHeight, easedT);

            tempLayout.preferredWidth = width;
            tempLayout.preferredHeight = height;
            tempLayout.minWidth = width;
            tempLayout.minHeight = height;

            yield return null;
        }

        tempLayout.preferredWidth = targetWidth;
        tempLayout.preferredHeight = targetHeight;
        tempLayout.minWidth = targetWidth;
        tempLayout.minHeight = targetHeight;

        _resizeCoroutine = null;
    }

    public void AnimateRemoval(AnimationCurve curve, float duration)
    {
        if(_swapCoroutine != null) StopCoroutine(_swapCoroutine);
        if(_resizeCoroutine != null) StopCoroutine(_resizeCoroutine);

        canvasGroup.blocksRaycasts = false;

        Vector3 worldPos = contents.position;

        rectTransform.SetParent(_rootcanvas.transform, true);
        rectTransform.position = worldPos;

        contents.SetParent(rectTransform, true);
        contents.anchoredPosition = Vector2.zero;

        var deathCanvas = gameObject.AddComponent<Canvas>();
        deathCanvas.overrideSorting = true;
        deathCanvas.sortingOrder = 999;

        StartCoroutine(ShrinkAndDestroy(curve, duration));

    }

    private IEnumerator ShrinkAndDestroy(AnimationCurve curve, float duration)
    {
        var startScale = rectTransform.localScale;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float easedT = curve.Evaluate(Mathf.Clamp01(elapsedTime / duration));
            rectTransform.localScale = Vector3.LerpUnclamped(startScale, Vector3.zero, easedT);
            yield return null;
        }
        rectTransform.localScale = Vector3.zero;
        OnBeingRemoved?.Invoke(this);
        Destroy(gameObject);
    }
} 
