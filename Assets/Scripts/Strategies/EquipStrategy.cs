using UnityEngine;

public class EquipStrategy : IDropStrategy
{
    private readonly IAudioService audio;
    private readonly IUIService ui;
    private readonly float speed;

    public EquipStrategy(IAudioService audio, IUIService ui, float speed = 6f)
    {
        this.audio = audio;
        this.ui = ui;
        this.speed = speed;
    }

    public void Execute(ItemController item, Vector3 target)
    {
        item.SmoothMove(target, speed, () =>
        {
            item.SetState(ItemController.ItemState.Equipped);
            audio?.PlayHit();
            ui?.ShowResetButton(true);
        });
    }
}