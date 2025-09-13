using System;
using UnityEngine.UI;

/// <summary>
/// Реализация IUIService для управления кнопкой Reset.
/// UI остаётся «тупым», только показывает/прячет кнопку и вызывает коллбек.
/// </summary>
public class UIService : IUIService
{
    private readonly Button resetButton;
    private Action onReset;

    public UIService(Button resetButton)
    {
        this.resetButton = resetButton;
        this.resetButton.onClick.RemoveAllListeners();
        this.resetButton.onClick.AddListener(HandleResetClicked);
    }

    public void ShowResetButton(bool show)
    {
        if (resetButton != null)
            resetButton.gameObject.SetActive(show);
    }

    public void RegisterResetAction(Action onReset)
    {
        this.onReset = onReset;
    }

    private void HandleResetClicked()
    {
        onReset?.Invoke();
    }
}