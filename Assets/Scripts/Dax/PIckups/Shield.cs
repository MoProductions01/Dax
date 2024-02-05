using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : BoardObject
{
    public enum eShieldTypes { HIT, SINGLE_KILL};
     public static List<string> SHIELD_STRINGS = new List<string> {"Hit_Shield", "Single_Kill_Shield"};
    //public static float DEFAULT_TIMER = 10f;

    [Header("Shield Data")]   
    public eShieldTypes ShieldType;
    //public float Timer = DEFAULT_TIMER;

    // monote
    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {       
        name = spawnNode.name + "--Shield--" + ShieldType.ToString();
        //BoardObjectType = eBoardObjectType.SHIELD;
        base.InitForChannelNode(spawnNode, dax);       
    }
}