using UnityEngine;

public class DragInput : MonoBehaviour
{
    private ItemController currentItem;
    private Camera mainCam;
    private int activeTouchId = -1;

    private void Awake()
    {
        mainCam = Camera.main;
        if (mainCam == null) mainCam = Camera.current;
    }

    private void Update()
    {
        // --- Touch (мобайл) ---
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
                        break;

                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (currentItem != null && activeTouchId == touch.fingerId)
                            currentItem.OnDrag(wp);
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (currentItem != null && activeTouchId == touch.fingerId)
                        {
                            currentItem.OnDrop(wp);
                            currentItem = null;
                            activeTouchId = -1;
                        }
                        break;
                }
            }
            return; // если тач, мышь игнорим
        }

        // --- Mouse (ПК) ---
        if (Input.GetMouseButtonDown(0))
            TryPick(ScreenToWorld(Input.mousePosition), -1);

        if (Input.GetMouseButton(0) && currentItem != null && activeTouchId == -1)
            currentItem.OnDrag(ScreenToWorld(Input.mousePosition));

        if (Input.GetMouseButtonUp(0) && currentItem != null && activeTouchId == -1)
        {
            currentItem.OnDrop(ScreenToWorld(Input.mousePosition));
            currentItem = null;
        }
    }

    private void TryPick(Vector3 worldPos, int touchId)
    {
        Collider2D hit = Physics2D.OverlapPoint(worldPos);
        if (hit == null) return;

        var item = hit.GetComponent<ItemController>() ?? hit.GetComponentInParent<ItemController>();
        if (item == null) return;

        // можно тянуть только если предмет в Idle
        if (item.GetState() != ItemController.ItemState.Idle) return;

        currentItem = item;
        activeTouchId = touchId;
        currentItem.OnPick(worldPos);
    }

    private Vector3 ScreenToWorld(Vector3 screenPos)
    {
        var p = mainCam.ScreenToWorldPoint(screenPos);
        p.z = 0f;
        return p;
    }
}
