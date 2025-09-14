using UnityEngine;

namespace StoryToys.DragDrop
{
    public interface IDropStrategy
    {
        void Execute(ItemController item, Vector3 target);
    }
}
