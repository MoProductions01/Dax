using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMod : BoardObject // monote - make these generic
{
    public enum eGameModType { EXTRA_POINTS, POINTS_MULTIPLIER };

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