using UnityEngine;
using UnityEngine.UI;

namespace StoryToys.DragDrop
{
    public class GameContext : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private ItemController jacket;
        [SerializeField] private Button resetButton;
        [SerializeField] private AudioSource sfxSource;

        [Header("Settings (optional)")]
        [SerializeField] private DragDropSettings settings;

        [Header("Fallback (when no Settings)")]
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip missClip;
        [SerializeField] private float equipSpeed = 6f;
        [SerializeField] private float returnSpeed = 6f;

        private IAudioService audioService;
        private IUIService uiService;

        private void Awake()
        {
            if (jacket == null) jacket = FindFirstObjectByType<ItemController>();
            if (resetButton == null) resetButton = FindFirstObjectByType<Button>();
            if (sfxSource == null) sfxSource = FindFirstObjectByType<AudioSource>();

            var useHit = settings && settings.hitClip ? settings.hitClip : hitClip;
            var useMiss = settings && settings.missClip ? settings.missClip : missClip;
            var useEquip = settings ? settings.equipSpeed : equipSpeed;
            var useReturn= settings ? settings.returnSpeed : returnSpeed;

            audioService = new AudioService(sfxSource, useHit, useMiss);
            uiService    = new UIService(resetButton);

            var equipStrategy  = new EquipStrategy(audioService, uiService, useEquip);
            var returnStrategy = new ReturnStrategy(audioService, useReturn);

            jacket?.Init(equipStrategy, returnStrategy, uiService);

            uiService.RegisterResetAction(() => jacket?.ResetImmediate());
            uiService.ShowResetButton(false);
        }
    }
}
