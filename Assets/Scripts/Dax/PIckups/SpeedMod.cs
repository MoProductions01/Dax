using UnityEngine;
using System.Collections.Generic;

public class SpeedMod : BoardObject
{
    public enum eSpeedModType { PLAYER_SPEED, ENEMY_SPEED, RING_SPEED};
    public static List<string> SPEED_MOD_STRINGS = new List<string> {"Player_Speed", "Enemy_Speed", "Ring_Speed"};

    public static float DEFAULT_MOD_VAL = 2f;    


    [Header("Speed Mod Specific")]    
    public eSpeedModType SpeedModType;
    public float SpeedModVal = DEFAULT_MOD_VAL;
}
