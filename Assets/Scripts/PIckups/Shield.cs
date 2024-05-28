using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shield class.
/// </summary>
public class Shield : BoardObject
{
    // Shield type
    // HIT - allows you to survive a hazard collision but goes away afterwards
    // SINGLE_KILL - kills the hazard you collide with but goes away afterwards
    public enum eShieldTypes { HIT, SINGLE_KILL};         
    [field: SerializeField] public eShieldTypes ShieldType {get; set;}

    /// <summary>
    /// Overridden function for initting the Shield object
    /// </summary>
    /// <param name="spawnNode">Node we're spawning on</param>
    /// <param name="dax">Dax root game object</param>
    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {       
        name = spawnNode.name + "--Shield--" + ShieldType.ToString();        
        base.InitForChannelNode(spawnNode, dax);       
    }

    /// <summary>
    /// Handles player colliding with a Shield
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="facet">Facet object</param>
    public override void HandleCollisionWithPlayer(Player player, BoardObject boardObject)
    {
        Shield shield = (Shield)boardObject;   
        // Add the shield to the player's shield collection if you're not full           
        if( player.AddShield(shield) == true )
        {                        
            Dax.AddPoints(5); // Give player some points
        }
    }
}