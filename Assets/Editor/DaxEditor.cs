using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;


[CustomEditor(typeof(DaxPuzzleSetup))]
public class DaxEditor : Editor
{
    public GameObject LastSelectedObject = null;    // used for checking for changes in the selected object
    int SelectedRing = 0; // index of our currently selected ring   
   // bool IsDialogueShowing = false; // this is to get around a weird Unity bug where dialogues show up twice.  FUN
   // bool StartChannelError = false; // if we're waiting for a system popup on a start channel error    
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
        if(so.hasModifiedProperties == true) Debug.Log("so: " + so.ToString() + ", hasModifiedProperties");
        so.FindProperty(propName).enumValueIndex = enumValue;
        bool changesMade = so.ApplyModifiedProperties();
        if(changesMade == true) Debug.Log("so: " + so.ToString() + ", enum: " + propName + ", changed to: " + enumValue);
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
        DaxPuzzleSetup daxSetup = (DaxPuzzleSetup)target;        
        SerializedObject daxSetupSO = new SerializedObject(daxSetup);
        daxSetupSO.Update();
        Dax dax = mcp._Dax.GetComponent<Dax>();
        SerializedObject daxSO = new SerializedObject(dax);
        daxSO.Update();

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
        for (int i = 0; i < dax.CurWheel.NumFacetsOnBoard.Count-1; i++)
        {
            Facet.eFacetColors curColor = (Facet.eFacetColors)i;
            EditorGUILayout.LabelField(curColor.ToString() + " To Collect: " + dax.CurWheel.NumFacetsOnBoard[(int)curColor]);
        }  
       /* if(dax.CurWheel.VictoryCondition == Dax.eVictoryConditions.COLLECTION)
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
        }   */    

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
            int newNumRings = EditorGUILayout.IntPopup("Number of Rings", dax.CurWheel.NumActiveRings, DaxPuzzleSetup.NUM_RINGS_NAMES, DaxPuzzleSetup.NUM_RINGS_TOTALS);
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
         #region INIT_SAVE
            // **** Puzzle initialization/saving
            EditorGUILayout.Separator();
            //EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
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
            #endregion

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
                float newSpeed = EditorGUILayout.FloatField("Rotate Speed: ", selRing.RotateSpeed);
                if (newSpeed != selRing.RotateSpeed)
                {
                    selRing.RotateSpeed = newSpeed;
                    SerializedObject selRingSO = new SerializedObject(selRing);
                    selRingSO.Update();
                    UpdateFloatProperty(selRingSO, "RotateSpeed", selRing.RotateSpeed);                    
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
                
                /*EditorGUILayout.Separator();
                BoardObject.eMoveDir newMoveDir = (BoardObject.eMoveDir)EditorGUILayout.EnumPopup("Start Direction:", player.MoveDir);
                if (newMoveDir != player.MoveDir)
                {
                    player.MoveDir = newMoveDir;
                    UpdateEnumProperty(playerSO, "MoveDir", (int)player.MoveDir);                    
                }*/
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
                   // if (newBumperType == Bumper.eBumperType.COLOR_MATCH) selBumper.BumperColor = Facet.eFacetColors.RED;
                    //else selBumper.BumperColor = Facet.eFacetColors.WHITE;
                    selBumper.BumperColor = Facet.eFacetColors.RED;
                    selBumper.gameObject.GetComponent<MeshRenderer>().material = GetBumperMaterial(newBumperType, selBumper.BumperColor);                    
                    UpdateEnumProperty(selBumperSO, "BumperColor", (int)selBumper.BumperColor);
                }
                EditorGUILayout.Separator();
                if (selBumper.BumperType == Bumper.eBumperType.COLOR_MATCH)
                {
                    Facet.eFacetColors newBumperColor = (Facet.eFacetColors)EditorGUILayout.EnumPopup("Bumper Color", selBumper.BumperColor);
                    if (newBumperColor != selBumper.BumperColor)
                    {
                        //if (newBumperColor == Facet.eFacetColors.WHITE) { Debug.LogError("Bumper Color can't be White."); return; }
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
                        int startChannelIndex = Int32.Parse(selChannelNode.name.Substring(19, 2)) - 1;                        
                       // dax.StartChannelIndex = startChannelIndex;
                        FindObjectOfType<Player>().SetStartChannel(startChannelIndex);
                       // UpdateIntProperty(daxSO, "StartChannelIndex", dax.StartChannelIndex);                                    
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
            // If you've got a BoardObject selected then make sure the HandleBoardObject stuff is taken care of
            if(selected.transform.parent != null && selected.transform.parent.GetComponent<BoardObject>() != null)
            {                
                BoardObject parent = selected.transform.parent.GetComponent<BoardObject>();
                ChannelNode selChannelNode = parent.SpawningNode;
                StartNewSelection(selChannelNode.name, "Channel Node");
                HandleBoardObject(selChannelNode, mcp, dax);
                EditorGUILayout.Separator();
                EditorGUILayout.EndVertical();
            }                
            #endregion            
        }

        // Save data                
        if(daxSetupSO.hasModifiedProperties == true) Debug.Log("daxSetupSO.hasModifiedProperties == true"); //else Debug.Log("daxSetupSO.hasModifiedProperties == false");
        if(daxSO.hasModifiedProperties == true) Debug.Log("daxSO.hasModifiedProperties == true"); //else Debug.Log("daxSO.hasModifiedProperties == false");
        bool daxSetupChanged = daxSetupSO.ApplyModifiedProperties();
        bool daxChanged = daxSO.ApplyModifiedProperties();
        if(daxSetupChanged == true) Debug.Log("daxSetupChanged == true"); //else Debug.Log("daxSetupChanged == false");
        if(daxChanged == true) Debug.Log("daxChanged == true");  //else Debug.Log("daxChanged == false");              
        Undo.RecordObject(daxSetup, "OnInspectorGUI");
        EditorUtility.SetDirty(daxSetup);
    }

    
    void HandleCreateBoardObject(ChannelNode selChannelNode, MCP mcp, Dax dax)
    {
        float buttonWidth = 200f;
        if (GUILayout.Button("Create Facet", GUILayout.Width(buttonWidth)))
        {            
            mcp.CreateFacet(selChannelNode, dax, Facet.eFacetColors.RED);
        }
        //******************************************************************************************
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Hazard", GUILayout.Width(buttonWidth)))
        {            
            string prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.HAZARD] + 
                Hazard.HAZARD_STRINGS[(int)Hazard.eHazardType.ENEMY];
            mcp.CreateBoardObject<Hazard>(selChannelNode, dax, prefabString);                      
        }    
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Facet Collect", GUILayout.Width(buttonWidth)))
        {
            string prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.FACET_COLLECT] + 
                FacetCollect.FACET_COLLECT_STRINGS[(int)FacetCollect.eFacetCollectTypes.RING];
            mcp.CreateBoardObject<FacetCollect>(selChannelNode, dax, prefabString);            
        } 
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Shield", GUILayout.Width(buttonWidth)))
        {
            string prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.SHIELD] + 
                Shield.SHIELD_STRINGS[(int)Shield.eShieldTypes.HIT];
            mcp.CreateBoardObject<Shield>(selChannelNode, dax, prefabString);                  
        }
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Speed Mod", GUILayout.Width(buttonWidth)))
        {
            string prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.SPEED_MOD] +
                SpeedMod.SPEED_MOD_STRINGS[(int)SpeedMod.eSpeedModType.PLAYER_SPEED];
            mcp.CreateBoardObject<SpeedMod>(selChannelNode, dax, prefabString);                        
        }
        EditorGUILayout.Separator();
        if (GUILayout.Button("Create Game Mod", GUILayout.Width(buttonWidth)))
        {
            string prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.GAME_MOD] +
                GameMod.GAME_MOD_STRINGS[(int)GameMod.eGameModType.EXTRA_POINTS];
            mcp.CreateBoardObject<GameMod>(selChannelNode, dax, prefabString);                   
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
                    string prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.HAZARD] + 
                        Hazard.HAZARD_STRINGS[(int)(Hazard.eHazardType)newHazardType];
                    hazard = mcp.CreateBoardObject<Hazard>(selChannelNode, dax, prefabString);
                    //hazard = mcp.CreateHazard(selChannelNode, dax, newHazardType);

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
                //gameMod = mcp.CreateGameMod(selChannelNode, dax, newGameModType);
                string prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.GAME_MOD] +
                    GameMod.GAME_MOD_STRINGS[(int)(GameMod.eGameModType)newGameModType];
                gameMod = mcp.CreateBoardObject<GameMod>(selChannelNode, dax, prefabString);  
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
                //facetCollect = mcp.CreateFacetCollect(selChannelNode, dax, newFacetCollectType);
                string prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.FACET_COLLECT] +
                    FacetCollect.FACET_COLLECT_STRINGS[(int)(FacetCollect.eFacetCollectTypes)newFacetCollectType];
                facetCollect = mcp.CreateBoardObject<FacetCollect>(selChannelNode, dax, prefabString); 
            }            
        }                
        
        else if (bo.BoardObjectType == BoardObject.eBoardObjectType.SHIELD)
        {           
            EditorGUILayout.Separator();
            Shield shield = (Shield)bo;
            
            Shield.eShieldTypes newShieldType = (Shield.eShieldTypes)EditorGUILayout.EnumPopup("Type: ", shield.ShieldType);
            if(newShieldType != shield.ShieldType)
            {
                //Debug.LogError("Implement this");
                DestroyImmediate(selChannelNode.SpawnedBoardObject.gameObject);
                //shield = mcp.CreateShield(selChannelNode, dax, newShieldType);                
                string prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.SHIELD] + 
                    Shield.SHIELD_STRINGS[(int)(Shield.eShieldTypes)newShieldType];
                shield = mcp.CreateBoardObject<Shield>(selChannelNode, dax, prefabString); 
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
                //speedMod = mcp.CreateSpeedMod(selChannelNode, dax, newSpeedModType);
                string prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.SPEED_MOD] +
                    SpeedMod.SPEED_MOD_STRINGS[(int)(SpeedMod.eSpeedModType)newSpeedModType];
                mcp.CreateBoardObject<SpeedMod>(selChannelNode, dax, prefabString); 

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
