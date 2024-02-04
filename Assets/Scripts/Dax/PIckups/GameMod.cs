using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMod : BoardObject // monote - make these generic
{
    public enum eGameModType { EXTRA_POINTS, POINTS_MULTIPLIER };
    public static List<string> GAME_MOD_STRINGS = new List<string> {"Extra_Points", "Points_Multiplier"};

    [Header("GameMod Data")]
    public eGameModType GameModType;
    public int GameModVal;
    public float GameModTime = 5f;
    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {
        name = spawnNode.name + "--GameMod";
        // BoardObjectType = eBoardObjectType.MAGNET;
        base.InitForChannelNode(spawnNode, dax);
    }

    
}