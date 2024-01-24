using UnityEngine;

public class SpeedMod : BoardObject
{
    public enum eSpeedModType { SPEED_UP, SPEED_DOWN, ENEMY_UP, ENEMY_DOWN, RING_UP, RING_DOWN, WHEEL_UP, WHEEL_DOWN, RING_STOP, TIME_STOP, RING_REVERSE, MEGA_RING_REVERSE};

    public static float DEFAULT_MOD_VAL = 2f;    


    [Header("Speed Mod Specific")]    
    public eSpeedModType SpeedModType;
    public float SpeedModVal = DEFAULT_MOD_VAL;
}
