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
    public Shield ActiveShield = null; // The shield that's currently active (if any)
    public List<Shield> Shields = new List<Shield>(); // List of the collected Shields
    public Hazard TempEnemyIgnore = null; // ignore this enemy after a shield collision until you're not collided

    public List<FacetCollect> FacetCollects = new List<FacetCollect>(); // List of the FacetCollect objects we have    
    public BoardObject CarriedFacet = null; // The Facet that we're currently carrying

    public Hazard.eHazardType EffectType; // Holds the current effect type
    
    public float GlueStickTime; // The time a Glue hazard holds you in place    
    public float SpeedSave; // Holds the speed the Player will return to after the Glu is done

    public static int MAX_FACETCOLLECT_OR_SHIELD = 12; // Can only have 12 FacetCollect or Shields and once

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
    /// Clears out any Shield or FacetCollec objects we've gathered
    /// </summary>
    public void ClearInventory()
    {
       // for(int i=0; i < Shields.Count; i++) DestroyImmediate(Shields[i].gameObject);        
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
        if (FacetCollects.Count == 1) _Dax._UIRoot.ChangeFacetCollectIcon(facetCollect); // Update the UI for clicking on it to activate
        return true; // We added the FacetCollect so let the calling code know
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
        if(Shields.Count == 1) _Dax._UIRoot.ChangeShieldIcon(shield); // Update the UI for clicking on it to activate
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
       // ActiveShield.transform.parent = this.transform;
       // ActiveShield.transform.position = this.transform.position;         
        Shields.RemoveAt(0); // Remove shield from list
        _Dax._UIRoot.DestroyShieldIcon(); // Destroy UI icon
        if (Shields.Count > 0) _Dax._UIRoot.ChangeShieldIcon(Shields[0]); // If we have more Shields update the UI
    }

    /// <summary>
    /// Activates the FacetCollect on the list
    /// </summary>
    public void ActivateFacetCollect()
    {
        if (FacetCollects.Count == 0) return;

        FacetCollect facetCollect = FacetCollects[0]; // Grab oldest FacetCollect
        FacetCollects.RemoveAt(0); // Remove from list
        _Dax._UIRoot.DestroyFacetCollectIcon(); // Destroy UI icon
        if (FacetCollects.Count > 0) _Dax._UIRoot.ChangeFacetCollectIcon(FacetCollects[0]); // Update UI if we have more available

        switch(facetCollect.FacetCollectType)
        {            
            case FacetCollect.eFacetCollectTypes.RING:                
                // Collect all Facets on this ring
                CurChannel.MyRing.CollectAllFacets();
                break;
            case FacetCollect.eFacetCollectTypes.WHEEL:                
                // Collect all Facets from all Rings
                foreach(Ring ring in _Dax.Wheel.Rings)
                {
                    ring.CollectAllFacets();
                }
                break;
        }
        
    }
    
    private void LateUpdate() // monote
{        
   // string s = "pos: " + transform.position + ", localPos: " + transform.localPosition + "\n";
    //s += "forward: " + transform.forward + ", CurChannel: " + CurChannel.name;
     //RRDManager.SetText(s, RifRafDebug.eDebugTextType.GAME_STATE);   
    
    if(CarriedFacet != null)
    {
        CarriedFacet.transform.position = this.transform.position + (this.transform.up * .1f);
        /*if(MoveDir == eMoveDir.OUTWARD)
        {
            CarriedColorFacet.transform.position = this.transform.position - (this.transform.forward * .1f);
        }            
        else
        {
            CarriedColorFacet.transform.position = this.transform.position + (this.transform.forward * .1f);
        }*/
    }
    if (ActiveShield != null)
    {
        ActiveShield.transform.position = this.transform.position;            
    }
    
}

    private void OnCollisionExit(Collision collision)
    {
        //string s = "Player.OnCollisionExit() collision name: " + collision.collider.name + ", collision parent name: " + collision.collider.transform.parent.name + ", Dest Gate: ";
        //s += (DestGateJustWapredTo == null ? "no dest gate" : DestGateJustWapredTo.name);
        //Debug.Log(s);        
      /*  if(DestGateJustWarpedTo != null && collision.collider.name.Equals(DestGateJustWarpedTo.name))
        {   // this is so we don't warp back 'n forth between gates after a warp            
            DestGateJustWarpedTo = null;
        }    */
        if (collision.collider.gameObject.GetComponentInParent<Hazard>() != null)
        {            
            if(TempEnemyIgnore == collision.collider.gameObject.GetComponentInParent<Hazard>())
            {                
                TempEnemyIgnore = null;
            }
        }
    }
    
    private void Update()
    {   
        /*if(ActiveShield != null && (ActiveShield.ShieldType == Shield.eShieldTypes.TIMED || ActiveShield.ShieldType == Shield.eShieldTypes.TIMED_KILL))
        {
            ActiveShield.Timer -= Time.deltaTime;
            if (ActiveShield.Timer <= 0f)
            {
                DestroyImmediate(ActiveShield.gameObject);
                ActiveShield = null;
            }
        } */
           
        if(EffectType == Hazard.eHazardType.GLUE)
        {
            GlueStickTime -= Time.deltaTime;
            if(GlueStickTime <= 0f)
            {
                EffectType = Hazard.eHazardType.ENEMY; // moupdate
                Speed = SpeedSave;
            }
        }
    }   

    public void ResetPlayer(Dax.BoardObjectSave playerSave = null)
    {
        // clear out player inventory
        ClearInventory();
        if (_Dax._UIRoot == null)
        {
            Debug.LogWarning("Update UIRoot on new scene");
        }
        else
        {
            _Dax._UIRoot.DestroyFacetCollectIcon();
            _Dax._UIRoot.DestroyShieldIcon();
        }
        
        transform.position = Vector3.zero;
        if(playerSave != null)
        {            
            this.Speed = playerSave.Speed;
            CurChannel = GameObject.Find(playerSave.StartChannel).GetComponent<Channel>();
            this.transform.parent = CurChannel.MyRing.transform;
            this.transform.LookAt(CurChannel.StartNode.transform);
            //this.MoveDir = playerSave.MoveDir; monewsave
        }
        ActiveShield = null;
        TempEnemyIgnore = null;
        CarriedFacet = null;               
    }

    public void SetStartChannel(int channelIndex)
    {
        //Debug.Log("Player.SetStartChannel: channelIndex: " + channelIndex);
        CurChannel = _Dax.Wheel.Rings[0].transform.GetComponentsInChildren<Channel>().ToList()[channelIndex];
        this.transform.parent = CurChannel.MyRing.transform;
        SpawningNode = CurChannel.StartNode;
        transform.LookAt(SpawningNode.transform);
    }

    public ChannelNode GetStartChannelNode()
    {
        return SpawningNode;
    }



#if true
    DaxPuzzleSetup DS = null;
    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.green;
        //Gizmos.DrawSphere(transform.position + transform.forward * .1f, .04f);
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
#endif
}
