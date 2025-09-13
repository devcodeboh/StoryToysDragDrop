using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ItemController : MonoBehaviour
{
    public enum ItemState { Idle, Dragging, Returning, Equipped }

    [Header("References")]
    [SerializeField] private Transform startTransform;   // точка возврата (нижний левый угол)
    [SerializeField] private Transform equipTransform;   // точка экипировки (на торсе)

    [Header("Config")]
    [SerializeField] private float defaultMoveSpeed = 6f;

    private IDropStrategy equipStrategy;
    private IDropStrategy returnStrategy;
    private IUIService uiService;

    private ItemState state = ItemState.Idle;
    private Vector3 pickOffset;
    private Coroutine moveCoroutine;

    // --- DI из GameContext ---
    public void Init(IDropStrategy equip, IDropStrategy ret, IUIService ui)
    {
        equipStrategy = equip;
        returnStrategy = ret;
        uiService = ui;

        if (startTransform)
            transform.position = startTransform.position;

        state = ItemState.Idle;
    }

    // --- Ввод (из DragInput) ---
    public void OnPick(Vector3 worldPos)
    {
        if (state != ItemState.Idle) return;

        pickOffset = transform.position - worldPos;
        SetState(ItemState.Dragging);

        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }
    }

    public void OnDrag(Vector3 worldPos)
    {
        if (state != ItemState.Dragging) return;
        transform.position = worldPos + pickOffset;
    }

    public void OnDrop(Vector3 worldPos)
    {
        if (state != ItemState.Dragging) return;
        SetState(ItemState.Idle);

        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        if (hit != null && hit.CompareTag("Torso"))
        {
            Vector3 target = equipTransform ? equipTransform.position : hit.transform.position;
            equipStrategy?.Execute(this, target);
        }
        else
        {
            Vector3 target = startTransform ? startTransform.position : Vector3.zero;
            returnStrategy?.Execute(this, target);
        }
    }

    // --- Reset (по кнопке) ---
    public void ResetImmediate()
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine);
            moveCoroutine = null;
        }

        transform.position = startTransform ? startTransform.position : Vector3.zero;
        SetState(ItemState.Idle);
        uiService?.ShowResetButton(false);
    }

    // --- Smooth move (для стратегий) ---
    public Coroutine SmoothMove(Vector3 target, float speed, Action onComplete)
    {
        return StartCoroutine(SmoothMoveRoutine(target, speed, onComplete));
    }

    private IEnumerator SmoothMoveRoutine(Vector3 target, float speed, Action onComplete)
    {
        Vector3 start = transform.position;
        float t = 0f;
        float duration = 1f / Mathf.Max(0.0001f, speed);

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        transform.position = target;
        onComplete?.Invoke();
    }

    // --- FSM ---
    public void SetState(ItemState newState) => state = newState;
    public ItemState GetState() => state;
}
