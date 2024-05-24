using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    public static int NUM_OUTER_RING_CHANNELS = 48;
    public static int NUM_CENTER_RING_CHANNELS = 12;

    
    public bool VictoryConditionsMet = false;

    public List<Ring> Rings = new List<Ring>();
    public int NumActiveRings = 4;
    public bool CenterRingLock = false;

    //public List<ChannelNode> StartNodes = new List<ChannelNode>();

    public Dax DaxRef;

    [Header("Wheel Conditions/State")]
    public Dax.eVictoryConditions VictoryCondition = Dax.eVictoryConditions.COLLECTION;    
    public List<int> NumFacetsOnBoard = new List<int>();
    public List<int> NumFacetsCollected = new List<int>();

    //List<int> list = new List<int>(new int[size]);
    // monote - this is bullshit becauase you only need the inner channel empty
    public bool SelectStartingChannel()
    {
        Player player = DaxRef._Player;
        Ring ring0 = Rings[0];
        List<Channel> ring0Channels = ring0.transform.GetComponentsInChildren<Channel>().ToList();
        List<Channel> validStartChannels = new List<Channel>();
        foreach(Channel channel in ring0Channels)
        {
            if (channel.IsChannelEmpty() == true)
            {
               // Debug.Log("we have a valid channel: " + channel.name);
                validStartChannels.Add(channel);
            }
        }

        if (validStartChannels.Count == 0)
        {
            //Debug.LogError("No valid start channels");
            return false;
        }
        else
        {
            //Debug.Log("We have: " + validStartChannels.Count + " valid start channels to choose from.");
            int index = Random.Range(0, validStartChannels.Count);            
            Channel startChannel = validStartChannels[index];            
            player.CurChannel = startChannel;
            player.SpawningNode = startChannel.StartNode;                        
        }

        return true;                       
    }

    public void InitWheelFacets(/*int numWhite, int numRed, int numGreen, int numBlue*/)
    {
        List<Facet> facets = GetComponentsInChildren<Facet>().ToList();
        for (int i = 0; i < NumFacetsOnBoard.Count; i++)
        {
            NumFacetsOnBoard[i] = facets.RemoveAll(x => x._Color == (Facet.eFacetColors)i);
            //Debug.Log("num " + ((BoardObject.eFacetColors)i).ToString() + " on board: " + NumFacetsOnBoard[i]);
        }
       // int x = 5;
       // x++;
    }         
    
    public void CollectPickupFacet(Facet facet)
    {
        //Debug.LogError("CollectPickupFacet()");

        //NumPickupFacetsCollected++;
        NumFacetsCollected[(int)facet._Color]++;
        DaxRef._UIRoot.SetFacetColorText(facet._Color, NumFacetsCollected[(int)facet._Color]);
        DaxRef.AddPoints(5);
        DestroyImmediate(facet.gameObject);
        CheckVictoryConditions();
    }   

    public void MatchedFacetColor(Facet colorFacetCarried) // moupdate - don't send the facet, just the color so it can be destroyed
    {        
        NumFacetsCollected[(int)colorFacetCarried._Color]++;
        DaxRef.AddPoints(5);
        DaxRef._Player.CarriedColorFacet = null;
        DaxRef._UIRoot.SetFacetColorText(colorFacetCarried._Color, NumFacetsCollected[(int)colorFacetCarried._Color]);       
    }

    public bool CheckVictoryConditions()
    {       
        bool gameWon = true;

       /* switch (VictoryCondition)
        {
            case Dax.eVictoryConditions.COLLECTION:
                if (NumFacetsCollected[(int)Facet.eFacetColors.WHITE] == NumFacetsOnBoard[(int)Facet.eFacetColors.WHITE])
                {
                    gameWon = true;
                }
                    break;
            case Dax.eVictoryConditions.COLOR_MATCH:                
                gameWon = true;
                for(int i = 0; i < NumFacetsCollected.Count-1; i++)
                {                   
                    if (NumFacetsCollected[i] != NumFacetsOnBoard[i])
                    {
                        gameWon = false;
                        break;
                    }
                    
                }
                break;               
        }*/        
        for(int i = 0; i < NumFacetsCollected.Count-1; i++)
        {                   
            if (NumFacetsCollected[i] != NumFacetsOnBoard[i])
            {
                gameWon = false;
                break;
            }
            
        }

        if(gameWon == true)
        {
            DaxRef.EndGame("You won!!!");
           /* if(this == DaxRef.Wheels[0] )
            {
                // Debug.Log("You won the wheel, and since it's the last wheel you win.  This is TEMP until I get the other game modes in.");
                //DaxRef.GameState = Dax.eGameState.GAME_OVER;
                DaxRef.EndGame("You won!!!");
              //  DaxRef.ResetTimer = 0f;
            }
            else
            {
                Debug.LogError("Move onto the next wheel");
#if false
                int curWheelIndex = DaxRef.Wheels.IndexOf(DaxRef.CurWheel);
                DaxRef.SetCurrentWheel(DaxRef.Wheels[curWheelIndex-1]);
                DaxRef.CurWheel.SelectStartingChannel();
                DaxRef.Player.ResetSpawningPosition();
#endif
            }    */        
        }       
        
        return gameWon;        
       // return false;
    }
        

    public void TurnOnRings(int numRings)
    {
       // Debug.Log("Wheel.TurnOnRings(): " + numRings + " --MoSave--");
        if (numRings < 1 || numRings > 4) { Debug.LogError("ERROR: invalid number of rings: " + numRings); return; }

        // GetComponentInParent<DaxSetup>().NumRings = numRings;
        NumActiveRings = numRings;
        for (int i = 1; i <= 4; i++)
        {
            Rings[i].Toggle(i <= numRings ? true : false, i == numRings);
        }

        // set up the camera position        
        Camera.main.transform.position = new Vector3(0f, DaxPuzzleSetup.CAMERA_Y_VALUES[numRings-1], 0f);
    }

    public Ring GetOuterRing()
    {
        // return Rings[DaxRef.GetComponent<DaxSetup>().NumRings];
        return Rings[NumActiveRings];
    }
}