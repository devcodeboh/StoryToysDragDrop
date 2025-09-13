using System;

public interface IUIService
{
    void ShowResetButton(bool show);
    void RegisterResetAction(Action onReset);
}