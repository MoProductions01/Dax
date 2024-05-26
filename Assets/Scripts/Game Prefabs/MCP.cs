using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;

/// <summary>
/// This class is the main overseer of the of the project.  Manages 
/// the rest of the
/// </summary>
public class MCP : MonoBehaviour
{
    public Dax _Dax;    
    public UIRoot _UIRoot; 

    /// <summary>
    /// List of the root strings for board object prefabs.  Combined with the
    /// info for the board object will get you the correct string to load
    /// </summary>
    public static List<string> PREFAB_ROOT_STRINGS = new List<string> {"Dax/Prefabs/", "Dax/Prefabs/Pickups/",
    "Dax/Prefabs/Hazards/", "Dax/Prefabs/Pickups/Facet_Collects/", "Dax/Prefabs/Pickups/Shields/", 
    "Dax/Prefabs/Pickups/Speed_Mods/", "Dax/Prefabs/Pickups/Point_Mods/"};

    // Strings for the specific board objects to load.  Combined with the root to get the correct string 
    // for the board object to load
    public static List<List<string>> PREFAB_BOARDOBJECT_STRINGS = new List<List<string>> 
    {
        new List<string> { "Player_Diode" }, // Player
        new List<string> { "Facet" },   // Facet
        new List<string> {"Enemy_Diode", "Glue", "Dynamite" }, // Hazard
        new List<string> {"Facet_Collect_Ring", "Facet_Collect_Wheel"}, // Facet Collect
        new List<string> {"Hit_Shield", "Single_Kill_Shield"}, // Sheild
        new List<string> {"Player_Speed", "Enemy_Speed", "Ring_Speed"}, // Speed Mod
        new List<string> {"Extra_Points", "Points_Multiplier"} // Point Mod
    };  

    /// <summary>
    /// Loads up a saved puzzle
    /// </summary>
    /// <param name="puzzlePath">Name of puzzle</param>
    public void LoadPuzzle(string puzzlePath)
    {        
        // This assumes that the current puzzle has been trashed and re-created by the RadientGames.LoadPuzzle() stuff
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(puzzlePath, FileMode.Open);
        Dax.PuzzleSaveData saveData = (Dax.PuzzleSaveData)bf.Deserialize(file);
        file.Close();
        _Dax.ResetPuzzleFromSave(saveData);        
    }

    /// <summary>
    /// Serializes the puzzle data and saves it to a binary file
    /// </summary>
    public void SavePuzzle() 
    {                
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file;
        string fileName = Application.dataPath + "/Resources/Puzzles/" + _Dax.PuzzleName + ".Dax";
        if (File.Exists(fileName))
        {
            bool saveResponse = EditorUtility.DisplayDialog("Warning", 
                                "This will overrite an existing save file. OK?", "Yes", "No");            
            if(saveResponse == false) return;            
            file = File.Open(fileName, FileMode.Open);
        }
        else
        {
            file = File.Create(fileName);
        }
        Dax.PuzzleSaveData puzzleSave = _Dax.CreateSaveData(_Dax);
        bf.Serialize(file, puzzleSave);
        file.Close();
    }   

    /// <summary>
    /// Destroys all of the board objects on the currently selected ring
    /// </summary>
    /// <param name="ring">The ring to destroy all the board objets on</param>
    void ResetBoardObjects(Ring ring)
    {
        BoardObject[] boardObjects = ring.GetComponentsInChildren<BoardObject>();
        for (int i = 0; i < boardObjects.Length; i++)
        {            
            BoardObject boardObject = boardObjects[i];
            if (boardObject == _Dax._Player) continue;
            boardObject.SpawningNode.SpawnedBoardObject = null;
            DestroyImmediate(boardObject.gameObject);
        }
    }

    /// <summary>
    /// Restores all of the chanel pieces on the selected ring
    /// </summary>
    /// <param name="ring">The ring to reset all the channel pieces for</param>
    void ResetRingChannels(Ring ring)
    {        
        List<ChannelPiece> channelPieces = ring.GetComponentsInChildren<ChannelPiece>().ToList();
        foreach (ChannelPiece channelPiece in channelPieces)
        {
            channelPiece.SetActive(true);
        }
    }

    /// <summary>
    /// Resets all of the Ring details 
    /// </summary>
    /// <param name="ring">The ring to reset</param>
    public void ResetRing(Ring ring)
    {
        ring.transform.eulerAngles = Vector3.zero;  
        ring.RotateSpeed = 0;
        ResetBoardObjects(ring);
        ResetRingChannels(ring);
    }    

    /// <summary>
    /// Resets all of the Rings and the number of facets collected
    /// </summary>
    /// <param name="wheel"></param>
    public void ResetWheel(Wheel wheel)
    {        
        foreach (Ring ring in wheel.Rings)
        {
            ResetRing(ring);
        }
        for (int i = 0; i < wheel.NumFacetsCollected.Count; i++) 
        {
            wheel.NumFacetsCollected[i] = 0;       
        }
    }

    /// <summary>
    /// If they exist, it trashes all of the elements of the previous puzzle.  
    /// If not, it skips that process.
    /// </summary>
    static void TrashCurrentPuzzle()
    {
        MCP mcp = GameObject.FindObjectOfType<MCP>();
        if(mcp != null) UnityEngine.Object.DestroyImmediate(mcp.gameObject);        
        DaxPuzzleSetup daxSetup = GameObject.FindObjectOfType<DaxPuzzleSetup>();
        if (daxSetup != null) UnityEngine.Object.DestroyImmediate(daxSetup.gameObject);
        Dax dax = GameObject.FindObjectOfType<Dax>();
        if (dax != null) UnityEngine.Object.DestroyImmediate(dax.gameObject);
        UIRoot uiRoot = GameObject.FindObjectOfType<UIRoot>();
        if (uiRoot != null) UnityEngine.Object.DestroyImmediate(uiRoot.gameObject);
    }

    /// <summary>
    /// Trashes any existing puzzle in the scene then creates and brand new one from scratch.
    /// Also takes care of setting up existing GameObjects.  
    /// </summary>
    public static void CreateNewPuzzle()
    {      
        // First trash all the components used in the previous puzzle (if they exist)
        TrashCurrentPuzzle();       

        // Create an MCP.  Description above since we're in that class
        GameObject mcpGO = new GameObject("MCP");
        ResetTransform(mcpGO.transform);
        MCP mcp = mcpGO.AddComponent<MCP>();
                
        // Dax Puzzle Setup is the main component for creating puzzles using
        // Unity's in-editor functionality
        GameObject decGO = new GameObject("Dax Puzzle Setup");
        ResetTransform(decGO.transform);                
        DaxPuzzleSetup daxSetup = decGO.AddComponent<DaxPuzzleSetup>();
        

        // Dax is the main overall gameplay class.  Pretty much manages everything
        mcp._Dax = new GameObject("Dax").AddComponent<Dax>();        
        daxSetup._Dax = mcp._Dax;

        // Wheel is the container class for the gameboard (Rings, etc)
        Wheel wheel = CreateWheel(mcp/*, mcp._Dax.gameObject, mcp._Dax, 0*/);
        mcp._Dax.Wheel = wheel;

        // Set up the camera propertly
        Camera mainCamera = Camera.main;
        mainCamera.transform.position = new Vector3(0f, DaxPuzzleSetup.CAMERA_Y_VALUES[Dax.MAX_NUM_RINGS-1], 0f);
        mainCamera.transform.eulerAngles = new Vector3(90f, 0f, 0f);        
        mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));

        // UI container
        UIRoot uiRootPrefab = Resources.Load<UIRoot>("Dax/Prefabs/UI/UI Root");
        mcp._UIRoot = UnityEngine.Object.Instantiate<UIRoot>(uiRootPrefab);

        // Create the Player and set it up
        Player playerPrefab = Resources.Load<Player>("Dax/Prefabs/Player_Diode");
        Player player = UnityEngine.Object.Instantiate<Player>(playerPrefab, mcp._Dax.transform);        
        player.name = "Player Diode";        
        mcp._Dax._Player = player;
        mcp._Dax._Player.Dax = mcp._Dax;
        player.SetStartChannel(0);        
        player.InitForChannelNode(null, mcp._Dax);
        player.ResetPlayer();        
        
        // We use a debug class that shows up on display 2 so it doesn't interfere with 
        // the in game display          
        RadientDebug rifRafDebugPrefab = Resources.Load<RadientDebug>("_RadientDebug");
        RadientDebug rifRafDebug = UnityEngine.Object.Instantiate<RadientDebug>(rifRafDebugPrefab, mcp._Dax.gameObject.transform);
        RRDManager.Init(rifRafDebug);        
        daxSetup.RadientDebugRef = rifRafDebug;
    }    

    /// <summary>
    /// Creates the gameboard from prefabs via code.  It can be customized however you need it.
    /// </summary>
    /// <param name="mcp">MCP is the main project overseer class</param>
    /// <returns></returns>
    public static Wheel CreateWheel(MCP mcp/*, GameObject rootGO, Dax daxRef, int wheelNum*/)
    {
        // Create the Wheel GameObject/Class and make it a child of Dax
        GameObject go = new GameObject("Wheel");
        MCP.ResetTransform(go.transform);
        go.transform.parent = mcp._Dax.gameObject.transform;
        Wheel wheel = go.AddComponent<Wheel>();
        wheel.DaxRef = mcp._Dax;

        // Create a list for the facets on board and collected based on the enum
        wheel.NumFacetsOnBoard = new List<int>(new int[((int)Facet.eFacetColors.ORANGE) + 1]);
        wheel.NumFacetsCollected = new List<int>(new int[((int)Facet.eFacetColors.ORANGE) + 1]);        
        
        // Various GameObjects and prefabs used in the creation of the rings
        GameObject ringColliderPrefab = null;
        GameObject ringGameObject = null;
        GameObject bumperGroup = null;
        GameObject nodesContainer = null;
        GameObject locatorsPrefab = Resources.Load<GameObject>("Dax/Board_Parts/DAX_Locators"); // monote - this is in the Nodes prefab
        GameObject locators = GameObject.Instantiate<GameObject>(locatorsPrefab);
        GameObject nodesPrefab = Resources.Load<GameObject>("Dax/Board_Parts/Nodes");
        GameObject nodes = GameObject.Instantiate<GameObject>(nodesPrefab);
        wheel.Rings.Clear();
        
        // Create rings
        for (int ringIndex = 0; ringIndex <= Dax.MAX_NUM_RINGS; ringIndex++)
        {
            int locatorIndex;
            string ringIndexString = ringIndex.ToString("D2");
            string ringString = "Ring_" + ringIndexString;

            GameObject locatorGroup = locators.transform.GetChild(0).transform.GetChild(ringIndex).gameObject;

            // Ring objects                        
            ringColliderPrefab = Resources.Load<GameObject>("Dax/Board_Parts/" + ringString + "_Collider");
            ringGameObject = GameObject.Instantiate<GameObject>(ringColliderPrefab);
            UnityEngine.Object.DestroyImmediate(ringGameObject.GetComponent<MeshFilter>());
            ringGameObject.GetComponent<MeshCollider>().convex = false;
            ringGameObject.transform.parent = wheel.gameObject.transform;
            ringGameObject.name = ringString;
            ringGameObject.layer = LayerMask.NameToLayer("Main Touch Control");
            ringGameObject.AddComponent<Ring>();

            // Add the Ring to the wheel
            wheel.Rings.Add(ringGameObject.GetComponent<Ring>());

            // The center ring has no bumpers so if it's any other ring load up and create those
            if (ringIndex != 0)
            {
                // Bumpers
                bumperGroup = new GameObject("Ring_" + ringIndex.ToString("D2") + "_Bumpers");
                bumperGroup.transform.parent = wheel.gameObject.transform;
                bumperGroup.AddComponent<BumperGroup>();
            }

            // Container for the node locations
            nodesContainer = nodes.transform.Find(ringString + "_Nodes").gameObject;
            // Center ring has few channels than the rest
            int numChannels = (ringIndex == 0 ? Wheel.NUM_CENTER_RING_CHANNELS : Wheel.NUM_OUTER_RING_CHANNELS);

            // Create each channel
            for (int i = 0; i < numChannels; i++)
            {
                string channelString = (i + 1).ToString("D2");
                string nodeLocatorChannelString = channelString;                

                // Create bumpers if you're not on the center ring
                if (ringIndex != 0)
                {
                    // BUMPER
                    GameObject bumperLocator = locatorGroup.transform.GetChild(i).gameObject;
                    GameObject bumperPrefab = Resources.Load<GameObject>("Dax/Board_Parts/" + ringString + "_Bumper_01");
                    GameObject bumper = GameObject.Instantiate<GameObject>(bumperPrefab, bumperLocator.transform);
                    bumper.transform.parent = bumperGroup.transform;
                    bumper.name = ringString + "_Bumper_" + channelString;
                    bumper.AddComponent<MeshCollider>();
                    bumper.GetComponent<MeshCollider>().convex = true;
                    bumper.AddComponent<Bumper>();
                    bumper.gameObject.GetComponent<MeshRenderer>().material = mcp.GetBumperMaterial(Bumper.eBumperType.REGULAR, Facet.eFacetColors.ORANGE);                    
                }

                // Create the Channel object
                GameObject channel = new GameObject(ringString + "_Channel_" + channelString);
                channel.transform.parent = ringGameObject.transform;
                channel.AddComponent<Channel>();

                // Channels have an inner and outer section.
                // Each of the two channel sections begin with a channel piece that
                // will block the player.  Removing those is the main part of the 
                // puzzle creation to let the player navigate the wheel.

                // Inner floor
                locatorIndex = i + (2 * numChannels);
                if (ringIndex == 0) locatorIndex -= numChannels;
                GameObject innerFloorLocator = locatorGroup.transform.GetChild(locatorIndex).gameObject;
                GameObject innerFloorPrefab = Resources.Load<GameObject>("Dax/Board_Parts/" + ringString + "_Inner_Floor_01");
                GameObject innerFloor = GameObject.Instantiate<GameObject>(innerFloorPrefab, innerFloorLocator.transform);
                innerFloor.transform.parent = channel.transform;
                innerFloor.name = ringString + "_Inner_Floor_" + channelString;
                innerFloor.gameObject.GetComponent<MeshRenderer>().enabled = false;

                // Inner channel
                locatorIndex = i + numChannels;
                if (ringIndex == 0) locatorIndex -= numChannels;
                GameObject innerChannelLocator = locatorGroup.transform.GetChild(locatorIndex).gameObject;
                GameObject innerChannelPrefab = Resources.Load<GameObject>("Dax/Board_Parts/" + ringString + "_Inner_Channel_01");
                GameObject innerChannel = GameObject.Instantiate<GameObject>(innerChannelPrefab, innerChannelLocator.transform);
                innerChannel.transform.parent = channel.transform;
                innerChannel.transform.name = ringString + "_Inner_Channel_" + channelString;
                innerChannel.gameObject.layer = LayerMask.NameToLayer("Player Generic Collider");
                innerChannel.AddComponent<MeshCollider>();
                innerChannel.GetComponent<MeshCollider>().convex = true;                
                innerChannel.AddComponent<ChannelPiece>();

                 // Outer floor     
                locatorIndex = i + (Dax.MAX_NUM_RINGS * numChannels);
                if (ringIndex == 0) locatorIndex -= numChannels;
                GameObject outerFloorLocator = locatorGroup.transform.GetChild(locatorIndex).gameObject;
                GameObject outerFloorPrefab = Resources.Load<GameObject>("Dax/Board_Parts/" + ringString + "_Outer_Floor_01");
                GameObject outerFloor = GameObject.Instantiate<GameObject>(outerFloorPrefab, outerFloorLocator.transform);
                outerFloor.transform.parent = channel.transform;
                outerFloor.name = ringString + "_Outer_Floor_" + channelString;
                outerFloor.gameObject.GetComponent<MeshRenderer>().enabled = false;

                // Outer channel
                locatorIndex = i + (3 * numChannels);
                if (ringIndex == 0) locatorIndex -= numChannels;
                GameObject outerChannelLocator = locatorGroup.transform.GetChild(locatorIndex).gameObject;
                GameObject outerChannelPrefab = Resources.Load<GameObject>("Dax/Board_Parts/" + ringString + "_Outer_Channel_01");
                GameObject outerChannel = GameObject.Instantiate<GameObject>(outerChannelPrefab, outerChannelLocator.transform);
                outerChannel.transform.parent = channel.transform;
                outerChannel.transform.name = ringString + "_Outer_Channel_" + channelString;
                outerChannel.gameObject.layer = LayerMask.NameToLayer("Player Generic Collider");
                outerChannel.AddComponent<MeshCollider>();
                outerChannel.GetComponent<MeshCollider>().convex = true;                
                outerChannel.AddComponent<ChannelPiece>();

                // Each channel has 3 nodes, a start, end and middle.  The start
                // and end nodes are what the Player uses to jump from Ring to Ring.
                // The middle node is where board objects are placed.

                // Start node
                GameObject startNodePrefab = nodesContainer.transform.Find(ringString + "_Start_Node_" + nodeLocatorChannelString).gameObject;
                GameObject startNode = GameObject.Instantiate<GameObject>(startNodePrefab);
                startNode.transform.parent = channel.transform;
                startNode.transform.name = ringString + "_Start_Node_" + channelString;
                startNode.AddComponent<ChannelNode>();

                // Middle node
                GameObject middleNodePrefab = nodesContainer.transform.Find(ringString + "_Middle_Node_" + nodeLocatorChannelString).gameObject;
                GameObject middleNode = GameObject.Instantiate<GameObject>(middleNodePrefab);
                middleNode.transform.parent = channel.transform;
                middleNode.transform.name = ringString + "_Middle_Node_" + channelString;
                middleNode.AddComponent<ChannelNode>();

                // End node
                GameObject endNodePrefab = nodesContainer.transform.Find(ringString + "_End_Node_" + nodeLocatorChannelString).gameObject;
                GameObject endNode = GameObject.Instantiate<GameObject>(endNodePrefab);
                endNode.transform.parent = channel.transform;
                endNode.transform.name = ringString + "_End_Node_" + channelString;
                endNode.AddComponent<ChannelNode>();                                                       
            }

            // The wedges are the pieces inbetween the channels.  
            // The player bounces off of them but they can't be
            // modified for game play.
            
            // Create wedges
            for (int i = 0; i < numChannels; i++)
            {
                string channelString = (i + 1).ToString("D2");                
                locatorIndex = i + (5 * numChannels);
                if (ringIndex == 0) locatorIndex -= numChannels;
                GameObject wedgeLocator = locatorGroup.transform.GetChild(locatorIndex).gameObject;
                GameObject wedgePrefab = Resources.Load<GameObject>("Dax/Board_Parts/" + ringString + "_Wedge_01");
                GameObject wedge = GameObject.Instantiate<GameObject>(wedgePrefab, wedgeLocator.transform);
                wedge.transform.parent = ringGameObject.transform;
                wedge.name = ringString + "_Wedge_" + channelString;
                wedge.gameObject.layer = LayerMask.NameToLayer("Player Generic Collider");
                wedge.AddComponent<MeshCollider>();
                wedge.GetComponent<MeshCollider>().convex = true;
            }
        }        

        // Now that the board componets are created, start assembling the Rings
        for (int ringIndex = 0; ringIndex <= Dax.MAX_NUM_RINGS; ringIndex++)
        {
            Ring ring = wheel.Rings[ringIndex];
            ring.DaxRef = mcp._Dax;
            ring.RotateSpeed = 0f;

            // Add bumpers if we're not the center ring
            if (ringIndex != 0)
            {
                ring.BumperGroup = wheel.gameObject.transform.Find(ring.name + "_Bumpers").GetComponent<BumperGroup>();
                ring.BumperGroup.Bumpers = ring.BumperGroup.GetComponentsInChildren<Bumper>().ToList();
            }            

            // Add the channels 
            List<Channel> ringChannels = ring.transform.GetComponentsInChildren<Channel>().ToList();
            ringChannels = ringChannels.OrderBy(x => x.name).ToList();
            int numChannels = (ringIndex == 0 ? Wheel.NUM_CENTER_RING_CHANNELS : Wheel.NUM_OUTER_RING_CHANNELS);
            for (int i = 0; i < numChannels; i++)
            {
                Channel channel = ringChannels[i];
                channel.MyRing = ring;

                List<ChannelPiece> channelPieces = channel.GetComponentsInChildren<ChannelPiece>().ToList();
                channel.InnerChannel = channelPieces[0];
                channel.OuterChannel = channelPieces[1];
                List<ChannelNode> channelNodes = channel.GetComponentsInChildren<ChannelNode>().ToList();
                channel.StartNode = channelNodes[0];
                channel.MidNode = channelNodes[1];
                channel.EndNode = channelNodes[2];

                channelPieces[0].InitFromCreation(channel);
                channelPieces[1].InitFromCreation(channel);

                channelNodes[0].InitFromCreation(channel);
                channelNodes[1].InitFromCreation(channel);
                channelNodes[2].InitFromCreation(channel);
            }
        }        

        // Get rid of objects used for location reference
        UnityEngine.Object.DestroyImmediate(locators);
        UnityEngine.Object.DestroyImmediate(nodes);

        // Make sure all the Rings are on        
        wheel.TurnOnRings(Dax.MAX_NUM_RINGS);

        return wheel;
    }

    
    /// <summary>
    /// When you get a powerup that will collect all the facets on either a ring or the 
    /// entire wheel, put an icon in the center of the wheel for the user to click on.
    /// </summary>
    /// <param name="type">Whether it's a collect facet powerup for a ring or the whole wheel</param>
    /// <returns></returns>
    public GameObject CreateFacetCollectIcon( FacetCollect.eFacetCollectTypes type)
    {        
        GameObject facetCollectIconPrefab = null;
        switch (type)
        {
            case FacetCollect.eFacetCollectTypes.RING:
                facetCollectIconPrefab = Resources.Load<GameObject>("Dax/Prefabs/HUD_Items/Facet_Collect_Ring_HUD");
                break;
            case FacetCollect.eFacetCollectTypes.WHEEL:
                facetCollectIconPrefab = Resources.Load<GameObject>("Dax/Prefabs/HUD_Items/Facet_Collect_Wheel_HUD");
                break;
        }
        GameObject facetCollectIcon = Instantiate<GameObject>(facetCollectIconPrefab, _Dax._UIRoot.transform);
        return facetCollectIcon;
    }

    /// <summary>
    /// Creates a shield icon for the center of the wheel when the user collects a shield powerup
    /// </summary>
    /// <param name="type">Hit Shield or Single Kill shield</param>
    /// <returns></returns>
    public GameObject CreateShieldIcon(Shield.eShieldTypes type)
    {
        GameObject shieldIconPrefab = null;
        switch (type)
        {   // 
            case Shield.eShieldTypes.HIT:
                shieldIconPrefab = Resources.Load<GameObject>("Dax/Prefabs/HUD_Items/Hit_Shield_HUD");
                break;
            case Shield.eShieldTypes.SINGLE_KILL:
                shieldIconPrefab = Resources.Load<GameObject>("Dax/Prefabs/HUD_Items/Single_Kill_Shield_HUD");
                break;           
        }
        GameObject shieldIcon = Instantiate<GameObject>(shieldIconPrefab, _Dax._UIRoot.transform);
        return shieldIcon;       
    }    
   
    /// <summary>
    /// Instantiates and returns a material for a specific facet color
    /// </summary>
    /// <param name="color">Color of the facet</param>
    /// <returns></returns>
    public Material GetFacetMaterial(Facet.eFacetColors color)
    {
        switch (color)
        {
            case Facet.eFacetColors.RED: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Red"));
            case Facet.eFacetColors.GREEN: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Green"));
            case Facet.eFacetColors.BLUE: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Blue"));
            case Facet.eFacetColors.YELLOW: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Yellow"));
            case Facet.eFacetColors.PURPLE: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Purple"));            
            case Facet.eFacetColors.ORANGE: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Orange"));           
            default: Debug.LogError("GetFacetMaterial(): Invalid Bumper color: " + color); return null;
        }
    }

    /// <summary>
    /// Gets the material for the type and color of the bumper
    /// </summary>
    /// <param name="type"></param>
    /// <param name="color"></param>
    /// <returns></returns>
    public Material GetBumperMaterial(Bumper.eBumperType type, Facet.eFacetColors color)
    {
        if (type != Bumper.eBumperType.COLOR_MATCH)
        {   // When the bumper type is either non colored or a death bumper
            switch (type)
            {
                case Bumper.eBumperType.REGULAR: return Instantiate<Material>(Resources.Load<Material>("Dax/Bumper Materials/Bumper None"));
                case Bumper.eBumperType.DEATH: return Instantiate<Material>(Resources.Load<Material>("Dax/Bumper Materials/Bumper Death"));
                default: Debug.LogError("GetBumperMaterial(): Invalid Bumper type: " + type); return null;
            }
        }
        else
        {
            // This is a color match bumper so use this
            return GetFacetMaterial(color);
        }
    }
            
    /// <summary>
    /// Called from the DaxEditor to change the color of a facet
    /// </summary>
    /// <param name="facet">The selected facet</param>
    /// <param name="color">The color to change it to</param>
    public void ChangeFacetColor(Facet facet, Facet.eFacetColors color)
    {
        facet._Color = color;
        Material colorFacetMaterial = GetFacetMaterial(color);
        List<MeshRenderer> meshRenderers = facet.GetComponentsInChildren<MeshRenderer>().ToList();
        foreach (MeshRenderer mr in meshRenderers) mr.material = colorFacetMaterial;
    }
        
    /// <summary>
    /// This initializes all of the data and information for a board object if loaded from a save file
    /// </summary>
    /// <param name="bo">The board object to initialize</param>
    /// <param name="boSave">Save data for the board object</param>
    public void InitBoardObjectFromSave(BoardObject bo, Dax.BoardObjectSave boSave)
    {
        // get the starting channel
        bo.CurChannel = GameObject.Find(boSave.StartChannel).GetComponent<Channel>();
        // Init the starting direction and speed
        bo.StartDir = boSave.StartDir; 
        bo.Speed = boSave.Speed; 

        // Get the information out of the save datat depending on what kind of board object it is
        switch (bo.BoardObjectType)
        {
            case BoardObject.eBoardObjectType.SHIELD:
                Shield shield = (Shield)bo;
                shield.ShieldType = (Shield.eShieldTypes)boSave.IntList[0];               
                break;
            case BoardObject.eBoardObjectType.SPEED_MOD:
                SpeedMod speedMod = (SpeedMod)bo;
                speedMod.SpeedModType = (SpeedMod.eSpeedModType)boSave.IntList[0];
                speedMod.SpeedModVal = boSave.FloatList[0];
                break;
            case BoardObject.eBoardObjectType.HAZARD:
                Hazard hazard = (Hazard)bo;
                hazard.HazardType = (Hazard.eHazardType)boSave.IntList[0];
                hazard.EffectTime = boSave.FloatList[0];
                //hazard.EffectRadius = boSave.FloatList[1];
                if(hazard.HazardType == Hazard.eHazardType.ENEMY)
                { 
                    hazard.transform.LookAt(bo.StartDir == BoardObject.eStartDir.OUTWARD ? hazard.CurChannel.EndNode.transform : hazard.CurChannel.StartNode.transform);
                }               
                break;
            case BoardObject.eBoardObjectType.POINT_MOD:
                PointMod pointMod = (PointMod)bo;
                pointMod.PointModType = (PointMod.ePointModType)boSave.IntList[0];
                pointMod.PointModVal = boSave.IntList[1];
                pointMod.PointModTime = boSave.FloatList[0];
                break;            
        }
    }      

    /// <summary>
    /// Creates the string for the prefab to load
    /// </summary>
    /// <param name="boardObjectIndex">The root board object to load</param>
    /// <param name="boardObjectTypeIndex">The index of that board object to load</param>
    /// <returns></returns>
    public string CreatePrefabString(int boardObjectIndex, int boardObjectTypeIndex)
    {                
        // Combine the boardObjectIndex and boardObjectTypeIndex to build
        // the string out of the static definitions above
        return PREFAB_ROOT_STRINGS[boardObjectIndex] + 
            PREFAB_BOARDOBJECT_STRINGS[boardObjectIndex][boardObjectTypeIndex];          
    }
   
    /// <summary>
    /// Creates a board object from scratch
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="channelNode">The channel node this board object will be on</param>
    /// <param name="dax">Reference to the main game play object</param>
    /// <param name="boardObjectIndex">For getting the prefab root folder string</param>
    /// <param name="boardObjectTypeIndex">For getting the specific type of board object</param>
    /// <returns></returns>
    public T CreateBoardObject<T>(ChannelNode channelNode, Dax dax, 
                                  int boardObjectIndex, int boardObjectTypeIndex) where T : BoardObject
    {                          
        string prefabString = CreatePrefabString(boardObjectIndex, boardObjectTypeIndex); // Create the prefab string to load
        T prefab = Resources.Load<T>(prefabString);
        T instantiaedObject = Instantiate<T>(prefab, channelNode.transform);
        instantiaedObject.InitForChannelNode(channelNode, dax);
        return instantiaedObject;        
    }    

    /// <summary>
    /// This creates a board object from save data
    /// </summary>
    /// <param name="channelNode">Channel node this object will spawn on</param>
    /// <param name="boSave">Save info for the board object</param>
    /// <param name="dax">Root gameplay object</param>
    public void CreateBoardObjectFromSaveData(ChannelNode channelNode, Dax.BoardObjectSave boSave, Dax dax)
    {
        //Debug.Log("Dax.CreateBoardObjectForNode(): " + channelNode.name + ", of type: " + boSave.Type); // moupdate *!!! this is being called 100 times for the player        
        // Create the board object based on the type in the save data
        switch (boSave.Type)
        {                        
            case BoardObject.eBoardObjectType.FACET:                
                Facet facet = CreateBoardObject<Facet>(channelNode, dax, 
                    (int)BoardObject.eBoardObjectType.FACET, 0);
                ChangeFacetColor(facet, (Facet.eFacetColors)boSave.IntList[0]);     
                break;
            case BoardObject.eBoardObjectType.HAZARD:                
                CreateBoardObject<Hazard>(channelNode, dax, 
                    (int)BoardObject.eBoardObjectType.HAZARD, boSave.IntList[0]);         
                break;
            case BoardObject.eBoardObjectType.FACET_COLLECT:                                
                CreateBoardObject<FacetCollect>(channelNode, dax, 
                    (int)BoardObject.eBoardObjectType.FACET_COLLECT, boSave.IntList[0]);
                break;       
            case BoardObject.eBoardObjectType.SHIELD:                                                    
                CreateBoardObject<Shield>(channelNode, dax, 
                    (int)BoardObject.eBoardObjectType.SHIELD, boSave.IntList[0]);
                break;  
            case BoardObject.eBoardObjectType.SPEED_MOD:                
                CreateBoardObject<SpeedMod>(channelNode, dax, 
                    (int)BoardObject.eBoardObjectType.SPEED_MOD, boSave.IntList[0]);
                break;       
            case BoardObject.eBoardObjectType.POINT_MOD:                
                CreateBoardObject<PointMod>(channelNode, dax, 
                    (int)BoardObject.eBoardObjectType.POINT_MOD, boSave.IntList[0]);
                break;         
        }
    }

    /// <summary>
    /// Helper function to reset all of the info for the transform
    /// </summary>
    /// <param name="t">Transform to reset</param>
    public static void ResetTransform(Transform t)
    {
        t.position = Vector3.zero;
        t.eulerAngles = Vector3.zero;
        t.localScale = Vector3.one;
    }    

    // This was used to get some debug info about the board
 /* if(ringIndex == 0)
    { 
        for(int channelIndex=0; channelIndex<numChannels; channelIndex++)
        {
            Transform startNode_ = nodesContainer.transform.GetChild(channelIndex*3);
            Transform endNode_ = nodesContainer.transform.GetChild(channelIndex*3 + 2);

            double radian = Math.Atan2(endNode_.transform.position.z - startNode_.transform.position.z,
                                        endNode_.transform.position.x - startNode_.transform.position.x);
            double angle = radian * (180 / Math.PI);
            Debug.Log("-------------------------------------------\nchannelIndex: " + channelIndex + ", startNode transform.position: " + startNode_.transform.position + 
            ", endNode: transform.position: " + endNode_.transform.position + "\n startNode transform.localPosition: " + startNode_.transform.localPosition + 
            ", endNode: transform.localPosition: " + endNode_.transform.localPosition + ", radian: " + radian + ", angle: " + angle);

            Vector3 newPosA = Vector3.zero;
            newPosA.x = startNode_.position.x + (endNode_.position.x - startNode_.position.x) / 2;
            newPosA.y = startNode_.position.y + (endNode_.position.y - startNode_.position.y) / 2;
            newPosA.z = startNode_.position.z + (endNode_.position.z - startNode_.position.z) / 2;
            Vector3 newPosB = Vector3.Lerp(startNode_.transform.position, endNode_.transform.position, .5f);
            Debug.Log( "transform.position newPosA: " + newPosA + ", newPosB: " + newPosB);
            newPosA.x = startNode_.localPosition.x + (endNode_.localPosition.x - startNode_.localPosition.x) / 2;
            newPosA.y = startNode_.localPosition.y + (endNode_.localPosition.y - startNode_.localPosition.y) / 2;
            newPosA.z = startNode_.localPosition.z + (endNode_.localPosition.z - startNode_.localPosition.z) / 2;
            newPosB = Vector3.Lerp(startNode_.transform.localPosition, endNode_.transform.localPosition, .5f);
            Debug.Log( "transform.localPosition newPosA: " + newPosA + ", newPosB: " + newPosB);
        }
    }*/ 

    //  You can rotate a direction Vector3 with a Quaternion by multiplying the quaternion with the direction(in that order).
    //  Then you just use Quaternion.AngleAxis to create the rotation.
}