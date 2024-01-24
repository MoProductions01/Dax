using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

[CustomEditor(typeof(DaxSetup))]
public class DaxEditor : Editor
{
    public GameObject LastSelectedObject = null;    // used for checking for changes in the selected object
    int SelectedRing = 0; // index of our currently selected ring   
    bool IsDialogueShowing = false; // this is to get around a weird Unity bug where dialogues show up twice.  FUN
    bool StartChannelError = false; // if we're waiting for a system popup on a start channel error    
    bool RingsChangePopupActive = false; // to confirm if the user does in fact want to reduce the number of rings since that also destroys objects
    int NumRingsUserWants = 4; // how many rings the user wants to change to

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

    /* Helper to get a new section of the interface going */
    void StartNewSelection(string selectionName, string selectionType)
    {
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField("Selected Object: " + selectionName);
        EditorGUILayout.LabelField("Type: " + selectionType);
        EditorGUILayout.Separator();
    }

    /* This is for keyboard shortcuts */
    private void OnSceneGUI()
    {
        if (Selection.activeGameObject == null) return;
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
        RifRafDebug rrd = FindObjectOfType<RifRafDebug>();
        if (rrd != null)
        {
            SceneVisibilityManager sv = SceneVisibilityManager.instance;
            if (sv.IsHidden(rrd.gameObject) == false) sv.Hide(rrd.gameObject, true);
        }
    }        

    /* Main control code for the engine */
    public override void OnInspectorGUI()
    {
        HandleSceneView(); // make sure the RifRaf Debug object is not visible        
                
        MCP mcp = GameObject.FindObjectOfType<MCP>();
        DaxSetup daxSetup = (DaxSetup)target;        
        SerializedObject daxSetupSO = new SerializedObject(daxSetup);
        daxSetupSO.Update();
        Dax dax = mcp._Dax.GetComponent<Dax>();
        SerializedObject daxSO = new SerializedObject(dax);

        if (dax == null) { Debug.LogError("ERROR: no Dax component on this game object: " + this.name); return; }

#region PUZZLE_INFO
        // ************ Puzzle Name
        EditorGUILayout.Separator();
        string newName = EditorGUILayout.TextField(dax.PuzzleName);
        if(newName != dax.PuzzleName)
        {
            dax.PuzzleName = newName;
            UpdateStringProperty(daxSO, "PuzzleName", dax.PuzzleName);            
        }        

        // **************** Starting Channel
        EditorGUILayout.Separator();
        EditorGUILayout.LabelField("Starting Channel " + (dax.StartChannelIndex+1));

        // ************ Level Time
        EditorGUILayout.Separator();
        float newLevelTime = EditorGUILayout.FloatField("Level Time ", dax.LevelTime);
        if (newLevelTime <= 10f) newLevelTime = 10f;
        if(newLevelTime != dax.LevelTime)
        {
            dax.LevelTime = newLevelTime;
            UpdateFloatProperty(daxSO, "LevelTime", dax.LevelTime);           
        }
        // ************ Victory Condition
        EditorGUILayout.Separator();
        Dax.eVictoryConditions newVictoryCondition = (Dax.eVictoryConditions)EditorGUILayout.EnumPopup("Victory Conditions", dax.CurWheel.VictoryCondition);
        if (newVictoryCondition != dax.CurWheel.VictoryCondition)
        {
            dax.CurWheel.VictoryCondition = newVictoryCondition;
            UpdateEnumProperty(new SerializedObject(dax.CurWheel), "VictoryCondition", (int)dax.CurWheel.VictoryCondition);            
        }
        // ************ Pickup Facets on board/to collect
        EditorGUILayout.Separator();
        if(dax.CurWheel.VictoryCondition == Dax.eVictoryConditions.COLLECTION)
        {            
            EditorGUILayout.LabelField("Pickup Facets To Collect " + dax.CurWheel.NumFacetsOnBoard[(int)Facet.eFacetColors.WHITE]);
        }
        // ************ Color Facets on board/to match
        if (dax.CurWheel.VictoryCondition == Dax.eVictoryConditions.COLOR_MATCH)
        {           
            for (int i = 0; i < dax.CurWheel.NumFacetsOnBoard.Count-1; i++)
            {
                Facet.eFacetColors curColor = (Facet.eFacetColors)i;
                EditorGUILayout.LabelField(curColor.ToString() + " To Collect: " + dax.CurWheel.NumFacetsOnBoard[(int)curColor]);
            }           
        }       

        // ************ Rings        
        EditorGUILayout.Separator();
        if (RingsChangePopupActive == true)
        {   // Waiting on to decide to reduce the number of rings.  This has a lot of board matinence involved so warn the user about this if necessary.      
            bool newNumRingsResponse = EditorUtility.DisplayDialog("Are you sure you want to decrease the number of rings?", "This will reset all rings beyond the new number.", "yes", "no");
            if (newNumRingsResponse == true)
            {
                RingsChangePopupActive = false;               
                if (NumRingsUserWants < dax.CurWheel.NumActiveRings)
                {   // reducing numer of rings, so make sure to 
                    for (int i = dax.CurWheel.NumActiveRings; i > NumRingsUserWants; i--) mcp.ResetRing(dax.CurWheel.Rings[i]);                    
                }
                Selection.activeGameObject = null;
                SelectedRing = 0;
                dax.CurWheel.TurnOnRings(NumRingsUserWants);                
            }
            else RingsChangePopupActive = false;
            RemoveWarpNullEntries(dax.CurWheel);
        }
        else
        {   // ************ Number of rings
            int newNumRings = EditorGUILayout.IntPopup("Number of Rings", dax.CurWheel.NumActiveRings, DaxSetup.NUM_RINGS_NAMES, DaxSetup.NUM_RINGS_TOTALS);
            if (newNumRings != dax.CurWheel.NumActiveRings)
            {
                if (newNumRings > dax.CurWheel.NumActiveRings)
                {   // if we're increasing the number of rings just go ahead and do it because the rings have been cleared already                    
                    dax.CurWheel.TurnOnRings(newNumRings);
                }
                else
                {   // we're decreasing the number of rings, which will destroy all the stuff on them, so give the user a popup
                    RingsChangePopupActive = true;
                    NumRingsUserWants = newNumRings;
                }
            }
        }
        #endregion
        #region OBJECT_SELECTION
        // ************ Ring selection is handled via the tool Inspector window
        EditorGUILayout.Separator();
        if (Selection.activeGameObject == null || (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Ring>() == null))
        {   // we have something other than a ring selected now so make sure that there's no ring selected
            SelectedRing = 0;
        }
        List<string> ringNames = new List<string> { "None", "Center", "Ring 01" };
        for (int i = 2; i <= dax.CurWheel.NumActiveRings; i++) ringNames.Add("Ring " + i.ToString("D2"));        
        string[] ringNamesArray = ringNames.ToArray();
        int newRing = EditorGUILayout.Popup("Select Ring", SelectedRing, ringNamesArray);
        if (newRing != SelectedRing)
        {
            if (newRing == 0)
            {
                Selection.activeGameObject = null;
            }
            else
            {
                Selection.activeGameObject = dax.CurWheel.Rings[newRing - 1].gameObject;
            }
            SelectedRing = newRing;
        }

        // ********** Objects selected by mouse
        GameObject selected = Selection.activeGameObject;        
        if (LastSelectedObject != selected) LastSelectedObject = selected;        
        if (selected != null)
        {
            // ******* Ring (selected via tool above)      
            if (selected.GetComponent<Ring>() != null)
            {
                StartNewSelection(selected.name, "Ring");
                Ring selRing = selected.GetComponent<Ring>();
                //    public bool IsCenterRing()
                if(selRing != selRing.IsCenterRing())
                {   // center ring doesn't spin
                    float newSpeed = EditorGUILayout.FloatField("Rotate Speed: ", selRing.RotateSpeed);
                    if (newSpeed != selRing.RotateSpeed)
                    {
                        selRing.RotateSpeed = newSpeed;
                        SerializedObject selRingSO = new SerializedObject(selRing);
                        selRingSO.Update();
                        UpdateFloatProperty(selRingSO, "RotateSpeed", selRing.RotateSpeed);                    
                    }
                }
                
                EditorGUILayout.Separator();
                if (GUILayout.Button("Reset Ring", GUILayout.Width(300)))
                {
                    mcp.ResetRing(selRing);
                }
                EditorGUILayout.EndVertical();
            }
            // ******* Player            
            if (selected.GetComponent<Player>() != null)
            {
                StartNewSelection(selected.name, "Player");
                Player player = selected.GetComponent<Player>();
                SerializedObject playerSO = new SerializedObject(player);
                playerSO.Update();
                float newSpeed = EditorGUILayout.Slider("Speed", player.Speed, 0f, Dax.MAX_SPEED);
                if (newSpeed != player.Speed)
                {
                    player.Speed = newSpeed;
                    UpdateFloatProperty(playerSO, "Speed", player.Speed);                    
                }
                EditorGUILayout.Separator();
                BoardObject.eMoveDir newMoveDir = (BoardObject.eMoveDir)EditorGUILayout.EnumPopup("Start Direction:", player.MoveDir);
                if (newMoveDir != player.MoveDir)
                {
                    player.MoveDir = newMoveDir;
                    UpdateEnumProperty(playerSO, "MoveDir", (int)player.MoveDir);                    
                }
                EditorGUILayout.EndVertical();
            }
            // ************ Bumper
            if (selected.GetComponent<Bumper>() != null)
            {
                StartNewSelection(selected.name, "Bumper");
                Bumper selBumper = selected.GetComponent<Bumper>();
                SerializedObject selBumperSO = new SerializedObject(selBumper);
                selBumperSO.Update();

                Bumper.eBumperType newBumperType = (Bumper.eBumperType)EditorGUILayout.EnumPopup("Bumper Type", selBumper.BumperType);                
                if (newBumperType != selBumper.BumperType)
                {
                    selBumper.BumperType = newBumperType;
                    UpdateEnumProperty(selBumperSO, "BumperType", (int)selBumper.BumperType);
                    if (newBumperType == Bumper.eBumperType.COLOR_MATCH) selBumper.BumperColor = Facet.eFacetColors.RED;
                    else selBumper.BumperColor = Facet.eFacetColors.WHITE;
                    selBumper.gameObject.GetComponent<MeshRenderer>().material = GetBumperMaterial(newBumperType, selBumper.BumperColor);                    
                    UpdateEnumProperty(selBumperSO, "BumperColor", (int)selBumper.BumperColor);
                }
                EditorGUILayout.Separator();
                if (selBumper.BumperType == Bumper.eBumperType.COLOR_MATCH)
                {
                    Facet.eFacetColors newBumperColor = (Facet.eFacetColors)EditorGUILayout.EnumPopup("Bumper Color", selBumper.BumperColor);
                    if (newBumperColor != selBumper.BumperColor)
                    {
                        if (newBumperColor == Facet.eFacetColors.WHITE) { Debug.LogError("Bumper Color can't be White."); return; }
                        selBumper.BumperColor = newBumperColor;
                        selBumper.gameObject.GetComponent<MeshRenderer>().material = GetBumperMaterial(selBumper.BumperType, newBumperColor);
                        UpdateEnumProperty(selBumperSO, "BumperColor", (int)selBumper.BumperColor);
                    }
                }
                EditorGUILayout.EndVertical();
            }
            // ************ Channel Node
            if (selected.GetComponent<ChannelNode>() != null)
            {
                StartNewSelection(selected.name, "Channel Node");               
                ChannelNode selChannelNode = selected.GetComponent<ChannelNode>();
               // if (selChannelNode.IsMidNode() == false) return; // only mid nodes can spawn objets                                                                                                                                             
                if (selChannelNode.IsMidNode() == true) // only mid nodes can spawn objets   
                {
                    // only one type of object can be spawned on each node                    
                    if(selChannelNode.SpawnedBoardObject == null)
                    {   // no board object so handle creating one
                        HandleCreateBoardObject(selChannelNode, mcp, dax);                  
                    }
                    else
                    {   // there's a board object on this node so handle that
                        HandleBoardObject(selChannelNode, mcp, dax);                    
                    }
                }    
                else if( selChannelNode.name.Contains("Ring_00_Start"))
                {
                    //Debug.Log("Selected a potential start node");                    
                    if (GUILayout.Button("Make Starting Channel", GUILayout.Width(200f)))
                    {                                                
                        int channelIndex = Int32.Parse(selChannelNode.name.Substring(19, 2)) - 1;
                        Debug.Log($"node name: {selChannelNode.name}, channel Index: {channelIndex}");

                        dax.StartChannelIndex = channelIndex;
                        FindObjectOfType<Player>().SetStartChannel(channelIndex);
                        UpdateIntProperty(daxSO, "StartChannelIndex", dax.StartChannelIndex);                                    
                    }
                }                                                                                                                                      
                
                EditorGUILayout.Separator();
                EditorGUILayout.EndVertical();
            }
            // CHANNEL PIECE
            if (selected.GetComponent<ChannelPiece>() != null)
            {
                StartNewSelection(selected.name, "Channel Piece");                
                ChannelPiece selChannelPiece = selected.GetComponent<ChannelPiece>();               
                string s = selChannelPiece.IsActive() == true ? "Turn Off" : "Turn On";
                if (GUILayout.Button(s, GUILayout.Width(150f)))
                {
                    selected.GetComponent<ChannelPiece>().Toggle();
                }
                EditorGUILayout.EndVertical();
            }
            
            // wheel.StartNodes = wheel.transform.transform.GetComponentsInChildren<ChannelNode>().ToList();
            // wheel.StartNodes.RemoveAll(x => x.name.Contains("Ring_00_Start_Node") == false);

            #endregion
            #region INIT_SAVE
            // **** Puzzle initialization/saving
            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            // first check for any errors 
            if (StartChannelError == true && IsDialogueShowing == false)
            {   // you tried to init but the player's starting channel is not valid                      
                IsDialogueShowing = true;           
                EditorUtility.DisplayDialog("Player Start Channel Error", "Chosen start channel is not available", "OK");
                IsDialogueShowing = false;
                StartChannelError = false;
            }       
            else
            {   // no init errors so give the user an option to retry
                if (GUILayout.Button("Init Puzzle"))
                {
                    // check to see if the chosen start channel is valid
                   // StartChannelError = !dax._Player.CurChannel.IsValidStartChannel();                               
                    //if (StartChannelError == true) return; // bail if the player's starting channel isn't valid                
                    dax.CurWheel.InitWheelFacets();
                    dax.CreateSaveData(dax);
                    Debug.Log("*******Puzzle Initted*********"); // moupdate - restrict to center nodes
                }
                EditorGUILayout.Separator();
                if(GUILayout.Button("Save Puzzle"))
                {
                    mcp.SavePuzzle();                
                }                                                                                         
            }
            #endregion
        }

        // Save data        
        daxSetupSO.ApplyModifiedProperties();
        daxSO.ApplyModifiedProperties();
        Undo.RecordObject(daxSetup, "OnInspectorGUI");
        EditorUtility.SetDirty(daxSetup);
    }

    void HandleCreateBoardObject(ChannelNode selChannelNode, MCP mcp, Dax dax)
    {
        float buttonWidth = 200f;
        if (GUILayout.Button("Create Facet", GUILayout.Width(buttonWidth)))
        {
            mcp.CreateFacet(selChannelNode, dax, Facet.eFacetColors.WHITE);
        }
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Hazard", GUILayout.Width(buttonWidth)))
        {
            mcp.CreateHazard(selChannelNode, dax, Hazard.eHazardType.ENEMY);
        }
        /*EditorGUILayout.Separator();
        if (GUILayout.Button("Create Game Mod", GUILayout.Width(buttonWidth)))
        {
            mcp.CreateGameMod(selChannelNode, dax, GameMod.eGameModType.EXTRA_POINTS);
        }*/
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Shield", GUILayout.Width(buttonWidth)))
        {
            Shield shield = mcp.CreateShield(selChannelNode.transform, Shield.eShieldTypes.HIT);
            shield.InitForChannelNode(selChannelNode, dax);
        }
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Magnet", GUILayout.Width(buttonWidth)))
        {
            mcp.CreateMagnet(selChannelNode, dax, Magnet.eMagnetTypes.REGULAR);
        }
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Interactable", GUILayout.Width(buttonWidth)))
        {
            mcp.CreateInteractable(selChannelNode, dax, Interactable.eInteractableType.TOGGLE);
        }
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Speed Mod", GUILayout.Width(buttonWidth)))
        {
            mcp.CreateSpeedMod(selChannelNode, dax, SpeedMod.eSpeedModType.SPEED_UP);
        }
    }

    // moupdate - get the MCP/Dax confusion sorted out
    void HandleBoardObject(ChannelNode selChannelNode, MCP mcp, Dax dax)
    {
        BoardObject bo = selChannelNode.SpawnedBoardObject;
        SerializedObject selBoardObjectSO = new SerializedObject(bo);
        selBoardObjectSO.Update();
        string boName = bo.GetBoardObjectName();

        // ****** Ping/Delete board object
        if (GUILayout.Button("Ping " + boName, GUILayout.Width(150f)))
        {
            EditorGUIUtility.PingObject(bo);
            Selection.activeGameObject = bo.gameObject;
        }
        EditorGUILayout.Separator();
        if (GUILayout.Button("Delete " + boName, GUILayout.Width(150f)))
        {
            if(bo.LastPositionObject != null) DestroyImmediate(bo.LastPositionObject);
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
                    mcp.ChangeFacetColor(facet, facet._Color);
                    UpdateEnumProperty(selBoardObjectSO, "_Color", (int)facet._Color);                    
                }
                break;
            case BoardObject.eBoardObjectType.HAZARD:
                Hazard hazard = (Hazard)bo;
                Hazard.eHazardType newHazardType = (Hazard.eHazardType)EditorGUILayout.EnumPopup("Type: ", hazard.HazardType);
                if (newHazardType != hazard.HazardType)
                {
                    DestroyImmediate(selChannelNode.SpawnedBoardObject.gameObject);                    
                    hazard = mcp.CreateHazard(selChannelNode, dax, newHazardType);
                }
                if (hazard.HazardType == Hazard.eHazardType.ENEMY || hazard.HazardType == Hazard.eHazardType.EMP /*(|| hazard.HazardType == Hazard.eHazardType.BOMB*/)
                {   // Speed and Movement Direction
                    EditorGUILayout.Separator();
                    float newSpeed = EditorGUILayout.Slider("Speed", bo.Speed, 0f, Dax.MAX_SPEED);
                    if (newSpeed != bo.Speed)
                    {
                        bo.Speed = newSpeed;
                        UpdateFloatProperty(selBoardObjectSO, "Speed",  bo.Speed);                        
                    }
                    
                    EditorGUILayout.Separator();
                    BoardObject.eMoveDir newMoveDir = (BoardObject.eMoveDir)EditorGUILayout.EnumPopup("Start Direction:", bo.MoveDir);
                    if (newMoveDir != bo.MoveDir)
                    {
                        bo.MoveDir = newMoveDir;
                        UpdateEnumProperty(selBoardObjectSO, "MoveDir", (int)bo.MoveDir);                        
                    }                    
                }
                if (hazard.HazardType == Hazard.eHazardType.EMP /*|| hazard.HazardType == Hazard.eHazardType.TIMED_MINE*/)
                {   // These have an Effect Timer
                    EditorGUILayout.Separator();

                    float newEffectTime = EditorGUILayout.Slider("Effect Time", hazard.EffectTime, .1f, Hazard.MAX_EFFECT_TIME);
                    if (newEffectTime != hazard.EffectTime)
                    {
                        hazard.EffectTime = newEffectTime;
                        UpdateFloatProperty(selBoardObjectSO, "EffectTime", hazard.EffectTime);                        
                    }
                }
                /*if (hazard.HazardType == Hazard.eHazardType.PROXIMITY_MINE)
                {   // proximity mine has a radius
                    EditorGUILayout.Separator();

                    float newEffectRadius = EditorGUILayout.Slider("Effect Radius", hazard.EffectRadius, .05f, .2f);
                    if (newEffectRadius != hazard.EffectRadius)
                    {
                        hazard.EffectRadius = newEffectRadius;
                        hazard.GetComponent<SphereCollider>().radius = hazard.EffectRadius;
                        UpdateFloatProperty(selBoardObjectSO, "EffectRadius", hazard.EffectRadius);                                    
                    }
                }*/
                break;
        }
       /* if(bo.BoardObjectType == BoardObject.eBoardObjectType.GAME_MOD)
        {
            EditorGUILayout.Separator();
            GameMod gameMod = (GameMod)bo;
            GameMod.eGameModType newGameModType = (GameMod.eGameModType)EditorGUILayout.EnumPopup("Game Mod Type: ", gameMod.GameModType);
            if (newGameModType != gameMod.GameModType)
            {
                DestroyImmediate(selChannelNode.SpawnedBoardObject.gameObject);          
                gameMod = mcp.CreateGameMod(selChannelNode, dax, newGameModType);
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
        else */
        if (bo.BoardObjectType == BoardObject.eBoardObjectType.MAGNET)
        {
            EditorGUILayout.Separator();
            Magnet magnet = (Magnet)bo;
            
            Magnet.eMagnetTypes newMagnetType = (Magnet.eMagnetTypes)EditorGUILayout.EnumPopup("Type: ", magnet.MagnetType);
            if (newMagnetType != magnet.MagnetType)
            {
                DestroyImmediate(selChannelNode.SpawnedBoardObject.gameObject);                
                magnet = mcp.CreateMagnet(selChannelNode, dax, newMagnetType);
            }            
        }                
        else if(bo.BoardObjectType == BoardObject.eBoardObjectType.INTERACTABLE)
        {
            EditorGUILayout.Separator();
            Interactable interactable = (Interactable)bo;

            Interactable.eInteractableType newInteractableType = (Interactable.eInteractableType)EditorGUILayout.EnumPopup("Type: ", interactable.InteractableType);
            if(newInteractableType != interactable.InteractableType)
            {
                DestroyImmediate(selChannelNode.SpawnedBoardObject.gameObject); // moupdate - make this consistent, like destroy interactlbe.gameobject instead                
                interactable = mcp.CreateInteractable(selChannelNode, dax, newInteractableType);
            }
            if(interactable.InteractableType == Interactable.eInteractableType.SWITCH)
            {                
                selBoardObjectSO.Update();
                SerializedProperty switchOffSP = selBoardObjectSO.FindProperty("PiecesToTurnOff");
                EditorGUILayout.PropertyField(switchOffSP);
                SerializedProperty switchOnSP = selBoardObjectSO.FindProperty("PiecesToTurnOn");
                EditorGUILayout.PropertyField(switchOnSP);

                if (selBoardObjectSO.ApplyModifiedProperties() == true)
                {   // make sure there are no duplicates                
                    interactable.PiecesToTurnOff = TrimDuplicates(interactable.PiecesToTurnOff);
                    interactable.PiecesToTurnOn = TrimDuplicates(interactable.PiecesToTurnOn);
                    selBoardObjectSO.ApplyModifiedProperties();
                }
            } 
            if (interactable.InteractableType == Interactable.eInteractableType.WARP_GATE || interactable.InteractableType == Interactable.eInteractableType.WORMHOLE)
            {
                EditorGUILayout.Separator();                

                if(interactable.InteractableType == Interactable.eInteractableType.WORMHOLE)
                {
                    float newSpeed = EditorGUILayout.Slider("Speed", interactable.Speed, 0f, Dax.MAX_SPEED); 
                    if (newSpeed != interactable.Speed)
                    {
                        interactable.Speed = newSpeed;
                        UpdateFloatProperty(selBoardObjectSO, "Speed", interactable.Speed);                       
                    }
                    EditorGUILayout.Separator();
                }

                List<Interactable> preChangeDestGates = interactable.DestGates.ToList();
                selBoardObjectSO = new SerializedObject(interactable);
                SerializedProperty selDiodeDestSP = selBoardObjectSO.FindProperty("DestGates");
                EditorGUILayout.PropertyField(selDiodeDestSP);

                if (selBoardObjectSO.ApplyModifiedProperties() == true)
                {
                    // check for duplicates
                    interactable.DestGates = TrimDuplicates(interactable.DestGates);

                    if (preChangeDestGates.Count < interactable.DestGates.Count)
                    {
                        // Debug.Log("Number of warp destinations has increased but the duplicate code above should take care of it.");
                    }
                    else if (preChangeDestGates.Count > interactable.DestGates.Count)
                    {
                        // Debug.LogWarning("Number of warp destinations has decreased so make sure all connections are still there.");
                        // Unity removes the last elements in the list so just take care of the ones that got chopped off
                        // // monote - I don't think we need this anymore since this is cleaning up DEST gate's WARP sources
                        /* for (int i = warpDiode.DestGates.Count; i < preChangeDestGates.Count; i++)
                         {
                             if (preChangeDestGates[i] == null) continue;
                             DestDiode removedDestDiode = preChangeDestGates[i];
                             if (removedDestDiode.WarpSources.Contains(warpDiode) == false) { Debug.LogError("ERROR: this DESTINAION node: " + removedDestDiode.name + " was wiped out by shrinking paths size.  But the WARP node: " + warpDiode.name + " was never in the DESTINATION node's WarpSources list. Bailing."); break; }
                             removedDestDiode.WarpSources.Remove(warpDiode);
                         }*/
                    }
                    else
                    {
                        // Debug.Log("same number of destination nodes so something else must have changed");
                        // number of destinations stayed the same so check for something else                   
                        Interactable changedWarpGate = (Interactable)selDiodeDestSP.serializedObject.targetObject;
                        for (int i = 0; i < interactable.DestGates.Count; i++)
                        {
                            if (preChangeDestGates[i] != changedWarpGate.DestGates[i])
                            {
                                if (preChangeDestGates[i] == null && changedWarpGate.DestGates[i] != null)
                                {
                                    // Debug.Log("added a new connection at spot: " + i + " + with new name: " + changedWarpdDiode.WarpDestinations[i].name);
                                    /*DestDiode newDestDiode = changedWarpdDiode.DestGates[i];
                                    if (newDestDiode.WarpSources.Contains(changedWarpdDiode) == true) { Debug.LogError("ERROR: Trying to add this new DESTINAION node: " + newDestDiode.name + " to this WARP node: " + changedWarpdDiode + " but the WARP node was already in the new DESTINATION node's WarpSources list. Bailing."); break; }
                                    newDestDiode.WarpSources.Add(changedWarpdDiode);*/
                                }
                                else if (preChangeDestGates[i] != null && changedWarpGate.DestGates[i] == null)
                                {
                                    // Debug.Log("removed a connection");
                                    /* DestDiode delDestDiode = preChangeDestGates[i];
                                     if (delDestDiode.WarpSources.Contains(changedWarpdDiode) == false) { Debug.LogError("ERROR: Trying to remove this DESTINATION node: " + delDestDiode + " from this WARP node: " + changedWarpdDiode + " but the WARP node was never in the DESTINATION node's WarpSources list. Bailing"); break; }
                                     delDestDiode.WarpSources.Remove(changedWarpdDiode);*/
                                }
                                else
                                {
                                    //  Debug.Log("changed a connection");
                                    /* DestDiode oldDestDiode = preChangeDestGates[i];
                                     DestDiode newDestDiode = changedWarpdDiode.DestGates[i];
                                     // remove old one
                                     if (oldDestDiode.WarpSources.Contains(changedWarpdDiode) == false) { Debug.LogError("ERROR: Trying to swap out this DESTINATION node: " + oldDestDiode + " with a new one but this WARP node: " + changedWarpdDiode + " was never in the old DESTINATION node's WarpSources list. Bailing."); break; }
                                     oldDestDiode.WarpSources.Remove(changedWarpdDiode);
                                     // add new one
                                     if (newDestDiode.WarpSources.Contains(changedWarpdDiode) == true) { Debug.LogError("ERROR: Trying to swap out a DESTINATION node with this new one: " + newDestDiode + " on this WARP node: " + changedWarpdDiode + " but the new DESTINATION node already had the WARP node in it's WarpSources list. Bailing."); break; }
                                     newDestDiode.WarpSources.Add(changedWarpdDiode);*/
                                }
                            }
                        }
                    }
                }
            }
        }
        else if (bo.BoardObjectType == BoardObject.eBoardObjectType.SHIELD)
        {           
            EditorGUILayout.Separator();
            Shield shield = (Shield)bo;
            
            Shield.eShieldTypes newShieldType = (Shield.eShieldTypes)EditorGUILayout.EnumPopup("Type: ", shield.ShieldType);
            if(newShieldType != shield.ShieldType)
            {
                DestroyImmediate(shield.gameObject);
                shield = mcp.CreateShield(selChannelNode.transform, newShieldType);
                shield.InitForChannelNode(selChannelNode, dax); // moupdate check how shields do stuff
            }
            if(shield.ShieldType == Shield.eShieldTypes.TIMED || shield.ShieldType == Shield.eShieldTypes.TIMED_KILL)
            {
                float newTimer = EditorGUILayout.FloatField("Timer: ", shield.Timer);
                if (newTimer < 1.0f) newTimer = 1.0f;
                if (newTimer != shield.Timer)
                {
                    shield.Timer = newTimer;
                    UpdateFloatProperty(selBoardObjectSO, "GameModTime", shield.Timer);                    
                }
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
                speedMod = mcp.CreateSpeedMod(selChannelNode, dax, newSpeedModType);
            }            
            if((int)speedMod.SpeedModType <= (int)SpeedMod.eSpeedModType.WHEEL_DOWN)
            {
                float newSpeedModVal = EditorGUILayout.FloatField("Val: ", speedMod.SpeedModVal);
                if (newSpeedModVal != speedMod.SpeedModVal)
                {                    
                    speedMod.SpeedModVal = newSpeedModVal;
                    UpdateFloatProperty(selBoardObjectSO, "SpeedModVal", speedMod.SpeedModVal);                    
                }
            }            
        }
    }      
    
    /* Get the Material that the bumper needs for it's type and/or color if it's a COLOR_MATCH type */
    public Material GetBumperMaterial(Bumper.eBumperType type, Facet.eFacetColors color)
    {
        if (type != Bumper.eBumperType.COLOR_MATCH)
        {
            switch (type)
            {
                case Bumper.eBumperType.REGULAR: return Instantiate<Material>(Resources.Load<Material>("Dax/Bumper Materials/Bumper None"));
                case Bumper.eBumperType.DEATH: return Instantiate<Material>(Resources.Load<Material>("Dax/Bumper Materials/Bumper Death"));
                default: Debug.LogError("GetBumperMaterial(): Invalid Bumper type: " + type); return null;
            }
        }
        else
        {
            return (FindObjectOfType<MCP>().GetFacetMaterial(color));            
        }
    }

    /* This removes all the null DestGate entries from WarpGates and Wormholes in case puzzle changes */
    void RemoveWarpNullEntries(Wheel wheel)
    {
        // clean up any warp gates or wormholes
        List<Interactable> warpGates = wheel.GetComponentsInChildren<Interactable>().ToList();
        warpGates.RemoveAll((x => (x.InteractableType != Interactable.eInteractableType.WARP_GATE && x.InteractableType != Interactable.eInteractableType.WORMHOLE)));
        foreach (Interactable warpGate in warpGates)
        {
            warpGate.DestGates.RemoveAll(x => x == null);
        }
    }

    /* Trims duplicates from a list but keeps any empty entries around */
    List<T> TrimDuplicates<T>(List<T> list)
    {
        int numOffBefore = list.Count; // get the size of the List before duplicate trimming
        list = list.Distinct().ToList(); // get rid of the duplicates
        int numToAdd = numOffBefore - list.Count; // find the number of entries to add       
        list.AddRange(new T[numToAdd]); // re-add the null objects to the end
        return list;
    }
}
