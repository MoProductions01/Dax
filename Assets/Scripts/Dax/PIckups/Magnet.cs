using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magnet : BoardObject
{
    public enum eMagnetTypes { REGULAR, SUPER };

    [Header("Magnet Data")]
    public eMagnetTypes MagnetType;
    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {
        name = spawnNode.name + "--Magnet";
       // BoardObjectType = eBoardObjectType.MAGNET;
        base.InitForChannelNode(spawnNode, dax);        
    }
    // magnet.SpawningNode.MyChannel.MyRing.CollectAllPickupFacets();        
    public static void ActivateMagnetType(eMagnetTypes type, Player player)
    {
        switch(type)
        {
            case eMagnetTypes.REGULAR:
                player.CurChannel.MyRing.CollectAllPickupFacets();
                break;
        }
    }
}