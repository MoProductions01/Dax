using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoardObject : MonoBehaviour
{    
    public enum eBoardObjectType { PLAYER, FACET, HAZARD, FACET_COLLECT, SHIELD, SPEED_MOD, GAME_MOD };
    public eBoardObjectType BoardObjectType;

    public enum eStartDir {OUTWARD, INWARD}; // monewsave
    public eStartDir StartDir = eStartDir.OUTWARD;

    // Shared      
    [HideInInspector]
    public Dax _Dax;                  
    public Channel CurChannel = null;
       
    public float Speed = 0f;    

    [Header("Starting State Stuff")]
    public ChannelNode SpawningNode = null;    

    //public Interactable DestGateJustWarpedTo = null;

    private void Awake()
    {
       // Debug.Log("Awake() Diode: " + this.name + " of type: " + DiodeType.ToString());   
    }

    private void Start()
    {
       // Debug.Log("Start() Diode: " + this.name + " of type: " + DiodeType.ToString());
    }

    /*public void SetMovementInfo(eMoveDir movedir, float speed)
    {
        MoveDir = movedir;
        Speed = speed;
    }*/

    // monote - change to array
    public string GetBoardObjectName()
    {
        switch(BoardObjectType)
        {
            case eBoardObjectType.PLAYER: return "Player";
            case eBoardObjectType.HAZARD: return "Hazard";            
            case eBoardObjectType.FACET: return "Facet";
            case eBoardObjectType.FACET_COLLECT: return "Facet Collect";             
            //case eBoardObjectType.INTERACTABLE: return "Interactable";
            case eBoardObjectType.SHIELD: return "Shield";
            case eBoardObjectType.SPEED_MOD: return "Speed Mod";
            case eBoardObjectType.GAME_MOD: return "Game Mod";
            default: return "ERROR: UNKNOWN Board Object TYPE: " + BoardObjectType.ToString();
        }
    }    

    public virtual void InitForPlayer(Player player, Dax dax)
    {
        _Dax = dax;
    }
    public virtual void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {        
        _Dax = dax;       
       
        if (spawnNode != null) // if it's not the player do the spawn stuff right away moupdate - we now have another thing for initting on player
        {
            SpawningNode = spawnNode;
            spawnNode.SpawnedBoardObject = this;
            CurChannel = SpawningNode.MyChannel;
            this.transform.parent = CurChannel.MyRing.transform;         
            transform.position = SpawningNode.transform.position;                                              
            transform.LookAt(CurChannel.EndNode.transform);
        }        
    }    

    private void CheckForNewChannel(List<Collider> overlapColliders)
    {
        // Channel nodes
        List<Collider> cnColliders = overlapColliders.ToList();
        cnColliders.RemoveAll(c => c.GetComponent<ChannelNode>() == null);                        
        cnColliders.RemoveAll(c => c.GetComponent<ChannelNode>().CanBeOnPath() == false);            
        cnColliders.RemoveAll(c => c.GetComponent<ChannelNode>().MyChannel == CurChannel);   
        cnColliders.RemoveAll(c => Vector3.Dot( (c.transform.position - this.transform.position), this.transform.forward) <= 0);                     
        if(cnColliders.Count > 0)
        {
            if(cnColliders.Count > 1) 
            {
                Debug.LogError("Why do we have more than 1 Channel Node Colliders?: " + cnColliders.Count);                    
            }

            ChannelNode cn = cnColliders[0].GetComponent<ChannelNode>();
            Vector3 heading = cn.transform.position - this.transform.position;
            float dot = Vector3.Dot(heading, this.transform.forward);         
            if(dot > 0f) // monote - get rid of these checks since they're supposed to be handled above
            {
                CurChannel = cn.MyChannel;                
                this.transform.parent = CurChannel.MyRing.transform;
                //LastPositionObject.transform.parent = CurChannel.MyRing.transform;                
                this.transform.position = cn.transform.position;
                transform.LookAt(cn.IsStartNode() ? cn.MyChannel.EndNode.transform : cn.MyChannel.StartNode.transform);      
            } 
            else
            {
                Debug.LogError("Why do we have a Channel Node that's behind us?");
            }             
        }      
    }

    private void CheckGenericBounce(List<Collider> pgColliders)
    {
        // Player Generic (walls, etc)
        //List<Collider> pgColliders = overlapColliders.ToList();
        pgColliders.RemoveAll(c => c.gameObject.layer != LayerMask.NameToLayer("Player Generic Collider"));  
        pgColliders.RemoveAll(c => Vector3.Dot( (c.transform.position - this.transform.position), this.transform.forward) <= 0);            
        if(pgColliders.Count > 0)
        {               

            if(pgColliders.Count > 1) 
            {                
                pgColliders = pgColliders.OrderBy(o => Vector3.Distance(o.transform.position, this.transform.position)).ToList();                   
            }

            Collider pg = pgColliders[0];
            Vector3 heading = pg.transform.position - this.transform.position;
            float dot = Vector3.Dot(heading, this.transform.forward);         
            if(dot > 0f)
            {                         
                transform.forward = -transform.forward;         
            } 
            else
            {
                Debug.LogError("Why do we have a Player Generic Collider that's behind us?");
            }                                          
        }
    }

    private void CheckBumpers(List<Collider> bumperColliders)
    {
        // BUMPER collisions                
       // List<Collider> bumperColliders = curSphereColliders.ToList();
        for(int i=0; i<bumperColliders.Count; i++)
        {
            if(bumperColliders[i] == null)
            {
                Debug.LogError("wtF");
            }
        }
        bumperColliders.RemoveAll(x => x.GetComponent<Bumper>() == null);
        if (bumperColliders.Count > 0)
        {
            Bumper bumper = bumperColliders[0].GetComponent<Bumper>();
            if (this.BoardObjectType == eBoardObjectType.PLAYER)
            {
                Player player = FindObjectOfType<Player>();
                switch (bumper.BumperType)
                {
                    case Bumper.eBumperType.DEATH:
                        FindObjectOfType<Dax>().EndGame("Death Bumper!");
                        break;
                    case Bumper.eBumperType.COLOR_MATCH:
                        if (player.CarriedColorFacet == null) break;
                        Facet playerCarriedColorFacet = (Facet)player.CarriedColorFacet;
                        if (playerCarriedColorFacet._Color == bumper.BumperColor)
                        {                                
                            FindObjectOfType<Dax>().CurWheel.MatchedFacetColor(playerCarriedColorFacet);
                            //curSphereColliders.Remove(playerCarriedColorFacet.GetComponentInChildren<Collider>());
                            DestroyImmediate(playerCarriedColorFacet.gameObject);
                            if(_Dax.CurWheel.VictoryCondition == Dax.eVictoryConditions.COLOR_MATCH) _Dax.CurWheel.CheckVictoryConditions();
                        }
                        break;
                }
            }
            transform.forward = -transform.forward;   
           // transform.position = LastPositionObject.transform.position;
            //MoveDir = (MoveDir == eMoveDir.INWARD ? eMoveDir.OUTWARD : eMoveDir.INWARD);
            //Debug.Log("bounced off: " + bumperColliders[0].name);
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
                if(_Dax.CurWheel.VictoryCondition == Dax.eVictoryConditions.COLLECTION)
                {
                    //Debug.LogError("Pickup facet collect");
                    facet.SpawningNode.SpawnedBoardObject = null;
                    //curSphereColliders.Remove(facet.GetComponentInChildren<Collider>()); // moupdate - loook in making this a function
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
            case eBoardObjectType.GAME_MOD:
                    GameMod gameMod = boardObjectColliders[0].GetComponentInParent<GameMod>();
                    switch(gameMod.GameModType)
                    {
                        case GameMod.eGameModType.EXTRA_POINTS:
                            _Dax.AddPoints(gameMod.GameModVal);
                            break;
                        case GameMod.eGameModType.POINTS_MULTIPLIER:
                            _Dax.BeginPointMod(gameMod.GameModTime, gameMod.GameModVal);
                            break;
                    }
                    //curSphereColliders.Remove(gameMod.GetComponent<Collider>());
                    DestroyImmediate(gameMod.gameObject);
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
                        List<Hazard> enemies = _Dax.CurWheel.GetComponentsInChildren<Hazard>().ToList();
                        float speedModVal = (speedMod.SpeedModType == SpeedMod.eSpeedModType.ENEMY_SPEED ? speedMod.SpeedModVal : -speedMod.SpeedModVal); // moupdate optimize all this
                        foreach (Hazard e in enemies) e.Speed += speedModVal; // moupdate - optimize this --- use generic <T> or an overwritten activate.  maybe go back to one class for each and have a generic activate overwriten                                                                                                             
                        break;
                    case SpeedMod.eSpeedModType.RING_SPEED:
                  //  case SpeedMod.eSpeedModType.RING_DOWN:
                        float speedModVal2 = (speedMod.SpeedModType == SpeedMod.eSpeedModType.RING_SPEED ? speedMod.SpeedModVal : -speedMod.SpeedModVal);
                        player.CurChannel.MyRing.RotateSpeed += speedModVal2;
                        break;
                 /*   case SpeedMod.eSpeedModType.WHEEL_UP:
                    case SpeedMod.eSpeedModType.WHEEL_DOWN:
                        float speedModVal3 = (speedMod.SpeedModType == SpeedMod.eSpeedModType.WHEEL_UP ? speedMod.SpeedModVal : -speedMod.SpeedModVal);
                        List<Ring> rings = _Dax.CurWheel.GetComponentsInChildren<Ring>().ToList();
                        foreach (Ring ring in rings) ring.RotateSpeed += speedModVal3; // moupdate - move this to the actual script so that it's not bloating up all this
                        break;    */                            
                   /* case SpeedMod.eSpeedModType.RING_STOP:
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
                        break;*/
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