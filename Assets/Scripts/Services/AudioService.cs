using UnityEngine;


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


    public void PlayHit()
    {
        if (hitClip is null) return;
        audioSource.Stop();
        audioSource.clip = hitClip;
        audioSource.Play();
    }


    public void PlayMiss()
    {
        if (missClip == null) return;
        if (audioSource.isPlaying && audioSource.clip == missClip) return;
        audioSource.Stop();
        audioSource.clip = missClip;
        audioSource.Play();
    }
}