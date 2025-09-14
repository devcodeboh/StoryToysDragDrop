using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace StoryToys.DragDrop
{
    public class TutorialController : MonoBehaviour
    {
        private Canvas canvas;
        private RectTransform overlay;
        private Text messageText;
        private Button skipButton;

        private ItemController jacket;
        private EquipSlot torsoSlot;
        private TutorialSteps stepsAsset;
        private int stepIndex = -1;

        private const string PrefKey = "TutorialCompleted";

        private void Start()
        {
            if (PlayerPrefs.GetInt(PrefKey, 0) == 1) { Destroy(gameObject); return; }

            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            canvas.overrideSorting = true; // гарантируем поверх всего
            canvas.sortingOrder = 1000;

            jacket = FindObjectOfType<ItemController>();
            torsoSlot = FindObjectOfType<EquipSlot>();

            BuildOverlay();
            LoadSteps();
            // Показать первый шаг сразу
            if (stepsAsset != null && stepsAsset.steps.Count > 0)
            {
                messageText.text = stepsAsset.steps[0].message;
            }
            StartCoroutine(Run());
        }

        private void BuildOverlay()
        {
            var go = new GameObject("TutorialOverlay", typeof(RectTransform), typeof(CanvasGroup));
            overlay = go.GetComponent<RectTransform>();
            overlay.SetParent(canvas.transform, false);
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = Vector2.zero; overlay.offsetMax = Vector2.zero;

            // Dim background
            var dimGO = new GameObject("Dim", typeof(Image));
            dimGO.transform.SetParent(overlay, false);
            var dim = dimGO.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);
            var dimRT = dim.rectTransform; dimRT.anchorMin = Vector2.zero; dimRT.anchorMax = Vector2.one; dimRT.offsetMin = Vector2.zero; dimRT.offsetMax = Vector2.zero;
            dim.raycastTarget = true;

            // Message box
            var msgGO = new GameObject("Message", typeof(Image));
            msgGO.transform.SetParent(overlay, false);
            var msgBG = msgGO.GetComponent<Image>(); msgBG.color = new Color(0f,0f,0f,0.8f);
            var msgRT = msgBG.rectTransform;
            msgRT.sizeDelta = new Vector2(520, 110);
            // Привяжем к нижней части экрана
            msgRT.anchorMin = new Vector2(0.5f, 0.15f);
            msgRT.anchorMax = new Vector2(0.5f, 0.15f);
            msgRT.anchoredPosition = Vector2.zero;
            var txtGO = new GameObject("Text", typeof(Text)); txtGO.transform.SetParent(msgGO.transform, false);
            messageText = txtGO.GetComponent<Text>();
            messageText.alignment = TextAnchor.MiddleCenter; messageText.color = Color.white; messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var txtRT = messageText.rectTransform; txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one; txtRT.offsetMin = new Vector2(10,10); txtRT.offsetMax = new Vector2(-10,-10);

            // Skip button
            var skipGO = new GameObject("Skip", typeof(Image)); skipGO.transform.SetParent(overlay, false);
            var skipBG = skipGO.GetComponent<Image>(); skipBG.color = new Color(0f,0f,0f,0.6f);
            var skipRT = skipBG.rectTransform; skipRT.sizeDelta = new Vector2(100, 40); skipRT.anchorMin = new Vector2(1,1); skipRT.anchorMax = new Vector2(1,1); skipRT.anchoredPosition = new Vector2(-80, -50);
            var skipTxtGO = new GameObject("Text", typeof(Text)); skipTxtGO.transform.SetParent(skipGO.transform, false);
            var skipTxt = skipTxtGO.GetComponent<Text>(); skipTxt.text = "Skip"; skipTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); skipTxt.alignment = TextAnchor.MiddleCenter; skipTxt.color = Color.white;
            var skt = skipTxt.rectTransform; skt.anchorMin = Vector2.zero; skt.anchorMax = Vector2.one; skt.offsetMin = Vector2.zero; skt.offsetMax = Vector2.zero;
            skipButton = skipGO.gameObject.AddComponent<Button>();
            skipButton.targetGraphic = skipBG; skipButton.onClick.AddListener(Skip);
        }

        private void LoadSteps()
        {
            stepsAsset = Resources.Load<TutorialSteps>("Config/TutorialSteps");
            if (stepsAsset == null || stepsAsset.steps.Count == 0)
            {
                // fallback default 2 steps
                stepsAsset = ScriptableObject.CreateInstance<TutorialSteps>();
                stepsAsset.steps.Add(new TutorialStepData { message = "Drag the jacket", target = TutorialTarget.Jacket, gatePick = true, gateDropOnSlot = false });
                stepsAsset.steps.Add(new TutorialStepData { message = "Drop on the torso", target = TutorialTarget.TorsoSlot, gatePick = false, gateDropOnSlot = true });
            }
        }

        private IEnumerator Run()
        {
            TutorialGate.Active = true;

            for (stepIndex = 0; stepIndex < stepsAsset.steps.Count; stepIndex++)
            {
                var step = stepsAsset.steps[stepIndex];
                messageText.text = step.message;
                // gating
                TutorialGate.AllowAnyPick = false;
                TutorialGate.AllowedItem = step.gatePick ? jacket : null;
                TutorialGate.AllowedDropSlot = step.gateDropOnSlot ? torsoSlot : null;

                // wait for condition
                if (step.target == TutorialTarget.Jacket)
                {
                    // wait until jacket is picked
                    yield return new WaitUntil(() => jacket != null && jacket.GetState() == ItemController.ItemState.Dragging);
                }
                else if (step.target == TutorialTarget.TorsoSlot)
                {
                    // wait until equipped
                    yield return new WaitUntil(() => jacket != null && jacket.GetState() == ItemController.ItemState.Equipped);
                }
                yield return null;
            }

            Complete();
        }

        private void Skip()
        {
            Complete();
        }

        private void Complete()
        {
            TutorialGate.Reset();
            PlayerPrefs.SetInt(PrefKey, 1);
            PlayerPrefs.Save();
            if (overlay != null) Destroy(overlay.gameObject);
            Destroy(gameObject);
        }
    }
}
