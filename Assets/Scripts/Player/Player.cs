using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VisCircle;

/// <summary>
/// Class for the Player object
/// </summary>
public class Player : BoardObject
{    
    public static int MAX_FACETCOLLECT_OR_SHIELD = 12; // Can only have 12 FacetCollect or Shields and once

    // None of the data for the Player is saved so there's no need for [field: SerializeField]
    public Shield ActiveShield {get; set;} // The shield that's currently active (if any)
    public List<Shield> Shields {get; set;} = new List<Shield>(); // List of the collected Shields
    
    public Hazard TempEnemyIgnore {get; set;} // ignore this enemy after a shield collision until you're not collided

    public List<FacetCollect> FacetCollects {get; set;} = new List<FacetCollect>(); // List of the FacetCollect objects we have    
    public BoardObject CarriedFacet {get; set;} = null; // The Facet that we're currently carrying

    public Hazard.eHazardType EffectType {get; set;} // Holds the current effect type    
    public float GlueStickTime {get; set;} // The time a Glue hazard holds you in place    
    public float SpeedSave {get; set;} // Holds the speed the Player will return to after the Glu is done
    
    /// <summary>
    /// Resets the Player back to it's starting state
    /// </summary>
    /// <param name="playerSave">Player save data</param>
    public void ResetPlayer(DaxSaveData.BoardObjectSave playerSave = null)
    {
        // clear out player inventory
        ClearInventory();
        // Reset the UI
        if (Dax.UIRoot == null)
        {
            Debug.LogWarning("Update UIRoot on new scene"); // monote - UI stuff
        }
        else
        {
            Dax.UIRoot.DestroyFacetCollectIcon();
            Dax.UIRoot.DestroyShieldIcon();
        }
        
        transform.position = Vector3.zero; // Put player back at the center of the wheel
        if(playerSave != null)
        {   // If we have save data, then update the Player with that
            this.Speed = playerSave.Speed;
            CurChannel = GameObject.Find(playerSave.StartChannel).GetComponent<Channel>();
            this.transform.parent = CurChannel.MyRing.transform;
            this.transform.LookAt(CurChannel.StartNode.transform);            
        }
        // Null out any GameObject references
        ActiveShield = null;
        TempEnemyIgnore = null;
        CarriedFacet = null;               
    }

    /// <summary>
    /// Sets the starting channel for the Player
    /// </summary>
    /// <param name="channelIndex">Which channel is the start channel</param>
    public void SetStartChannel(int channelIndex)
    {        
        CurChannel = Dax.Wheel.Rings[0].transform.GetComponentsInChildren<Channel>().ToList()[channelIndex];
        this.transform.parent = CurChannel.MyRing.transform;
        SpawningNode = CurChannel.StartNode;
        transform.LookAt(SpawningNode.transform);
    }

    /// <summary>
    /// Returs the ChannelNode that the player will start moving towards when the game starts
    /// </summary>
    /// <returns></returns>
    public ChannelNode GetStartChannelNode()
    {
        return SpawningNode;
    }

    /// <summary>
    /// Clears out any Shield or FacetCollec objects we've gathered
    /// </summary>
    public void ClearInventory()
    {            
        Shields.Clear();
        FacetCollects.Clear();
    }

    /// <summary>
    /// Adds a FacetCollect object to our list
    /// </summary>
    /// <param name="facetCollect">FacetCollect to add</param>
    /// <returns></returns>    
    public bool AddFacetCollect(FacetCollect facetCollect)
    {
        if (FacetCollects.Count == MAX_FACETCOLLECT_OR_SHIELD) return false; // Maxed out

        Destroy(facetCollect.GetComponent<Collider>()); // Destroy the collider but keep the object to add to the list
        facetCollect.SpawningNode.SpawnedBoardObject = null; // Remove it from the ChannelNode
        facetCollect.transform.parent = this.transform; // Player is now the parent
        facetCollect.gameObject.SetActive(false); // Turn it off for now
        FacetCollects.Add(facetCollect); // Add the FacetCollect to the list
        if (FacetCollects.Count == 1) Dax.UIRoot.ChangeFacetCollectIcon(facetCollect); // Update the UI for clicking on it to activate
        return true; // We added the FacetCollect so let the calling code know
    }

    /// <summary>
    /// Activates the FacetCollect on the list
    /// </summary>
    public void ActivateFacetCollect()
    {
        if (FacetCollects.Count == 0) return;

        FacetCollect facetCollect = FacetCollects[0]; // Grab oldest FacetCollect
        FacetCollects.RemoveAt(0); // Remove from list
        Dax.UIRoot.DestroyFacetCollectIcon(); // Destroy UI icon
        if (FacetCollects.Count > 0) Dax.UIRoot.ChangeFacetCollectIcon(FacetCollects[0]); // Update UI if we have more available

        switch(facetCollect.FacetCollectType)
        {            
            case FacetCollect.eFacetCollectTypes.RING:                
                // Collect all Facets on this ring
                CurChannel.MyRing.CollectAllFacets();
                break;
            case FacetCollect.eFacetCollectTypes.WHEEL:                
                // Collect all Facets from all Rings
                foreach(Ring ring in Dax.Wheel.Rings)
                {
                    ring.CollectAllFacets();
                }
                break;
        }        
    }

    /// <summary>
    /// Adds a Shield object to our list
    /// </summary>
    /// <param name="shield"></param>
    /// <returns></returns>
    public bool AddShield(Shield shield)
    {
        if (Shields.Count == MAX_FACETCOLLECT_OR_SHIELD) return false; // Can't go over max shields

        Destroy(shield.GetComponent<Collider>()); // Destroy collider but not object so we can add it to our list
        shield.SpawningNode.SpawnedBoardObject = null; // Remove it from it's spawn node
        shield.transform.parent = this.transform; // Player is now parent
        shield.gameObject.SetActive(false); // Turn it off
        Shields.Add(shield); // Add it to our list
        if(Shields.Count == 1) Dax.UIRoot.ChangeShieldIcon(shield); // Update the UI for clicking on it to activate
        return true; // We added the Shield so let the calling code know
    }     

    /// <summary>
    /// Activates the oldest Shield on the list
    /// </summary>
    public void ActivateShield()
    {
        if (Shields.Count == 0 || ActiveShield != null) return;

        ActiveShield = Shields[0]; // Get the Shield from the list
        ActiveShield.gameObject.SetActive(true); // Turn Shield on        
        ActiveShield.transform.GetComponentInChildren<PowerUpAnimation>().enabled = false; // monote - look into this
        ActiveShield.transform.GetChild(0).transform.eulerAngles = new Vector3(-82f, 0f, 0f); 
        ActiveShield.transform.position = this.transform.position;
        ActiveShield.transform.parent = this.transform;               
        Shields.RemoveAt(0); // Remove shield from list
        Dax.UIRoot.DestroyShieldIcon(); // Destroy UI icon
        if (Shields.Count > 0) Dax.UIRoot.ChangeShieldIcon(Shields[0]); // If we have more Shields update the UI
    }

    /// <summary>
    /// Tells Player that they've hit a Glue hazard
    /// </summary>
    /// <param name="effectTime">Time the Glue hazard lasts</param>
    public void GlueHit(float effectTime)
    {
        EffectType = Hazard.eHazardType.GLUE; 
        GlueStickTime = effectTime;
        SpeedSave = Speed;
        Speed = 0f;
    }    
   
    /// <summary>
    /// This is used to see if we should stop temporarily ignoring a Hazard
    /// that we saved ourselves from with a shield.
    /// </summary>
    /// <param name="collision"></param>
    private void OnCollisionExit(Collision collision)
    {       
        if (collision.collider.gameObject.GetComponentInParent<Hazard>() != null)
        {            
            if(TempEnemyIgnore == collision.collider.gameObject.GetComponentInParent<Hazard>())
            {                
                TempEnemyIgnore = null;
            }
        }
    }
    
    /// <summary>
    /// This is used for checking a GLUe effect
    /// </summary>
    private void Update()
    {                      
        if(EffectType == Hazard.eHazardType.GLUE)
        {   // If we're being held by Glue, reduce the time and see if we can move again
            GlueStickTime -= Time.deltaTime;
            if(GlueStickTime <= 0f)
            {   // Glue time is over so turn off the Glue and get moving
                EffectType = Hazard.eHazardType.ENEMY;
                Speed = SpeedSave;
            }
        }
    }   
        
    DaxPuzzleSetup DS = null;
    private void OnDrawGizmos()
    {        
        if (DS == null) DS = FindObjectOfType<DaxPuzzleSetup>();        
        if (DS != null && DS.ShowGizmos == true)            
        {           
            if (CurChannel != null)
            {
                Gizmos.color = Color.yellow / 1.5f;
                Gizmos.DrawWireSphere(CurChannel.StartNode.transform.position, .08f);
                Gizmos.DrawWireSphere(CurChannel.MidNode.transform.position, .08f);
                Gizmos.DrawWireSphere(CurChannel.EndNode.transform.position, .08f);
            }       
        }        
    }
}
