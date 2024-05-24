using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This is the main root object for the gameplay.
/// </summary>
public class Dax : MonoBehaviour
{   
    // The victory conditions (or game type).
    // COLLECTION: Player needs to run over the facets to collect them.  When they have them all you win.
    // COLOR_MATCH: Player must run over the facet to start carrying it. Then
    //              run into a bumper at the edge of the ring of the corresponding color
    public enum eVictoryConditions { COLLECTION, COLOR_MATCH };

    public enum eGameState { PRE_GAME, RUNNING, GAME_OVER }; // Various states the game can me in
    public eGameState GameState;    
    
    public static float MAX_SPIN_SPEED = 20f; // Maximum speed the player can spin a ring
    public static float MAX_SPEED = 1f; // Maximum speed a board object or the player can go

    public static int MAX_NUM_RINGS = 4; // Maximum number of rings the game can have

    public string PuzzleName = "Default Puzzle"; //Name of the puzzle    
           
    public Wheel Wheel; // Ref to the wheel in the game
    public Ring CurTouchedRing = null; // The ring the user is currently touching
        
    // Ring Controls // monote - add headers
    LayerMask RingMask; // Layer mask for rings
    Vector2 RingCenterPoint = Vector3.zero; // Center for the currently selected ring
    Vector2 MousePosition;  // Position of the mouse
    float RingRot = 0f;      // Rotation of the ring
    float PointerPrevAngle; // Angle between the center point and the mouse pointer
    
    bool GameModActive; // Whether or not a gameplay modifier is active
    float GameModTimer; // Timer for the current gameplay modifier
    int GameModVal;     // Value of the current gameplay modifier

    public Player _Player;  // Ref to the player game object

    public UIRoot _UIRoot;  // Ref to the root UI

    [Header("Game Data")]
    public float LevelTime = 120f;  // Amount of time for the current level
    public int Score = 0;       // Player score

    [Header("Save Data")]
    public PuzzleSaveData _PuzzleSaveData = null;   // Save data for the current puzzle  
        

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
        GameModActive = true;
        GameModTimer = time;
        GameModVal = val;
    }

    /// <summary>
    /// Adds points to the player's current score
    /// </summary>
    /// <param name="points"></param>
    public void AddPoints(int points)
    {
        // Account for the game mod if active
        if(GameModActive == true)
        {
            points *= GameModVal;
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
        
        // Count down the game mod timer if it's on
        if(GameModActive == true)
        {
            GameModTimer -= Time.deltaTime;
            if(GameModTimer <= 0)
            {
                GameModActive = false;
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
        
        List<Hazard> hazards = transform.GetComponentsInChildren<Hazard>().ToList(); // moupdate wtf. all BoardObjects should get a fixed update
        foreach(Hazard ed in hazards) // moupdate - figure this out
        {
            ed.BoardObjectFixedUpdate(Time.deltaTime);
        }
        List<Interactable> interactables = transform.GetComponentsInChildren<Interactable>().ToList(); 
        foreach (Interactable interactable in interactables) 
        {
            interactable.BoardObjectFixedUpdate(Time.deltaTime);
        }
        RotateRings();        
    }
   
    //  You can rotate a direction Vector3 with a Quaternion by multiplying the quaternion with the direction(in that order).
    //  Then you just use Quaternion.AngleAxis to create the rotation.
    void RotateRings()
    {   // note: a BumperGroup is null if it's the center ring.  Otherwise it's just toggled
        if (Wheel == null) { Debug.LogError("ERROR: No CurWheel."); return; }
        foreach(Ring ring in Wheel.Rings)
        {
            if (ring == CurTouchedRing)
            {
                CurTouchedRing.transform.localEulerAngles = new Vector3(0f, RingRot, 0f);
//                Debug.Log("new rot: " + RingAngle);
                if (CurTouchedRing.BumperGroup != null) CurTouchedRing.BumperGroup.transform.localEulerAngles = new Vector3(0f, RingRot, 0f);
            }
            else
            {
                ring.Rotate();
                if (ring.BumperGroup != null) ring.BumperGroup.transform.localEulerAngles = new Vector3(0f, ring.transform.localEulerAngles.y, 0f);
            }
        }                
    }               
    
    public void EndGame(string reason)
    {
        GameState = eGameState.GAME_OVER;
        _UIRoot.ToggleEndGameItems(reason, true);
    }

    public void StartGame()
    {
        _UIRoot.PreGameButton.SetActive(false);
        GameState = eGameState.RUNNING;
    }

    
    public PuzzleSaveData CreateSaveData(Dax dax)
    {
        Debug.Log("Dax.SavePuzzle() /*creates save data*/ --MoSave--");
        _PuzzleSaveData = null;        
        _PuzzleSaveData = new PuzzleSaveData(dax);
        return _PuzzleSaveData;
    }

    //public Channel InnerChannel, OuterChannel;

    public bool ResetPuzzleFromSave(PuzzleSaveData saveData = null)
    {   // note - this assumes the puzzle is created and fresh
        if(saveData != null)
        {
            _PuzzleSaveData = null;
            _PuzzleSaveData = saveData;
        }

        MCP mcp = FindObjectOfType<MCP>();

        // right now the default puzzle is loaded and ready to be modified
        string s = "Dax.ResetPuzzleFromSave(). NumRings: " + _PuzzleSaveData.NumRings + ", ";
        s += "RingSaves count: " + _PuzzleSaveData.RingSaves.Count;
       // Debug.Log(s);
        // puzzle data
        this.PuzzleName = _PuzzleSaveData.PuzzleName;
        //CurTouchedRing RingAngle PointerPrevAngle
        CurTouchedRing = null;
        RingRot = 0f;
        PointerPrevAngle = 0f;
        // reset the wheel to starting state
        //mcp.ResetWheel(Wheels[0]);
        mcp.ResetWheel(Wheel);
        // set up # of rings
        //Wheels[0].VictoryCondition = _PuzzleSaveData.VictoryCondition;                
        //Wheels[0].TurnOnRings(_PuzzleSaveData.NumRings);    
        Wheel.VictoryCondition = _PuzzleSaveData.VictoryCondition;                
        Wheel.TurnOnRings(_PuzzleSaveData.NumRings);        

        // first pass through all the rings to create all the board objects
        for(int i=0; i<_PuzzleSaveData.RingSaves.Count; i++)
        {            
            int numChannels = (i == 0 ? Wheel.NUM_CENTER_RING_CHANNELS : Wheel.NUM_OUTER_RING_CHANNELS);
            RingSave ringSave = _PuzzleSaveData.RingSaves[i];
            if (numChannels != ringSave.ChannelSaves.Count) { Debug.LogError("numChannels: " + numChannels + ", doesn't match ChannelSaves.Count: " + ringSave.ChannelSaves.Count); return false; }
           // Ring ring = Wheels[0].Rings[i];
            Ring ring = Wheel.Rings[i];
            
            List<Channel> ringChannels = ring.transform.GetComponentsInChildren<Channel>().ToList();
            ringChannels = ringChannels.OrderBy(x => x.name).ToList();
            ring.RotateSpeed = ringSave.RotSpeed;

            // now create all the board objects
            for(int j=0; j<numChannels; j++)
            {
                ChannelSave channelSave = ringSave.ChannelSaves[j];
                Channel channel = ringChannels[j];                
                channel.InnerChannel.SetActive(channelSave.InnerActive);
                channel.OuterChannel.SetActive(channelSave.OuterActive);                
                //if (channelSave.StartNodeBO != null) mcp.CreateBoardObjectFromSaveData(channel.StartNode, channelSave.StartNodeBO, this);                
                // Unity handles serialization by creating MidNodeBO's even if you set them to null, so 
                // a lot of calls are attempted for empty objects, so check to see if there's a start channel defined.
                // Not the best solution but sorting this out is a future task
                if (channelSave.MidNodeBO != null && channelSave.MidNodeBO.StartChannel != "") 
                {
                    mcp.CreateBoardObjectFromSaveData(channel.MidNode, channelSave.MidNodeBO, this);
                }
                
                //if (channelSave.EndNodeBO != null) mcp.CreateBoardObjectFromSaveData(channel.EndNode, channelSave.EndNodeBO, this);                                
            }            
            // bumpers
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
           // else Debug.Log("ringSave.BumperSaves is null so must be center ring");
        }
        // now another pass to init anything that requires all the objects on the wheel to be created
        for (int i = 0; i < _PuzzleSaveData.RingSaves.Count; i++)
        {
            int numChannels = (i == 0 ? Wheel.NUM_CENTER_RING_CHANNELS : Wheel.NUM_OUTER_RING_CHANNELS);
            RingSave ringSave = _PuzzleSaveData.RingSaves[i];
            if (numChannels != ringSave.ChannelSaves.Count) { Debug.LogError("numChannels: " + numChannels + ", doesn't match ChannelSaves.Count: " + ringSave.ChannelSaves.Count); return false; }
           // Ring ring = Wheels[0].Rings[i];
            Ring ring = Wheel.Rings[i];
            List<Channel> ringChannels = ring.transform.GetComponentsInChildren<Channel>().ToList();
            ringChannels = ringChannels.OrderBy(x => x.name).ToList();

            for (int j = 0; j < numChannels; j++)
            {
                ChannelSave channelSave = ringSave.ChannelSaves[j];
                Channel channel = ringChannels[j];                
                //if (channel.StartNode.SpawnedBoardObject != null) mcp.InitBoardObjectFromSave(channel.StartNode.SpawnedBoardObject, channelSave.StartNodeBO);                
                if (channel.MidNode.SpawnedBoardObject != null) mcp.InitBoardObjectFromSave(channel.MidNode.SpawnedBoardObject, channelSave.MidNodeBO);
                //if (channel.EndNode.SpawnedBoardObject != null) mcp.InitBoardObjectFromSave(channel.EndNode.SpawnedBoardObject, channelSave.EndNodeBO);                
            }
        }

        //reset player        
        _Player.ResetForPuzzleRestart(_PuzzleSaveData.PlayerSave);        
        //Wheels[0].InitWheelFacets();       
        Wheel.InitWheelFacets();     
        
        // game state       
        GameState = eGameState.RUNNING;

        return true;
    }      
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
 
    [System.Serializable]
    public class BoardObjectSave
    {
        public BoardObject.eBoardObjectType Type;
        public string StartChannel;
        public BoardObject.eStartDir StartDir; // monewsave
        public float Speed;                          

        // generic vars for various specific classes
        // string lists, for example used in Warp nodes to keep destination list
        public List<string> StringList01; 
        public List<string> StringList02;

        // bools and floats for toggles 'n timers, etc
        public List<bool> BoolList;
        public List<int> IntList;
        public List<float> FloatList;
        public BoardObjectSave(BoardObject bo)
        {
            Type = bo.BoardObjectType;
            StartChannel = bo.CurChannel.name;        

            // movement
            StartDir = bo.StartDir; // monewsave
            Speed = bo.Speed;                        

            // connection lists
            if(Type == BoardObject.eBoardObjectType.HAZARD)
            {
                Hazard hazard = (Hazard)bo; 
                IntList = new List<int>();
                IntList.Add((int)hazard.HazardType); 
                FloatList = new List<float>();
                FloatList.Add(hazard.EffectTime);
                FloatList.Add(hazard.EffectRadius);
            }
            if(Type == BoardObject.eBoardObjectType.GAME_MOD)
            {
                GameMod gameMod = (GameMod)bo;
                IntList = new List<int>();
                IntList.Add((int)gameMod.GameModType);
                IntList.Add((int)gameMod.GameModVal);
                FloatList = new List<float>();
                FloatList.Add(gameMod.GameModTime);
            }
            else 
            if(Type == BoardObject.eBoardObjectType.FACET)
            {
                Facet facet = (Facet)bo;
                IntList = new List<int>();
                IntList.Add((int)facet._Color);
            }
            /*else if(Type == BoardObject.eBoardObjectType.INTERACTABLE)
            {
                Interactable interactable = (Interactable)bo;
                IntList = new List<int>();
                IntList.Add((int)interactable.InteractableType);
                if(interactable.InteractableType == Interactable.eInteractableType.SWITCH)
                {
                    StringList01 = new List<string>();
                    StringList02 = new List<string>();
                    foreach (ChannelPiece channelPiece in interactable.PiecesToTurnOff)
                    {
                        // s += ", turn off: " + channelPiece.name;
                        StringList01.Add(channelPiece.name);
                    }
                    foreach (ChannelPiece channelPiece in interactable.PiecesToTurnOn)
                    {
                        // s += ", turn on: " + channelPiece.name;
                        StringList02.Add(channelPiece.name);
                    }
                    // Debug.Log(s);
                }
                else if(interactable.InteractableType == Interactable.eInteractableType.WARP_GATE)
                {
                    StringList01 = new List<string>();
                    Interactable warpGate = (Interactable)bo;
                    foreach (Interactable destGate in warpGate.DestGates)
                    {
                        StringList01.Add(destGate.name);
                    }
                }
            }           */
            else if (Type == BoardObject.eBoardObjectType.SHIELD) // moupdate - make sure all these are in the same order
            {
                Shield shield = (Shield)bo;
                IntList = new List<int>();
                IntList.Add((int)shield.ShieldType);
                FloatList = new List<float>();
                //FloatList.Add(shield.Timer);                
            }
            else if (Type == BoardObject.eBoardObjectType.SPEED_MOD)
            {
                SpeedMod speedMod = (SpeedMod)bo;
                
                IntList = new List<int>(); // moupdate - fix the save/load system
                IntList.Add((int)speedMod.SpeedModType);
                FloatList = new List<float>();
                FloatList.Add(speedMod.SpeedModVal);
            }
            else if(Type == BoardObject.eBoardObjectType.FACET_COLLECT)
            {                
                FacetCollect facetCollect = (FacetCollect)bo;
                IntList = new List<int>();
                IntList.Add((int)facetCollect.FacetCollectType);               
            }
        }
    }
    [System.Serializable]
    public class ChannelSave
    {
        public bool InnerActive;
        public bool OuterActive;

        //public BoardObjectSave StartNodeBO = null;
        public BoardObjectSave MidNodeBO = null;
        //public BoardObjectSave EndNodeBO = null;

        public ChannelSave(Channel channel)
        {            
            InnerActive = channel.InnerChannel.Active;
            OuterActive = channel.OuterChannel.Active;
            //BoardObject bo = channel.StartNode.SpawnedBoardObject;            
            //if (bo != null) StartNodeBO = new BoardObjectSave(bo);
            BoardObject bo = channel.MidNode.SpawnedBoardObject;
            if (bo != null) MidNodeBO = new BoardObjectSave(bo);
            //bo = channel.EndNode.SpawnedBoardObject;
            //if (bo != null) EndNodeBO = new BoardObjectSave(bo);
        }
    }
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
            //Wheel wheel = dax.Wheels[0];
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