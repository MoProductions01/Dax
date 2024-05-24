using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// BoardObject is the base class for all the objects on the board
/// </summary>
public class BoardObject : MonoBehaviour
{    
    // All the types of BoardObjects that we can have
    public enum eBoardObjectType { PLAYER, FACET, HAZARD, FACET_COLLECT, SHIELD, SPEED_MOD, POINT_MOD }; 
    public eBoardObjectType BoardObjectType;

    // Strings used in the DaxEditor to show in the Inspector window
    public static List<string> BOARD_OBJECT_EDITOR_NAMES = new List<string> {"Player", "Facet", "Hazard", "Facet Collect", 
                                                                                "Shield", "Speed Mod", "Point Mod"};  

    // Movement info.  
    public enum eStartDir {OUTWARD, INWARD}; 
    public eStartDir StartDir = eStartDir.OUTWARD;
    public float Speed = 0f;    

    // Shared      
    [HideInInspector]
    public Dax _Dax;   // Root Dax game object ref               
    public Channel CurChannel = null;   // Current channel that the board object is in
           
    [Header("Starting State Stuff")]
    public ChannelNode SpawningNode = null;    // The node you spawn on 

    /// <summary>
    /// Handles generic/universal initialization for BoardObjects
    /// </summary>
    /// <param name="spawnNode">Node we're spawning on</param>
    /// <param name="dax">Root game reference</param>
    public virtual void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {                
        _Dax = dax;              
        if (spawnNode != null)
        {
            SpawningNode = spawnNode;
            spawnNode.SpawnedBoardObject = this;
            CurChannel = SpawningNode.MyChannel;
            this.transform.parent = CurChannel.MyRing.transform;         
            transform.position = SpawningNode.transform.position;                                              
            transform.LookAt(CurChannel.EndNode.transform);
        }        
    }   
         
    /// <summary>
    /// Checks for the transition from one channel to another for any moving game objects
    /// </summary>
    /// <param name="overlapColliders">List of colliders we're currently overlapping</param>
    private void CheckForNewChannel(List<Collider> overlapColliders)
    {        
        List<Collider> channelNodeColliders = overlapColliders.ToList(); // Make a new list of the colliders we're overlapping
        channelNodeColliders.RemoveAll(c => c.GetComponent<ChannelNode>() == null); // Remove anything that's not a ChannelNode
        channelNodeColliders.RemoveAll(c => c.GetComponent<ChannelNode>().CanBeOnPath() == false);   // Remove any paths we can't be on         
        channelNodeColliders.RemoveAll(c => c.GetComponent<ChannelNode>().MyChannel == CurChannel);   // Remove our channel if necessary
        channelNodeColliders.RemoveAll(c => Vector3.Dot( (c.transform.position - this.transform.position), this.transform.forward) <= 0); // Remove anything behind us                     
        if(channelNodeColliders.Count > 0)
        {
            if(channelNodeColliders.Count > 1) 
            {
                Debug.LogError("Why do we have more than 1 Channel Node Colliders?: " + channelNodeColliders.Count);                    
            }

            ChannelNode channelNode = channelNodeColliders[0].GetComponent<ChannelNode>();
            Vector3 heading = channelNode.transform.position - this.transform.position;
            float dot = Vector3.Dot(heading, this.transform.forward);         
            if(dot > 0f) // Make sure channel node is in front of us.  It should be but better safe than sorry
            {   // If we're here then we can move to a new channel
                CurChannel = channelNode.MyChannel;                
                this.transform.parent = CurChannel.MyRing.transform;                
                this.transform.position = channelNode.transform.position;
                transform.LookAt(channelNode.IsStartNode() ? channelNode.MyChannel.EndNode.transform : channelNode.MyChannel.StartNode.transform);      
            } 
            else
            {
                Debug.LogError("Why do we have a Channel Node that's behind us?");
            }             
        }      
    }

    /// <summary>
    /// Check for a moving BoardObject colliding with something and bouncing back in the other direction
    /// </summary>
    /// <param name="overlapColliders">List of colliders we're overlapping with.</param>
    private void CheckGenericBounce(List<Collider> overlapColliders)
    {
        // Player Generic (walls, etc)        
        overlapColliders.RemoveAll(c => c.gameObject.layer != LayerMask.NameToLayer("Player Generic Collider"));  
        // Remove anything behind us
        overlapColliders.RemoveAll(c => Vector3.Dot( (c.transform.position - this.transform.position), this.transform.forward) <= 0);            
        if(overlapColliders.Count > 0)
        {               
            if(overlapColliders.Count > 1) 
            {   // Sort overlapping colliders by distance     
                overlapColliders = overlapColliders.OrderBy(o => Vector3.Distance(o.transform.position, this.transform.position)).ToList();                   
            }

            Collider collider = overlapColliders[0];
            Vector3 heading = collider.transform.position - this.transform.position;
            float dot = Vector3.Dot(heading, this.transform.forward);         
            if(dot > 0f)
            {   // If the colliders is in front of us then bounce back in the other direction                      
                transform.forward = -transform.forward;         
            } 
            else
            {
                Debug.LogError("Why do we have a Player Generic Collider that's behind us?");
            }                                          
        }
    }

    /// <summary>
    /// Handles colliding with a bumper
    /// </summary>
    /// <param name="bumperColliders">Bumper colliders we're overlapping with</param>
    private void CheckBumpers(List<Collider> bumperColliders)
    {        
        // Quick error checking
        for(int i=0; i<bumperColliders.Count; i++)
        {
            if(bumperColliders[i] == null)
            {
                Debug.LogError("Null bumper collider");
                return;
            }
        }
        // Remove all colliders that aren't bumpers
        bumperColliders.RemoveAll(x => x.GetComponent<Bumper>() == null);
        if (bumperColliders.Count > 0)
        {
            Bumper bumper = bumperColliders[0].GetComponent<Bumper>();
            if (this.BoardObjectType == eBoardObjectType.PLAYER)
            {   // Only Players have to check for things like matching facet colors.  Enemies just bounce regardless
                Player player = FindObjectOfType<Player>();
                switch (bumper.BumperType)
                {
                    case Bumper.eBumperType.DEATH:
                        // Collided with a Death bumper so game over
                        FindObjectOfType<Dax>().EndGame("Death Bumper!");
                        break;
                    case Bumper.eBumperType.COLOR_MATCH:
                        // Color match collider so check it against any facets the player is carrying
                        if (player.CarriedColorFacet == null) break; // No facet being carried so bail
                        Facet playerCarriedColorFacet = (Facet)player.CarriedColorFacet;
                        if (playerCarriedColorFacet._Color == bumper.BumperColor)
                        {   // Player is carrying a facet that's the same color as the bumper so update game state                           
                            FindObjectOfType<Dax>().Wheel.MatchedFacetColor(playerCarriedColorFacet);                            
                            DestroyImmediate(playerCarriedColorFacet.gameObject);
                            if(_Dax.Wheel.VictoryCondition == Dax.eVictoryConditions.COLOR_MATCH) _Dax.Wheel.CheckVictoryConditions();
                        }
                        break;
                }
            }
            // Every object bounces back to the other direction
            transform.forward = -transform.forward;             
        }
    }

    private void CheckBoardObjects(List<Collider> boardObjectColliders)
    {        
        Player player = FindObjectOfType<Player>();
        boardObjectColliders.RemoveAll(x => x.GetComponentInParent<BoardObject>() == null);                
        boardObjectColliders.Remove(this.gameObject.GetComponent<Collider>());
        // monote - might not need this
        if (player.ActiveShield != null) boardObjectColliders.Remove(player.ActiveShield.GetComponent<Collider>());
        if (boardObjectColliders.Count == 0) return; // bail if no collisions

        BoardObject bo = boardObjectColliders[0].GetComponentInParent<BoardObject>(); // monote - maybe loop over this
        switch(bo.BoardObjectType) 
        {
            case eBoardObjectType.FACET:                            
                Facet facet = boardObjectColliders[0].GetComponentInParent<Facet>();
                //if(facet._Color == Facet.eFacetColors.WHITE)
                if(_Dax.Wheel.VictoryCondition == Dax.eVictoryConditions.COLLECTION)
                {
                    //Debug.LogError("Pickup facet collect");
                    facet.SpawningNode.SpawnedBoardObject = null;
                    //curSphereColliders.Remove(facet.GetComponentInChildren<Collider>()); // moupdate - loook in making this a function
                    this._Dax.Wheel.CollectFacet(facet);                                
                    //DestroyImmediate(facet.gameObject);
                    //_Dax.CurWheel.CheckVictoryConditions();
                }
                else
                {
                    //Debug.LogError("Color facet collect");
                    if (player.CarriedColorFacet != null) { /*Debug.Log("you already have a facet: " + playerDiode.CarriedFacet.name);*/ }
                    else player.CarriedColorFacet = bo;
                }
                break;  
            case eBoardObjectType.HAZARD:
                Hazard hazard = boardObjectColliders[0].GetComponentInParent<Hazard>();
                if (hazard == player.TempEnemyIgnore) break; // moupdate = make sure this works
                switch(hazard.HazardType)
                {
                    case Hazard.eHazardType.ENEMY:                    
                    case Hazard.eHazardType.DYNAMITE:                    
                        void DestroyShield()
                        {
                            //curSphereColliders.Remove(player.ActiveShield.GetComponent<Collider>());
                            DestroyImmediate(player.ActiveShield.gameObject);
                            player.ActiveShield = null;
                        }
                        void DestroyHazard(Hazard enemy)
                        {
                            //curSphereColliders.Remove(enemy.GetComponentInChildren<Collider>());
                            DestroyImmediate(enemy.gameObject);
                            _Dax.AddPoints(5);
                        }
                        if (hazard == player.TempEnemyIgnore) break;
                        if (player.ActiveShield != null)
                        {
                            switch (player.ActiveShield.ShieldType)
                            {
                                case Shield.eShieldTypes.HIT:
                                    DestroyShield();
                                    player.TempEnemyIgnore = hazard;
                                    break;
                                case Shield.eShieldTypes.SINGLE_KILL:
                                    DestroyShield();
                                    DestroyHazard(hazard);
                                    break;                             
                            }
                        }
                        else
                        {   // no shield so die already
                            if(hazard.HazardType == Hazard.eHazardType.ENEMY) FindObjectOfType<Dax>().EndGame("Killed By Enemy");                            
                            else if (hazard.HazardType == Hazard.eHazardType.DYNAMITE) FindObjectOfType<Dax>().EndGame("Killed By Dynamite");                            
                        }
                        break;
                    case Hazard.eHazardType.GLUE:
                            if (hazard == player.TempEnemyIgnore) break;
                            if (player.ActiveShield != null)
                            {
                                switch (player.ActiveShield.ShieldType)
                                {
                                    case Shield.eShieldTypes.HIT:
                                        DestroyShield();
                                        player.TempEnemyIgnore = hazard;
                                        break;
                                    case Shield.eShieldTypes.SINGLE_KILL:
                                        DestroyShield();
                                        DestroyHazard(hazard);
                                        break;                                    
                                }
                            }
                            else
                            {   // no shield so stay frozen                               
                                //FindObjectOfType<Dax>().EndGame("Killed By Enemy");                                        
                                //moving, enemy that ‘stuns’ the player leaving them frozen in place for x seconds
                                //Debug.Log("do that emp shit");
                                player.EMPHit(hazard.EffectTime);
                                DestroyHazard(hazard);
                            }
                            break;                        
                }                        
            break;
            case eBoardObjectType.SHIELD:
                // if (player.ActiveShield != null) break; // already have a shield                                                        
                Shield shield = boardObjectColliders[0].GetComponentInParent<Shield>();                
                if( player.AddShield(shield) == true )
                {
                    //shield.SpawningNode.SpawnedBoardObject = null;
                    // curSphereColliders.Remove(shield.GetComponentInChildren<Collider>());
                    // DestroyImmediate(shield.gameObject);
                    _Dax.AddPoints(5);
                }                                                        
                break;
            case eBoardObjectType.FACET_COLLECT:
                FacetCollect facetCollect = boardObjectColliders[0].GetComponentInParent<FacetCollect>();
                if(player.AddFacetCollect(facetCollect) == true)
                {
                   // facetCollect.SpawningNode.SpawnedBoardObject = null;
                    //curSphereColliders.Remove(facetCollect.GetComponentInChildren<Collider>());
                   // DestroyImmediate(facetCollect.gameObject);
                    _Dax.AddPoints(5);
                }                                                                           
                break;
            case eBoardObjectType.POINT_MOD:
                    PointMod pointMod = boardObjectColliders[0].GetComponentInParent<PointMod>();
                    switch(pointMod.PointModType)
                    {
                        case PointMod.ePointModType.EXTRA_POINTS:
                            _Dax.AddPoints(pointMod.PointModVal);
                            break;
                        case PointMod.ePointModType.POINTS_MULTIPLIER:
                            _Dax.BeginPointMod(pointMod.PointModTime, pointMod.PointModVal);
                            break;
                    }                    
                    DestroyImmediate(pointMod.gameObject);
                    break;
            case eBoardObjectType.SPEED_MOD:
                SpeedMod speedMod = boardObjectColliders[0].GetComponentInParent<SpeedMod>();
                List<GameObject> gameObjectsToMod = new List<GameObject>();
                // Transform objectsToSpeedModParent;
                switch(speedMod.SpeedModType)
                {
                    case SpeedMod.eSpeedModType.PLAYER_SPEED:
                        //Debug.Log("Speed before: " + player.Speed);
                        player.Speed += speedMod.SpeedModVal;
                        //Debug.Log("Speed after: " + player.Speed);
                        break;
                  //  case SpeedMod.eSpeedModType.SPEED_DOWN:
                    //    player.Speed -= speedMod.SpeedModVal;
                   //     break;
                    case SpeedMod.eSpeedModType.ENEMY_SPEED:
                   // case SpeedMod.eSpeedModType.ENEMY_DOWN:
                        List<Hazard> enemies = _Dax.Wheel.GetComponentsInChildren<Hazard>().ToList();
                        float speedModVal = (speedMod.SpeedModType == SpeedMod.eSpeedModType.ENEMY_SPEED ? speedMod.SpeedModVal : -speedMod.SpeedModVal); // moupdate optimize all this
                        foreach (Hazard e in enemies) e.Speed += speedModVal; // moupdate - optimize this --- use generic <T> or an overwritten activate.  maybe go back to one class for each and have a generic activate overwriten                                                                                                             
                        break;
                    case SpeedMod.eSpeedModType.RING_SPEED:
                  //  case SpeedMod.eSpeedModType.RING_DOWN:
                        float speedModVal2 = (speedMod.SpeedModType == SpeedMod.eSpeedModType.RING_SPEED ? speedMod.SpeedModVal : -speedMod.SpeedModVal);
                        player.CurChannel.MyRing.RotateSpeed += speedModVal2;
                        break;                 
                }
                speedMod.SpawningNode.SpawnedBoardObject = null;
                boardObjectColliders.Remove(speedMod.GetComponentInChildren<Collider>());
                DestroyImmediate(speedMod.gameObject);
                _Dax.AddPoints(5);
            break;    
        }
        
        #if false            
        // Check Player collisions against BoardObjects
        if(this.BoardObjectType == eBoardObjectType.PLAYER) 
        {
            List<Collider> boardObjectColliders = curSphereColliders.ToList();                
            boardObjectColliders.RemoveAll(x => x.GetComponentInParent<BoardObject>() == null);                
            boardObjectColliders.Remove(this.gameObject.GetComponent<Collider>());
            if (player.ActiveShield != null) boardObjectColliders.Remove(player.ActiveShield.GetComponent<Collider>());
            if (boardObjectColliders.Count > 0)
            {                    
                BoardObject bo = boardObjectColliders[0].GetComponentInParent<BoardObject>();
                switch(bo.BoardObjectType) 
                {                                            
                    case eBoardObjectType.HAZARD:
                        Hazard hazard = boardObjectColliders[0].GetComponentInParent<Hazard>();
                        if (hazard == player.TempEnemyIgnore) break; // moupdate = make sure this works
                        switch(hazard.HazardType)
                        {
                            case Hazard.eHazardType.ENEMY:
                            // case Hazard.eHazardType.BOMB:
                            case Hazard.eHazardType.DYNAMITE:
                            // case Hazard.eHazardType.PROXIMITY_MINE:
                                void DestroyShield()
                                {
                                    curSphereColliders.Remove(player.ActiveShield.GetComponent<Collider>());
                                    DestroyImmediate(player.ActiveShield.gameObject);
                                    player.ActiveShield = null;
                                }
                                void DestroyHazard(Hazard enemy)
                                {
                                    curSphereColliders.Remove(enemy.GetComponentInChildren<Collider>());
                                    DestroyImmediate(enemy.gameObject);
                                    _Dax.AddPoints(5);
                                }
                                if (hazard == player.TempEnemyIgnore) break;
                                if (player.ActiveShield != null)
                                {
                                    switch (player.ActiveShield.ShieldType)
                                    {
                                        case Shield.eShieldTypes.HIT:
                                            DestroyShield();
                                            player.TempEnemyIgnore = hazard;
                                            break;
                                        case Shield.eShieldTypes.SINGLE_KILL:
                                            DestroyShield();
                                            DestroyHazard(hazard);
                                            break;
                                        case Shield.eShieldTypes.TIMED:                                                
                                            break;
                                        case Shield.eShieldTypes.TIMED_KILL:
                                            DestroyHazard(hazard);
                                            break;
                                    }
                                }
                                else
                                {   // no shield so die already
                                    if(hazard.HazardType == Hazard.eHazardType.ENEMY) FindObjectOfType<Dax>().EndGame("Killed By Enemy");
                                    // else if(hazard.HazardType == Hazard.eHazardType.BOMB ) FindObjectOfType<Dax>().EndGame("Killed By Bomb");
                                    else if (hazard.HazardType == Hazard.eHazardType.DYNAMITE) FindObjectOfType<Dax>().EndGame("Killed By Dynamite");
                                    //else if (hazard.HazardType == Hazard.eHazardType.PROXIMITY_MINE) FindObjectOfType<Dax>().EndGame("Killed By Proximity Mine");
                                }
                                break;
                            //case Hazard.eHazardType.TIMED_MINE:
                                //  hazard.ActivateTimer();
                                //  break;
                            case Hazard.eHazardType.EMP:
                                if (hazard == player.TempEnemyIgnore) break;
                                if (player.ActiveShield != null)
                                {
                                    switch (player.ActiveShield.ShieldType)
                                    {
                                        case Shield.eShieldTypes.HIT:
                                            DestroyShield();
                                            player.TempEnemyIgnore = hazard;
                                            break;
                                        case Shield.eShieldTypes.SINGLE_KILL:
                                            DestroyShield();
                                            DestroyHazard(hazard);
                                            break;
                                        case Shield.eShieldTypes.TIMED:
                                            break;
                                        case Shield.eShieldTypes.TIMED_KILL:
                                            DestroyHazard(hazard);
                                            break;
                                    }
                                }
                                else
                                {   // no shield so stay frozen                               
                                    //FindObjectOfType<Dax>().EndGame("Killed By Enemy");                                        
                                    //moving, enemy that ‘stuns’ the player leaving them frozen in place for x seconds
                                    //Debug.Log("do that emp shit");
                                    player.EMPHit(hazard.EffectTime);
                                    DestroyHazard(hazard);
                                }
                                break;
                        }
                        break;
                    case eBoardObjectType.SHIELD:
                        // if (player.ActiveShield != null) break; // already have a shield                                                        
                        Shield shield = boardObjectColliders[0].GetComponentInParent<Shield>();
                        if( player.AddShield(shield) == true )
                        {
                            //shield.SpawningNode.SpawnedBoardObject = null;
                            // curSphereColliders.Remove(shield.GetComponentInChildren<Collider>());
                            // DestroyImmediate(shield.gameObject);
                            _Dax.AddPoints(5);
                        }                                                        
                        break;
                    case eBoardObjectType.FACET_COLLECT:
                        FacetCollect facetCollect = boardObjectColliders[0].GetComponentInParent<FacetCollect>();
                        if(player.AddFacetCollect(facetCollect.FacetCollectType) == true)
                        {
                            facetCollect.SpawningNode.SpawnedBoardObject = null;
                            curSphereColliders.Remove(facetCollect.GetComponentInChildren<Collider>());
                            DestroyImmediate(facetCollect.gameObject);
                            _Dax.AddPoints(5);
                        }                                                                           
                        break;
                    case eBoardObjectType.FACET:                            
                        Facet facet = boardObjectColliders[0].GetComponentInParent<Facet>();
                        if(facet._Color == Facet.eFacetColors.WHITE)
                        {
                            //Debug.LogError("Pickup facet collect");
                            facet.SpawningNode.SpawnedBoardObject = null;
                            curSphereColliders.Remove(facet.GetComponentInChildren<Collider>()); // moupdate - loook in making this a function
                            this._Dax.CurWheel.CollectPickupFacet(facet);                                
                            //DestroyImmediate(facet.gameObject);
                            //_Dax.CurWheel.CheckVictoryConditions();
                        }
                        else
                        {
                            //Debug.LogError("Color facet collect");
                            if (player.CarriedColorFacet != null) { /*Debug.Log("you already have a facet: " + playerDiode.CarriedFacet.name);*/ }
                            else player.CarriedColorFacet = bo;
                        }
                        break;                                                
                    case eBoardObjectType.SPEED_MOD:
                        SpeedMod speedMod = boardObjectColliders[0].GetComponentInParent<SpeedMod>();
                        List<GameObject> gameObjectsToMod = new List<GameObject>();
                        // Transform objectsToSpeedModParent;
                        switch(speedMod.SpeedModType)
                        {
                            case SpeedMod.eSpeedModType.SPEED_UP:
                                player.Speed += speedMod.SpeedModVal;
                                break;
                            case SpeedMod.eSpeedModType.SPEED_DOWN:
                                player.Speed -= speedMod.SpeedModVal;
                                break;
                            case SpeedMod.eSpeedModType.ENEMY_UP:
                            case SpeedMod.eSpeedModType.ENEMY_DOWN:
                                List<Hazard> enemies = _Dax.CurWheel.GetComponentsInChildren<Hazard>().ToList();
                                float speedModVal = (speedMod.SpeedModType == SpeedMod.eSpeedModType.ENEMY_UP ? speedMod.SpeedModVal : -speedMod.SpeedModVal); // moupdate optimize all this
                                foreach (Hazard e in enemies) e.Speed += speedModVal; // moupdate - optimize this --- use generic <T> or an overwritten activate.  maybe go back to one class for each and have a generic activate overwriten                                                                                                             
                                break;
                            case SpeedMod.eSpeedModType.RING_UP:
                            case SpeedMod.eSpeedModType.RING_DOWN:
                                float speedModVal2 = (speedMod.SpeedModType == SpeedMod.eSpeedModType.RING_UP ? speedMod.SpeedModVal : -speedMod.SpeedModVal);
                                player.CurChannel.MyRing.RotateSpeed += speedModVal2;
                                break;
                            case SpeedMod.eSpeedModType.WHEEL_UP:
                            case SpeedMod.eSpeedModType.WHEEL_DOWN:
                                float speedModVal3 = (speedMod.SpeedModType == SpeedMod.eSpeedModType.WHEEL_UP ? speedMod.SpeedModVal : -speedMod.SpeedModVal);
                                List<Ring> rings = _Dax.CurWheel.GetComponentsInChildren<Ring>().ToList();
                                foreach (Ring ring in rings) ring.RotateSpeed += speedModVal3; // moupdate - move this to the actual script so that it's not bloating up all this
                                break;                                
                            case SpeedMod.eSpeedModType.RING_STOP:
                                player.CurChannel.MyRing.RotateSpeed = 0f;
                                break;
                            case SpeedMod.eSpeedModType.TIME_STOP: // moupdate - maybe split this up from SpeedMod to EnvMod or something
                                List<Ring> rings2 = _Dax.CurWheel.GetComponentsInChildren<Ring>().ToList();
                                List<Hazard> enemies2 = _Dax.CurWheel.GetComponentsInChildren<Hazard>().ToList();
                                foreach(Ring ring in rings2) ring.RotateSpeed = 0f;
                                foreach (Hazard e in enemies2) e.Speed = 0f; ;
                                break;
                            case SpeedMod.eSpeedModType.RING_REVERSE:
                                player.CurChannel.MyRing.RotateSpeed = -player.CurChannel.MyRing.RotateSpeed;
                                break;
                            case SpeedMod.eSpeedModType.MEGA_RING_REVERSE: // moupdate - maybe rename this
                                List<Ring> rings3 = _Dax.CurWheel.GetComponentsInChildren<Ring>().ToList();
                                foreach (Ring ring in rings3) ring.RotateSpeed = -ring.RotateSpeed;
                                break;
                        }
                        speedMod.SpawningNode.SpawnedBoardObject = null;
                        curSphereColliders.Remove(speedMod.GetComponentInChildren<Collider>());
                        DestroyImmediate(speedMod.gameObject);
                        _Dax.AddPoints(5);
                        break;                                                                                                                                                                             
                    case eBoardObjectType.INTERACTABLE:
                        Interactable interactable = boardObjectColliders[0].GetComponent<Interactable>();
                        switch(interactable.InteractableType)
                        {
                            case Interactable.eInteractableType.TOGGLE:
                                transform.position += (moveDir * .17f);
                                interactable.ToggleChannelPieces(player.MoveDir);
                                //boardObjectColliders[0].GetComponent<Toggle>().ToggleChannelPieces(player.MoveDir);
                                break;
                            case Interactable.eInteractableType.SWITCH:
                                interactable.Activate();
                                //boardObjectColliders[0].GetComponent<Switch>().Activate();
                                break;
                            case Interactable.eInteractableType.WARP_GATE:
                            case Interactable.eInteractableType.WORMHOLE:
                                if (DestGateJustWarpedTo != null) break;
                                Interactable warpGate = boardObjectColliders[0].GetComponent<Interactable>();
                                int index;
                                Interactable destGate = null;
                                if (warpGate.DestGates.Count == 0)
                                {
                                    //List<WarpGate> warpGatesOnWheel = this._Dax.CurWheel.GetComponentsInChildren<WarpGate>().ToList();
                                    List<Interactable> warpGatesOnWheel = this._Dax.CurWheel.GetComponentsInChildren<Interactable>().ToList();
                                    warpGatesOnWheel.RemoveAll(x => x.InteractableType != Interactable.eInteractableType.WARP_GATE);
                                    // RedFacetsCounted = colorFacets.RemoveAll(x => x._Color == ColorFacet.eFacetColors.RED);
                                    warpGatesOnWheel.Remove(warpGate);
                                    if(warpGatesOnWheel.Count == 0) { Debug.LogError("Must have at least 1 dest gate on wheel"); break; }
                                    index = Random.Range(0, warpGatesOnWheel.Count);
                                    destGate = warpGatesOnWheel[index];
                                }
                                else
                                {
                                    index = Random.Range(0, warpGate.DestGates.Count);
                                    destGate = warpGate.DestGates[index];
                                }
                                DestGateJustWarpedTo = destGate;
                                CurChannel = destGate.SpawningNode.MyChannel;
                                this.transform.parent = CurChannel.MyRing.transform;
                                LastPositionObject.transform.parent = CurChannel.MyRing.transform;
                                this.transform.position = destGate.SpawningNode.transform.position;
                                LastPositionObject.transform.position = this.transform.position;
                                break;
                        }
                        break;                                                                 
                    }
                }             
            }                                               
            #endif
    }

    public void BoardObjectFixedUpdate(float deltaTime)
    {        
        if(CurChannel == null) {Debug.LogError("Why do we have no Channel assigned? " + this.name); return; }

        if (Speed != 0f)
        {                       
            // move along the forward vector
            transform.Translate(Vector3.forward * deltaTime * Speed);            
            // now check colliders
            List<Collider> overlapColliders = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius).ToList();
            if(overlapColliders.Count > 0)
            {                
                CheckForNewChannel(overlapColliders.ToList());             
                CheckBumpers(overlapColliders.ToList());                
                CheckGenericBounce(overlapColliders.ToList());  
                if(this.BoardObjectType == eBoardObjectType.PLAYER) 
                {
                    CheckBoardObjects(overlapColliders.ToList());
                }
                
            }                                                                                    
        }                       
    }     
}