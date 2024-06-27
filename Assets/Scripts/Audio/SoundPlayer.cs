using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    [field: SerializeField] public AudioSource BackgroundMusicSource {get; set;}
    [field: SerializeField] public AudioSource SoundFXSource {get; set;}
        
    private void Awake() 
    {
        SoundPlayerStatic.sp = this;
    }
    public void PlaySoundOneOff(string soundFXName)
    {
        AudioClip clip = Resources.Load<AudioClip>("Dax/SoundFX/" + soundFXName);
        SoundFXSource.PlayOneShot(clip);
    }   
}

 public static class SoundPlayerStatic
{        
    public static SoundPlayer sp;

    public static void PlaySoundFX(string name)
    {
        sp.PlaySoundOneOff(name);
    }
}
