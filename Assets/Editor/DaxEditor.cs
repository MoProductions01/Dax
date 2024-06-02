using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


/// <summary>
/// This is the class for handling all of the in-engine puzzle design and layout functionality.
/// Because it's all self-contained in one file and isn't game play code I just used globals for everything.
/// The game play code uses normal coding practices.
/// </summary>
[CustomEditor(typeof(DaxPuzzleSetup))]
public class DaxEditor : Editor
{   
    GameObject SelectedGameObject;   // The GameObject the engine says we have selected.  Can be manually re-assigned if necessary        
    ChannelNode SelectedChannelNode; // The middle Channel Node that the user has selected for creating/modifying BoardObjects    
    BoardObject SelectedBoardObject; // BoardObject specific class for the BoardObject the user has selected on the board
    SerializedObject SelectedBoardObjectSO; // SerializedObject for the currently selected BoardObject        

    MCP MCP;   // The Master Control Program object from the main game
    Dax Dax;   // The Dax object from the main game
    DaxPuzzleSetup _DaxPuzzleSetup; // The MonoBehavior that this editor script extends
    SerializedObject DaxSetupSO;    // The SerializedObject for the DaxPuzzleSetup object    
    SerializedObject DaxSO; // The SerializedObject for the _Dax object

    int SelectedRing = 0; // Index of our currently selected ring          
    int NumRingsUserWants = Dax.MAX_NUM_RINGS; // How many rings the user wants to change to
    bool RingsNumChangePopupActive = false; // For confirmation if the user does in fact want to reduce the number of rings since that also destroys objects
    bool RingsResetPopupActive = false; // For confirmation if the user wants to reset the Ring since it clears all BoardObjects and resets ChannelPieces

    /* These are helper functions to take care of the multiple serialized property updates */
    void UpdateStringProperty(SerializedObject so, string propName, string stringValue)
    {
        if(so.FindProperty(propName) == null) return;

        so.FindProperty(propName).stringValue = stringValue;
        so.ApplyModifiedProperties();        
    }
    void UpdateFloatProperty(SerializedObject so, string propName, float floatValue)
    {
        if(so.FindProperty(propName) == null) return;

        so.FindProperty(propName).floatValue = floatValue;
        so.ApplyModifiedProperties();
    }
    void UpdateIntProperty(SerializedObject so, string propName, int intValue)
    {
        if(so.FindProperty(propName) == null) return;

        so.FindProperty(propName).intValue = intValue;
        so.ApplyModifiedProperties();
    }
    void UpdateEnumProperty(SerializedObject so, string propName, int enumValue)
    {   
        if(so.FindProperty(propName) == null) return;
             
        so.FindProperty(propName).enumValueIndex = enumValue;
        so.ApplyModifiedProperties();        
    }

    /// <summary>
    /// Utility function when you want to start a new section of the level creator
    /// </summary>
    /// <param name="selectionName">The name of the currently selected GameObject. </param>
    /// <param name="selectionType">Non-coding name of selection we're creating.</param>
    void StartNewSelection(string selectionName, string selectionType)
    {
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Selected Object: " + selectionName);
        EditorGUILayout.LabelField("Type: " + selectionType);
        EditorGUILayout.Separator();
    }

    /// <summary>
    /// This is for handling keyboard shortcuts.
    /// </summary>
    private void OnSceneGUI()
    {
        if (Selection.activeGameObject == null) return;
        // Since adding/removing Channel Pieces is so common we hooked it up to the 'B' key.
        ChannelPiece selectedChannelPiece = Selection.activeGameObject.GetComponent<ChannelPiece>();
        // if (Selection.activeGameObject.GetComponent<ChannelPiece>() != null) modelete toggle
        if(selectedChannelPiece != null)
        {           
            Event e = Event.current;
            if(e.type == EventType.KeyDown && e.keyCode == KeyCode.B)
            {
                selectedChannelPiece.Active = !selectedChannelPiece.Active;
                //ChannelPiece channelPiece = Selection.activeGameObject.GetComponent<ChannelPiece>();
                //channelPiece.Active = !channelPiece.Active;
                //Selection.activeGameObject.GetComponent<ChannelPiece>().Toggle(); modelete toggle              
                e.Use();
            }
        }            
    }

    /// <summary>
    /// This is to make sure the RadientDebug object is always hidden.  It's a bit hacky but it's the only thing that works
    /// </summary>
    /*void HandleSceneView()
    {        
        RadientDebug rrd = FindObjectOfType<RadientDebug>();
        if (rrd != null)
        {
            SceneVisibilityManager sv = SceneVisibilityManager.instance;
            if (sv.IsHidden(rrd.gameObject) == false) sv.Hide(rrd.gameObject, true);
        }
    } */       

    /// <summary>
    /// Handles the number of rings and ring selection
    /// </summary>    
    void HandleNumRingsAndRingSelection()
    {           
        EditorGUILayout.Separator();
        // If you reduce the number of rings you get a popup warning you that all the objects on the rings that will 
        // go away will be destroyed.
        if (RingsNumChangePopupActive == true)
        {   
            bool newNumRingsResponse = EditorUtility.DisplayDialog("Are you sure you want to decrease the number of rings?", 
                                                                    "This will reset all rings beyond the new number.", "Yes", "No");
            if (newNumRingsResponse == true)
            {   // User wants to reduce rings, so sort that out.
                RingsNumChangePopupActive = false;         
                // ResetRing destroys all the board objects and restores any removed ChannelPieces      
                for (int i = Dax.Wheel.NumActiveRings; i > NumRingsUserWants; i--) MCP.ResetRing(Dax.Wheel.Rings[i]);                      
                Selection.activeGameObject = null;
                SelectedRing = 0;
                Dax.Wheel.TurnOnRings(NumRingsUserWants); // This will turn on the correct # of puzzle rings
            }
            else RingsNumChangePopupActive = false; // User chose no so just shut off the popup            
        }
        else
        {   // Number of rings
            int newNumRings = EditorGUILayout.IntPopup("Number of Rings", Dax.Wheel.NumActiveRings, DaxPuzzleSetup.NUM_RINGS_NAMES, DaxPuzzleSetup.NUM_RINGS_TOTALS);
            if (newNumRings != Dax.Wheel.NumActiveRings)
            {
                if (newNumRings > Dax.Wheel.NumActiveRings)
                {   // If we're increasing the number of rings just go ahead and do it because the rings have been cleared already                    
                    Dax.Wheel.TurnOnRings(newNumRings);
                }
                else
                {   // We're decreasing the number of rings, which will destroy all the stuff on them, so give the user a popup
                    RingsNumChangePopupActive = true;
                    NumRingsUserWants = newNumRings;
                }
            }
        }

        // Ring selection is handled via the tool Inspector window since there's so many other colliders on the board
        EditorGUILayout.Separator();
        if (Selection.activeGameObject == null || (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Ring>() == null))
        {   // We have something other than a ring selected now so make sure that there's no ring selected
            SelectedRing = 0;            
        }
        // You always have at least one Ring other than the Center ring so
        // set up the enum list based on the # of rings after that
        List<string> ringNames = new List<string> { "None", "Center", "Ring 01" };
        for (int i = 2; i <= Dax.Wheel.NumActiveRings; i++) ringNames.Add("Ring " + i.ToString("D2"));        
        string[] ringNamesArray = ringNames.ToArray();
        // Set up the enum pulldown
        int newRing = EditorGUILayout.Popup("Select Ring", SelectedRing, ringNamesArray);
        if (newRing != SelectedRing)
        {
            if (newRing == 0)
            {   // If you selected "None" on the enum list then you will have no active selected object
                Selection.activeGameObject = null;
            }
            else
            {   // You've chosen a ring so make that the activeGameObject
                Selection.activeGameObject = Dax.Wheel.Rings[newRing - 1].gameObject;
            }
            SelectedRing = newRing;
        }
    }

    /// <summary>
    /// The overall puzzle info is always in the inspector.  Individual
    /// puzzle elements change depending on what's selected.
    /// </summary>    
    void HandlePuzzleInfo()
    {
        // Puzzle Name
        EditorGUILayout.Separator();
        string newName = EditorGUILayout.TextField(Dax.PuzzleName);
        if(newName != Dax.PuzzleName)
        {
            Dax.PuzzleName = newName;
            UpdateStringProperty(DaxSO, "PuzzleName", Dax.PuzzleName);            
        }                

        // Level Time
        EditorGUILayout.Separator();
        float newLevelTime = EditorGUILayout.FloatField("Level Time ", Dax.LevelTime);
        if (newLevelTime <= 10f) newLevelTime = 10f;
        if(newLevelTime != Dax.LevelTime)
        {
            Dax.LevelTime = newLevelTime;
            UpdateFloatProperty(DaxSO, "LevelTime", Dax.LevelTime);           
        }

        // Victory Condition
        EditorGUILayout.Separator();
        Dax.eVictoryConditions newVictoryCondition = (Dax.eVictoryConditions)EditorGUILayout.EnumPopup("Victory Conditions", Dax.Wheel.VictoryCondition);
        if (newVictoryCondition != Dax.Wheel.VictoryCondition)
        {
            Dax.Wheel.VictoryCondition = newVictoryCondition;
            UpdateEnumProperty(new SerializedObject(Dax.Wheel), "VictoryCondition", (int)Dax.Wheel.VictoryCondition);            
        }

        // A list of all the facets that you need to collect on the board
        EditorGUILayout.Separator();
        for (int i = 0; i < Dax.Wheel.NumFacetsOnBoard.Count-1; i++)
        {
            Facet.eFacetColors curColor = (Facet.eFacetColors)i;
            EditorGUILayout.LabelField(curColor.ToString() + " To Collect: " + Dax.Wheel.NumFacetsOnBoard[(int)curColor]);
        }         

        // Handle the number of rings and Ring selection
        HandleNumRingsAndRingSelection();

        // Puzzle initialization/saving
        EditorGUILayout.Separator();        
        if (GUILayout.Button("Init Puzzle"))
        {
            // You need to initialize the puzzle in order for a restart without restarting
            // the editor.  It creates in-game save data which will be used to reload the
            // level if you win or die and want to restart.     
            Dax.Wheel.ResetFacetsCount();
            Dax.CreateSaveData(Dax);            
        }        
    }

    /// <summary>
    /// User has selected a Ring from the tool so handle that
    /// </summary>
    void HandleRingSelected()
    {
        StartNewSelection(SelectedGameObject.name, "Ring");
        Ring selRing = SelectedGameObject.GetComponent<Ring>();
        // Check if the reset ring popup is active
        if(RingsResetPopupActive)
        {
            bool ringResetResponse = EditorUtility.DisplayDialog("Are you sure you want to reset this Ring?", 
                                                                "This will remove all BoardObjects and reset Channel Pieces", "Yes", "No");
            if(ringResetResponse == true)
            {   // User chose to reset, so reset the Ring
                MCP.ResetRing(selRing);
            }
            RingsResetPopupActive = false;
        }
        else
        {
            // Check for a difference in ring rotation speed
            float newSpeed = EditorGUILayout.FloatField("Rotate Speed: ", selRing.RotateSpeed);
            if (newSpeed != selRing.RotateSpeed)
            {
                // User changed ring rotation speed so sort that out and update serialized data
                selRing.RotateSpeed = newSpeed;
                SerializedObject selRingSO = new SerializedObject(selRing);
                selRingSO.Update();
                UpdateFloatProperty(selRingSO, "RotateSpeed", selRing.RotateSpeed);                    
            }            
            EditorGUILayout.Separator();
            // Check if the user wants to reset the ring. This spawns a popup for confirmation
            if (GUILayout.Button("Reset Ring", GUILayout.Width(300)))
            {                
                RingsResetPopupActive = true;
            }
        }        
        EditorGUILayout.EndVertical();
    }   

    /// <summary>
    /// Handle user selecting Player
    /// </summary>
    void HandlePlayerSelected()
    {
        StartNewSelection(SelectedGameObject.name, "Player");
        Player player = SelectedGameObject.GetComponent<Player>();
        SerializedObject playerSO = new SerializedObject(player);
        playerSO.Update();
        // Check if the starting speed has changed
        float newSpeed = EditorGUILayout.Slider("Speed", player.Speed, 0f, Dax.MAX_SPEED);
        if (newSpeed != player.Speed)
        {
            player.Speed = newSpeed;
            UpdateFloatProperty(playerSO, "Speed", player.Speed);                    
        }                                
        EditorGUILayout.EndVertical();
    }     

    /// <summary>
    /// Handle bumper is selected
    /// </summary>
    void HandleBumperSelected()
    {
        StartNewSelection(SelectedGameObject.name, "Bumper");
        Bumper selBumper = SelectedGameObject.GetComponent<Bumper>();
        SerializedObject selBumperSO = new SerializedObject(selBumper);
        selBumperSO.Update();

        // Check for changing bumper type
        Bumper.eBumperType newBumperType = (Bumper.eBumperType)EditorGUILayout.EnumPopup("Bumper Type", selBumper.BumperType);                
        if (newBumperType != selBumper.BumperType)
        {   // Bumper type has changed so update that and the material
            selBumper.BumperType = newBumperType;                        
            selBumper.gameObject.GetComponent<MeshRenderer>().material = MCP.GetBumperMaterial(newBumperType, selBumper.BumperColor);                    
            UpdateEnumProperty(selBumperSO, "BumperType", (int)selBumper.BumperType);            
        }
        EditorGUILayout.Separator();
        if (selBumper.BumperType == Bumper.eBumperType.COLOR_MATCH)
        {   // Check for new Bumper color
            Facet.eFacetColors newBumperColor = (Facet.eFacetColors)EditorGUILayout.EnumPopup("Bumper Color", selBumper.BumperColor);
            if (newBumperColor != selBumper.BumperColor)
            {
                // Color changed so update the data and material
                selBumper.BumperColor = newBumperColor;
                selBumper.gameObject.GetComponent<MeshRenderer>().material = MCP.GetBumperMaterial(selBumper.BumperType, newBumperColor);
                UpdateEnumProperty(selBumperSO, "BumperColor", (int)selBumper.BumperColor);
            }
        }
        EditorGUILayout.EndVertical();
    }    

    /// <summary>
    /// Handles toggling Channel Pieces on and off
    /// </summary>
    void HandleChannelPiece()
    {
        StartNewSelection(SelectedGameObject.name, "Channel Piece");                
        ChannelPiece selChannelPiece = SelectedGameObject.GetComponent<ChannelPiece>();               
        // Set up the button text
        // string s = selChannelPiece.IsActive() == true ? "Turn Off" : "Turn On"; modelete get
        string s = selChannelPiece.Active == true ? "Turn Off" : "Turn On";
        if (GUILayout.Button(s, GUILayout.Width(150f)))
        {   // User clicked the button so toggle the ChannelPiece
            selChannelPiece.Active = !selChannelPiece.Active;
            //ChannelPiece channelPiece = SelectedGameObject.GetComponent<ChannelPiece>();
            //channelPiece.Active = !channelPiece.Active;
            // SelectedGameObject.GetComponent<ChannelPiece>().Toggle(); modelete toggle
        }
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Channel Nodes are the core of setting up the level because these are 
    /// where you place all of the BoardObjects.
    /// </summary>
    void HandleChannelNode()
    {
        StartNewSelection(SelectedGameObject.name, "Channel Node");               
        SelectedChannelNode = SelectedGameObject.GetComponent<ChannelNode>();
        
        // For creating BoardObjects make sure it's a middle node since those are the
        // only ones that can have anything spawned on it.
        
        if (SelectedChannelNode.IsMidNode() == true) 
        //if (SelectedChannelNode.MyChannel.MidNode == true) modelete node
        {
            // Nodes can only have one BoardObject on them
            if(SelectedChannelNode.SpawnedBoardObject == null)
            {   // no board object so handle creating one
                HandleCreateBoardObject();                  
            }
            else
            {   // There's a BoardObject on this node so handle that
                HandleSelectedBoardObjectNode();                    
            }
        }    
        else if( SelectedChannelNode.name.Contains("Ring_00_Start")) 
        {
            // If it's not a middle node make sure it's a start node
            // on the center ring because those are the ones used to
            // select the Player's starting channel
            Player player = FindObjectOfType<Player>();
            // Check if the selected node is already the start node
            if(SelectedChannelNode != player.GetStartChannelNode())
            {
                if (GUILayout.Button("Make Starting Channel", GUILayout.Width(200f)))
                {   // Change the player's starting channel based on the selected node
                    int startChannelIndex = Int32.Parse(SelectedChannelNode.name.Substring(19, 2)) - 1;                                               
                    player.SetStartChannel(startChannelIndex);                            
                }
            }            
        }                                                                                                                                      
        
        EditorGUILayout.Separator();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// This is a special case since sometimes a BoardObject on the node can make it difficult to
    /// select the node itself in the editor.  So if you've clicked on the BoardObject spawned on
    /// the node handle it as if Selection.activeGameObject is the node underneath.  
    /// </summary>
    void HandleSpawnedBoardObjectSelected()
    {
        BoardObject parent = SelectedGameObject.transform.parent.GetComponent<BoardObject>();
        SelectedChannelNode = parent.SpawningNode;
        StartNewSelection(SelectedChannelNode.name, "Channel Node");
        HandleSelectedBoardObjectNode();
        EditorGUILayout.Separator();
        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// This handles the case where the engine says we have an active
    /// GameObject selected
    /// </summary>
    void HandleActiveGameObject()
    {
        // Handle the selected game object depending on what it is

        if (SelectedGameObject.GetComponent<Ring>() != null)
        {
            HandleRingSelected();            
        }        
        if (SelectedGameObject.GetComponent<Player>() != null)
        {
            HandlePlayerSelected();
        }        
        if (SelectedGameObject.GetComponent<Bumper>() != null)
        {
            HandleBumperSelected();
        }        
        if (SelectedGameObject.GetComponent<ChannelPiece>() != null)
        {
            HandleChannelPiece();
        }         
        if (SelectedGameObject.GetComponent<ChannelNode>() != null)
        {
            HandleChannelNode();
        }        
        if(SelectedGameObject.transform.parent != null && SelectedGameObject.transform.parent.GetComponent<BoardObject>() != null)
        {          
            // This is a special case for when the user clicks on a spawned BoardObject. 
            // See function's comments for full explanation.
            HandleSpawnedBoardObjectSelected();
        }  
    }

    /// <summary>
    /// This is the callback from the engine that is the core of interacting
    /// with the in-engine tool Inspector window
    /// </summary>
    public override void OnInspectorGUI()
    {
        if(Application.isPlaying) return;        
        //HandleSceneView(); // make sure the Radient Debug object is not visible NOTE: only needed if we're using the RadientDebug stuff         
    
        // Gather the main GameObjects and SerializedObjects used for everything
        MCP = GameObject.FindObjectOfType<MCP>();
        _DaxPuzzleSetup = (DaxPuzzleSetup)target;        
        DaxSetupSO = new SerializedObject(_DaxPuzzleSetup);
        DaxSetupSO.Update();
        Dax = MCP.Dax.GetComponent<Dax>();
        DaxSO = new SerializedObject(Dax);
        DaxSO.Update();        

        // This is for the overall puzzle data, not individual objects on the puzzle
        HandlePuzzleInfo();
           
        // Check to see if the engine is telling us we have a GameObject selected
        SelectedGameObject = Selection.activeGameObject;              
        //if (LastSelectedGameObject != SelectedGameObject) LastSelectedGameObject = SelectedGameObject;        

        if (SelectedGameObject != null)
        {
            // The engine is telling us we have a GameObject selected so handle that.
            HandleActiveGameObject();                                                                                                                                        
        }

        if (GUILayout.Button("Clear ", GUILayout.Width(150f)))
        {
            List<ChannelPiece> pieces = Dax.GetComponentsInChildren<ChannelPiece>().ToList();
            foreach(ChannelPiece cp in pieces)
            {
                cp.GetComponent<MeshRenderer>().enabled = false;
            }
        }

        // Save data                        
        DaxSetupSO.ApplyModifiedProperties();
        DaxSO.ApplyModifiedProperties();            
        Undo.RecordObject(_DaxPuzzleSetup, "OnInspectorGUI");
        EditorUtility.SetDirty(_DaxPuzzleSetup);
    }   
    
    /// <summary>
    /// Handles when the user has an empty middle node selected and is
    /// ready to add a BoardObject
    /// </summary>    
    void HandleCreateBoardObject()
    {
        float buttonWidth = 200f;
        if (GUILayout.Button("Create Facet", GUILayout.Width(buttonWidth)))
        {                        
            Facet facet = MCP.CreateBoardObject<Facet>(SelectedChannelNode, Dax, 
               (int)BoardObject.eBoardObjectType.FACET, 0);
            MCP.ChangeFacetColor(facet, Facet.eFacetColors.RED);                 
        }        
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Hazard", GUILayout.Width(buttonWidth)))
        {                          
            MCP.CreateBoardObject<Hazard>(SelectedChannelNode, Dax, 
               (int)BoardObject.eBoardObjectType.HAZARD, (int)Hazard.eHazardType.ENEMY);                                  
        }             
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Facet Collect", GUILayout.Width(buttonWidth)))
        {            
            MCP.CreateBoardObject<FacetCollect>(SelectedChannelNode, Dax, 
                (int)BoardObject.eBoardObjectType.FACET_COLLECT, (int)FacetCollect.eFacetCollectTypes.RING);                            
        } 
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Shield", GUILayout.Width(buttonWidth)))
        {            
            MCP.CreateBoardObject<Shield>(SelectedChannelNode, Dax, 
                (int)BoardObject.eBoardObjectType.SHIELD, (int)Shield.eShieldTypes.HIT);                  
        }
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Speed Mod", GUILayout.Width(buttonWidth)))
        {            
            MCP.CreateBoardObject<SpeedMod>(SelectedChannelNode, Dax, 
                (int)BoardObject.eBoardObjectType.SPEED_MOD, (int)SpeedMod.eSpeedModType.PLAYER_SPEED);                        
        }
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Point Mod", GUILayout.Width(buttonWidth)))
        {            
            MCP.CreateBoardObject<PointMod>(SelectedChannelNode, Dax, 
                (int)BoardObject.eBoardObjectType.POINT_MOD, (int)PointMod.ePointModType.EXTRA_POINTS);                   
        }                                   
    }

    /// <summary>
    /// User has a FACET type selected
    /// </summary>
    void HandleFacetSelected()
    {
        Facet facet = (Facet)SelectedBoardObject;
        Facet.eFacetColors newFacetColor = (Facet.eFacetColors)EditorGUILayout.EnumPopup("Facet Color", facet._Color);
        if (newFacetColor != facet._Color)
        {   // User wants to change color of the facet so handle that
            facet._Color = newFacetColor;
            MCP.ChangeFacetColor(facet, facet._Color);
            UpdateEnumProperty(SelectedBoardObjectSO, "_Color", (int)facet._Color);                    
        }
    }

    /// <summary>
    /// Handle a HAZARD type BoardObject selected
    /// </summary>    
    void HandleHazardSelected()
    {
        Hazard hazard = (Hazard)SelectedBoardObject;
        Hazard.eHazardType newHazardType = (Hazard.eHazardType)EditorGUILayout.EnumPopup("Type: ", hazard.HazardType);
        if (newHazardType != hazard.HazardType)
        {   // User wants to change the type of HAZARD, so trash the current one and create the new one
            DestroyImmediate(SelectedChannelNode.SpawnedBoardObject.gameObject);                                        
            hazard = MCP.CreateBoardObject<Hazard>(SelectedChannelNode, Dax, 
                (int)BoardObject.eBoardObjectType.HAZARD, (int)newHazardType); 
        }
        if (hazard.HazardType == Hazard.eHazardType.ENEMY )
        {   // Handle an ENEMY type of HAZARD
            EditorGUILayout.Separator();            
            BoardObject.eStartDir newStartDir = (BoardObject.eStartDir)EditorGUILayout.EnumPopup("Start Direction:", SelectedBoardObject.StartDir);
            if (newStartDir != SelectedBoardObject.StartDir) // monewsave
            {   // Change of direction the enemy will move when the game begins
                SelectedBoardObject.StartDir = newStartDir;
                UpdateEnumProperty(SelectedBoardObjectSO, "StartDir", (int)SelectedBoardObject.StartDir);                        
                hazard.transform.LookAt(SelectedBoardObject.StartDir == BoardObject.eStartDir.OUTWARD ? hazard.CurChannel.EndNode.transform : hazard.CurChannel.StartNode.transform);
            }                      
            EditorGUILayout.Separator();
            float newSpeed = EditorGUILayout.Slider("Speed", SelectedBoardObject.Speed, 0f, Dax.MAX_SPEED);
            if (newSpeed != SelectedBoardObject.Speed)
            {   // Change the starting speed of the ENEMY
                SelectedBoardObject.Speed = newSpeed;
                UpdateFloatProperty(SelectedBoardObjectSO, "Speed",  SelectedBoardObject.Speed);                        
            }                                                         
        }
        if (hazard.HazardType == Hazard.eHazardType.GLUE )
        {   // Handle a GLUE type of HAZARD
            EditorGUILayout.Separator();
            float newEffectTime = EditorGUILayout.Slider("Effect Time", hazard.EffectTime, Hazard.MIN_EFFECT_TIME, Hazard.MAX_EFFECT_TIME);
            if (newEffectTime != hazard.EffectTime)
            {   // Change of the amount of time the GLUE will keep the player in place
                hazard.EffectTime = newEffectTime;
                UpdateFloatProperty(SelectedBoardObjectSO, "EffectTime", hazard.EffectTime);                        
            }
        }  
    }

    /// <summary>
    /// Handle a FacetCollect BoardObject type selected
    /// </summary>    
    void HandleFacetCollectSelected()
    {
        EditorGUILayout.Separator();
        FacetCollect facetCollect = (FacetCollect)SelectedBoardObject;
        
        FacetCollect.eFacetCollectTypes newFacetCollectType = (FacetCollect.eFacetCollectTypes)EditorGUILayout.EnumPopup("Type: ", facetCollect.FacetCollectType);
        if (newFacetCollectType != facetCollect.FacetCollectType)
        {   // User changed the type of FacetCollect so trash old one and create a new one
            DestroyImmediate(SelectedChannelNode.SpawnedBoardObject.gameObject);                                
            facetCollect = MCP.CreateBoardObject<FacetCollect>(SelectedChannelNode, Dax, 
                (int)BoardObject.eBoardObjectType.FACET_COLLECT, (int)newFacetCollectType);
        } 
    }

    /// <summary>
    /// Handle a Shield BoardObject type selected
    /// </summary>
    void HandleShieldSelected()
    {
        EditorGUILayout.Separator();
        Shield shield = (Shield)SelectedBoardObject;
        
        Shield.eShieldTypes newShieldType = (Shield.eShieldTypes)EditorGUILayout.EnumPopup("Type: ", shield.ShieldType);
        if(newShieldType != shield.ShieldType)
        {   // User changed Shield type so trash old one and create new one
            DestroyImmediate(SelectedChannelNode.SpawnedBoardObject.gameObject);                
            shield = MCP.CreateBoardObject<Shield>(SelectedChannelNode, Dax, 
                (int)BoardObject.eBoardObjectType.SHIELD, (int)newShieldType);     
        }   
    }

    /// <summary>
    /// Handle a SpeedMod BoardObject type selected
    /// </summary>
    void HandleSpeedModSelected()
    {
        EditorGUILayout.Separator();
        SpeedMod speedMod = (SpeedMod)SelectedBoardObject;

        SpeedMod.eSpeedModType newSpeedModType = (SpeedMod.eSpeedModType)EditorGUILayout.EnumPopup("Type: ", speedMod.SpeedModType);
        if (newSpeedModType != speedMod.SpeedModType)
        {   // User changed SpeedMod type so trash old one and create a new one
            DestroyImmediate(SelectedChannelNode.SpawnedBoardObject.gameObject);                                
            speedMod = MCP.CreateBoardObject<SpeedMod>(SelectedChannelNode, Dax, 
                (int)BoardObject.eBoardObjectType.SPEED_MOD, (int)newSpeedModType);

        }        
        // Different min and max values for the SpeedMod type
        float minSpeedModVal, maxSpeedModVal;
        if(speedMod.SpeedModType == SpeedMod.eSpeedModType.RING_SPEED)
        {
            minSpeedModVal = -10f;
            maxSpeedModVal = 10f;
        }
        else
        {
            minSpeedModVal = 0f;
            maxSpeedModVal = 1f;
        }
        float newSpeedModVal =  EditorGUILayout.Slider("Speed Mod:", speedMod.SpeedModVal, minSpeedModVal, maxSpeedModVal);
        if (newSpeedModVal != speedMod.SpeedModVal)
        {   // SpeedMod value changed so update and save data                        
            speedMod.SpeedModVal = newSpeedModVal;
            UpdateFloatProperty(SelectedBoardObjectSO, "SpeedModVal", speedMod.SpeedModVal);                    
        }   
    }

    /// <summary>
    /// Handle a PointMod BoardObject type selected
    /// </summary>    
    void HandlePointModSelected()
    {
        EditorGUILayout.Separator();

        PointMod pointMod = (PointMod)SelectedBoardObject;
                
        PointMod.ePointModType newPointModType = (PointMod.ePointModType)EditorGUILayout.EnumPopup("Point Mod Type: ", pointMod.PointModType);
        if (newPointModType != pointMod.PointModType)
        {   // User is changing the type of PointMod so destroy the current one and create a new one
            DestroyImmediate(SelectedChannelNode.SpawnedBoardObject.gameObject);                          
            pointMod = MCP.CreateBoardObject<PointMod>(SelectedChannelNode, Dax, 
                (int)BoardObject.eBoardObjectType.POINT_MOD, (int)newPointModType);
            SelectedBoardObject = pointMod;
                return;
        }
        // the PointModVal is either how many points the user will get or how much of a 
        // multiplier the user will get.
        int newPointModVal = EditorGUILayout.IntField("Point Mod Val: ", pointMod.PointModVal);
        if (newPointModVal <= 0f) newPointModVal = 1; // Make sure it's at least 1
        if (newPointModVal != pointMod.PointModVal)
        {   // Update the PointModVal
            pointMod.PointModVal = newPointModVal;
            UpdateIntProperty(SelectedBoardObjectSO, "PointModVal", pointMod.PointModVal);     // mofeh           
        }
        if(pointMod.PointModType == PointMod.ePointModType.POINTS_MULTIPLIER)
        {   // PointsMultipliers have a timer
            float newTimer = EditorGUILayout.FloatField("Timer: ", pointMod.PointModTime);
            if (newTimer < 1.0f) newTimer = 1.0f; // Make sure at least 1 second for the timer
            if (newTimer != pointMod.PointModTime)
            {   // Update changed mod timer
                pointMod.PointModTime = newTimer;
                UpdateFloatProperty(SelectedBoardObjectSO, "PointModTime", pointMod.PointModTime);                   
            }
        }
    }

    /// <summary>
    /// Handle when the user has selected a node with a BoardObject
    /// spawned on it.
    /// </summary>    
    void HandleSelectedBoardObjectNode()
    {
        SelectedBoardObject = SelectedChannelNode.SpawnedBoardObject;
        SelectedBoardObjectSO = new SerializedObject(SelectedBoardObject);
        SelectedBoardObjectSO.Update();
        string selectedBoardObjectName = BoardObject.BOARD_OBJECT_EDITOR_NAMES[(int)SelectedBoardObject.BoardObjectType];

        // If the user clicks the "ping" button it will show the BoardObject
        // in the editor's Heirarchy window
        if (GUILayout.Button("Ping " + selectedBoardObjectName, GUILayout.Width(150f)))
        {
            EditorGUIUtility.PingObject(SelectedBoardObject);            
        }
        EditorGUILayout.Separator();
        // Handles deleting the spawned BoardObject
        if (GUILayout.Button("Delete " + selectedBoardObjectName, GUILayout.Width(150f)))
        {           
            DestroyImmediate(SelectedBoardObject.gameObject);
            SelectedChannelNode.SpawnedBoardObject = null;
        }
        EditorGUILayout.Separator();
        
        // Start handling the different kinds of BoardObjects that can be selected/modified
        switch (SelectedBoardObject.BoardObjectType)
        {
            case BoardObject.eBoardObjectType.FACET:
                HandleFacetSelected();
                break;
            case BoardObject.eBoardObjectType.HAZARD:
                HandleHazardSelected();
                break;
            case BoardObject.eBoardObjectType.FACET_COLLECT:
                HandleFacetCollectSelected();
                break;
            case BoardObject.eBoardObjectType.SHIELD:
                HandleShieldSelected();
                break;
            case BoardObject.eBoardObjectType.SPEED_MOD:
                HandleSpeedModSelected();
                break;
            case BoardObject.eBoardObjectType.POINT_MOD:
                HandlePointModSelected();
                break;
        }
    }              
}
