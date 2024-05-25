using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Facet class.  These are the things you collect or color match to win the game
/// </summary>
public class Facet : BoardObject
{
    // Which color facet we are
    public enum eFacetColors { RED, GREEN, BLUE, YELLOW, PURPLE, ORANGE };
    public eFacetColors _Color;

    /// <summary>
    /// Overridden function for initting the Facet object
    /// </summary>
    /// <param name="spawnNode">Node we're spawning on</param>
    /// <param name="dax">Dax root game object</param>
    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {
        name = spawnNode.name + "--Facet";
        BoardObjectType = eBoardObjectType.FACET;       
        base.InitForChannelNode(spawnNode, dax);
    }

    /// <summary>
    /// Handles player colliding with a facet
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="facet">Facet object</param>
    public override void HandleCollisionWithPlayer(Player player, BoardObject boardObject)
    {
        Facet facet = (Facet)boardObject; // Get a ref to the facet
        if(Dax.Wheel.VictoryCondition == Dax.eVictoryConditions.COLLECTION)
        {   // COLLECTION victory condition so collect the facet
            facet.SpawningNode.SpawnedBoardObject = null;                     
            this.Dax.Wheel.CollectFacet(facet);                                                                                
        }
        else
        {   // COLOR_MATCH condition so start carrying the facet if you 
            // aren't already carrying one  
            if(player.CarriedFacet == null)
            {
                facet.transform.position = player.transform.position + (player.transform.up * .1f);    
                facet.transform.parent = player.transform;                
                player.CarriedFacet = facet;                
            }                                            
        }
    }
}