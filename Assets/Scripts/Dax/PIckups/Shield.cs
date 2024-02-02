using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shield : BoardObject
{
    public enum eShieldTypes { HIT, SINGLE_KILL};
    //public static float DEFAULT_TIMER = 10f;

    [Header("Shield Data")]   
    public eShieldTypes ShieldType;
    //public float Timer = DEFAULT_TIMER;

    // monote
   /* public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {
       // Debug.Log("new shield create --MoNew--");
        name = spawnNode.name + "--" + ShieldType.ToString();
        //BoardObjectType = eBoardObjectType.SHIELD;
        base.InitForChannelNode(spawnNode, dax);       
    }*/
}