using UnityEngine;

namespace StoryToys.DragDrop
{
    [CreateAssetMenu(menuName = "StoryToys/DragDrop Tutorial Style", fileName = "TutorialStyle")]
    public class TutorialStyle : ScriptableObject
    {
        [Header("Message Panel")]
        public Sprite messageBackground;
        // Light neutral by default so black text is readable
        public Color messageColor = new Color(0.93f, 0.93f, 0.93f, 1f);
        public Color messageTextColor = Color.white;
        public Vector2 messageSize = new Vector2(520, 110);
        [Min(0f)] public float messageCornerRadius = 18f; // used if no sprite

        [Header("Skip Button")]
        public Sprite skipBackground;
        public Color skipColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        public Color skipTextColor = Color.white;
        public Vector2 skipSize = new Vector2(100, 40);
        [Min(0f)] public float skipCornerRadius = 12f; // used if no sprite
        public Vector2 skipAnchorOffset = new Vector2(-60f, -50f);

        [Header("Hint Button (!)")]
        public Sprite hintBackground;
        public Color hintColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        public Vector2 hintSize = new Vector2(44f, 44f);
        [Min(0f)] public float hintCornerRadius = 12f;
        public Vector2 hintAnchorOffset = new Vector2(-160f, -50f);
    }
}
