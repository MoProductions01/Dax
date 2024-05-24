using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FacetCollect : BoardObject
{
    public enum eFacetCollectTypes { RING, WHEEL };
    public static List<string> FACET_COLLECT_STRINGS = new List<string> {"Facet_Collect_Ring", "Facet_Collect_Wheel"};

    [Header("Facet Collect Data")]
    public eFacetCollectTypes FacetCollectType;
    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {
        name = spawnNode.name + "--FacetCollect--" + FacetCollectType.ToString();
       // BoardObjectType = eBoardObjectType.FACET_COLLECT;
        base.InitForChannelNode(spawnNode, dax);        
    }
    // facetCollect.SpawningNode.MyChannel.MyRing.CollectAllPickupFacets();        
    public static void ActivateFacetCollectType(eFacetCollectTypes type, Player player)
    {
        switch(type) 
        {
            case eFacetCollectTypes.RING:
                player.CurChannel.MyRing.CollectAllFacets();
                break;
        }
    }
}