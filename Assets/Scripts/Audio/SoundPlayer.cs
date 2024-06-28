using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Static sound effects class that can be called from anywhere and then calls a
/// MonoBehavior to get access to necessary functions that you can't access in a static class.
/// I used a static class because the sound is stateless and there's only one instance of everything
/// </summary>
static class SoundFXPlayer
{        
    static SoundPlayer soundPlayer; // reference to the MonoBehavior component

    /// <summary>
    /// Init just sets up the reference to the SoundPlayer
    /// </summary>
    /// <param name="sp">SoundPlayer reference</param>
    public static void Init(SoundPlayer sp)
    {
        soundPlayer = sp;
    }

    /// <summary>
    /// Call used all around the code
    /// </summary>
    /// <param name="name">Name of Sound Effect to play</param>
    public static void PlaySoundFX(string name, float vol)
    {
        soundPlayer.PlaySoundOneOff(name, vol);
    }
}

/// <summary>
/// This is the class that's called from SoundFXPlayer to play a sound.
/// I can't do everything I need to do in a static class (like Resources.Load)
/// so I use this to handle the actually playing.
/// </summary>
public class SoundPlayer : MonoBehaviour
{
    // AudioSource for the SoundFX that's attached to it's GameObject    
    [field: SerializeField] public AudioSource SoundFXSource {get; set;}
        
    private void Awake() 
    {
        // The Init sets up the reference for the 
        SoundFXPlayer.Init(this);
    }

    /// <summary>
    /// Handles the actual loading and playing of the sound effect
    /// </summary>
    /// <param name="soundFXName">Name of the effect to load up and play</param>
    public void PlaySoundOneOff(string soundFXName, float vol)
    {
        AudioClip clip = Resources.Load<AudioClip>("Dax/SoundFX/" + soundFXName);        
        SoundFXSource.PlayOneShot(clip, vol);        
    }       
}

/*
BG Music
Click to start
Generic Bounce
Bumper Bounce
Channel Change
Collect Facet
EnemyDeath
Dynamite
Glue
Pickup Facet Color Match
Pickup Ring FC
Pickup Wheel FC
Pickup Shield Single Kill
Pickup Shield Hit
PIckup Speed Mod Player
Pickup Speed Mod Wheel
Pickup Speed Mod Enemy
Pickup PointMod 1
Pickup PointMod 2
Shield Collision Hit
Shield Collision Single Kill
Activate Facet Collect Ring
Activate Facet Collect Wheel
Activate Shield Hit
Activate Hield Single Kill
Victory voice
Victory sound
Defeat voice
Defeat sound


click try again button
*/
