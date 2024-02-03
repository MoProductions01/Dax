using UnityEngine;

public class SpeedMod : BoardObject
{
    public enum eSpeedModType { PLAYER_SPEED, ENEMY_SPEED, RING_SPEED, /*RING_STOP, TIME_STOP, RING_REVERSE, MEGA_RING_REVERSE*/};

    public static float DEFAULT_MOD_VAL = 2f;    


    [Header("Speed Mod Specific")]    
    public eSpeedModType SpeedModType;
    public float SpeedModVal = DEFAULT_MOD_VAL;
}
