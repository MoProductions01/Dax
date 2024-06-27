using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    [field: SerializeField] public AudioSource BackgroundMusicSource {get; set;}
    [field: SerializeField] public AudioSource SoundFXSource {get; set;}
        
    private void Awake() 
    {
        SoundFXPlayer.Init(this);
    }
    public void PlaySoundOneOff(string soundFXName)
    {
        AudioClip clip = Resources.Load<AudioClip>("Dax/SoundFX/" + soundFXName);
        SoundFXSource.PlayOneShot(clip);        
    }   
}

 // Using a static class because sound is stateless
static class SoundFXPlayer
{        
    static SoundPlayer soundPlayer;

    public static void Init(SoundPlayer sp)
    {
        soundPlayer = sp;
    }
    public static void PlaySoundFX(string name)
    {
        soundPlayer.PlaySoundOneOff(name);
    }
}
