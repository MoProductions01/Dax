using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Hazard class.  
/// </summary>
public class Hazard : BoardObject
{
    // Some statics for the time the hazard can be active
    public static float DEFAULT_EFFECT_TIME = 2f; 
    public static float MAX_EFFECT_TIME = 10f;
    public static float MIN_EFFECT_TIME = .1f;
    public float EffectTime = DEFAULT_EFFECT_TIME;           

    // Type of Hazard we are.
    // ENEMY - moves around board and kills you unless you have a shield
    // DYNAMITE - stays still on board and kills you unless you have shield
    // GLUE - stays still on board and makes you get stuck unless you have a shield
    public enum eHazardType { ENEMY, DYNAMITE, GLUE};
    [field: SerializeField] public eHazardType HazardType {get; set;}      

    /// <summary>
    /// Overridden function for initting the Hazard object
    /// </summary>
    /// <param name="spawnNode">Node we're spawning on</param>
    /// <param name="dax">Dax root game object</param>
    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {        
        name = spawnNode.name + "--Hazard--" + HazardType.ToString();
        base.InitForChannelNode(spawnNode, dax);
    }        

    /// <summary>
    /// Handles player colliding with a Hazard
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="facet">Facet object</param>
    public override void HandleCollisionWithPlayer(Player player, BoardObject boardObject)
    {        
        Hazard hazard = (Hazard)boardObject; // Get a ref to the hazard
//        Debug.Log("Hazard.HandleCollision() type: " + hazard.HazardType);
        // Check if we're temporarily ignoring this due to a shield collision
        if (hazard == player.TempEnemyIgnore) return; 
        // A couple of local functions to handle destroying shields or hazards                   
        // Destroy the shield if we had one going during the collision
        void DestroyShield()
        {            
            DestroyImmediate(player.ActiveShield.gameObject);
            player.ActiveShield = null;
        }
        // Destroy the hazard and add points
        void DestroyHazard(Hazard enemy)
        {                    
            DestroyImmediate(enemy.gameObject);
            Dax.AddPoints(5);
        }

        if (player.ActiveShield != null)
        {   // Collided with hazard but you have a shield
           // FindObjectOfType<VFX>().PlayShieldImpactCollectVFX(player.ActiveShield.ShieldType, this.transform.position);  modelete
           VFXPlayer.PlayShieldImpactVFX(player.ActiveShield.ShieldType, this.transform.position);
            switch (player.ActiveShield.ShieldType)
            {   
                case Shield.eShieldTypes.HIT:
                    // HIT just destroys the shield                                            
                    DestroyShield();
                    player.TempEnemyIgnore = hazard; // Temporarily ignore the hazard you collided with that destroys the shield
                    SoundFXPlayer.PlaySoundFX("ShieldCollideHit", .8f);
                    break;
                case Shield.eShieldTypes.SINGLE_KILL:
                    // Kill shield so destroy the shield and the hazard                    
                    DestroyShield();
                    DestroyHazard(hazard);
                    SoundFXPlayer.PlaySoundFX("ShieldCollideSingleKill", .8f);
                    break;                             
            }
        }
        else
        {   // No shield
            if(hazard.HazardType == eHazardType.GLUE)
            {
                // Collided with GLUE so player gets stuck for the time specified on the object
                SoundFXPlayer.PlaySoundFX("Glue", .4f);
                player.GlueHit(hazard.EffectTime);
                DestroyHazard(hazard);
            }
            else
            {
                VFXPlayer.PlayHazardVFX(hazard.HazardType, this.transform.position);
                // Collided with an Enemy or Dynamite so you're toast
                if(hazard.HazardType == Hazard.eHazardType.ENEMY) 
                {
                    //FindObjectOfType<VFX>().PlayHazardVFX(Hazard.eHazardType.ENEMY, this.transform.position);                         
                    //FindObjectOfType<Dax>().EndGame("Killed By Enemy");    
                    SoundFXPlayer.PlaySoundFX("EnemyDeath", .8f);                
                    Dax.EndGame("Killed By Enemy", false);                       
                    //DestroyHazard(hazard);                    
                }
                else if (hazard.HazardType == Hazard.eHazardType.DYNAMITE) 
                {
                    //FindObjectOfType<VFX>().PlayHazardVFX(Hazard.eHazardType.DYNAMITE, this.transform.position);                      modelete
                    //FindObjectOfType<Dax>().EndGame("Killed By Dynamite");                                                                       
                    SoundFXPlayer.PlaySoundFX("Dynamite", .8f);
                    Dax.EndGame("Killed By Dynamite", false);                    
                    DestroyHazard(hazard); 
                }
            }
            
        }         
    }
}
