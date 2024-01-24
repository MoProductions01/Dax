using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoardObject : MonoBehaviour
{    
    public enum eBoardObjectType { PLAYER, HAZARD, FACET, MAGNET, INTERACTABLE, SHIELD, SPEED_MOD, /*GAME_MOD,*/ NONE };
    public eBoardObjectType BoardObjectType;
 
    public enum eMoveDir { OUTWARD, INWARD };

    

    // Shared      
    [HideInInspector]
    public Dax _Dax;        
    public GameObject LastPositionObject = null;        
    public Channel CurChannel = null;

    public eMoveDir MoveDir = eMoveDir.OUTWARD;
    public float Speed = 0f;    

    [Header("Starting State Stuff")]
    public ChannelNode SpawningNode = null;    

    public Interactable DestGateJustWarpedTo = null;

    private void Awake()
    {
       // Debug.Log("Awake() Diode: " + this.name + " of type: " + DiodeType.ToString());   
    }

    private void Start()
    {
       // Debug.Log("Start() Diode: " + this.name + " of type: " + DiodeType.ToString());
    }

    public void SetMovementInfo(eMoveDir movedir, float speed)
    {
        MoveDir = movedir;
        Speed = speed;
    }

    public string GetBoardObjectName()
    {
        switch(BoardObjectType)
        {
            case eBoardObjectType.PLAYER: return "Player";
            case eBoardObjectType.HAZARD: return "Hazard";            
            case eBoardObjectType.FACET: return "Facet";
            case eBoardObjectType.MAGNET: return "Magnet";             
            case eBoardObjectType.INTERACTABLE: return "Interactable";
            case eBoardObjectType.SHIELD: return "Shield";
            case eBoardObjectType.SPEED_MOD: return "Speed Mod";
           // case eBoardObjectType.GAME_MOD: return "Game Mod";
            default: return "ERROR: UNKNOWN Board Object TYPE: " + BoardObjectType.ToString();
        }
    }    

    public virtual void InitForPlayer(Player player, Dax dax)
    {
        _Dax = dax;
    }
    public virtual void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {
       // Debug. Log("BoardObject.InitFromCreation(): " + this.name + " --MoSave--");        
        _Dax = dax;
        if (LastPositionObject != null) Debug.LogError("WTF there should be no last position object");
        LastPositionObject = new GameObject("Last Position_" + this.gameObject.name); ;
        LastPositionObject.transform.parent = this.transform.parent;
       
        if (spawnNode != null) // if it's not the player do the spawn stuff right away moupdate - we now have another thing for initting on player
        {
            SpawningNode = spawnNode;
            spawnNode.SpawnedBoardObject = this;
            CurChannel = SpawningNode.MyChannel;

            this.transform.parent = CurChannel.MyRing.transform;
            LastPositionObject.transform.parent = CurChannel.MyRing.transform;

            transform.position = SpawningNode.transform.position;                        
            LastPositionObject.transform.position = transform.position;
            
            transform.LookAt(CurChannel.EndNode.transform);
        }        
    }

    bool AmITrappedByCollider(Collider colliderToCheck)
    {
        RRDManager.AppendText("------AmITrappedByCollider(): " + colliderToCheck.name + "\n", RifRafDebug.eDebugTextType.PHYSICS);
        if(colliderToCheck.GetComponent<ChannelPiece>() == null)
        {
            RRDManager.AppendText("You can't get trapped by non ChannelPiece objects for now. Free.\n", RifRafDebug.eDebugTextType.PHYSICS);
            return false;
        }
        bool trapped = true;

        string dictString = colliderToCheck.name.Substring(0, 13);
        float extentsZ = _Dax.ChannelSizes[dictString];
        float diodeToBoundsCenterDist = Vector3.Distance(this.transform.position, colliderToCheck.bounds.center);
        if (diodeToBoundsCenterDist >= extentsZ)
        {   // more than 50% outside the collider so you're free automatically            
            RRDManager.AppendText("You're >= away from bounds center than it's extentsZ, so at least half out. Free.\n", RifRafDebug.eDebugTextType.PHYSICS);
            trapped = false;
        }
        else
        {                 
            float delta = extentsZ - diodeToBoundsCenterDist;
            float ourSize = this.GetComponent<SphereCollider>().radius;
            float percentageIn = 50f + ((delta / ourSize) * 50f);                        
            if (percentageIn >= _Dax.TrappedPercentage)
            {
                RRDManager.AppendText("you are " + percentageIn.ToString("F3") + "% in which is more than the trappedPercentage tolerance: " + _Dax.TrappedPercentage.ToString("F3") + "% so Trapped.\n", RifRafDebug.eDebugTextType.PHYSICS);
                trapped = true;
            }
            else
            {
                RRDManager.AppendText("you are " + percentageIn.ToString("F3") + "% in which is less than the trappedPercentage tolerance: " + _Dax.TrappedPercentage.ToString("F3") + "% so Free.\n", RifRafDebug.eDebugTextType.PHYSICS);
                trapped = false;
            }
        }
        return trapped;
    }
    bool AmIFree(List<Collider> spherePGCs/*, List<RaycastHit> rayPGCs*/)
    {
       // PGCsNotTrappedBy.Clear();
        bool amIFree;
        if (spherePGCs.Count == 0 /*&& rayPGCs.Count == 0*/)
        {   // Sphere false so you're fully free
            //Dax.DebugText.text += "no Sphere or Ray hits, so you're fully free\n";
            RRDManager.AppendText("No Sphere hits. Free.\n", RifRafDebug.eDebugTextType.PHYSICS);
            //Dax.DebugSphere.transform.position = new Vector3(999999f, 999999f, 999999f);
            amIFree = true;
        }
        else if (spherePGCs.Count == 1 /*&& rayPGCs.Count > 0*/)
        {   // Sphere hit so you're at least partially contained within the collider bounds            
            RRDManager.AppendText(this.name + " has 1 Sphere PGC (" + spherePGCs[0].name + ") so you're at least partially contained.\n", RifRafDebug.eDebugTextType.PHYSICS);

            amIFree = !AmITrappedByCollider(spherePGCs[0]);           
        }
        else
        {   // more than one sphere hit so see what's up
            RRDManager.AppendText("**** More than 1 PGC, which could be either you're in between rings with at least one object on each ring, or you're bumping into a channel node and wedge on the next ring at the same time. Sort it out\n", RifRafDebug.eDebugTextType.PHYSICS);
            amIFree = true;
            List<int> ringsInvolved = new List<int>();            
            // The only way to get trapped with more than one PGC is if they're on separate rings, so 
            foreach(Collider c in spherePGCs)
            {                
                bool amITrapped = AmITrappedByCollider(c);
                if (amITrapped == true)
                {
                    amIFree = false;
                    break;
                }

                int colliderRingIndex;
                int.TryParse(c.name.Substring(5, 2), out colliderRingIndex);
                if (ringsInvolved.Contains(colliderRingIndex) == false) ringsInvolved.Add(colliderRingIndex);
            }
            if(amIFree == true)
            {   // if we're here then we're not trapped by anything, so see if you're colliding with things between rings
                if(ringsInvolved.Count > 1)
                {
                    RRDManager.AppendText("You're colliding with more than one object on " + ringsInvolved.Count + " rings, so you're stuck between them. Trapped!\n", RifRafDebug.eDebugTextType.PHYSICS);
                    amIFree = false;
                }
                else
                {
                    RRDManager.AppendText("You're colliding with more than one object on " + ringsInvolved.Count + " rings, so treat it like a normal bounce back collision. Free. \n", RifRafDebug.eDebugTextType.PHYSICS);
                    amIFree = true;
                }
            }            
            else
            {
                RRDManager.AppendText("You're contained by at least one PGC. Trapped.\n", RifRafDebug.eDebugTextType.PHYSICS);
            }
        }
        
        return amIFree;
    }    

    public void BoardObjectFixedUpdate(float deltTime)
    {        
        if (CurChannel != null)
        {           
            if (MoveDir == eMoveDir.OUTWARD) transform.LookAt(CurChannel.EndNode.transform);
            else transform.LookAt(CurChannel.StartNode.transform);
            RRDManager.AppendText("--------PRE MOVE-------: + " + this.MoveDir.ToString() + ", " + this.transform.position.ToString("F3") + "\n", RifRafDebug.eDebugTextType.PHYSICS);            
            CurChannel.HaveNodesLookAtEachOther();            

           // if(Speed == 0f) return; // no need to check for collisions if we're not moving       
            Player player = FindObjectOfType<Player>();
            // check to see if we're trapped
            List<Collider> preMoveSphereColliders = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius).ToList();
            List<Collider> preMoveSpherePGCs = preMoveSphereColliders.ToList();
            preMoveSpherePGCs.RemoveAll(x => x.gameObject.layer != LayerMask.NameToLayer("Player Generic Collider"));
                      
            bool amIFree = AmIFree(preMoveSpherePGCs);
                        
            if (amIFree == true) RRDManager.AppendText("Pre-move conclusion: FREE!\n", RifRafDebug.eDebugTextType.PHYSICS);
            else RRDManager.AppendText("Pre-move conclusion: TRAPPED!\n", RifRafDebug.eDebugTextType.PHYSICS);
            if (amIFree == false) return;           
            
            // We are not trapped, so move and start checking collision                            
            Vector3 moveDir = (MoveDir == eMoveDir.OUTWARD ? CurChannel.StartNode.transform.forward : CurChannel.EndNode.transform.forward);
            transform.position += (moveDir * deltTime * (Speed));
            
            
            // CHANNEL NODES for Path switching       
            List<Collider> curSphereColliders = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius).ToList();            
           // List<RaycastHit> curRayHits = Physics.RaycastAll(this.transform.position + (Vector3.up * .25f), Vector3.down).ToList();

            List<Collider> channelNodeColliders = curSphereColliders.ToList();                        
            channelNodeColliders.RemoveAll(x => x.GetComponent<ChannelNode>() == null);                        
            channelNodeColliders.RemoveAll(x => x.GetComponent<ChannelNode>().CanBeOnPath() == false);            
            channelNodeColliders.RemoveAll(x => x.GetComponent<ChannelNode>().MyChannel == CurChannel);                        
            if (MoveDir == eMoveDir.OUTWARD)
            {
                channelNodeColliders.RemoveAll(x => x.GetComponent<ChannelNode>().IsEndNode() == true);
            }
            else
            {
                channelNodeColliders.RemoveAll(x => x.GetComponent<ChannelNode>().IsStartNode() == true &&
                                                    x.GetComponent<ChannelNode>().IsOnCenterRing() == false);
            }                       
            
            if (channelNodeColliders.Count > 1) { Debug.LogError("You have too many channel node colliders: " + channelNodeColliders.Count); return; }
            if (channelNodeColliders.Count == 1)
            {
                RRDManager.AppendText("you've collided with a valid new Channel Node: " + channelNodeColliders[0].name + "\n", RifRafDebug.eDebugTextType.PHYSICS);
                // Debug.Log("you've collided with a valid new Channel Node: " + channelNodeColliders[0].name);
                ChannelNode newChannelNode = channelNodeColliders[0].GetComponent<ChannelNode>();                
                // special case if moving through center ring
                if (CurChannel.IsOnCenterRing() && newChannelNode.IsOnCenterRing())
                {
                    MoveDir = (MoveDir == eMoveDir.OUTWARD ? eMoveDir.INWARD : eMoveDir.OUTWARD);
                }
                CurChannel = newChannelNode.MyChannel;
                this.transform.parent = CurChannel.MyRing.transform;
                LastPositionObject.transform.parent = CurChannel.MyRing.transform;                
                this.transform.position = newChannelNode.transform.position;
            }

            // GENERIC COLLIDERS
            List<Collider> curSpherePGCs = curSphereColliders.ToList();            
            curSpherePGCs.RemoveAll(x => x.gameObject.layer != LayerMask.NameToLayer("Player Generic Collider"));

            RRDManager.AppendText("--------POST MOVE-------: " + this.MoveDir.ToString() + ", " + this.transform.position.ToString("F3") + "\n", RifRafDebug.eDebugTextType.PHYSICS);
            amIFree = AmIFree(curSpherePGCs/*, curRayPGCs*/);            
            if(amIFree == true)
            {
                RRDManager.AppendText("Post-move conclusion: FREE! So do the usual collider checks\n", RifRafDebug.eDebugTextType.PHYSICS);
            }
            else
            {
                RRDManager.AppendText("Post-move conclusion: TRAPPED!  So stay put\n", RifRafDebug.eDebugTextType.PHYSICS);
                return;
            }

            RRDManager.ResetText();
            if (curSpherePGCs.Count > 0)
            {
                RRDManager.AppendText("If we're here we've collided with at least one PGC: " + curSpherePGCs.Count + ", but by being here we're not trapped so no matter how many colliders there are treat them like a single normal PGC and bounce back\n", RifRafDebug.eDebugTextType.PHYSICS);
                //Debug.Log("If we're here we've collided with at least one PGC: " + curSpherePGCs.Count + ", but by being here we're not trapped so no matter how many colliders there are treat them like a single normal PGC and bounce back");

                //string s = "----curSpherePGCs count: " + curSpherePGCs.Count + "\n";
                //foreach (Collider c in curSpherePGCs) s += c.name + ", ";
               // RRDManager.AppendText(s + "\n", RifRafDebug.eDebugTextType.NEW_UNDEFINED);
              //  Debug.Log(s);

                float diodeDist = Vector3.Distance(this.transform.position, Vector3.zero);
                float PGCdist = Vector3.Distance(curSpherePGCs[0].bounds.center, Vector3.zero);

                int curChannelRingIndex = _Dax.CurWheel.Rings.IndexOf(CurChannel.MyRing);
               // Debug.Log("curChannelRingIndex: " + curChannelRingIndex + ", CurChannel: " + CurChannel.name + ", MyRing: " + CurChannel.MyRing.name);
                if(curChannelRingIndex == 0)
                {                    
                    float distLastFrame = Vector3.Distance(LastPositionObject.transform.position, Vector3.zero);
                    float distThisFrame = Vector3.Distance(this.transform.position, Vector3.zero);
                    
                    if (MoveDir == eMoveDir.OUTWARD && distThisFrame < distLastFrame)
                    {
                        RRDManager.AppendText("You're moving OUTWARD but moving closer to the center, so you crossed the center of the puzzle, so swap diodeDist and PGCdist for the collisions.\n", RifRafDebug.eDebugTextType.PHYSICS);
                        // Debug.LogWarning("You're moving OUTWARD but moving closer to the center, so you crossed the center of the puzzle, so swap diodeDist and PGCdist for the collisions.\n");
                        float tmp = diodeDist;
                        diodeDist = PGCdist;
                        PGCdist = tmp;
                    }
                    if (MoveDir == eMoveDir.INWARD && distThisFrame > distLastFrame)
                    {   
                        RRDManager.AppendText("You're moving INWARD but moving further from the center, so you crossed the center of the puzzle, so swap diodeDist and PGCdist for the collisions.\n", RifRafDebug.eDebugTextType.PHYSICS);
                        //Debug.Log("You're moving INWARD but moving further from the center, so you crossed the center of the puzzle, so swap diodeDist and PGCdist for the collisions.\n");
                        // Debug.LogWarning(" You're moving INWARD but are moving further from the center, so you crossed the center of the puzzle, so swap diodeDist and PGCdist for the collisions.\n");
                        float tmp = diodeDist;
                        diodeDist = PGCdist;
                        PGCdist = tmp;
                    }
                }
                if (MoveDir == eMoveDir.OUTWARD)
                {
                    if (diodeDist >= PGCdist)
                    {
                        RRDManager.AppendText("Moving OUTWARD and Diode dist to center is >= PGC dist to center, then ignore because it's behind us.\n", RifRafDebug.eDebugTextType.PHYSICS);
                    }
                    else
                    {
                        RRDManager.AppendText("Moving OUTWARD and Diode dist to center is < PGC dist to center, then it's something to bounce off of because it's in front of us.\n", RifRafDebug.eDebugTextType.PHYSICS);
                        transform.position = LastPositionObject.transform.position;
                        MoveDir = (MoveDir == eMoveDir.INWARD ? eMoveDir.OUTWARD : eMoveDir.INWARD);
                      //  Debug.Log("bounced off: " + curSpherePGCs[0].name);
                    }
                }
                else
                {
                    if (diodeDist <= PGCdist)
                    {
                        RRDManager.AppendText("Moving INWARD and Diode dist to center is <= PGC dist to center, then ignore because it's behind us.\n", RifRafDebug.eDebugTextType.PHYSICS);
                    }
                    else
                    {   
                        RRDManager.AppendText("Moving INWARD and Diode dist to center is > PGC dist to center, then it's something to bounce off of because it's in front of us.\n", RifRafDebug.eDebugTextType.PHYSICS);
                       // Debug.Log("Moving INWARD and Diode dist to center is > PGC dist to center, then it's something to bounce off of because it's in front of us.\n");
                        transform.position = LastPositionObject.transform.position;
                        MoveDir = (MoveDir == eMoveDir.INWARD ? eMoveDir.OUTWARD : eMoveDir.INWARD);
                     //   Debug.Log("bounced off: " + curSpherePGCs[0].name);
                    }
                }
            }
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
                        /*case eBoardObjectType.GAME_MOD:
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
                            curSphereColliders.Remove(gameMod.GetComponent<Collider>());
                            DestroyImmediate(gameMod.gameObject);
                            break;*/
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
                        case eBoardObjectType.MAGNET:
                            Magnet magnet = boardObjectColliders[0].GetComponentInParent<Magnet>();
                            if(player.AddMagnet(magnet.MagnetType) == true)
                            {
                                magnet.SpawningNode.SpawnedBoardObject = null;
                                curSphereColliders.Remove(magnet.GetComponentInChildren<Collider>());
                                DestroyImmediate(magnet.gameObject);
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

            // BUMPER collisions                
            List<Collider> bumperColliders = curSphereColliders.ToList();
            for(int i=0; i<bumperColliders.Count; i++)
            {
                if(bumperColliders[i] == null)
                {
                    Debug.Log("wtF");
                }
            }
            bumperColliders.RemoveAll(x => x.GetComponent<Bumper>() == null);
            if (bumperColliders.Count > 0)
            {
                Bumper bumper = bumperColliders[0].GetComponent<Bumper>();
                if (this.BoardObjectType == eBoardObjectType.PLAYER)
                {
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
                                curSphereColliders.Remove(playerCarriedColorFacet.GetComponentInChildren<Collider>());
                                DestroyImmediate(playerCarriedColorFacet.gameObject);
                                if(_Dax.CurWheel.VictoryCondition == Dax.eVictoryConditions.COLOR_MATCH) _Dax.CurWheel.CheckVictoryConditions();
                            }
                            break;
                    }
                }
                transform.position = LastPositionObject.transform.position;
                MoveDir = (MoveDir == eMoveDir.INWARD ? eMoveDir.OUTWARD : eMoveDir.INWARD);
                //Debug.Log("bounced off: " + bumperColliders[0].name);
            }
                        
            // clean up
            float endDistLastFrame = Vector3.Distance(LastPositionObject.transform.position, Vector3.zero);
            float endDistThisFrame = Vector3.Distance(this.transform.position, Vector3.zero);
            LastPositionObject.transform.position = transform.position;
            RRDManager.AppendText("-----END UPDATE-----: " + this.MoveDir.ToString() + ", " + this.transform.position.ToString("F3") + "\ndistLastFrame: " + endDistLastFrame.ToString("F3") + "\ndistThisFrame: " + endDistThisFrame.ToString("F3") + "\n", RifRafDebug.eDebugTextType.PHYSICS);
        }        
    }    
}