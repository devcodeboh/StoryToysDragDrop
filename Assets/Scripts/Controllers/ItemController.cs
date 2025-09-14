using System;
using System.Collections;
using UnityEngine;

namespace StoryToys.DragDrop
{
    [RequireComponent(typeof(Collider2D))]
    public class ItemController : MonoBehaviour
    {
        public enum ItemState { Idle, Dragging, Returning, Equipping, Equipped }

        [Tooltip("Start position the item returns to")]
        private Transform startTransform;

        private IDropStrategy equipStrategy;
        private IDropStrategy returnStrategy;
        private IUIService uiService;

        private ItemState state = ItemState.Idle;
        private Vector3 pickOffset;
        private Coroutine moveCoroutine;
        private Vector3 startPosition;

        public void Init(IDropStrategy equip, IDropStrategy ret, IUIService ui)
        {
            equipStrategy = equip;
            returnStrategy = ret;
            uiService = ui;

            startPosition = startTransform ? startTransform.position : transform.position;
            transform.position = startPosition;
            state = ItemState.Idle;
        }

        public void OnPick(Vector3 worldPos)
        {
            if (state != ItemState.Idle) return;
            pickOffset = transform.position - worldPos;
            SetState(ItemState.Dragging);
            if (moveCoroutine != null) { StopCoroutine(moveCoroutine); moveCoroutine = null; }
            TutorialHintButton.Hide();
        }

        public void OnDrag(Vector3 worldPos)
        {
            if (state != ItemState.Dragging) return;
            transform.position = worldPos + pickOffset;
        }

        public void OnDrop(Vector3 worldPos)
        {
            if (state != ItemState.Dragging) return;
            var hits = Physics2D.OverlapPointAll(worldPos);
            EquipSlot foundSlot = null; Transform slotTransform = null;
            for (int i = 0; i < hits.Length; i++)
            {
                var slot = hits[i].GetComponentInParent<EquipSlot>();
                if (slot != null) { foundSlot = slot; slotTransform = hits[i].transform; break; }
            }
            if (foundSlot != null)
            {
                Vector3 target = foundSlot.anchor != null ? foundSlot.anchor.position : slotTransform.position;
                equipStrategy?.Execute(this, target);
            }
            else
            {
                Vector3 target = startTransform ? startTransform.position : startPosition;
                returnStrategy?.Execute(this, target);
            }
        }

        public void ResetImmediate()
        {
            if (moveCoroutine != null) { StopCoroutine(moveCoroutine); moveCoroutine = null; }
            transform.position = startTransform ? startTransform.position : startPosition;
            SetState(ItemState.Idle);
            uiService?.ShowResetButton(false);
            TutorialHintButton.ShowIfAvailable();
        }

        public void SmoothMove(Vector3 target, float speed, Action onComplete)
        {
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(SmoothMoveRoutine(target, speed, onComplete));
        }

        private IEnumerator SmoothMoveRoutine(Vector3 target, float speed, Action onComplete)
        {
            Vector3 start = transform.position; float t = 0f; float duration = 1f / Mathf.Max(0.0001f, speed);
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
                transform.position = Vector3.Lerp(start, target, eased);
                yield return null;
            }
            transform.position = target; onComplete?.Invoke();
        }

        public IEnumerator PunchScale(float duration = 0.12f, float magnitude = 0.05f)
        {
            Vector3 baseScale = transform.localScale; float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime; float p = t / duration; float s = 1f + Mathf.Sin(p * Mathf.PI) * magnitude;
                transform.localScale = baseScale * s; yield return null;
            }
            transform.localScale = baseScale;
        }

        public IEnumerator Shake(float duration = 0.1f, float magnitude = 0.05f)
        {
            Vector3 origin = transform.position; float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime; float strength = (1f - Mathf.Clamp01(t / duration));
                float ox = (UnityEngine.Random.value * 2f - 1f) * magnitude * strength;
                float oy = (UnityEngine.Random.value * 2f - 1f) * magnitude * strength;
                transform.position = origin + new Vector3(ox, oy, 0f); yield return null;
            }
            transform.position = origin;
        }

        public void SetState(ItemState newState) => state = newState;
        public ItemState GetState() => state;
    }
}
