using UnityEngine;

namespace StoryToys.DragDrop
{
    [CreateAssetMenu(menuName = "StoryToys/DragDrop Outline Settings", fileName = "OutlineSettings")]
    public class OutlineSettings : ScriptableObject
    {
        // Default to #A4B1C1 (RGB 164,177,193)
        public Color outlineColor = new Color(164f/255f, 177f/255f, 193f/255f, 1f);
        [Min(0f)] public float thicknessPx = 1f;
        [Range(0f,1f)] public float softness = 0f;
        [Range(0f,1f)] public float alphaThreshold = 0.5f;
    }
}
