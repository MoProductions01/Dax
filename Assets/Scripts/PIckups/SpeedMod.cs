using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// SpeedMod class
/// </summary>
public class SpeedMod : BoardObject
{
    // SpeedMod types
    // PLAYER_SPEED - modified Player speed
    // ENEMY_SPEED - modifies Enemy speed
    // RING_SPEED - modifies Ring rotatin speed
    public enum eSpeedModType { PLAYER_SPEED, ENEMY_SPEED, RING_SPEED};
    public eSpeedModType SpeedModType;       

    // The value that the SpeedMod has
    public static float DEFAULT_MOD_VAL = 2f;            
    public float SpeedModVal = DEFAULT_MOD_VAL;

    /// <summary>
    /// Overridden function for initting the SpeedMod object
    /// </summary>
    /// <param name="spawnNode">Node we're spawning on</param>
    /// <param name="dax">Dax root game object</param>
    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {       
        name = spawnNode.name + "--SpeedMod--" + SpeedModType.ToString();        
        base.InitForChannelNode(spawnNode, dax);       
    }

    /// <summary>
    /// Handles player colliding with a SpeedMod
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="facet">Facet object</param>
    public override void HandleCollisionWithPlayer(Player player, BoardObject boardObject)
    {
        SpeedMod speedMod = (SpeedMod)boardObject;           
        switch(speedMod.SpeedModType)
        {
            case SpeedMod.eSpeedModType.PLAYER_SPEED:
                // Update the player speed                
                player.Speed += speedMod.SpeedModVal;                
                break;        
            case SpeedMod.eSpeedModType.ENEMY_SPEED:
                // Get a list of all the ENEMIES on the board        
                List<Hazard> enemies = Dax.Wheel.GetComponentsInChildren<Hazard>().ToList();
                enemies.RemoveAll(c => c.HazardType != Hazard.eHazardType.ENEMY); // Remove anything that's not an ENEMY since they're the only Hazards that move                
                // Update each enemy's speed
                foreach (Hazard e in enemies) 
                {
                    e.Speed += speedMod.SpeedModVal; 
                }
                break;
            case SpeedMod.eSpeedModType.RING_SPEED:
                // Update the current Ring's speed                    
                player.CurChannel.MyRing.RotateSpeed += speedMod.SpeedModVal;
                break;                 
        }        
        // Destroy SpeedMod and give points
        DestroyImmediate(speedMod.gameObject);
        Dax.AddPoints(5);
    }
}