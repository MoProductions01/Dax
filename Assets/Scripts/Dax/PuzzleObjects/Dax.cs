using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This is the main root object for the gameplay.
/// </summary>
public class Dax : MonoBehaviour
{   
    public static float MAX_SPIN_SPEED = 20f; // Maximum speed the player can spin a ring
    public static float MAX_SPEED = 1f; // Maximum speed a board object or the player can go
    public static int MAX_NUM_RINGS = 4; // Maximum number of rings the game can have
     
    // The victory conditions (or game type).
    // COLLECTION: Player needs to run over the facets to collect them.  When they have them all you win.
    // COLOR_MATCH: Player must run over the facet to start carrying it. Then
    //              run into a bumper at the edge of the ring of the corresponding color
    public enum eVictoryConditions { COLLECTION, COLOR_MATCH };

    public Player _Player;  // Ref to the player game object

     // Various states the game can be in
    public enum eGameState { PRE_GAME, RUNNING, GAME_OVER };
    public eGameState GameState;  
                   
    public Wheel Wheel; // Ref to the wheel in the game
    
    public Ring CurTouchedRing = null; // The ring the user is currently touching            
    LayerMask RingMask; // Layer mask for rings
    Vector2 RingCenterPoint = Vector3.zero; // Center for the currently selected ring
    Vector2 MousePosition;  // Position of the mouse
    float RingRot = 0f;      // Rotation of the ring
    float PointerPrevAngle; // Angle between the center point and the mouse pointer
    
    bool PointModActive; // Whether or not a gameplay modifier is active
    float PointModTimer; // Timer for the current gameplay modifier
    int PointModVal;     // Value of the current gameplay modifier
            
    public float LevelTime = 120f;  // Amount of time for the current level
    public int Score = 0;       // Player score
    
    public PuzzleSaveData _PuzzleSaveData = null;   // Save data for the current puzzle  
        
    public UIRoot _UIRoot;  // Ref to the root UI
    public string PuzzleName {get; set;} = "Default Puzzle"; //Name of the puzzle    

    /// <summary>
    /// Sets all of the basic gameplay info on startup
    /// </summary>
    private void Awake()    
    {
        GameState = eGameState.PRE_GAME; // Dax.Awake()
        RingMask = LayerMask.GetMask("Main Touch Control");        
        Score = 0;
        _UIRoot = FindObjectOfType<UIRoot>();
        _UIRoot.Init();
    }      
    
    /// <summary>
    /// Starts a point modifier
    /// </summary>
    /// <param name="time">How long the modifier will last</param>
    /// <param name="val">Mod val (point multiplier, etc) for the current mod0</param>
    public void BeginPointMod(float time, int val)
    {
        PointModActive = true;
        PointModTimer = time;
        PointModVal = val;
    }

    /// <summary>
    /// Adds points to the player's current score
    /// </summary>
    /// <param name="points"></param>
    public void AddPoints(int points)
    {
        // Account for the point mod if active
        if(PointModActive == true)
        {
            points *= PointModVal;
        }
        Score += points;
        _UIRoot.ScoreText.SetText(Score.ToString());
    }   

    /// <summary>
    /// Updates the game every frame and handles user input
    /// </summary>
    void Update()
    {
        if (GameState != eGameState.RUNNING) return; // Bail if the game isn't in it's running state     
        
        // Count down the point mod timer if it's on
        if(PointModActive == true)
        {
            PointModTimer -= Time.deltaTime;
            if(PointModTimer <= 0)
            {
                PointModActive = false;
            }
        }

        // Some local variables to calculate the difference between data this frame and the last frame
        float pointerNewAngle = -999999f;
        float ringAngleBefore = -9999999f;
        float angleDiff = -99999f;
        if (Input.GetMouseButtonDown(0))
        {   
            // User clicked the mouse (or touched the screen) so see if they clicked on a ring         
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);           
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit, Mathf.Infinity, RingMask))
            {   // User clicked on a ring, so gather information based on where they clicked       
                CurTouchedRing = hit.collider.gameObject.GetComponent<Ring>();                
                RingCenterPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, CurTouchedRing.transform.position);
                MousePosition = Input.mousePosition;
                PointerPrevAngle = Vector2.Angle(Vector2.up, MousePosition - RingCenterPoint);
                RingRot = CurTouchedRing.transform.localEulerAngles.y;
            }
        }
        else if (Input.GetMouseButton(0) && CurTouchedRing != null)
        {   // User is holding down mouse (or holding down touch) so check for ring rotation     
            Vector2 pointerPos = Input.mousePosition;
            pointerNewAngle = Vector2.Angle(Vector2.up, pointerPos - RingCenterPoint);
            float delta = (pointerPos - RingCenterPoint).sqrMagnitude;
            ringAngleBefore = RingRot;
            angleDiff = pointerNewAngle - PointerPrevAngle;
            // Check to see if the user rotated the ring enough but not too much
            if (delta >= 4f && Mathf.Abs(pointerNewAngle - PointerPrevAngle) < MAX_SPIN_SPEED)
            {
                if (pointerPos.x > RingCenterPoint.x)
                {
                    RingRot += angleDiff;
                }
                else
                {
                    RingRot -= angleDiff;
                }
            }
            PointerPrevAngle = pointerNewAngle;
        }
        else if (Input.GetMouseButtonUp(0))
        {   // User unclicked mouse (or stopped touching) so reset the currently selected ring to null
            if (CurTouchedRing != null) CurTouchedRing = null;            
        }
    }

    /// <summary>
    /// Handle the physics here since it's frame rate independent
    /// </summary>
    private void FixedUpdate()
    {
        if (GameState != eGameState.RUNNING) return;  
                
        // Update the player 
        _Player.BoardObjectFixedUpdate(Time.deltaTime);
        
        // Hazards (such as enemies) are the only non player board objects that move
        List<Hazard> hazards = transform.GetComponentsInChildren<Hazard>().ToList(); 
        hazards.RemoveAll(x => x.HazardType != Hazard.eHazardType.ENEMY); // Only enemies move
        foreach(Hazard hazard in hazards) 
        {
            hazard.BoardObjectFixedUpdate(Time.deltaTime);
        }
        
        RotateRings();  // Rotate the rings based on the data updated in Update
    }
   
    /// <summary>
    /// Handles rotating the rings based on information gatherd in Update
    /// </summary>
    void RotateRings()
    {  
        if (Wheel == null) { Debug.LogError("ERROR: No CurWheel."); return; }
        foreach(Ring ring in Wheel.Rings)
        {
            if (ring == CurTouchedRing)
            {   // Handle the rotation of the ring touched by the player
                CurTouchedRing.transform.localEulerAngles = new Vector3(0f, RingRot, 0f);
                if (CurTouchedRing.BumperGroup != null) CurTouchedRing.BumperGroup.transform.localEulerAngles = new Vector3(0f, RingRot, 0f);
            }
            else
            {   // Rotate the rings that are rotating on their own
                ring.Rotate();
                // Rotate the bumper group if necessary
                if (ring.BumperGroup != null) ring.BumperGroup.transform.localEulerAngles = new Vector3(0f, ring.transform.localEulerAngles.y, 0f);
            }
        }                
    }               
    
    /// <summary>
    /// Handles the end of the game
    /// </summary>
    /// <param name="reason">Reason for game ending (died, out of time, ect)</param>
    public void EndGame(string reason)
    {
        GameState = eGameState.GAME_OVER;
        _UIRoot.ToggleEndGameItems(reason, true);
    }

    /// <summary>
    /// Handles the start of the game.  The wheel is already setup before this
    /// </summary>
    public void StartGame()
    {
        _UIRoot.PreGameButton.SetActive(false);
        GameState = eGameState.RUNNING;
    }

    /// <summary>
    /// Creates new puzzle save data based on all of the information in the Dax object
    /// </summary>
    /// <param name="dax">Dax object that stores all the game data</param>
    /// <returns></returns>
    public PuzzleSaveData CreateSaveData(Dax dax)
    {     
        _PuzzleSaveData = null;        
        _PuzzleSaveData = new PuzzleSaveData(dax);
        return _PuzzleSaveData;
    }
    
    /// <summary>
    /// Handles the resetting of the puzzle from save data.  Called when loading
    /// a puzzle or restarting the current level
    /// </summary>
    /// <param name="saveData">Save data to reset from</param>
    /// <returns></returns>
    public bool ResetPuzzleFromSave(PuzzleSaveData saveData = null)
    {   
        // Check to see if we're going to override the current save data
        if(saveData != null)
        {
            _PuzzleSaveData = null;
            _PuzzleSaveData = saveData;
        }

        MCP mcp = FindObjectOfType<MCP>();       
        
        this.PuzzleName = _PuzzleSaveData.PuzzleName; // update puzzle's name
        // Reset ring and touch data        
        CurTouchedRing = null;
        RingRot = 0f;
        PointerPrevAngle = 0f;

        mcp.ResetWheel(Wheel); // reset the wheel to starting state                
        Wheel.VictoryCondition = _PuzzleSaveData.VictoryCondition;                
        Wheel.TurnOnRings(_PuzzleSaveData.NumRings); // turn on # of rings baesd on save data       

        // first pass through all the rings to create all the board objects
        for(int i=0; i<_PuzzleSaveData.RingSaves.Count; i++)
        {            
            int numChannels = (i == 0 ? Wheel.NUM_CENTER_RING_CHANNELS : Wheel.NUM_OUTER_RING_CHANNELS);
            // get the ring save data
            RingSave ringSave = _PuzzleSaveData.RingSaves[i];
            if (numChannels != ringSave.ChannelSaves.Count) { Debug.LogError("numChannels: " + numChannels + ", doesn't match ChannelSaves.Count: " + ringSave.ChannelSaves.Count); return false; }           
            Ring ring = Wheel.Rings[i];
            ring.RotateSpeed = ringSave.RotSpeed;
            
            // Get an ordered list of the channels on this ring
            List<Channel> ringChannels = ring.transform.GetComponentsInChildren<Channel>().ToList();
            ringChannels = ringChannels.OrderBy(x => x.name).ToList();            
            // now create all the board objects
            for(int j=0; j<numChannels; j++)
            {
                ChannelSave channelSave = ringSave.ChannelSaves[j];
                Channel channel = ringChannels[j];                
                channel.InnerChannel.SetActive(channelSave.InnerActive);
                channel.OuterChannel.SetActive(channelSave.OuterActive);                
                // Board objects can only be on the middle nodes, so check for that and the board object itself
                if (channelSave.MidNodeBO != null && channelSave.MidNodeBO.StartChannel != "") 
                {
                    mcp.CreateBoardObjectFromSaveData(channel.MidNode, channelSave.MidNodeBO, this);
                }                                
            }            
            // Now initialize all of the bumers if we're on the end ring
            if (ring.BumperGroup != null)
            {
                List<Bumper> bumpers = ring.BumperGroup.Bumpers.ToList();
                bumpers = bumpers.OrderBy(x => x.name).ToList();
                for (int j = 0; j < bumpers.Count; j++)
                {
                    Bumper bumper = bumpers[j];
                    bumper.BumperType = ringSave.BumperSaves[j].BumperType;
                    bumper.BumperColor = ringSave.BumperSaves[j]._Color;
                    bumper.gameObject.GetComponent<MeshRenderer>().material = mcp.GetBumperMaterial(bumper.BumperType, bumper.BumperColor);                                                                                        
                }
            }           
        }
        // Now another pass to init anything that requires all the objects on the wheel to be created
        for (int i = 0; i < _PuzzleSaveData.RingSaves.Count; i++)
        {   
            int numChannels = (i == 0 ? Wheel.NUM_CENTER_RING_CHANNELS : Wheel.NUM_OUTER_RING_CHANNELS); // the center ring has a different number of channels than the outer ones
            RingSave ringSave = _PuzzleSaveData.RingSaves[i];
            if (numChannels != ringSave.ChannelSaves.Count) { Debug.LogError("numChannels: " + numChannels + ", doesn't match ChannelSaves.Count: " + ringSave.ChannelSaves.Count); return false; }
            Ring ring = Wheel.Rings[i];
            List<Channel> ringChannels = ring.transform.GetComponentsInChildren<Channel>().ToList();
            ringChannels = ringChannels.OrderBy(x => x.name).ToList();
            for (int j = 0; j < numChannels; j++)
            {
                ChannelSave channelSave = ringSave.ChannelSaves[j];
                Channel channel = ringChannels[j];                                
                if (channel.MidNode.SpawnedBoardObject != null) mcp.InitBoardObjectFromSave(channel.MidNode.SpawnedBoardObject, channelSave.MidNodeBO);                
            }
        }
         
        _Player.ResetPlayer(_PuzzleSaveData.PlayerSave); //reset player                   
        Wheel.ResetFacetsCount();    // Init all of the facets on the current wheel                
        GameState = eGameState.RUNNING; // start the game running

        return true;
    }      

    /// <summary>
    /// Save data for a bumper
    /// </summary>
    [System.Serializable]
    public class BumperSave
    {
        public Bumper.eBumperType BumperType;
        public Facet.eFacetColors _Color;

        public BumperSave(Bumper.eBumperType type, Facet.eFacetColors color)
        {
            BumperType = type;
            _Color = color;
        }
    }
 
    /// <summary>
    /// Save data for all of the non player board objects
    /// </summary>
    [System.Serializable]
    public class BoardObjectSave
    {
        // The various data a board object can have
        public BoardObject.eBoardObjectType Type;
        public string StartChannel;
        public BoardObject.eStartDir StartDir; 
        public float Speed;                          

        // generic vars for various specific classes        
        public List<string> StringList01; 
        public List<string> StringList02;

        // bools and floats for timers, etc
        public List<bool> BoolList;
        public List<int> IntList;
        public List<float> FloatList;
        public BoardObjectSave(BoardObject bo)
        {
            Type = bo.BoardObjectType;
            StartChannel = bo.CurChannel.name;        

            // movement
            StartDir = bo.StartDir;
            Speed = bo.Speed;                        
            // Each kind of board object uses the save data in specific ways, so handle that based on the type
            switch(Type)
            {
                case BoardObject.eBoardObjectType.FACET:
                    Facet facet = (Facet)bo;
                    IntList = new List<int>();
                    IntList.Add((int)facet._Color);
                break;
                case BoardObject.eBoardObjectType.FACET_COLLECT:
                    FacetCollect facetCollect = (FacetCollect)bo;
                    IntList = new List<int>();
                    IntList.Add((int)facetCollect.FacetCollectType);         
                break;
                case BoardObject.eBoardObjectType.HAZARD:
                    Hazard hazard = (Hazard)bo; 
                    IntList = new List<int>();
                    IntList.Add((int)hazard.HazardType); 
                    FloatList = new List<float>();
                    FloatList.Add(hazard.EffectTime);
                    //FloatList.Add(hazard.EffectRadius);
                break;
                case BoardObject.eBoardObjectType.POINT_MOD:
                    PointMod pointMod = (PointMod)bo;
                    IntList = new List<int>();
                    IntList.Add((int)pointMod.PointModType);
                    IntList.Add((int)pointMod.PointModVal);
                    FloatList = new List<float>();
                    FloatList.Add(pointMod.PointModTime);
                break;
                case BoardObject.eBoardObjectType.SPEED_MOD:
                    SpeedMod speedMod = (SpeedMod)bo;                
                    IntList = new List<int>(); 
                    IntList.Add((int)speedMod.SpeedModType);
                    FloatList = new List<float>();
                    FloatList.Add(speedMod.SpeedModVal);
                break;                
                case BoardObject.eBoardObjectType.SHIELD:
                    Shield shield = (Shield)bo;
                    IntList = new List<int>();
                    IntList.Add((int)shield.ShieldType);
                    FloatList = new List<float>();                    
                break;                                
            }                       
        }
    }
    /// <summary>
    /// Save data for a channel
    /// </summary>
    [System.Serializable]
    public class ChannelSave
    {
        // Active data for each of the channel pieces
        public bool InnerActive; 
        public bool OuterActive;        
        // Only middle nodes can have board objects
        public BoardObjectSave MidNodeBO = null;

        public ChannelSave(Channel channel)
        {            
            InnerActive = channel.InnerChannel.Active;
            OuterActive = channel.OuterChannel.Active;            
            BoardObject bo = channel.MidNode.SpawnedBoardObject;
            if (bo != null) MidNodeBO = new BoardObjectSave(bo);            
        }
    }

    /// <summary>
    /// Save data for rings
    /// </summary>
    [System.Serializable]
    public class RingSave
    {
        public float RotX, RotY, RotZ;
        public float RotSpeed;
        public List<ChannelSave> ChannelSaves;
        public List<BumperSave> BumperSaves;
        
        public RingSave(Vector3 transformRot, float rotSpeed, int numChannels)
        {
            RotX = transformRot.x;
            RotY = transformRot.y;
            RotZ = transformRot.z;
            RotSpeed = rotSpeed;
            ChannelSaves = new List<ChannelSave>(numChannels);
        }
    }

    /// <summary>
    /// Save data for the puzzle information
    /// </summary>
    [System.Serializable]
    public class PuzzleSaveData
    {       
        // puzzle data
        public string PuzzleName;
        public eVictoryConditions VictoryCondition;
        
        public List<int> NumFacetsOnBoardSave = new List<int>();

        // wheel data
        public int NumRings;
        public List<RingSave> RingSaves = new List<RingSave>();

        // Player data
        public BoardObjectSave PlayerSave;

        public PuzzleSaveData(Dax dax)
        {            
            Wheel wheel = dax.Wheel;
            NumRings = wheel.NumActiveRings;
            int numRingsPlusCenter = NumRings + 1;

            PuzzleName = dax.PuzzleName;
            VictoryCondition = wheel.VictoryCondition;
            for(int i=0; i<wheel.NumFacetsOnBoard.Count; i++)
            {
                NumFacetsOnBoardSave.Add(wheel.NumFacetsOnBoard[i]);
            }
            
            for (int i=0; i< numRingsPlusCenter; i++)
            {                
                int numChannels = (i == 0 ? Wheel.NUM_CENTER_RING_CHANNELS : Wheel.NUM_OUTER_RING_CHANNELS);
                Ring ring = wheel.Rings[i];
                RingSave ringSave = new RingSave(ring.transform.eulerAngles, ring.RotateSpeed, numChannels);
                RingSaves.Add(ringSave);
                List<Channel> ringChannels = ring.transform.GetComponentsInChildren<Channel>().ToList();
                ringChannels = ringChannels.OrderBy(x => x.name).ToList();
                if (ringChannels.Count != numChannels) { Debug.LogError("Channel count mismatch. ringChannels.Count: " + ringChannels.Count + ", numChannels: " + numChannels);return; }
                // channels
                for (int j=0; j<numChannels; j++)
                {
                    Channel channel = ringChannels[j];
                    ChannelSave channelSave = new ChannelSave(channel);
                    ringSave.ChannelSaves.Add(channelSave);
                }
                // bumpers
                if (i != 0)
                {   // not center ring so it has a BumperGroup
                    ringSave.BumperSaves = new List<BumperSave>();
                    List<Bumper> bumpers = ring.BumperGroup.Bumpers.ToList();
                    bumpers = bumpers.OrderBy(x => x.name).ToList();
                    for(int j=0; j<bumpers.Count; j++)
                    {
                        Bumper bumper = bumpers[j];
                        BumperSave bumperSave = new BumperSave(bumper.BumperType, bumper.BumperColor);
                        ringSave.BumperSaves.Add(bumperSave);
                    }
                }
            }

            // Player
            Player player = FindObjectOfType<Player>();
            PlayerSave = new BoardObjectSave(player);
        }
    }    
}