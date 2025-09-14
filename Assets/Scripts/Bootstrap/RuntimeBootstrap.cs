using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StoryToys.DragDrop
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            var config = Resources.Load<BootstrapConfig>("Config/BootstrapConfig");
            var settings = config != null && config.settings != null ? config.settings : Resources.Load<DragDropSettings>("Config/DragDropSettings");

            // EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            // Camera
            if (Camera.main == null)
            {
                var camGo = new GameObject("MainCamera");
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true; cam.orthographicSize = 5f;
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.1f,0.1f,0.1f,1f);
                camGo.tag = "MainCamera"; camGo.transform.position = new Vector3(0,0,-10);
            }

            // Canvas
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            GameObject TrySpawn(GameObject prefab, string path)
            {
                if (prefab != null) return Object.Instantiate(prefab);
                var loaded = Resources.Load<GameObject>(path);
                if (loaded != null) return Object.Instantiate(loaded);
                return null;
            }

            var bg     = TrySpawn(config ? config.backgroundPrefab : null, "Prefabs/Background");
            var robot  = TrySpawn(config ? config.robotPrefab      : null, "Prefabs/Robot");
            var jacket = TrySpawn(config ? config.jacketPrefab     : null, "Prefabs/Jacket");
            var sfx    = TrySpawn(config ? config.sfxPrefab        : null, "Prefabs/SFX");
            var reset  = TrySpawn(config ? config.resetButtonPrefab: null, "Prefabs/UI/ResetButton") ?? TrySpawn(null, "Prefabs/ResetButton");
            if (reset) reset.transform.SetParent(canvasGo.transform, false);

            // Frame background
            var camMain = Camera.main;
            if (bg && camMain)
            {
                var sr = bg.GetComponentInChildren<SpriteRenderer>();
                if (sr && sr.sprite)
                {
                    var b = sr.bounds; var p = b.center; p.z = -10f; camMain.transform.position = p;
                    var v = b.extents.y; var h = b.extents.x / Mathf.Max(0.0001f, camMain.aspect);
                    camMain.orthographicSize = Mathf.Max(v, h);
                }
            }

            // Systems (create disabled to set fields before Awake)
            var systems = new GameObject("Systems");
            systems.SetActive(false);
            var ctx = systems.AddComponent<GameContext>();
            var input = systems.AddComponent<DragInput>();

            // Resolve refs
            var item = jacket ? jacket.GetComponentInChildren<ItemController>() : Object.FindFirstObjectByType<ItemController>();
            var btn  = reset  ? reset.GetComponentInChildren<Button>()          : Object.FindFirstObjectByType<Button>();
            var audio= sfx    ? sfx.GetComponentInChildren<AudioSource>()       : Object.FindFirstObjectByType<AudioSource>();

            // Start anchor at lower-left
            if (item)
            {
                var cam = Camera.main; if (cam)
                {
                    var vp = new Vector3(0.05f, 0.05f, -cam.transform.position.z);
                    var wp = cam.ViewportToWorldPoint(vp); wp.z = 0f;
                    var anchorsRoot = GameObject.Find("Anchors") ?? new GameObject("Anchors");
                    var startAnchor = new GameObject("StartAnchor");
                    startAnchor.transform.SetParent(anchorsRoot.transform); startAnchor.transform.position = wp;
                    jacket.transform.position = wp;
                    // assign private startTransform via reflection
                    var f = typeof(ItemController).GetField("startTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (f != null) f.SetValue(item, startAnchor.transform);
                }
            }

            // Auto-assign EquipAnchor if exists
            if (robot)
            {
                var slot = robot.GetComponentInChildren<EquipSlot>();
                if (slot && slot.anchor == null)
                {
                    var tr = slot.transform.Find("EquipAnchor"); if (tr) slot.anchor = tr;
                }
            }

            // Wire context via reflection (private fields)
            void SetPrivate(string name, object value)
            {
                var ft = typeof(GameContext).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (ft != null && value != null) ft.SetValue(ctx, value);
            }
            SetPrivate("jacket", item);
            SetPrivate("resetButton", btn);
            SetPrivate("sfxSource", audio);
            SetPrivate("settings", settings);

            // Enable systems so GameContext.Awake runs with fields set
            systems.SetActive(true);
        }
    }
}
