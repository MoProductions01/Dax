using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// This class holds all of the classes for the game's various save data
/// </summary>
public class DaxSaveData
{   
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
        public Dax.eVictoryConditions VictoryCondition;
        
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
            Player player = GameObject.FindFirstObjectByType<Player>();
            PlayerSave = new BoardObjectSave(player);
        }
    }    
}
