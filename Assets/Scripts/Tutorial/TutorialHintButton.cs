using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace StoryToys.DragDrop
{
    public class TutorialHintButton : MonoBehaviour
    {
        public static TutorialHintButton Instance { get; private set; }

        private Button button;
        private CanvasGroup cg;
        private ItemController jacket;

        public void Initialize(ItemController item)
        {
            Instance = this;
            jacket = item;
            BuildUI();
            // Show if not equipped and tutorial not running
            if (!TutorialGate.Active && jacket != null && jacket.GetState() != ItemController.ItemState.Equipped)
                ShowFade(0.25f);
            else
                HideImmediate();
        }

        private void BuildUI()
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            var style = Resources.Load<TutorialStyle>("Config/TutorialStyle");
            var goBtn = new GameObject("TutorialButton", typeof(RectTransform), typeof(CanvasGroup));
            goBtn.transform.SetParent(canvas.transform, false);
            cg = goBtn.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            Graphic bgGraphic;
            if (style != null && style.hintBackground != null)
            {
                var img = goBtn.AddComponent<Image>();
                img.sprite = style.hintBackground;
                img.type = (img.sprite != null && img.sprite.border.sqrMagnitude > 0) ? Image.Type.Sliced : Image.Type.Simple;
                bgGraphic = img;
            }
            else
            {
                var ri = goBtn.AddComponent<RoundedImage>();
                ri.Radius = style != null ? style.hintCornerRadius : 12f;
                bgGraphic = ri;
            }
            bgGraphic.color = style != null ? style.hintColor : new Color(0.95f, 0.95f, 0.95f, 1f);
            var rt = (RectTransform)goBtn.transform;
            rt.sizeDelta = style != null ? style.hintSize : new Vector2(44, 44);
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.anchoredPosition = style != null ? style.hintAnchorOffset : new Vector2(-160, -50);
            rt.anchoredPosition = new Vector2(-130, -50); // рядом с Reset

            var txtGO = new GameObject("Text", typeof(Text));
            txtGO.transform.SetParent(goBtn.transform, false);
            var txt = txtGO.GetComponent<Text>();
            txt.text = "!";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var tr = (RectTransform)txtGO.transform;
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;

            button = goBtn.AddComponent<Button>();
            button.targetGraphic = goBtn.GetComponent<Graphic>();
            button.onClick.AddListener(OnClickHint);
        }

        private void OnClickHint()
        {
            if (jacket != null && jacket.GetState() == ItemController.ItemState.Equipped)
                return; // не запускаем, когда одета

            HideImmediate();
            var tgo = new GameObject("Tutorial");
            var ctrl = tgo.AddComponent<TutorialController>();
            ctrl.forceRun = true; // запускать даже если уже пройден ранее
        }

        public void ShowFade(float duration = 0.25f)
        {
            if (cg == null) return;
            StopAllCoroutines();
            StartCoroutine(FadeTo(1f, duration));
        }

        public void HideImmediate()
        {
            if (cg == null) return;
            StopAllCoroutines();
            cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;
        }

        public void HideFade(float duration = 0.15f)
        {
            if (cg == null) return;
            StopAllCoroutines();
            StartCoroutine(FadeTo(0f, duration));
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            if (cg == null) yield break;
            float start = cg.alpha; float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime; float k = duration > 0 ? Mathf.Clamp01(t / duration) : 1f;
                cg.alpha = Mathf.Lerp(start, target, k);
                yield return null;
            }
            cg.alpha = target;
            cg.interactable = cg.alpha > 0.99f;
            cg.blocksRaycasts = cg.interactable;
        }

        // внешние хуки
        public static void ShowIfAvailable()
        {
            if (!TutorialGate.Active && Instance != null && Instance.jacket != null && Instance.jacket.GetState() != ItemController.ItemState.Equipped)
                Instance.ShowFade();
        }

        public static void Hide()
        {
            if (Instance != null) Instance.HideFade();
        }
    }
}
