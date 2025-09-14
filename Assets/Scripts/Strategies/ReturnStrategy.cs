using UnityEngine;

namespace StoryToys.DragDrop
{
    public class ReturnStrategy : IDropStrategy
    {
        private readonly IAudioService audio;
        private readonly float speed;

        public ReturnStrategy(IAudioService audio, float speed = 6f)
        {
            this.audio = audio;
            this.speed = speed;
        }

        public void Execute(ItemController item, Vector3 target)
        {
            item.SetState(ItemController.ItemState.Returning);
            audio?.PlayMiss();
            item.StartCoroutine(ReturnSequence(item, target));
        }

        private System.Collections.IEnumerator ReturnSequence(ItemController item, Vector3 target)
        {
            yield return item.StartCoroutine(item.Shake(0.1f, 0.05f));
            item.SmoothMove(target, speed, () => item.SetState(ItemController.ItemState.Idle));
        }
    }
}
