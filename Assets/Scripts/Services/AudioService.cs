using UnityEngine;

namespace StoryToys.DragDrop
{
    public class AudioService : IAudioService
    {
        private readonly AudioSource audioSource;
        private readonly AudioClip hitClip;
        private readonly AudioClip missClip;

        public AudioService(AudioSource audioSource, AudioClip hitClip, AudioClip missClip)
        {
            this.audioSource = audioSource;
            this.hitClip = hitClip;
            this.missClip = missClip;
        }

        public void PlayHit() => PlayClip(hitClip);

        public void PlayMiss() => PlayClip(missClip);

        private void PlayClip(AudioClip clip)
        {
            if (clip == null) return;
            if (audioSource.isPlaying && audioSource.clip == clip) return;

            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
