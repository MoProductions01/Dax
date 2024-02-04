using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class MCP : MonoBehaviour
{
    public Dax _Dax;
    public UIRoot _UIRoot;

    public void LoadPuzzle(string puzzlePath)
    {        
        // This assumes that the current puzzle has been trashed and re-created by the RifRafGames.LoadPuzzle() stuff
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(puzzlePath, FileMode.Open);
        Dax.PuzzleSaveData saveData = (Dax.PuzzleSaveData)bf.Deserialize(file);
        file.Close();
        _Dax.ResetPuzzleFromSave(saveData);        
    }
    public void SavePuzzle() 
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file;
        string fileName = Application.dataPath + "/Resources/Puzzles/" + _Dax.PuzzleName;
        if (File.Exists(fileName))
        {
            Debug.LogWarning("WARNING: replacing puzzle without warning for now: " + _Dax.PuzzleName);
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
    void ResetRingChannels(Ring ring)
    {
        //Debug.Log("ResetringChannels(): " + ring.name);
        List<ChannelPiece> channelPieces = ring.GetComponentsInChildren<ChannelPiece>().ToList();
        foreach (ChannelPiece channelPiece in channelPieces)
        {
            channelPiece.SetActive(true);
        }
    }
    public void ResetRing(Ring ring)
    {
        ring.transform.eulerAngles = Vector3.zero;
        ring.RotateSpeed = 0;
        ResetBoardObjects(ring);
        ResetRingChannels(ring);
    }    
    public void ResetWheel(Wheel wheel)
    {        
        foreach (Ring ring in wheel.Rings)
        {
            ResetRing(ring);
        }
        for (int i = 0; i < wheel.NumFacetsCollected.Count; i++) wheel.NumFacetsCollected[i] = 0;       
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
        mcp._Dax.CurWheel = wheel;

        // Set up the camera propertly
        Camera mainCamera = Camera.main;
        mainCamera.transform.position = new Vector3(0f, DaxPuzzleSetup.CAMERA_Y_VALUES[3], 0f);
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
        mcp._Dax._Player._Dax = mcp._Dax;
        player.SetStartChannel(0);        
        player.InitForChannelNode(null, mcp._Dax);
        player.ResetForPuzzleRestart();        
        
        // We use a debug class that shows up on display 2 so it doesn't interfere with 
        // the in game display          
        RifRafDebug rifRafDebugPrefab = Resources.Load<RifRafDebug>("_RifRafDebug");
        RifRafDebug rifRafDebug = UnityEngine.Object.Instantiate<RifRafDebug>(rifRafDebugPrefab, mcp._Dax.gameObject.transform);
        RRDManager.Init(rifRafDebug);        
        daxSetup.RifRafDebugRef = rifRafDebug;
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
               // for(int i=0; i<)

               


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

    public void CreateFacet(ChannelNode channelNode, Dax dax, Facet.eFacetColors color)
    {
        Facet colorFacetPrefab = Resources.Load<Facet>("Dax/Prefabs/Pickups/Facet");
        Facet colorFacet = Instantiate<Facet>(colorFacetPrefab, channelNode.transform);
        colorFacet.InitForChannelNode(channelNode, dax);
        ChangeFacetColor(colorFacet, color);        
    }      

    public T CreateBoardObject<T>(ChannelNode channelNode, Dax dax, string prefabString) where T : BoardObject
    {        
        // monote - look into adding the type in code so changing things won't fuck up prefabs
        Debug.Log("CreateBoardObject: " + typeof(T).FullName);
        T prefab = Resources.Load<T>(prefabString);
        T instantiaedObject = Instantiate<T>(prefab, channelNode.transform);
        instantiaedObject.InitForChannelNode(channelNode, dax);
        return instantiaedObject;        
    }

   /* public Hazard CreateHazard(ChannelNode channelNode, Dax dax, Hazard.eHazardType type)
    {                
        string prefabString = "Dax/Prefabs/Hazards/" + Hazard.HAZARD_STRINGS[(int)type];
        
        Hazard hazardPrefab = Resources.Load<Hazard>(prefabString);        
        Hazard hazard = Instantiate<Hazard>(hazardPrefab, channelNode.transform);
        hazard.InitForChannelNode(channelNode, dax);
        return hazard;        
    }*/
    
    

    /*public FacetCollect CreateFacetCollect(ChannelNode channelNode, Dax dax, FacetCollect.eFacetCollectTypes type)
    {
        FacetCollect facetCollectPrefab = null;
        switch (type)
        {
            case FacetCollect.eFacetCollectTypes.RING:
                facetCollectPrefab = Resources.Load<FacetCollect>("Dax/Prefabs/Pickups/Facet_Collects/Facet_Collect_Ring");
                break;
            case FacetCollect.eFacetCollectTypes.WHEEL:
                facetCollectPrefab = Resources.Load<FacetCollect>("Dax/Prefabs/Pickups/Facet_Collects/Facet_Collect_Wheel");
                break;
        }

        FacetCollect facetCollect = Instantiate<FacetCollect>(facetCollectPrefab, channelNode.transform);
        facetCollect.InitForChannelNode(channelNode, dax);
        return facetCollect;
    }*/

    

    /*public Shield CreateShield(ChannelNode channelNode, Dax dax, Shield.eShieldTypes type)
    {
       // Debug.Log("CreateShield() type: " + type.ToString());

        Shield shieldPrefab = null;
        switch (type)
        {   
            case Shield.eShieldTypes.HIT:
                shieldPrefab = Resources.Load<Shield>("Dax/Prefabs/Pickups/Shields/Hit_Shield");
                break;
            case Shield.eShieldTypes.SINGLE_KILL:
                shieldPrefab = Resources.Load<Shield>("Dax/Prefabs/Pickups/Shields/Single_Kill_Shield");
                break;           
        }
        Shield shield = Instantiate<Shield>(shieldPrefab, channelNode.transform);       
        shield.InitForChannelNode(channelNode, dax);
        return shield;        
    }*/

    /*public SpeedMod CreateSpeedMod(ChannelNode channelNode, Dax dax, SpeedMod.eSpeedModType type)
    {
        SpeedMod speedModPrefab = null;
        switch (type) // moupdate - send the Type to the functions instead of a different one for each type
        {   // moupdate - turn this into an array you look up based on index
            case SpeedMod.eSpeedModType.PLAYER_SPEED:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Speed_Mods/Player_Speed");
                break;            
            case SpeedMod.eSpeedModType.ENEMY_SPEED:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Speed_Mods/Enemy_Speed");
                break;           
            case SpeedMod.eSpeedModType.RING_SPEED:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Speed_Mods/Ring_Speed");
                break;          
        }
        SpeedMod speedMod = Instantiate<SpeedMod>(speedModPrefab, channelNode.transform);
        speedMod.InitForChannelNode(channelNode, dax);               
        return speedMod;
    }

    public GameMod CreateGameMod(ChannelNode channelNode, Dax dax, GameMod.eGameModType type)
    {
        GameMod gameModPrefab;
        if (type == GameMod.eGameModType.EXTRA_POINTS)
        {
            gameModPrefab = Resources.Load<GameMod>("Dax/Prefabs/Pickups/Point_Mods/Extra_Points"); // moupdate yeah use switch
        }
        else
        {
            gameModPrefab = Resources.Load<GameMod>("Dax/Prefabs/Pickups/Point_Mods/Points_Multiplier");
        }

        GameMod gameMod = Instantiate<GameMod>(gameModPrefab, channelNode.transform);
        gameMod.InitForChannelNode(channelNode, dax);        
        return gameMod;
    }*/

    
     
                 
    /*public Interactable CreateInteractable(ChannelNode channelNode, Dax dax, Interactable.eInteractableType type)
    {
        Interactable interactablePrefab = null;
        switch(type)
        {
            case Interactable.eInteractableType.TOGGLE:
                interactablePrefab = Resources.Load<Interactable>("Dax/Prefabs/Interactables/Channel_Toggle");
                break;
            case Interactable.eInteractableType.SWITCH:
                interactablePrefab = Resources.Load<Interactable>("Dax/Prefabs/Interactables/Remote_Switch");
                break;
            case Interactable.eInteractableType.WARP_GATE:
                interactablePrefab = Resources.Load<Interactable>("Dax/Prefabs/Interactables/Warp_Gate");
                break;
            case Interactable.eInteractableType.WORMHOLE:
                interactablePrefab = Resources.Load<Interactable>("Dax/Prefabs/Interactables/Wormhole");
                break;
        }
        Interactable interactable = Instantiate<Interactable>(interactablePrefab, channelNode.transform);
        interactable.InitForChannelNode(channelNode, dax);
        interactable.name = channelNode.name + "--Warp Gate"; // moupdate - get this whole init thing done
        return interactable;
    }*/
   

    
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
        GameObject shieldIcon = Instantiate<GameObject>(facetCollectIconPrefab, _Dax._UIRoot.transform);
        return shieldIcon;
    }
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
   
  
    public Material GetFacetMaterial(Facet.eFacetColors color)
    {
        switch (color)
        {
            case Facet.eFacetColors.RED: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Red"));
            case Facet.eFacetColors.GREEN: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Green"));
            case Facet.eFacetColors.BLUE: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Blue"));
            case Facet.eFacetColors.YELLOW: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Yellow"));
            case Facet.eFacetColors.PURPLE: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Purple"));
            //case Facet.eFacetColors.PINK: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Pink"));
            case Facet.eFacetColors.ORANGE: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Orange"));
           // case Facet.eFacetColors.WHITE: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_White"));
            default: Debug.LogError("GetFacetMaterial(): Invalid Bumper color: " + color); return null;
        }
    }
    // moassetstoget
    // bumer matierials None and Death
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

    

    

    
    public void ChangeFacetColor(Facet facet, Facet.eFacetColors color)
    {
        facet._Color = color;
        Material colorFacetMaterial = GetFacetMaterial(color);
        List<MeshRenderer> meshRenderers = facet.GetComponentsInChildren<MeshRenderer>().ToList();
        foreach (MeshRenderer mr in meshRenderers) mr.material = colorFacetMaterial;
    }
        
    public void InitBoardObjectFromSave(BoardObject bo, Dax.BoardObjectSave boSave)
    {
        // get the starting channel
        bo.CurChannel = GameObject.Find(boSave.StartChannel).GetComponent<Channel>();
        bo.StartDir = boSave.StartDir; // monewsave
        bo.Speed = boSave.Speed; // moupdate
                                 // Debug.Log("MCP.InitBoardObject(): " + bo.name + ", bo.CurChannel: " + bo.CurChannel.name);
                                 //if (bo.CurChannel == null) Debug.LogError("dfsd");
        switch (bo.BoardObjectType)
        {
            case BoardObject.eBoardObjectType.SHIELD:
                Shield shield = (Shield)bo;
                shield.ShieldType = (Shield.eShieldTypes)boSave.IntList[0]; // moupdate - check if this is necessary 
               // shield.Timer = boSave.FloatList[0];
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
                hazard.EffectRadius = boSave.FloatList[1];
                if(hazard.HazardType == Hazard.eHazardType.ENEMY)
                { // monewsave
                    hazard.transform.LookAt(bo.StartDir == BoardObject.eStartDir.OUTWARD ? hazard.CurChannel.EndNode.transform : hazard.CurChannel.StartNode.transform);
                }
               // if (hazard.HazardType == Hazard.eHazardType.PROXIMITY_MINE) hazard.GetComponent<SphereCollider>().radius = hazard.EffectRadius;                
                break;
            case BoardObject.eBoardObjectType.GAME_MOD:
                GameMod gameMod = (GameMod)bo;
                gameMod.GameModType = (GameMod.eGameModType)boSave.IntList[0];
                gameMod.GameModVal = boSave.IntList[1];
                gameMod.GameModTime = boSave.FloatList[0];
                break;            
        }
    }
    // public enum eBoardObjectType { PLAYER, FACET, HAZARD, FACET_COLLECT, SHIELD, SPEED_MOD, GAME_MOD };
    public static List<string> PREFAB_ROOT_STRINGS = new List<string> {"Dax/Prefabs/Player_Diode", "Dax/Prefabs/Pickups/Facet/",
        "Dax/Prefabs/Hazards/", "Dax/Prefabs/Pickups/Facet_Collects/", "Dax/Prefabs/Pickups/Shields/", 
        "Dax/Prefabs/Pickups/Speed_Mods/", "Dax/Prefabs/Pickups/Point_Mods/"};
    public void CreateBoardObjectFromSaveData(ChannelNode channelNode, Dax.BoardObjectSave boSave, Dax dax)
    {
        //Debug.Log("Dax.CreateBoardObjectForNode(): " + channelNode.name + ", of type: " + boSave.Type); // moupdate *!!! this is being called 100 times for the player

        string prefabString;
        switch (boSave.Type)
        {                        
            case BoardObject.eBoardObjectType.FACET:
                CreateFacet(channelNode, dax, (Facet.eFacetColors)(Facet.eFacetColors)boSave.IntList[0]);
                break;
            case BoardObject.eBoardObjectType.HAZARD:
                prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.HAZARD] + 
                    Hazard.HAZARD_STRINGS[(int)(Hazard.eHazardType)boSave.IntList[0]];
                CreateBoardObject<Hazard>(channelNode, dax, prefabString);   
                //CreateHazard(channelNode, dax, (Hazard.eHazardType)boSave.IntList[0]);
                break;
            case BoardObject.eBoardObjectType.FACET_COLLECT:                
                //prefabString = "Dax/Prefabs/Pickups/Facet_Collects/" + 
                prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.FACET_COLLECT] +
                    FacetCollect.FACET_COLLECT_STRINGS[(int)(FacetCollect.eFacetCollectTypes)boSave.IntList[0]];
                CreateBoardObject<FacetCollect>(channelNode, dax, prefabString);  
                //CreateFacetCollect(channelNode, dax, (FacetCollect.eFacetCollectTypes)boSave.IntList[0]);
                break;       
            case BoardObject.eBoardObjectType.SHIELD:                                
                prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.SHIELD] + 
                    Shield.SHIELD_STRINGS[(int)(Shield.eShieldTypes)boSave.IntList[0]];
                CreateBoardObject<Shield>(channelNode, dax, prefabString); 
                //CreateShield(channelNode, dax, (Shield.eShieldTypes)boSave.IntList[0]);                
                break;  
            case BoardObject.eBoardObjectType.SPEED_MOD:
                prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.SPEED_MOD] +
                    SpeedMod.SPEED_MOD_STRINGS[(int)(SpeedMod.eSpeedModType)boSave.IntList[0]];
                CreateBoardObject<SpeedMod>(channelNode, dax, prefabString); 
                //SpeedMod speedMod = CreateSpeedMod(channelNode, dax, (SpeedMod.eSpeedModType)boSave.IntList[0]);
                break;       
            case BoardObject.eBoardObjectType.GAME_MOD:
                prefabString = MCP.PREFAB_ROOT_STRINGS[(int)BoardObject.eBoardObjectType.GAME_MOD] +
                    GameMod.GAME_MOD_STRINGS[(int)(GameMod.eGameModType)boSave.IntList[0]];
                CreateBoardObject<GameMod>(channelNode, dax, prefabString);   
                //CreateGameMod(channelNode, dax, (GameMod.eGameModType)boSave.IntList[0]);
                break;         
        }
    }

    public static void ResetTransform(Transform t)
    {
        t.position = Vector3.zero;
        t.eulerAngles = Vector3.zero;
        t.localScale = Vector3.one;
    }    
}