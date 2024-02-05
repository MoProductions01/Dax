using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;



[CustomEditor(typeof(DaxPuzzleSetup))]
public class DaxEditor : Editor
{   
    public GameObject SelectedGameObject;   // The GameObject the engine says we have selected.  Can be manually re-assigned if necessary
    public GameObject LastSelectedGameObject;    // Used for checking for changes in the selected object    
    MCP _MCP;   // The Master Control Program object from the main game
    Dax _Dax;   // The Dax object from the main game
    DaxPuzzleSetup _DaxPuzzleSetup; // The MonoBehavior that this editor script extends
    SerializedObject DaxSetupSO;    // The SerializedObject for the DaxPuzzleSetup object    
    SerializedObject DaxSO; // The SerializedObject for the _Dax object

    int SelectedRing = 0; // Index of our currently selected ring          
    int NumRingsUserWants = 4; // How many rings the user wants to change to
    bool RingsNumChangePopupActive = false; // For confirmation if the user does in fact want to reduce the number of rings since that also destroys objects
    bool RingsResetPopupActive = false; // For confirmation if the user wants to reset the Ring since it clears all BoardObjects and resets ChannelPieces

    /* These are helper functions to take care of the multiple serialized property updates */
    void UpdateStringProperty(SerializedObject so, string propName, string stringValue)
    {
        so.FindProperty(propName).stringValue = stringValue;
        so.ApplyModifiedProperties();        
    }
    void UpdateFloatProperty(SerializedObject so, string propName, float floatValue)
    {
        so.FindProperty(propName).floatValue = floatValue;
        so.ApplyModifiedProperties();
    }
    void UpdateIntProperty(SerializedObject so, string propName, int intValue)
    {
        so.FindProperty(propName).intValue = intValue;
        so.ApplyModifiedProperties();
    }
    void UpdateEnumProperty(SerializedObject so, string propName, int enumValue)
    {        
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
        if (Selection.activeGameObject.GetComponent<ChannelPiece>() != null)
        {           
            Event e = Event.current;
            if(e.type == EventType.KeyDown && e.keyCode == KeyCode.B)
            {
                Selection.activeGameObject.GetComponent<ChannelPiece>().Toggle();
                e.Use();
            }
        }            
    }

    /* This is to make sure the RifRafDebug object is always hidden.  It's hacky but it's editor code so I'm ok with it */
    void HandleSceneView()
    {
        return; // monote - might not need this
        /*RifRafDebug rrd = FindObjectOfType<RifRafDebug>();
        if (rrd != null)
        {
            SceneVisibilityManager sv = SceneVisibilityManager.instance;
            if (sv.IsHidden(rrd.gameObject) == false) sv.Hide(rrd.gameObject, true);
        }*/
    }        

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
                for (int i = _Dax.CurWheel.NumActiveRings; i > NumRingsUserWants; i--) _MCP.ResetRing(_Dax.CurWheel.Rings[i]);                      
                Selection.activeGameObject = null;
                SelectedRing = 0;
                _Dax.CurWheel.TurnOnRings(NumRingsUserWants); // This will turn on the correct # of puzzle rings
            }
            else RingsNumChangePopupActive = false; // User chose no so just shut off the popup            
        }
        else
        {   // Number of rings
            int newNumRings = EditorGUILayout.IntPopup("Number of Rings", _Dax.CurWheel.NumActiveRings, DaxPuzzleSetup.NUM_RINGS_NAMES, DaxPuzzleSetup.NUM_RINGS_TOTALS);
            if (newNumRings != _Dax.CurWheel.NumActiveRings)
            {
                if (newNumRings > _Dax.CurWheel.NumActiveRings)
                {   // If we're increasing the number of rings just go ahead and do it because the rings have been cleared already                    
                    _Dax.CurWheel.TurnOnRings(newNumRings);
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
        for (int i = 2; i <= _Dax.CurWheel.NumActiveRings; i++) ringNames.Add("Ring " + i.ToString("D2"));        
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
                Selection.activeGameObject = _Dax.CurWheel.Rings[newRing - 1].gameObject;
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
        string newName = EditorGUILayout.TextField(_Dax.PuzzleName);
        if(newName != _Dax.PuzzleName)
        {
            _Dax.PuzzleName = newName;
            UpdateStringProperty(DaxSO, "PuzzleName", _Dax.PuzzleName);            
        }                

        // Level Time
        EditorGUILayout.Separator();
        float newLevelTime = EditorGUILayout.FloatField("Level Time ", _Dax.LevelTime);
        if (newLevelTime <= 10f) newLevelTime = 10f;
        if(newLevelTime != _Dax.LevelTime)
        {
            _Dax.LevelTime = newLevelTime;
            UpdateFloatProperty(DaxSO, "LevelTime", _Dax.LevelTime);           
        }

        // Victory Condition
        EditorGUILayout.Separator();
        Dax.eVictoryConditions newVictoryCondition = (Dax.eVictoryConditions)EditorGUILayout.EnumPopup("Victory Conditions", _Dax.CurWheel.VictoryCondition);
        if (newVictoryCondition != _Dax.CurWheel.VictoryCondition)
        {
            _Dax.CurWheel.VictoryCondition = newVictoryCondition;
            UpdateEnumProperty(new SerializedObject(_Dax.CurWheel), "VictoryCondition", (int)_Dax.CurWheel.VictoryCondition);            
        }

        // A list of all the facets that you need to collect on the board
        EditorGUILayout.Separator();
        for (int i = 0; i < _Dax.CurWheel.NumFacetsOnBoard.Count-1; i++)
        {
            Facet.eFacetColors curColor = (Facet.eFacetColors)i;
            EditorGUILayout.LabelField(curColor.ToString() + " To Collect: " + _Dax.CurWheel.NumFacetsOnBoard[(int)curColor]);
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
            _Dax.CurWheel.InitWheelFacets();
            _Dax.CreateSaveData(_Dax);            
        }
        // This saves the puzzle in a binary format
        EditorGUILayout.Separator();
        if(GUILayout.Button("Save Puzzle"))
        {
            _MCP.SavePuzzle();                
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
                _MCP.ResetRing(selRing);
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
            selBumper.gameObject.GetComponent<MeshRenderer>().material = _MCP.GetBumperMaterial(newBumperType, selBumper.BumperColor);                    
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
                selBumper.gameObject.GetComponent<MeshRenderer>().material = _MCP.GetBumperMaterial(selBumper.BumperType, newBumperColor);
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
        string s = selChannelPiece.IsActive() == true ? "Turn Off" : "Turn On";
        if (GUILayout.Button(s, GUILayout.Width(150f)))
        {   // User clicked the button so toggle the ChannelPiece
            SelectedGameObject.GetComponent<ChannelPiece>().Toggle();
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
        ChannelNode selChannelNode = SelectedGameObject.GetComponent<ChannelNode>();
        
        // For creating BoardObjects make sure it's a middle node since those are the
        // only ones that can have anything spawned on it.
        if (selChannelNode.IsMidNode() == true)
        {
            // Nodes can only have one BoardObject on them
            if(selChannelNode.SpawnedBoardObject == null)
            {   // no board object so handle creating one
                HandleCreateBoardObject(selChannelNode);                  
            }
            else
            {   // There's a BoardObject on this node so handle that
                HandleBoardObject(selChannelNode);                    
            }
        }    
        else if( selChannelNode.name.Contains("Ring_00_Start")) 
        {
            // If it's not a middle node make sure it's a start node
            // on the center ring because those are the ones used to
            // select the Player's starting channel
            Player player = FindObjectOfType<Player>();
            // Check if the selected node is already the start node
            if(selChannelNode != player.GetStartChannelNode())
            {
                if (GUILayout.Button("Make Starting Channel", GUILayout.Width(200f)))
                {   // Change the player's starting channel based on the selected node
                    int startChannelIndex = Int32.Parse(selChannelNode.name.Substring(19, 2)) - 1;                                               
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
        ChannelNode selChannelNode = parent.SpawningNode;
        StartNewSelection(selChannelNode.name, "Channel Node");
        HandleBoardObject(selChannelNode);
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
        HandleSceneView(); // make sure the RifRaf Debug object is not visible        
                
        #if false
        MCP _MCP;   // The Master Control Program object from the main game
        DaxPuzzleSetup _DaxPuzzleSetup; // The MonoBehavior that this editor script extends
        SerializedObject DaxSetupSO;    // The SerializedObject for the DaxPuzzleSetup object
        Dax _Dax;   // The Dax object from the main game
        SerializedObject DaxSO; // The SerializedObject for the _Dax object
        #endif                
    
        // Gather the main GameObjects and SerializedObjects used for everything
        _MCP = GameObject.FindObjectOfType<MCP>();
        _DaxPuzzleSetup = (DaxPuzzleSetup)target;        
        DaxSetupSO = new SerializedObject(_DaxPuzzleSetup);
        DaxSetupSO.Update();
        _Dax = _MCP._Dax.GetComponent<Dax>();
        DaxSO = new SerializedObject(_Dax);
        DaxSO.Update();        

        // This is for the overall puzzle data, not individual objects on the puzzle
        HandlePuzzleInfo();
           
        // Check to see if the engine is telling us we have a GameObject selected
        SelectedGameObject = Selection.activeGameObject;              
        if (LastSelectedGameObject != SelectedGameObject) LastSelectedGameObject = SelectedGameObject;        

        if (SelectedGameObject != null)
        {
            // The engine is telling us we have a GameObject selected so handle that.
            HandleActiveGameObject();                                                                                                                                        
        }

        // Save data                        
        DaxSetupSO.ApplyModifiedProperties();
        DaxSO.ApplyModifiedProperties();            
        Undo.RecordObject(_DaxPuzzleSetup, "OnInspectorGUI");
        EditorUtility.SetDirty(_DaxPuzzleSetup);
    }   
    
    void HandleCreateBoardObject(ChannelNode selChannelNode)
    {
        float buttonWidth = 200f;
        if (GUILayout.Button("Create Facet", GUILayout.Width(buttonWidth)))
        {                        
            Facet facet = _MCP.CreateBoardObject<Facet>(selChannelNode, _Dax, 
               (int)BoardObject.eBoardObjectType.FACET, 0);
            _MCP.ChangeFacetColor(facet, Facet.eFacetColors.RED);                 
        }
        //******************************************************************************************                     
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Hazard", GUILayout.Width(buttonWidth)))
        {                          
            _MCP.CreateBoardObject<Hazard>(selChannelNode, _Dax, 
               (int)BoardObject.eBoardObjectType.HAZARD, (int)Hazard.eHazardType.ENEMY);                                  
        }             
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Facet Collect", GUILayout.Width(buttonWidth)))
        {            
            _MCP.CreateBoardObject<FacetCollect>(selChannelNode, _Dax, 
                (int)BoardObject.eBoardObjectType.FACET_COLLECT, (int)FacetCollect.eFacetCollectTypes.RING);                            
        } 
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Shield", GUILayout.Width(buttonWidth)))
        {            
            _MCP.CreateBoardObject<Shield>(selChannelNode, _Dax, 
                (int)BoardObject.eBoardObjectType.SHIELD, (int)Shield.eShieldTypes.HIT);                  
        }
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Speed Mod", GUILayout.Width(buttonWidth)))
        {            
            _MCP.CreateBoardObject<SpeedMod>(selChannelNode, _Dax, 
                (int)BoardObject.eBoardObjectType.SPEED_MOD, (int)SpeedMod.eSpeedModType.PLAYER_SPEED);                        
        }
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Game Mod", GUILayout.Width(buttonWidth)))
        {            
            _MCP.CreateBoardObject<GameMod>(selChannelNode, _Dax, 
                (int)BoardObject.eBoardObjectType.GAME_MOD, (int)GameMod.eGameModType.EXTRA_POINTS);                   
        }                                   
    }

    // moupdate - get the MCP/Dax confusion sorted out
    void HandleBoardObject(ChannelNode selChannelNode)
    {
        BoardObject bo = selChannelNode.SpawnedBoardObject;
        SerializedObject selBoardObjectSO = new SerializedObject(bo);
        selBoardObjectSO.Update();
        string boName = BoardObject.BOARD_OBJECT_EDITOR_NAMES[(int)bo.BoardObjectType];//bo.GetBoardObjectName();

        // ****** Ping/Delete board object
        if (GUILayout.Button("Ping " + boName, GUILayout.Width(150f)))
        {
            EditorGUIUtility.PingObject(bo);
            Selection.activeGameObject = bo.gameObject;
        }
        EditorGUILayout.Separator();
        if (GUILayout.Button("Delete " + boName, GUILayout.Width(150f)))
        {
           // if(bo.LastPositionObject != null) DestroyImmediate(bo.LastPositionObject);
            DestroyImmediate(bo.gameObject);
            selChannelNode.SpawnedBoardObject = null;
        }

        EditorGUILayout.Separator();
        // BEGIN BOARD OBJECTS
        switch (bo.BoardObjectType)
        {
            case BoardObject.eBoardObjectType.FACET:
                Facet facet = (Facet)bo;
                Facet.eFacetColors newFacetColor = (Facet.eFacetColors)EditorGUILayout.EnumPopup("Facet Color", facet._Color);
                if (newFacetColor != facet._Color)
                {
                    facet._Color = newFacetColor;
                    _MCP.ChangeFacetColor(facet, facet._Color);
                    UpdateEnumProperty(selBoardObjectSO, "_Color", (int)facet._Color);                    
                }
                break;
            case BoardObject.eBoardObjectType.HAZARD:
                Hazard hazard = (Hazard)bo;
                Hazard.eHazardType newHazardType = (Hazard.eHazardType)EditorGUILayout.EnumPopup("Type: ", hazard.HazardType);
                if (newHazardType != hazard.HazardType)
                {
                    DestroyImmediate(selChannelNode.SpawnedBoardObject.gameObject);                                        
                    hazard = _MCP.CreateBoardObject<Hazard>(selChannelNode, _Dax, 
                        (int)BoardObject.eBoardObjectType.HAZARD, (int)newHazardType); 

                }
                if (hazard.HazardType == Hazard.eHazardType.ENEMY /*|| hazard.HazardType == Hazard.eHazardType.EMP (|| hazard.HazardType == Hazard.eHazardType.BOMB*/)
                {   // Speed and Starting Direction
                    EditorGUILayout.Separator();
                    EditorGUILayout.Separator();
                    BoardObject.eStartDir newStartDir = (BoardObject.eStartDir)EditorGUILayout.EnumPopup("Start Direction:", bo.StartDir);
                    if (newStartDir != bo.StartDir) // monewsave
                    {
                        bo.StartDir = newStartDir;
                        UpdateEnumProperty(selBoardObjectSO, "StartDir", (int)bo.StartDir);                        
                        hazard.transform.LookAt(bo.StartDir == BoardObject.eStartDir.OUTWARD ? hazard.CurChannel.EndNode.transform : hazard.CurChannel.StartNode.transform);
                    }                      

                    EditorGUILayout.Separator();
                    float newSpeed = EditorGUILayout.Slider("Speed", bo.Speed, 0f, Dax.MAX_SPEED);
                    if (newSpeed != bo.Speed)
                    {
                        bo.Speed = newSpeed;
                        UpdateFloatProperty(selBoardObjectSO, "Speed",  bo.Speed);                        
                    }                                                         
                }
                if (hazard.HazardType == Hazard.eHazardType.GLUE /*|| hazard.HazardType == Hazard.eHazardType.TIMED_MINE*/)
                {   // These have an Effect Timer
                    EditorGUILayout.Separator();

                    float newEffectTime = EditorGUILayout.Slider("Effect Time", hazard.EffectTime, .1f, Hazard.MAX_EFFECT_TIME);
                    if (newEffectTime != hazard.EffectTime)
                    {
                        hazard.EffectTime = newEffectTime;
                        UpdateFloatProperty(selBoardObjectSO, "EffectTime", hazard.EffectTime);                        
                    }
                }                
                break;
        }
        if(bo.BoardObjectType == BoardObject.eBoardObjectType.GAME_MOD)
        {
            EditorGUILayout.Separator();
            GameMod gameMod = (GameMod)bo;
            GameMod.eGameModType newGameModType = (GameMod.eGameModType)EditorGUILayout.EnumPopup("Game Mod Type: ", gameMod.GameModType);
            if (newGameModType != gameMod.GameModType)
            {
                DestroyImmediate(selChannelNode.SpawnedBoardObject.gameObject);                          
                gameMod = _MCP.CreateBoardObject<GameMod>(selChannelNode, _Dax, 
                    (int)BoardObject.eBoardObjectType.GAME_MOD, (int)newGameModType);
            }

            int newGameModVal = EditorGUILayout.IntField("Game Mod Val: ", gameMod.GameModVal);
            if (newGameModVal <= 0f) newGameModVal = 1;
            if (newGameModVal != gameMod.GameModVal)
            {
                gameMod.GameModVal = newGameModVal;
                UpdateIntProperty(selBoardObjectSO, "GameModVal", gameMod.GameModVal);                
            }
            if(gameMod.GameModType == GameMod.eGameModType.POINTS_MULTIPLIER)
            {
                float newTimer = EditorGUILayout.FloatField("Timer: ", gameMod.GameModTime);
                if (newTimer < 1.0f) newTimer = 1.0f;
                if (newTimer != gameMod.GameModTime)
                {
                    gameMod.GameModTime = newTimer;
                    UpdateFloatProperty(selBoardObjectSO, "GameModTime", gameMod.GameModTime);                   
                }
            }
        }             
        else if (bo.BoardObjectType == BoardObject.eBoardObjectType.FACET_COLLECT)
        {
            EditorGUILayout.Separator();
            FacetCollect facetCollect = (FacetCollect)bo;
            
            FacetCollect.eFacetCollectTypes newFacetCollectType = (FacetCollect.eFacetCollectTypes)EditorGUILayout.EnumPopup("Type: ", facetCollect.FacetCollectType);
            if (newFacetCollectType != facetCollect.FacetCollectType)
            {
                DestroyImmediate(selChannelNode.SpawnedBoardObject.gameObject);                                
                facetCollect = _MCP.CreateBoardObject<FacetCollect>(selChannelNode, _Dax, 
                    (int)BoardObject.eBoardObjectType.FACET_COLLECT, (int)newFacetCollectType);
            }            
        }                
        
        else if (bo.BoardObjectType == BoardObject.eBoardObjectType.SHIELD)
        {           
            EditorGUILayout.Separator();
            Shield shield = (Shield)bo;
            
            Shield.eShieldTypes newShieldType = (Shield.eShieldTypes)EditorGUILayout.EnumPopup("Type: ", shield.ShieldType);
            if(newShieldType != shield.ShieldType)
            {                
                DestroyImmediate(selChannelNode.SpawnedBoardObject.gameObject);                
                shield = _MCP.CreateBoardObject<Shield>(selChannelNode, _Dax, 
                    (int)BoardObject.eBoardObjectType.SHIELD, (int)newShieldType);     
            }           
        }         
        else if(bo.BoardObjectType == BoardObject.eBoardObjectType.SPEED_MOD)
        {
            EditorGUILayout.Separator();
            SpeedMod speedMod = (SpeedMod)bo;

            SpeedMod.eSpeedModType newSpeedModType = (SpeedMod.eSpeedModType)EditorGUILayout.EnumPopup("Type: ", speedMod.SpeedModType);
            if (newSpeedModType != speedMod.SpeedModType)
            {
                DestroyImmediate(selChannelNode.SpawnedBoardObject.gameObject);                                
                speedMod = _MCP.CreateBoardObject<SpeedMod>(selChannelNode, _Dax, 
                    (int)BoardObject.eBoardObjectType.SPEED_MOD, (int)newSpeedModType);

            }        
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
            {                                    
                speedMod.SpeedModVal = newSpeedModVal;
                UpdateFloatProperty(selBoardObjectSO, "SpeedModVal", speedMod.SpeedModVal);                    
            }

               
        }
    }              
}
