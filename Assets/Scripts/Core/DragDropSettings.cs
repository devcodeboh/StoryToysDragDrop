using UnityEngine;

namespace StoryToys.DragDrop
{
    [CreateAssetMenu(menuName = "StoryToys/DragDrop Settings", fileName = "DragDropSettings")]
    public class DragDropSettings : ScriptableObject
    {
        [Header("Tweens")]
        public float equipSpeed = 6f;
        public float returnSpeed = 6f;

        [Header("Audio")]
        public AudioClip hitClip;
        public AudioClip missClip;
    }
}

