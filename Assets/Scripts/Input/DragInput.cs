using UnityEngine;

namespace StoryToys.DragDrop
{
    public class DragInput : MonoBehaviour
    {
        private ItemController currentItem;
        private Camera mainCam;
        private int activeTouchId = -1;
        private EquipSlot hoverSlot;
        [SerializeField] private float slotProximity = 0.6f;
        [SerializeField] private float visibilityRange = 2.0f;
        [SerializeField, Range(0f,1f)] private float minLevelOnDrag = 0.40f; // яркий базовый свет сразу после захвата
        [SerializeField, Range(0.1f,1f)] private float approachBoostExp = 0.5f; // <1 => усиливает при приближении
        [SerializeField, Range(0f,1f)] private float progressBoostExp = 0.6f; // буст по прогрессу пути к цели
        private EquipSlot[] slots;
        private EquipSlot primarySlot;
        private float pickDistanceRef = -1f; // дистанция от точки захвата до цели (anchor)

        private void Awake()
        {
            mainCam = Camera.main; if (mainCam == null) mainCam = Camera.current;
            slots = Object.FindObjectsOfType<EquipSlot>();
            primarySlot = (slots != null && slots.Length > 0) ? slots[0] : null;
        }

        private void Update()
        {
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);
                    Vector3 wp = ScreenToWorld(touch.position);
                    switch (touch.phase)
                    {
                        case TouchPhase.Began:
                            TryPick(wp, touch.fingerId);
                            if (currentItem != null) UpdateHoverByProximity(currentItem, true);
                            break;
                        case TouchPhase.Moved:
                        case TouchPhase.Stationary:
                            if (currentItem != null && activeTouchId == touch.fingerId)
                            { currentItem.OnDrag(wp); UpdateHoverByProximity(currentItem, false); }
                            break;
                        case TouchPhase.Ended:
                        case TouchPhase.Canceled:
                            if (currentItem != null && activeTouchId == touch.fingerId)
                            { ClearHover(); currentItem.OnDrop(wp); currentItem = null; activeTouchId = -1; pickDistanceRef = -1f; }
                            break;
                    }
                }
                return;
            }

            if (Input.GetMouseButtonDown(0))
                TryPick(ScreenToWorld(Input.mousePosition), -1);
            if (currentItem != null && activeTouchId == -1)
                UpdateHoverByProximity(currentItem, true);

            if (Input.GetMouseButton(0) && currentItem != null && activeTouchId == -1)
            {
                var wp = ScreenToWorld(Input.mousePosition);
                currentItem.OnDrag(wp);
                UpdateHoverByProximity(currentItem, false);
            }

            if (Input.GetMouseButtonUp(0) && currentItem != null && activeTouchId == -1)
            {
                var wpUp = ScreenToWorld(Input.mousePosition);
                ClearHover(); currentItem.OnDrop(wpUp); currentItem = null; pickDistanceRef = -1f;
            }
        }

        private void TryPick(Vector3 worldPos, int touchId)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit == null) return;
            var item = hit.GetComponent<ItemController>() ?? hit.GetComponentInParent<ItemController>();
            if (item == null) return;
            if (item.GetState() != ItemController.ItemState.Idle) return;
            if (!TutorialGate.AllowPick(item)) return;
            currentItem = item; activeTouchId = touchId; currentItem.OnPick(worldPos);
            if (primarySlot != null)
            {
                var target = primarySlot.anchor != null ? primarySlot.anchor.position : primarySlot.transform.position;
                pickDistanceRef = Vector3.Distance(currentItem.transform.position, target);
                if (pickDistanceRef < 0.0001f) pickDistanceRef = 0.0001f;
            }
        }

        private void UpdateHoverByProximity(ItemController item, bool justPicked)
        {
            var itemCol = item.GetComponent<Collider2D>() ?? item.GetComponentInChildren<Collider2D>();
            EquipSlot newSlot = null; float best = float.MaxValue;
            if (itemCol != null && slots != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    var slot = slots[i]; if (slot == null) continue; var slotCol = slot.GetComponent<Collider2D>(); if (slotCol == null) continue;
                    var dist = slotCol.Distance(itemCol); float d = dist.isOverlapped ? 0f : dist.distance;
                    if (d < best) { best = d; newSlot = slot; }
                }
            }
            float level = 0f;
            if (newSlot != null)
            {
                float closeRange = Mathf.Max(0.0001f, slotProximity);
                float nearFactor = best <= 0f ? 1f : Mathf.Clamp01(1f - (best / closeRange));
                float boosted = Mathf.Pow(nearFactor, approachBoostExp);
                float baseline = (currentItem != null) ? minLevelOnDrag : 0f; // сразу светится после захвата
                float progress = 0f;
                if (currentItem != null && primarySlot != null && pickDistanceRef > 0f)
                {
                    var target = primarySlot.anchor != null ? primarySlot.anchor.position : primarySlot.transform.position;
                    float cur = Vector3.Distance(currentItem.transform.position, target);
                    progress = Mathf.Clamp01(1f - (cur / pickDistanceRef));
                }
                float progressBoost = Mathf.Pow(Mathf.Max(0f, progress), progressBoostExp);
                level = Mathf.Max(baseline, Mathf.Max(boosted, progressBoost));
            }

            if (newSlot != hoverSlot)
            {
                if (hoverSlot != null) { hoverSlot.SetHighlightLevel(0f); hoverSlot.SetDrawOnTop(false); }
                hoverSlot = newSlot;
            }
            if (hoverSlot != null)
            {
                hoverSlot.SetHighlightLevel(level);
                hoverSlot.SetDrawOnTop(false);
            }
        }

        private void ClearHover()
        {
            if (hoverSlot != null) { hoverSlot.SetDrawOnTop(false); hoverSlot.SetHighlight(false); hoverSlot = null; }
        }

        private Vector3 ScreenToWorld(Vector3 screenPos)
        {
            var p = mainCam.ScreenToWorldPoint(screenPos); p.z = 0f; return p;
        }
    }
}

