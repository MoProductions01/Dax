using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PointMod class.  
/// </summary>
public class PointMod : BoardObject 
{
    // PointMod types
    // EXTRA_POINTS - gives player a specified amount of points
    // POINTS_MULTIPLLIER - multiplies any points the player gets by this amount
    public enum ePointModType { EXTRA_POINTS, POINTS_MULTIPLIER };    
    [field: SerializeField] public ePointModType PointModType {get; set;}    
    
    [field: SerializeField] public int PointModVal {get; set;} // Num of points to give or the multiplier
    [field: SerializeField] public float PointModTime {get; set;} = 5f; // Time the point mod is active if multiplier

    /// <summary>
    /// Overridden function for initting the PointMod object
    /// </summary>
    /// <param name="spawnNode">Node we're spawning on</param>
    /// <param name="dax">Dax root game object</param>
    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {
        name = spawnNode.name + "--PointMod--" + PointModType.ToString();        
        base.InitForChannelNode(spawnNode, dax);
    }

    /// <summary>
    /// Handles player colliding with a Hazard
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="facet">Facet object</param>
    public override void HandleCollisionWithPlayer(Player player, BoardObject boardObject)
    {
        PointMod pointMod = (PointMod)boardObject;
        //FindObjectOfType<VFX>().PlayPointModVFX(pointMod.PointModType, this.transform.position); modelete
        VFXPlayer.PlayPointModVFX(pointMod.PointModType, this.transform.position);
        switch(pointMod.PointModType)
        {
            case PointMod.ePointModType.EXTRA_POINTS:
                // Give player specified points                
                Dax.AddPoints(pointMod.PointModVal);
                SoundFXPlayer.PlaySoundFX("PointModPickupExtraPoints", .8f);
                break;
            case PointMod.ePointModType.POINTS_MULTIPLIER:
                // Start a point multiplier for the specified time
                Dax.BeginPointMod(pointMod.PointModTime, pointMod.PointModVal);
                SoundFXPlayer.PlaySoundFX("PointModPickupPointMult", .8f);
                break;
        }                    
        // Destroy game object right away
        DestroyImmediate(pointMod.gameObject);
    }

    
}