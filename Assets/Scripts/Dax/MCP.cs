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
    public void SavePuzzle() // moupdate - why are there two save puzzles
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

    static void TrashCurrentPuzzle()
    {
        DaxSetup daxSetup = GameObject.FindObjectOfType<DaxSetup>();
        if (daxSetup != null) UnityEngine.Object.DestroyImmediate(daxSetup.gameObject);
        Dax dax = GameObject.FindObjectOfType<Dax>();
        if (dax != null) UnityEngine.Object.DestroyImmediate(dax.gameObject);
        UIRoot uiRoot = GameObject.FindObjectOfType<UIRoot>();
        if (uiRoot != null) UnityEngine.Object.DestroyImmediate(uiRoot.gameObject);
    }

    public static void CreateNewPuzzle()
    {
      //  Debug.Log("MCP.CreateNewPuzzle()");
        // First trash old puzzle
        TrashCurrentPuzzle();

        // Create an MCP instance if necessary
        MCP mcp = GameObject.FindObjectOfType<MCP>();
        if (mcp == null)
        {
            GameObject mcpGO = new GameObject("MCP");
            ResetTransform(mcpGO.transform);
            mcp = mcpGO.AddComponent<MCP>();
        }
        else
        {
            mcp._Dax = null;
        }
        // same with DaxEditControl
        DaxEditControl daxEditControl = GameObject.FindObjectOfType<DaxEditControl>();
        if (daxEditControl == null)
        {
            GameObject decGO = new GameObject("Dax Edit Control");
            MCP.ResetTransform(decGO.transform);
            daxEditControl = decGO.AddComponent<DaxEditControl>();
            daxEditControl.gameObject.AddComponent<DaxSetup>();
        }

        mcp._Dax = new GameObject("Dax").AddComponent<Dax>();
        daxEditControl.GetComponent<DaxSetup>()._Dax = mcp._Dax;

        // start off with just one wheel       
        Wheel wheel = CreateWheel(mcp, mcp._Dax.gameObject, mcp._Dax, 0);
        mcp._Dax.CurWheel = wheel;

        // set up the camera
        Camera mainCamera = Camera.main;
        mainCamera.transform.position = new Vector3(0f, DaxSetup.CAMERA_Y_VALUES[3], 0f);
        mainCamera.transform.eulerAngles = new Vector3(90f, 0f, 0f);        
        mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));

        // load up the UI
        UIRoot uiRootPrefab = Resources.Load<UIRoot>("Dax/Prefabs/UI/UI Root");
        mcp._UIRoot = UnityEngine.Object.Instantiate<UIRoot>(uiRootPrefab);

        // create player and debug canvas
        Player playerPrefab = Resources.Load<Player>("Dax/Prefabs/Player_Diode");
        Player player = UnityEngine.Object.Instantiate<Player>(playerPrefab, mcp._Dax.transform);        
        player.name = "Player Diode";        
        mcp._Dax._Player = player;
        mcp._Dax._Player._Dax = mcp._Dax;
        player.SetStartChannel(0);        
        player.InitForChannelNode(null, mcp._Dax);
        player.ResetForPuzzleRestart();
        //Debug.Log($"parent: {wheel.StartNodes[0].transform.parent.name}");
        //wheel.StartNodes[0].transform.parent.GetComponent<Channel>().InnerChannel.Toggle();       
        
        // debug          
        RifRafDebug rifRafDebugPrefab = Resources.Load<RifRafDebug>("_RifRafDebug");
        RifRafDebug rifRafDebug = UnityEngine.Object.Instantiate<RifRafDebug>(rifRafDebugPrefab, mcp._Dax.gameObject.transform);
        RRDManager.Init(rifRafDebug);
        daxEditControl.GetComponent<DaxSetup>().RifRafDebugRef = rifRafDebug;        
    }

    public static Wheel CreateWheel(MCP mcp, GameObject rootGO, Dax daxRef, int wheelNum)
    {
        GameObject go = new GameObject("Wheel_" + wheelNum.ToString("D2"));
        MCP.ResetTransform(go.transform);
        go.transform.parent = rootGO.gameObject.transform;
        Wheel wheel = go.AddComponent<Wheel>();

        wheel.NumFacetsOnBoard = new List<int>(new int[((int)Facet.eFacetColors.WHITE) + 1]);
        wheel.NumFacetsCollected = new List<int>(new int[((int)Facet.eFacetColors.WHITE) + 1]);        
        wheel.DaxRef = daxRef;

        GameObject ringColliderPrefab = null;
        GameObject ringGameObject = null;
        GameObject bumperGroup = null;
        GameObject nodesContainer = null;
        GameObject locatorsPrefab = Resources.Load<GameObject>("Dax/Board_Parts/DAX_Locators");
        GameObject locators = GameObject.Instantiate<GameObject>(locatorsPrefab);
        GameObject nodesPrefab = Resources.Load<GameObject>("Dax/Board_Parts/Nodes");
        GameObject nodes = GameObject.Instantiate<GameObject>(nodesPrefab);

        wheel.Rings.Clear();
        // Create rings
        for (int ringIndex = 0; ringIndex <= 4; ringIndex++)
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
            if(ringIndex != 0) ringGameObject.layer = LayerMask.NameToLayer("Main Touch Control");
            ringGameObject.AddComponent<Ring>();

            wheel.Rings.Add(ringGameObject.GetComponent<Ring>());

            if (ringIndex != 0)
            {
                // Bumpers
                bumperGroup = new GameObject("Ring_" + ringIndex.ToString("D2") + "_Bumpers");
                bumperGroup.transform.parent = wheel.gameObject.transform;
                bumperGroup.AddComponent<BumperGroup>();
            }

            // nodes
            nodesContainer = nodes.transform.Find(ringString + "_Nodes").gameObject;

            int numChannels = (ringIndex == 0 ? Wheel.NUM_CENTER_RING_CHANNELS : Wheel.NUM_OUTER_RING_CHANNELS);

            for (int i = 0; i < numChannels; i++)
            {
                string channelString = (i + 1).ToString("D2");
                string nodeLocatorChannelString = channelString;
                //if (i != 0) nodeLocatorChannelString = (((numChannels) - i) + 1).ToString("D2");

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
                    bumper.gameObject.GetComponent<MeshRenderer>().material = mcp.GetBumperMaterial(Bumper.eBumperType.REGULAR, Facet.eFacetColors.WHITE);                    
                }

                // CHANNEL CONTAINER
                GameObject channel = new GameObject(ringString + "_Channel_" + channelString);
                channel.transform.parent = ringGameObject.transform;
                channel.AddComponent<Channel>();

                // INNER FLOOR
                locatorIndex = i + (2 * numChannels);
                if (ringIndex == 0) locatorIndex -= numChannels;
                GameObject innerFloorLocator = locatorGroup.transform.GetChild(locatorIndex).gameObject;
                GameObject innerFloorPrefab = Resources.Load<GameObject>("Dax/Board_Parts/" + ringString + "_Inner_Floor_01");
                GameObject innerFloor = GameObject.Instantiate<GameObject>(innerFloorPrefab, innerFloorLocator.transform);
                innerFloor.transform.parent = channel.transform;
                innerFloor.name = ringString + "_Inner_Floor_" + channelString;
                innerFloor.gameObject.GetComponent<MeshRenderer>().enabled = false;

                // INNER CHANNEL
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
                // if (channelString.Contains("01")) Debug.Log(innerChannel.name + ", " + innerChannel.GetComponent<MeshCollider>().bounds.extents.ToString("F6"));                                    
                innerChannel.AddComponent<ChannelPiece>();

                // OUTER CHANNEL
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
                //if (channelString.Contains("01")) Debug.Log(outerChannel.name + ", " + outerChannel.GetComponent<MeshCollider>().bounds.extents.ToString("F6"));
                outerChannel.AddComponent<ChannelPiece>();

                // START NODE
                GameObject startNodePrefab = nodesContainer.transform.Find(ringString + "_Start_Node_" + nodeLocatorChannelString).gameObject;
                GameObject startNode = GameObject.Instantiate<GameObject>(startNodePrefab);
                startNode.transform.parent = channel.transform;
                startNode.transform.name = ringString + "_Start_Node_" + channelString;
                startNode.AddComponent<ChannelNode>();

                // MIDDLE NODE
                GameObject middleNodePrefab = nodesContainer.transform.Find(ringString + "_Middle_Node_" + nodeLocatorChannelString).gameObject;
                GameObject middleNode = GameObject.Instantiate<GameObject>(middleNodePrefab);
                middleNode.transform.parent = channel.transform;
                middleNode.transform.name = ringString + "_Middle_Node_" + channelString;
                middleNode.AddComponent<ChannelNode>();

                // END NODE
                GameObject endNodePrefab = nodesContainer.transform.Find(ringString + "_End_Node_" + nodeLocatorChannelString).gameObject;
                GameObject endNode = GameObject.Instantiate<GameObject>(endNodePrefab);
                endNode.transform.parent = channel.transform;
                endNode.transform.name = ringString + "_End_Node_" + channelString;
                endNode.AddComponent<ChannelNode>();

                // OUTER FLOOR
                locatorIndex = i + (4 * numChannels);
                if (ringIndex == 0) locatorIndex -= numChannels;
                GameObject outerFloorLocator = locatorGroup.transform.GetChild(locatorIndex).gameObject;
                GameObject outerFloorPrefab = Resources.Load<GameObject>("Dax/Board_Parts/" + ringString + "_Outer_Floor_01");
                GameObject outerFloor = GameObject.Instantiate<GameObject>(outerFloorPrefab, outerFloorLocator.transform);
                outerFloor.transform.parent = channel.transform;
                outerFloor.name = ringString + "_Outer_Floor_" + channelString;
                outerFloor.gameObject.GetComponent<MeshRenderer>().enabled = false;


            }
            // do wedges separate so they are sorted correctly
            for (int i = 0; i < numChannels; i++)
            {
                string channelString = (i + 1).ToString("D2");
                // WEDGE
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

        // Init rings
        for (int ringIndex = 0; ringIndex <= 4; ringIndex++)
        {
            Ring ring = wheel.Rings[ringIndex];

            ring.DaxRef = daxRef;
            if (ringIndex != 0)
            {
                ring.BumperGroup = wheel.gameObject.transform.Find(ring.name + "_Bumpers").GetComponent<BumperGroup>();
                ring.BumperGroup.Bumpers = ring.BumperGroup.GetComponentsInChildren<Bumper>().ToList();
            }
            ring.RotateSpeed = 0f;//(ringIndex % 2 == 0 ? 10f : -10f);

            // channels            
            List<Channel> ringChannels = ring.transform.GetComponentsInChildren<Channel>().ToList();
            ringChannels.OrderBy(x => x.name);
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

        // Gather start nodes        
        Debug.Log("Gather start nodes");
        wheel.StartNodes = wheel.transform.transform.GetComponentsInChildren<ChannelNode>().ToList();
        wheel.StartNodes.RemoveAll(x => x.name.Contains("Ring_00_Start_Node") == false);        
        wheel.StartNodes = wheel.StartNodes.OrderBy(o=>o.name).ToList();        

        // Make sure the channel diodes are pointing at each other
        List<Channel> channels = wheel.GetComponentsInChildren<Channel>().ToList();
        foreach (Channel channel in channels) channel.HaveNodesLookAtEachOther();

        UnityEngine.Object.DestroyImmediate(locators);
        UnityEngine.Object.DestroyImmediate(nodes);

        //daxRef.Wheels.Add(wheel);        
        wheel.TurnOnRings(4);

        return wheel;
    }

    public Hazard CreateHazard(ChannelNode channelNode, Dax dax, Hazard.eHazardType type)
    {
        Hazard hazardPrefab = null;
        switch(type)
        {
            case Hazard.eHazardType.ENEMY:
                hazardPrefab = Resources.Load<Hazard>("Dax/Prefabs/Hazards/Enemy_Diode");
                break;
            case Hazard.eHazardType.EMP:
                hazardPrefab = Resources.Load<Hazard>("Dax/Prefabs/Hazards/EMP");
                break;
           // case Hazard.eHazardType.BOMB:
             //   hazardPrefab = Resources.Load<Hazard>("Dax/Prefabs/Hazards/Bomb");
             //   break;
            case Hazard.eHazardType.DYNAMITE:
                hazardPrefab = Resources.Load<Hazard>("Dax/Prefabs/Hazards/Dynamite");
                break;
           // case Hazard.eHazardType.PROXIMITY_MINE:
           //     hazardPrefab = Resources.Load<Hazard>("Dax/Prefabs/Hazards/Proximity_Mine");
            //    break;
          //  case Hazard.eHazardType.TIMED_MINE:
           //     hazardPrefab = Resources.Load<Hazard>("Dax/Prefabs/Hazards/Timed_Mine");
           //     break;                
        }
        Hazard hazard = Instantiate<Hazard>(hazardPrefab, channelNode.transform);
        hazard.InitForChannelNode(channelNode, dax);
        return hazard;
        //Enemy enemyPrefab = Resources.Load<Enemy>("Dax/Prefabs/Hazards/Enemy_Diode");
        // Enemy enemy = Instantiate<Enemy>(enemyPrefab, channelNode.transform);
        // enemy.InitForChannelNode(channelNode, dax);
        // return enemy;        
    }
    public Interactable CreateInteractable(ChannelNode channelNode, Dax dax, Interactable.eInteractableType type)
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
    }
    public Magnet CreateMagnet(ChannelNode channelNode, Dax dax, Magnet.eMagnetTypes type)
    {
        Magnet magnetPrefab = null;
        switch (type)
        {
            case Magnet.eMagnetTypes.REGULAR:
                magnetPrefab = Resources.Load<Magnet>("Dax/Prefabs/Pickups/Magnet");
                break;
            case Magnet.eMagnetTypes.SUPER:
                magnetPrefab = Resources.Load<Magnet>("Dax/Prefabs/Pickups/Super_Magnet");
                break;
        }

        Magnet magnet = Instantiate<Magnet>(magnetPrefab, channelNode.transform);
        magnet.InitForChannelNode(channelNode, dax);
        return magnet;
    }

    public SpeedMod CreateSpeedMod(ChannelNode channelNode, Dax dax, SpeedMod.eSpeedModType type)
    {
        SpeedMod speedModPrefab = null;
        switch (type) // moupdate - send the Type to the functions instead of a different one for each type
        {   // moupdate - turn this into an array you look up based on index
            case SpeedMod.eSpeedModType.SPEED_UP:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Speed_Up");
                break;
            case SpeedMod.eSpeedModType.SPEED_DOWN:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Speed_Down");
                break;
            case SpeedMod.eSpeedModType.ENEMY_UP:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Enemy_Speed_Up");
                break;
            case SpeedMod.eSpeedModType.ENEMY_DOWN:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Enemy_Speed_Down");
                break;
            case SpeedMod.eSpeedModType.RING_UP:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Ring_Speed_Up");
                break;
            case SpeedMod.eSpeedModType.RING_DOWN:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Ring_Speed_Down");
                break;
            case SpeedMod.eSpeedModType.WHEEL_UP:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Wheel_Speed_Up");
                break;
            case SpeedMod.eSpeedModType.WHEEL_DOWN:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Wheel_Speed_Down");
                break;
            case SpeedMod.eSpeedModType.RING_STOP:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Ring_Stop");
                break;
            case SpeedMod.eSpeedModType.TIME_STOP:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Time_Stop");
                break;
            case SpeedMod.eSpeedModType.RING_REVERSE:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Ring_Reverse");
                break;
            case SpeedMod.eSpeedModType.MEGA_RING_REVERSE:
                speedModPrefab = Resources.Load<SpeedMod>("Dax/Prefabs/Pickups/Mega_Ring_Reverse");
                break;
        }
        SpeedMod speedMod = Instantiate<SpeedMod>(speedModPrefab, channelNode.transform);
        speedMod.InitForChannelNode(channelNode, dax);               
        return speedMod;
    }
    public GameObject CreateMagnetIcon( Magnet.eMagnetTypes type)
    {
        GameObject magnetIconPrefab = null;
        switch (type)
        {
            case Magnet.eMagnetTypes.REGULAR:
                magnetIconPrefab = Resources.Load<GameObject>("Dax/Prefabs/Pickups/HUD_Items/Magnet_HUD");
                break;
            case Magnet.eMagnetTypes.SUPER:
                magnetIconPrefab = Resources.Load<GameObject>("Dax/Prefabs/Pickups/HUD_Items/Super_Magnet_HUD");
                break;
        }
        GameObject shieldIcon = Instantiate<GameObject>(magnetIconPrefab, _Dax._UIRoot.transform);
        return shieldIcon;
    }
    public GameObject CreateShieldIcon(Shield.eShieldTypes type)
    {
        GameObject shieldIconPrefab = null;
        switch (type)
        {   // 
            case Shield.eShieldTypes.HIT:
                shieldIconPrefab = Resources.Load<GameObject>("Dax/Prefabs/Pickups/HUD_Items/Hit_Shield_HUD");
                break;
            case Shield.eShieldTypes.SINGLE_KILL:
                shieldIconPrefab = Resources.Load<GameObject>("Dax/Prefabs/Pickups/HUD_Items/Single_Kill_Shield_HUD");
                break;
            case Shield.eShieldTypes.TIMED:
                shieldIconPrefab = Resources.Load<GameObject>("Dax/Prefabs/Pickups/HUD_Items/Timed_Shield_HUD");
                break;
            case Shield.eShieldTypes.TIMED_KILL:
                shieldIconPrefab = Resources.Load<GameObject>("Dax/Prefabs/Pickups/HUD_Items/Timed_Kill_Shield_HUD");
                break;
        }
        GameObject shieldIcon = Instantiate<GameObject>(shieldIconPrefab, _Dax._UIRoot.transform);
        return shieldIcon;       
    }    
    public Shield CreateShield(Transform parent, Shield.eShieldTypes type)
    {
        Debug.Log("CreateShield() type: " + type.ToString());

        Shield shieldPrefab = null;
        switch (type)
        {   
            case Shield.eShieldTypes.HIT:
                shieldPrefab = Resources.Load<Shield>("Dax/Prefabs/Pickups/Hit_Shield");
                break;
            case Shield.eShieldTypes.SINGLE_KILL:
                shieldPrefab = Resources.Load<Shield>("Dax/Prefabs/Pickups/Single_Kill_Shield");
                break;
            case Shield.eShieldTypes.TIMED:
                shieldPrefab = Resources.Load<Shield>("Dax/Prefabs/Pickups/Timed_Shield");
                break;
            case Shield.eShieldTypes.TIMED_KILL:
                shieldPrefab = Resources.Load<Shield>("Dax/Prefabs/Pickups/Timed_Kill_Shield");
                break;
        }
        Shield shield = Instantiate<Shield>(shieldPrefab, parent);
        shield.name = transform.name + "--" + type.ToString();
        //shield.InitForChannelNode(channelNode, dax);        
        return shield;        
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
            case Facet.eFacetColors.PINK: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Pink"));
            case Facet.eFacetColors.ORANGE: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Orange"));
            case Facet.eFacetColors.WHITE: return Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_White"));
            default: Debug.LogError("GetFacetMaterial(): Invalid Bumper color: " + color); return null;
        }
    }

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

   /* public GameMod CreateGameMod(ChannelNode channelNode, Dax dax, GameMod.eGameModType type)
    {
        GameMod gameModPrefab;
        if (type == GameMod.eGameModType.EXTRA_POINTS)
        {
            gameModPrefab = Resources.Load<GameMod>("Dax/Prefabs/Pickups/Extra_Points"); // moupdate yeah use switch
        }
        else
        {
            gameModPrefab = Resources.Load<GameMod>("Dax/Prefabs/Pickups/Points_Multiplier");
        }

        GameMod gameMod = Instantiate<GameMod>(gameModPrefab, channelNode.transform);
        gameMod.InitForChannelNode(channelNode, dax);
        gameMod.GameModType = type;
        return gameMod;
    }    */

    

    
    public void ChangeFacetColor(Facet facet, Facet.eFacetColors color)
    {
        facet._Color = color;
        Material colorFacetMaterial = GetFacetMaterial(color);
        List<MeshRenderer> meshRenderers = facet.GetComponentsInChildren<MeshRenderer>().ToList();
        foreach (MeshRenderer mr in meshRenderers) mr.material = colorFacetMaterial;
    }
    public void CreateFacet(ChannelNode channelNode, Dax dax, Facet.eFacetColors color)
    {
        Facet colorFacetPrefab = Resources.Load<Facet>("Dax/Prefabs/Pickups/Facet");
        Facet colorFacet = Instantiate<Facet>(colorFacetPrefab, channelNode.transform);
        colorFacet.InitForChannelNode(channelNode, dax);
        ChangeFacetColor(colorFacet, color);        
    }        
    public void InitBoardObjectFromSave(BoardObject bo, Dax.BoardObjectSave boSave)
    {
        // get the starting channel
        bo.CurChannel = GameObject.Find(boSave.StartChannel).GetComponent<Channel>();
        bo.MoveDir = boSave.MoveDir;
        bo.Speed = boSave.Speed; // moupdate
                                 // Debug.Log("MCP.InitBoardObject(): " + bo.name + ", bo.CurChannel: " + bo.CurChannel.name);
                                 //if (bo.CurChannel == null) Debug.LogError("dfsd");
        switch (bo.BoardObjectType)
        {
            case BoardObject.eBoardObjectType.SHIELD:
                Shield shield = (Shield)bo;
                shield.ShieldType = (Shield.eShieldTypes)boSave.IntList[0]; // moupdate - check if this is necessary 
                shield.Timer = boSave.FloatList[0];
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
               // if (hazard.HazardType == Hazard.eHazardType.PROXIMITY_MINE) hazard.GetComponent<SphereCollider>().radius = hazard.EffectRadius;                
                break;
            /*case BoardObject.eBoardObjectType.GAME_MOD:
                GameMod gameMod = (GameMod)bo;
                gameMod.GameModType = (GameMod.eGameModType)boSave.IntList[0];
                gameMod.GameModVal = boSave.IntList[1];
                gameMod.GameModTime = boSave.FloatList[0];
                break;*/
            case BoardObject.eBoardObjectType.INTERACTABLE:
                Interactable interactable = (Interactable)bo;
                interactable.InteractableType = (Interactable.eInteractableType)boSave.IntList[0];
                if (interactable.InteractableType == Interactable.eInteractableType.SWITCH)
                {
                    //Switch _switch = (Switch)bo;
                    //string s = "SWITCH named: " + switchDiode.name + ": ";
                    foreach (string name in boSave.StringList01)
                    {
                        //  s += ", turn off: " + name;
                        ChannelPiece offPiece = GameObject.Find(name).GetComponent<ChannelPiece>();
                        interactable.PiecesToTurnOff.Add(offPiece);
                    }
                    foreach (string name in boSave.StringList02)
                    {
                        //  s += ", turn on: " + name;
                        ChannelPiece onPiece = GameObject.Find(name).GetComponent<ChannelPiece>();
                        interactable.PiecesToTurnOn.Add(onPiece);
                    }
                    // Debug.Log(s);
                }
                else if (interactable.InteractableType == Interactable.eInteractableType.WARP_GATE)
                {
                    foreach (string name in boSave.StringList01)
                    {
                        GameObject go = GameObject.Find(name);
                        Interactable destGate = GameObject.Find(name).GetComponent<Interactable>();
                        interactable.DestGates.Add(destGate);
                    }
                }
                break;
        }
    }

    public void CreateBoardObjectFromSaveData(ChannelNode channelNode, Dax.BoardObjectSave boSave, Dax dax)
    {
        //Debug.Log("Dax.CreateBoardObjectForNode(): " + channelNode.name + ", of type: " + boSave.Type); // moupdate *!!! this is being called 100 times for the player

        switch (boSave.Type)
        {
            case BoardObject.eBoardObjectType.FACET:
                CreateFacet(channelNode, dax, (Facet.eFacetColors)(Facet.eFacetColors)boSave.IntList[0]);
                break;
            case BoardObject.eBoardObjectType.HAZARD:
                CreateHazard(channelNode, dax, (Hazard.eHazardType)boSave.IntList[0]);
                break;
           // case BoardObject.eBoardObjectType.GAME_MOD:
             //   CreateGameMod(channelNode, dax, (GameMod.eGameModType)boSave.IntList[0]);
               // break;
            case BoardObject.eBoardObjectType.INTERACTABLE:
                Interactable interactable = CreateInteractable(channelNode, dax, (Interactable.eInteractableType)boSave.IntList[0]);
                break;
            case BoardObject.eBoardObjectType.SHIELD:                
                Shield shield = CreateShield(channelNode.transform, (Shield.eShieldTypes)boSave.IntList[0]);
                shield.InitForChannelNode(channelNode, dax);
                break;
            case BoardObject.eBoardObjectType.SPEED_MOD:
                SpeedMod speedMod = CreateSpeedMod(channelNode, dax, (SpeedMod.eSpeedModType)boSave.IntList[0]);
                break;            
            case BoardObject.eBoardObjectType.MAGNET:                
                CreateMagnet(channelNode, dax, (Magnet.eMagnetTypes)boSave.IntList[0]);
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