using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointMod : BoardObject 
{
    public enum ePointModType { EXTRA_POINTS, POINTS_MULTIPLIER };    

    [Header("Point Mod Data")]
    public ePointModType PointModType;
    public int PointModVal;
    public float PointModTime = 5f;
    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {
        name = spawnNode.name + "--PointMod--" + PointModType.ToString();        
        base.InitForChannelNode(spawnNode, dax);
    }

    
}