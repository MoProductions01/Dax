using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointMod : BoardObject 
{
    public enum eGameModType { EXTRA_POINTS, POINTS_MULTIPLIER };
    public static List<string> GAME_MOD_STRINGS = new List<string> {"Extra_Points", "Points_Multiplier"};

    [Header("GameMod Data")]
    public eGameModType GameModType;
    public int PointModVal;
    public float PointModTime = 5f;
    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {
        name = spawnNode.name + "--GameMod--" + GameModType.ToString();
        // BoardObjectType = eBoardObjectType.MAGNET;
        base.InitForChannelNode(spawnNode, dax);
    }

    
}