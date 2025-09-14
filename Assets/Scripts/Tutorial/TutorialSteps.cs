using System;
using System.Collections.Generic;
using UnityEngine;

namespace StoryToys.DragDrop
{
    public enum TutorialTarget
    {
        Jacket,
        TorsoSlot
    }

    [Serializable]
    public class TutorialStepData
    {
        public string message;
        public TutorialTarget target;
        public bool gatePick;  // only allow picking the jacket
        public bool gateDropOnSlot; // only allow dropping on torso slot
    }

    [CreateAssetMenu(menuName = "StoryToys/DragDrop Tutorial Steps", fileName = "TutorialSteps")]
    public class TutorialSteps : ScriptableObject
    {
        public List<TutorialStepData> steps = new List<TutorialStepData>();
    }
}

