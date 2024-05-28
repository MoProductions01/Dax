using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// BoardObject is the base class for all the game objects on the board
/// </summary>
public class BoardObject : MonoBehaviour
{    
    // Strings used in the DaxEditor to show in the Inspector window
    public static List<string> BOARD_OBJECT_EDITOR_NAMES = 
                                new List<string> {"Player", "Facet", "Hazard", "Facet Collect", 
                                                    "Shield", "Speed Mod", "Point Mod"};  

    // All the types of BoardObjects that we can have
    public enum eBoardObjectType { PLAYER, FACET, HAZARD, FACET_COLLECT, SHIELD, SPEED_MOD, POINT_MOD }; 
    [field: SerializeField] public eBoardObjectType BoardObjectType {get; set;}
    
    // Movement info.  
    public enum eStartDir {OUTWARD, INWARD}; 
    [field: SerializeField] public eStartDir StartDir = eStartDir.OUTWARD;
    [field: SerializeField] public float Speed {get; set;} = 0f;        

    [field: SerializeField] public Dax Dax {get; set;}   // Root Dax game object ref               
    [field: SerializeField] public Channel CurChannel {get; set;}   // Current channel that the board object is in               
    [field: SerializeField] public ChannelNode SpawningNode {get; set;}    // The node you spawn on 

    /// <summary>
    /// Handles generic/universal initialization for BoardObjects
    /// </summary>
    /// <param name="spawnNode">Node we're spawning on</param>
    /// <param name="dax">Root game reference</param>
    public virtual void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {                
        Dax = dax;              
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
    /// Overridable function for handling collisions with the player.
    /// </summary>
    /// <param name="player">Player object</param>
    /// <param name="boardObject">Object collided with</param>
    public virtual void HandleCollisionWithPlayer(Player player, BoardObject boardObject) {}   
         
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
                //transform.LookAt(channelNode.MyChannel.StartNode ? channelNode.MyChannel.EndNode.transform : channelNode.MyChannel.StartNode.transform);       // modelete node   
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
                        if (player.CarriedFacet == null) break; // No facet being carried so bail
                        Facet playerCarriedColorFacet = (Facet)player.CarriedFacet;
                        if (playerCarriedColorFacet._Color == bumper.BumperColor)
                        {   // Player is carrying a facet that's the same color as the bumper so update game state                           
                            FindObjectOfType<Dax>().Wheel.MatchedFacetColor(playerCarriedColorFacet);                            
                            DestroyImmediate(playerCarriedColorFacet.gameObject);
                            if(Dax.Wheel.VictoryCondition == Dax.eVictoryConditions.COLOR_MATCH) Dax.Wheel.CheckVictoryConditions();
                        }
                        break;
                }
            }
            // Every object bounces back to the other direction
            transform.forward = -transform.forward;             
        }
    }         
    
    /// <summary>
    /// Function for the Player checking against other board objects on the game
    /// </summary>
    /// <param name="boardObjectColliders">Colliders the player is overlapping with</param>
    private void CheckBoardObjectsForPlayer(List<Collider> boardObjectColliders)
    {        
        Player player = FindObjectOfType<Player>();
        boardObjectColliders.RemoveAll(x => x.GetComponentInParent<BoardObject>() == null); // Remove anything that's not a board boject             
        boardObjectColliders.Remove(this.gameObject.GetComponent<Collider>()); // Remove player's collider
        
        if (player.ActiveShield != null) boardObjectColliders.Remove(player.ActiveShield.GetComponent<Collider>()); // Remove shield collider if necessary
        if (boardObjectColliders.Count == 0) return; // bail if no collisions

        for(int i=0; i<boardObjectColliders.Count; i++)
        {
            // Call each BoardObject's HandleCollisionWithPlayer() function
            BoardObject boardObject = boardObjectColliders[i].GetComponentInParent<BoardObject>();             
            switch(boardObject.BoardObjectType) 
            {
                case eBoardObjectType.FACET:   
                    boardObject.GetComponentInParent<Facet>().HandleCollisionWithPlayer(player, boardObject);                                                                                     
                    break;  
                case eBoardObjectType.HAZARD:
                    boardObject.GetComponentInParent<Hazard>().HandleCollisionWithPlayer(player, boardObject);                         
                break;
                case eBoardObjectType.SHIELD:    
                    boardObject.GetComponentInParent<Shield>().HandleCollisionWithPlayer(player, boardObject);                                                                                            
                    break;
                case eBoardObjectType.FACET_COLLECT:
                    boardObject.GetComponentInParent<FacetCollect>().HandleCollisionWithPlayer(player, boardObject);                                                                                             
                    break;
                case eBoardObjectType.POINT_MOD:
                    boardObject.GetComponentInParent<PointMod>().HandleCollisionWithPlayer(player, boardObject);   
                    break;
                case eBoardObjectType.SPEED_MOD:
                    boardObject.GetComponentInParent<SpeedMod>().HandleCollisionWithPlayer(player, boardObject);  
                break;    
            }
        }
                        
    }

    /// <summary>
    /// Called from the root game class Dax for the Player and each Enemy
    /// </summary>
    /// <param name="deltaTime">DeltaTime from Dax</param>
    public void BoardObjectFixedUpdate(float deltaTime)
    {        
        if(CurChannel == null) {Debug.LogError("Why do we have no Channel assigned? " + this.name); return; }

        if (Speed != 0f) // This check might be unnecessary but better save than sorry
        {                       
            // move along the forward vector
            transform.Translate(Vector3.forward * deltaTime * Speed);            
            // now check colliders
            List<Collider> overlapColliders = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius).ToList();
            if(overlapColliders.Count > 0)
            {                
                CheckForNewChannel(overlapColliders.ToList()); // Check if we want to jump to another channel         
                CheckBumpers(overlapColliders.ToList());       // Check if we collided with a bumper
                CheckGenericBounce(overlapColliders.ToList()); // Check bouncing off non BoardObjects
                if(this.BoardObjectType == eBoardObjectType.PLAYER) 
                {
                    // Player gets a special case
                    CheckBoardObjectsForPlayer(overlapColliders.ToList());
                }                
            }                                                                                    
        }                       
    }     
}