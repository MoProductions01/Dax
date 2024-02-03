using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacetCollect : BoardObject
{
    public enum eFacetCollectTypes { RING, WHEEL };

    [Header("Facet Collect Data")]
    public eFacetCollectTypes FacetCollectType;
    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {
        name = spawnNode.name + "--Facet Collect";
       // BoardObjectType = eBoardObjectType.FACET_COLLECT;
        base.InitForChannelNode(spawnNode, dax);        
    }
    // facetCollect.SpawningNode.MyChannel.MyRing.CollectAllPickupFacets();        
    public static void ActivateFacetCollectType(eFacetCollectTypes type, Player player)
    {
        switch(type)
        {
            case eFacetCollectTypes.RING:
                player.CurChannel.MyRing.CollectAllPickupFacets();
                break;
        }
    }
}