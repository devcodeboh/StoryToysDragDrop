using System;

namespace StoryToys.DragDrop
{
    public interface IUIService
    {
        void ShowResetButton(bool show);
        void RegisterResetAction(Action onReset);
    }
}
