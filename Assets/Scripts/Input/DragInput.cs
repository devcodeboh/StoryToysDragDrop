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
        [SerializeField, Range(0f,1f)] private float minLevelOnDrag = 0.12f;
        private EquipSlot[] slots;

        private void Awake()
        {
            mainCam = Camera.main; if (mainCam == null) mainCam = Camera.current;
            slots = Object.FindObjectsOfType<EquipSlot>();
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
                            { ClearHover(); currentItem.OnDrop(wp); currentItem = null; activeTouchId = -1; }
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
            { ClearHover(); currentItem.OnDrop(ScreenToWorld(Input.mousePosition)); currentItem = null; }
        }

        private void TryPick(Vector3 worldPos, int touchId)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit == null) return;
            var item = hit.GetComponent<ItemController>() ?? hit.GetComponentInParent<ItemController>();
            if (item == null) return;
            if (item.GetState() != ItemController.ItemState.Idle) return;
            currentItem = item; activeTouchId = touchId; currentItem.OnPick(worldPos);
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
                float farRange = Mathf.Max(closeRange, visibilityRange);
                float nearFactor = best <= 0f ? 1f : Mathf.Clamp01(1f - (best / closeRange));
                float farFactor = Mathf.Clamp01(1f - (best / farRange));
                float baseline = (currentItem != null) ? minLevelOnDrag : 0f;
                level = Mathf.Max(nearFactor, farFactor * baseline);
            }

            if (newSlot != hoverSlot)
            {
                if (hoverSlot != null) { hoverSlot.SetHighlightLevel(0f); hoverSlot.SetDrawOnTop(false); }
                hoverSlot = newSlot;
            }
            if (hoverSlot != null)
            {
                hoverSlot.SetHighlightLevel(level);
                // Keep outline between Character (robot) and Clothing (jacket) — do not lift above jacket
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
