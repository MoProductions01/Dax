using System;
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
    public static int MAX_NUM_RINGS = 1; // Maximum number of rings the game can have // monotewheel
    public static float DEFAULT_LEVEL_TIME = 120f; // Default time for the level
     
    // The victory conditions (or game type).
    // COLLECTION: Player needs to run over the facets to collect them.  When they have them all you win.
    // COLOR_MATCH: Player must run over the facet to start carrying it. Then
    //              run into a bumper at the edge of the ring of the corresponding color to collect it.
    public enum eVictoryConditions { COLLECTION, COLOR_MATCH };
    // NOTE: The game originally was multi wheel and each wheel had it's own victory condition, so the
    // eVictoryCondition is part of the wheel.  It's single wheel for now for demo purposes but for future
    // proofing I'm keeping the victory conditions on the wheel in case I ever go back to multi-wheel
    //[field: SerializeField] public eVictoryConditions VictoryCondition {get; set;} see comment above
    
    [field: SerializeField] public Player Player {get; set;} // Ref to the player game object

     // Various states the game can be in
    public enum eGameState { PRE_GAME, RUNNING, GAME_OVER };
    public eGameState GameState {get; set;} // Current game state
                   
    [field: SerializeField] public Wheel Wheel {get; set;} // Ref to the wheel in the game
    
    private Ring CurTouchedRing;  // The ring the user is currently touching            
    private LayerMask RingMask; // Layer mask for rings
    private Vector2 RingCenterPoint = Vector3.zero; // Center for the currently selected ring
    private Vector2 MousePosition;  // Position of the mouse
    private float RingRot = 0f;      // Rotation of the ring
    private float PointerPrevAngle; // Angle between the center point and the mouse pointer
    
    public bool PointModActive; // Whether or not a gameplay modifier is active
    public float PointModTimer; // Timer for the current gameplay modifier // moui
    public int PointModVal;     // Value of the current gameplay modifier
            
    public float LevelTime {get; set;} = DEFAULT_LEVEL_TIME;  // Amount of time for the current level
    public int Score {get; set;} = 0;       // Player score // moui
    
    public TimeSpan ts;
    [field: SerializeField] public DaxSaveData.PuzzleSaveData PuzzleSaveData {get; set;}   // Save data for the current puzzle  
        
    public UIRoot UIRoot;  // Ref to the root UI moui
    [field: SerializeField] public string PuzzleName {get; set;} = "Default Puzzle"; //Name of the puzzle    

    /// <summary>
    /// Sets all of the basic gameplay info on startup
    /// </summary>
    private void Awake()    
    {
        GameState = eGameState.PRE_GAME; // Dax.Awake()
        RingMask = LayerMask.GetMask("Main Touch Control");        
        Score = 0;
        UIRoot = FindObjectOfType<UIRoot>(); // moui
        UIRoot.Init();
    }      
    
    /// <summary>
    /// Starts a point modifier
    /// </summary>
    /// <param name="time">How long the modifier will last</param>
    /// <param name="val">Mod val (point multiplier, etc) for the current mod0</param>
    public void BeginPointMod(float time, int val)
    {
        UIRoot.PointMultTimeText.transform.parent.gameObject.SetActive(true);
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
        UIRoot.ScoreText.SetText(Score.ToString()); // moui timer/score
    }   

    /// <summary>
    /// Updates the game every frame and handles user input
    /// </summary>
    void Update()
    {
        if (GameState != eGameState.RUNNING) return; // Bail if the game isn't in it's running state     
        
        // Handle the timer
        LevelTime -= Time.deltaTime;        
        UIRoot.SetTimerText(LevelTime);
        if(LevelTime <= 0f)
        {
            EndGame("Time Ran Out", false);
            UIRoot.SetTimerText("0:00"); // specal case to display 0:00 on the UI            
        }        
        // Count down the point mod timer if it's on
        if(PointModActive == true)
        {
            PointModTimer -= Time.deltaTime;
            UIRoot.SetPointModTimerText(PointModTimer);
            if(PointModTimer <= 0)
            {
                PointModActive = false;
                UIRoot.PointMultTimeText.transform.parent.gameObject.SetActive(false); // moui
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
        Player.BoardObjectFixedUpdate(Time.deltaTime);
        
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
    public void EndGame(string reason, bool isVictory)
    {
        GameState = eGameState.GAME_OVER;
        UIRoot.ShowEndGame(reason, true);
        if(isVictory == true)
        {
            SoundFXPlayer.PlaySoundFX("VictoryVoice", 1f);
            SoundFXPlayer.PlaySoundFX("VictorySound", .8f);
        }
        else
        {
            SoundFXPlayer.PlaySoundFX("DefeatVoice", 1f);
            SoundFXPlayer.PlaySoundFX("DefeatSound", .8f);
        }
    }

    /// <summary>
    /// Handles the start of the game.  The wheel is already setup before this
    /// </summary>
    public void StartGame()
    {
//        Debug.Log("Start Game");
        UIRoot.ClickToStartButton.SetActive(false);
        GameState = eGameState.RUNNING;
    }

    /// <summary>
    /// Creates new puzzle save data based on all of the information in the Dax object
    /// </summary>
    /// <param name="dax">Dax object that stores all the game data</param>
    /// <returns></returns>
    public DaxSaveData.PuzzleSaveData CreateSaveData(Dax dax)
    {     
        PuzzleSaveData = null;        
        PuzzleSaveData = new DaxSaveData.PuzzleSaveData(dax);
        return PuzzleSaveData;
    }
    
    /// <summary>
    /// Handles the resetting of the puzzle from save data.  Called when loading
    /// a puzzle or restarting the current level
    /// </summary>
    /// <param name="saveData">Save data to reset from</param>
    /// <returns></returns>
    public bool ResetPuzzleFromSave(DaxSaveData.PuzzleSaveData saveData = null)
    {   
        // Check to see if we're going to override the current save data
        if(saveData != null)
        {
            PuzzleSaveData = null;
            PuzzleSaveData = saveData;
        }

        MCP mcp = FindObjectOfType<MCP>(); // I know FindObjectOfType is bad but this code only happens when reloading a puzzle  
        
        this.PuzzleName = PuzzleSaveData.PuzzleName; // update puzzle's name
        // Reset ring and touch data        
        CurTouchedRing = null;
        RingRot = 0f;
        PointerPrevAngle = 0f;

        mcp.ResetWheel(Wheel); // reset the wheel to starting state                
        Wheel.VictoryCondition = PuzzleSaveData.VictoryCondition;                
        Wheel.TurnOnRings(PuzzleSaveData.NumRings); // turn on # of rings baesd on save data               
       
        // first pass through all the rings to create all the board objects
        for(int i=0; i<PuzzleSaveData.RingSaves.Count; i++)
        {            
            int numChannels = (i == 0 ? Wheel.NUM_CENTER_RING_CHANNELS : Wheel.NUM_OUTER_RING_CHANNELS);
            // get the ring save data
            DaxSaveData.RingSave ringSave = PuzzleSaveData.RingSaves[i];
            if (numChannels != ringSave.ChannelSaves.Count) { Debug.LogError("numChannels: " + numChannels + ", doesn't match ChannelSaves.Count: " + ringSave.ChannelSaves.Count); return false; }           
            Ring ring = Wheel.Rings[i];
            ring.RotateSpeed = ringSave.RotSpeed;
            
            // Get an ordered list of the channels on this ring
            List<Channel> ringChannels = ring.transform.GetComponentsInChildren<Channel>().ToList();
            ringChannels = ringChannels.OrderBy(x => x.name).ToList();            
            // now create all the board objects
            for(int j=0; j<numChannels; j++)
            {
                DaxSaveData.ChannelSave channelSave = ringSave.ChannelSaves[j];
                Channel channel = ringChannels[j];                
                // channel.InnerChannel.SetActive(channelSave.InnerActive); modelete set
                // channel.OuterChannel.SetActive(channelSave.OuterActive);         
                channel.InnerChannel.Active = channelSave.InnerActive; 
                channel.OuterChannel.Active = channelSave.OuterActive;       
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
        for (int i = 0; i < PuzzleSaveData.RingSaves.Count; i++)
        {   
            int numChannels = (i == 0 ? Wheel.NUM_CENTER_RING_CHANNELS : Wheel.NUM_OUTER_RING_CHANNELS); // the center ring has a different number of channels than the outer ones
            DaxSaveData.RingSave ringSave = PuzzleSaveData.RingSaves[i];
            if (numChannels != ringSave.ChannelSaves.Count) { Debug.LogError("numChannels: " + numChannels + ", doesn't match ChannelSaves.Count: " + ringSave.ChannelSaves.Count); return false; }
            Ring ring = Wheel.Rings[i];
            List<Channel> ringChannels = ring.transform.GetComponentsInChildren<Channel>().ToList();
            ringChannels = ringChannels.OrderBy(x => x.name).ToList();
            for (int j = 0; j < numChannels; j++)
            {
                DaxSaveData.ChannelSave channelSave = ringSave.ChannelSaves[j];
                Channel channel = ringChannels[j];                                
                if (channel.MidNode.SpawnedBoardObject != null) mcp.InitBoardObjectFromSave(channel.MidNode.SpawnedBoardObject, channelSave.MidNodeBO);                
            }
        }
         
        Player.ResetPlayer(PuzzleSaveData.PlayerSave); //reset player                   
        Wheel.ResetFacetsCount();    // Init all of the facets on the current wheel                        

        return true;
    }      
}