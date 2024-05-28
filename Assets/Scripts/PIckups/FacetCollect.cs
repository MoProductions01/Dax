using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FacetCollect class
/// </summary>
public class FacetCollect : BoardObject
{
    // Type of FacetCollect we are
    // RING - collects all facets on current Ring
    // WHEEL - collects all facets on wheel.  Wins game
    public enum eFacetCollectTypes { RING, WHEEL };
    [field: SerializeField] public eFacetCollectTypes FacetCollectType {get; set;}    
    
    /// <summary>
    /// Overridden function for initting the FacetCollect object
    /// </summary>
    /// <param name="spawnNode">Node we're spawning on</param>
    /// <param name="dax">Dax root game object</param>
    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {
        name = spawnNode.name + "--FacetCollect--" + FacetCollectType.ToString();       
        base.InitForChannelNode(spawnNode, dax);        
    }

    /// <summary>
    /// Handles player colliding with a FacetCollect
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="facet">Facet object</param>
    public override void HandleCollisionWithPlayer(Player player, BoardObject boardObject)
    {
        FacetCollect facetCollect = (FacetCollect)boardObject;
        // Add the FacetCollect to the player if you're not full
        if(player.AddFacetCollect(facetCollect) == true)
        {        
            Dax.AddPoints(5); // Give player some points
        }     
    }    
}