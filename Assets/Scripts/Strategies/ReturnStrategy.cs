using UnityEngine;

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

        item.SmoothMove(target, speed, () =>
        {
            item.SetState(ItemController.ItemState.Idle);
        });
    }
}