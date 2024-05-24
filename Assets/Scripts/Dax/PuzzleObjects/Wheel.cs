using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEngine;

/// <summary>
/// The Wheel is the game board holding all of the rings
/// </summary>
public class Wheel : MonoBehaviour
{
    // The center ring has a differennt number of channels than the rest
    public static int NUM_OUTER_RING_CHANNELS = 48;
    public static int NUM_CENTER_RING_CHANNELS = 12;
        
    // Ring data
    public List<Ring> Rings = new List<Ring>();
    public int NumActiveRings = 4;
    public bool CenterRingLock = false;    

    public Dax DaxRef; // monote - figure out the naming conventions

    [Header("Wheel Conditions/State")]
    public Dax.eVictoryConditions VictoryCondition = Dax.eVictoryConditions.COLLECTION;    
    public bool VictoryConditionsMet = false;
    public List<int> NumFacetsOnBoard = new List<int>();
    public List<int> NumFacetsCollected = new List<int>();
    

    /// <summary>
    /// Grabs all the facets on the wheel and gets the number of each color
    /// </summary>
    public void ResetFacetsCount()
    {
        List<Facet> facets = GetComponentsInChildren<Facet>().ToList();
        for (int i = 0; i < NumFacetsOnBoard.Count; i++)
        {
            NumFacetsOnBoard[i] = facets.RemoveAll(x => x._Color == (Facet.eFacetColors)i);            
        }       
    }         
    
    /// <summary>
    /// Handles the Player collecting a facet from the board
    /// </summary>
    /// <param name="facet"></param>
    public void CollectFacet(Facet facet)
    {        
        NumFacetsCollected[(int)facet._Color]++; // update number collected for this color
        DaxRef._UIRoot.SetFacetColorText(facet._Color, NumFacetsCollected[(int)facet._Color]); // update UI
        DaxRef.AddPoints(5);
        DestroyImmediate(facet.gameObject);
        CheckVictoryConditions(); // Check the victory conditions each time you collect a facet
    }   

    /// <summary>
    /// Handles matching a facet to the same color bumper.  Victory conditions are checked where this is called from
    /// </summary>
    /// <param name="colorFacetCarried"></param>
    public void MatchedFacetColor(Facet colorFacetCarried) 
    {        
        NumFacetsCollected[(int)colorFacetCarried._Color]++;
        DaxRef.AddPoints(5);
        DaxRef._Player.CarriedColorFacet = null;
        DaxRef._UIRoot.SetFacetColorText(colorFacetCarried._Color, NumFacetsCollected[(int)colorFacetCarried._Color]);       
    }

    /// <summary>
    /// Checks the victory conditions.  Both game modes are the same in that you 
    /// collect the same number of facets whether you collect them or match them to a bumper
    /// </summary>
    /// <returns></returns>
    public bool CheckVictoryConditions()
    {       
        bool gameWon = true;    // default to game won
         
        // If any of the number of facets to collect for each color isn't what
        // it's supposed to be it's not a victory yet
        for(int i = 0; i < NumFacetsCollected.Count-1; i++)
        {                   
            if (NumFacetsCollected[i] != NumFacetsOnBoard[i])
            {
                gameWon = false;
                break;
            }            
        }
        
        // Let the game know if you won
        if(gameWon == true)
        {
            DaxRef.EndGame("You won!!!");                
        }       
        
        return gameWon;               
    }
        
    /// <summary>
    /// Turns on the number of rings
    /// </summary>
    /// <param name="numRings"></param>
    public void TurnOnRings(int numRings)
    {       
        if (numRings < 1 || numRings > 4) { Debug.LogError("ERROR: invalid number of rings: " + numRings); return; }

        NumActiveRings = numRings;
        for (int i = 1; i <= 4; i++)
        {   // Toggle the ring on/off based on whether it's below the number of rings on the wheel
            Rings[i].Toggle(i <= numRings ? true : false, i == numRings);
        }

        // set up the camera position based on number of rings    
        Camera.main.transform.position = new Vector3(0f, DaxPuzzleSetup.CAMERA_Y_VALUES[numRings-1], 0f);
    }   
}