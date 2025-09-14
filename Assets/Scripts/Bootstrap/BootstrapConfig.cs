using UnityEngine;

namespace StoryToys.DragDrop
{
    [CreateAssetMenu(menuName = "StoryToys/DragDrop Bootstrap Config", fileName = "BootstrapConfig")]
    public class BootstrapConfig : ScriptableObject
    {
        [Header("Prefabs (optional)")]
        public GameObject backgroundPrefab;
        public GameObject robotPrefab;
        public GameObject jacketPrefab;
        public GameObject resetButtonPrefab;
        public GameObject sfxPrefab;

        [Header("Settings (optional)")]
        public DragDropSettings settings;
    }
}

