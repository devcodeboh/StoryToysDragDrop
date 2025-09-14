using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Composition Root — здесь мы собираем все зависимости и связываем систему.
/// </summary>
public class GameContext : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private ItemController jacket;      // Куртка
    [SerializeField] private Button resetButton;         // Кнопка RESET (Canvas → Button)
    [SerializeField] private AudioSource sfxSource;      // AudioSource (SFX)
    [SerializeField] private AudioClip hitClip;          // hit_sound.ogg
    [SerializeField] private AudioClip missClip;         // miss_sound.ogg

    [Header("Tuning")]
    [SerializeField] private float equipSpeed = 6f;      // скорость движения к торсу
    [SerializeField] private float returnSpeed = 6f;     // скорость возвращения в угол

    private IAudioService audioService;
    private IUIService uiService;

    private void Awake()
    {
        // --- Проверяем обязательные ссылки ---
        Debug.Assert(jacket, "❌ GameContext: назначь Jacket (ItemController) в инспекторе!");
        Debug.Assert(resetButton, "❌ GameContext: назначь Reset Button в инспекторе!");
        Debug.Assert(sfxSource, "❌ GameContext: назначь AudioSource (SFX) в инспекторе!");

        // --- Сервисы ---
        audioService = new AudioService(sfxSource, hitClip, missClip);
        uiService    = new UIService(resetButton);

        // --- Стратегии ---
        var equipStrategy = new EquipStrategy(audioService, uiService, equipSpeed);
        var returnStrategy = new ReturnStrategy(audioService, returnSpeed);

        // --- Инъекция зависимостей в куртку ---
        jacket.Init(equipStrategy, returnStrategy, uiService);

        // --- Логика кнопки Reset ---
        uiService.RegisterResetAction(() =>
        {
            jacket.ResetImmediate();
        });

        // --- По умолчанию кнопка скрыта ---
        uiService.ShowResetButton(false);
    }
}
