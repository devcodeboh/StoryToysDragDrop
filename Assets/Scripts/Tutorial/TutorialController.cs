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
        private TutorialStyle style;
        private RectTransform messageBGRect;
        // Keep background compact: just a bit larger than text
        private const float ExtraPadX = 8f;
        private const float ExtraPadY = 6f;

        private ItemController jacket;
        private EquipSlot torsoSlot;
        private TutorialSteps stepsAsset;
        private int stepIndex = -1;

        private const string PrefKey = "TutorialCompleted";
        [HideInInspector] public bool forceRun = false; // запускается даже если уже был пройден

        private void Start()
        {
            if (!forceRun && PlayerPrefs.GetInt(PrefKey, 0) == 1) { Destroy(gameObject); return; }

            // Пока идёт туториал — скрываем кнопку подсказки
            TutorialHintButton.Hide();

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

            style = Resources.Load<TutorialStyle>("Config/TutorialStyle");
            BuildOverlay();
            LoadSteps();
            // Показать первый шаг сразу
            if (stepsAsset != null && stepsAsset.steps.Count > 0)
            {
                messageText.text = stepsAsset.steps[0].message;
                ResizeMessageBackground();
            }
            StartCoroutine(Run());
        }

        private void BuildOverlay()
        {
            var go = new GameObject("TutorialOverlay", typeof(RectTransform), typeof(CanvasGroup));
            overlay = go.GetComponent<RectTransform>();
            overlay.SetParent(canvas.transform, false);
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one; overlay.offsetMin = Vector2.zero; overlay.offsetMax = Vector2.zero;

            // No dim background — оставляем сцену без затемнения

            // Message box
            var msgGO = new GameObject("Message");
            msgGO.transform.SetParent(overlay, false);
            Graphic msgBG;
            if (style != null && style.messageBackground != null)
            {
                var img = msgGO.AddComponent<Image>();
                img.sprite = style.messageBackground;
                img.type = (img.sprite != null && img.sprite.border.sqrMagnitude > 0) ? Image.Type.Sliced : Image.Type.Simple;
                msgBG = img;
            }
            else
            {
                var ri = msgGO.AddComponent<RoundedImage>();
                ri.Radius = style != null ? style.messageCornerRadius : 18f;
                msgBG = ri;
            }
            msgBG.color = style != null ? style.messageColor : new Color(0f,0f,0f,0.8f);
            messageBGRect = (msgBG.transform as RectTransform);
            messageBGRect.sizeDelta = style != null ? style.messageSize : new Vector2(520, 110);
            // Привяжем к нижней части экрана
            messageBGRect.anchorMin = new Vector2(0.5f, 0.15f);
            messageBGRect.anchorMax = new Vector2(0.5f, 0.15f);
            messageBGRect.anchoredPosition = Vector2.zero;
            // Сообщение — только для отображения, не блокирует клики
            msgBG.raycastTarget = false;
            var txtGO = new GameObject("Text", typeof(Text)); txtGO.transform.SetParent(msgGO.transform, false);
            messageText = txtGO.GetComponent<Text>();
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.color = style != null ? style.messageTextColor : Color.white;
            messageText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // Make text ~40% larger
            messageText.fontSize = Mathf.RoundToInt(messageText.fontSize * 1.4f);
            var txtRT = messageText.rectTransform; txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one; txtRT.offsetMin = new Vector2(10,10); txtRT.offsetMax = new Vector2(-10,-10);

            // Skip button
            var skipGO = new GameObject("Skip");
            skipGO.transform.SetParent(overlay, false);
            Graphic skipBG;
            if (style != null && style.skipBackground != null)
            {
                var img = skipGO.AddComponent<Image>();
                img.sprite = style.skipBackground;
                img.type = (img.sprite != null && img.sprite.border.sqrMagnitude > 0) ? Image.Type.Sliced : Image.Type.Simple;
                skipBG = img;
            }
            else
            {
                var ri = skipGO.AddComponent<RoundedImage>();
                ri.Radius = style != null ? style.skipCornerRadius : 12f;
                skipBG = ri;
            }
            skipBG.color = style != null ? style.skipColor : new Color(0f,0f,0f,0.6f);
            var skipRT = (RectTransform)skipBG.transform; 
            skipRT.sizeDelta = style != null ? style.skipSize : new Vector2(100, 40); 
            skipRT.anchorMin = new Vector2(1,1); skipRT.anchorMax = new Vector2(1,1); 
            var skipOffset = style != null ? style.skipAnchorOffset : new Vector2(-60, -50);
            skipRT.anchoredPosition = skipOffset;
            var skipTxtGO = new GameObject("Text", typeof(Text)); skipTxtGO.transform.SetParent(skipGO.transform, false);
            var skipTxt = skipTxtGO.GetComponent<Text>();
            skipTxt.text = "Skip";
            skipTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            skipTxt.alignment = TextAnchor.MiddleCenter;
            skipTxt.color = style != null ? style.skipTextColor : Color.white;
            var skt = skipTxt.rectTransform; skt.anchorMin = Vector2.zero; skt.anchorMax = Vector2.one; skt.offsetMin = Vector2.zero; skt.offsetMax = Vector2.zero;
            skipButton = skipGO.gameObject.AddComponent<Button>();
            skipButton.targetGraphic = skipBG; 
            skipButton.onClick.AddListener(Skip);
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
                ResizeMessageBackground();
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

        private void ResizeMessageBackground()
        {
            if (messageText == null || messageBGRect == null) return;
            float canvasScale = canvas != null ? canvas.scaleFactor : 1f;
            float maxWidth = Mathf.Min(520f, (Screen.width / Mathf.Max(0.01f, canvasScale)) * 0.6f);

            var settings = messageText.GetGenerationSettings(new Vector2(maxWidth, 0f));
            float prefW = messageText.cachedTextGeneratorForLayout.GetPreferredWidth(messageText.text, settings) / messageText.pixelsPerUnit;
            float prefH = messageText.cachedTextGeneratorForLayout.GetPreferredHeight(messageText.text, settings) / messageText.pixelsPerUnit;

            float width  = Mathf.Clamp(prefW, 1f, maxWidth) + 20f + 2f * ExtraPadX;
            float height = Mathf.Max(prefH + 20f + 2f * ExtraPadY, 44f);
            messageBGRect.sizeDelta = new Vector2(width, height);
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
            // Показать кнопку вызова снова (если куртка не одета)
            TutorialHintButton.ShowIfAvailable();
            Destroy(gameObject);
        }
    }
}
