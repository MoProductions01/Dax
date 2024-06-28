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
    [field: SerializeField] public List<Ring> Rings {get; set;} = new List<Ring>();
    [field: SerializeField] public int NumActiveRings {get; set;} = Dax.MAX_NUM_RINGS;       

    [field: SerializeField] public Dax Dax {get; set;} // Referernce to the root game object
    
    // For a while we had multiple wheels each with their own victory conditions.  Now
    // we're just using one wheel but so much code was written I'm keeping it here for
    // now until I get a chance to update
    [field: SerializeField] public Dax.eVictoryConditions VictoryCondition {get; set;} = Dax.eVictoryConditions.COLLECTION;        
    [field: SerializeField] public List<int> NumFacetsOnBoard {get; set;} = new List<int>();
    [field: SerializeField] public List<int> NumFacetsCollected {get; set;} = new List<int>();
    

    /// <summary>
    /// Grabs all the facets on the wheel and gets the number of each color
    /// </summary>
    public void ResetFacetsCount()
    {
        // modelete - I'm not sure if this funciton does anything useful
        List<Facet> facets = GetComponentsInChildren<Facet>().ToList();
        for (int i = 0; i < NumFacetsOnBoard.Count; i++)
        {
            NumFacetsOnBoard[i] = facets.RemoveAll(x => x._Color == (Facet.eFacetColors)i);            
        }       
    }         
    
    /// <summary>
    /// Handles the Player collecting a facet from the board
    /// </summary>
    /// <param name="facet">Facet collected</param>
    public void CollectFacet(Facet facet)
    {     
        //FindObjectOfType<VFX>().PlayFacetVFX(facet._Color, facet.transform.position);   // modelete - get a ref to VFX or make it static
        VFXPlayer.PlayFacetVFX(facet._Color, facet.transform.position);
        SoundFXPlayer.PlaySoundFX("FacetPickup", .8f);
        NumFacetsCollected[(int)facet._Color]++; // update number collected for this color
        Dax.UIRoot.SetFacetColorText(facet._Color, NumFacetsCollected[(int)facet._Color]); // update UI
        Dax.AddPoints(5);
        DestroyImmediate(facet.gameObject);        
        CheckVictoryConditions(); // Check the victory conditions each time you collect a facet
    }   

    /// <summary>
    /// Handles matching a facet to the same color bumper.  Victory conditions are checked where this is called from
    /// </summary>
    /// <param name="colorFacetCarried"></param>
    public void MatchedFacetColor(Facet colorFacetCarried) 
    {        
        //DestroyImmediate(playerCarriedColorFacet.gameObject); modelete
        CollectFacet(colorFacetCarried);
        //NumFacetsCollected[(int)colorFacetCarried._Color]++;
        //Dax.AddPoints(5);
        Dax.Player.CarriedFacet = null;
        //Dax.UIRoot.SetFacetColorText(colorFacetCarried._Color, NumFacetsCollected[(int)colorFacetCarried._Color]);       
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
            Dax.EndGame("You won!!!", true);                
        }       
        
        return gameWon;               
    }
        
    /// <summary>
    /// Turns on the number of rings
    /// </summary>
    /// <param name="numRings"></param>
    public void TurnOnRings(int numRings)
    {       
        if (numRings < 1 || numRings > Dax.MAX_NUM_RINGS) { Debug.LogError("ERROR: invalid number of rings: " + numRings); return; }

        NumActiveRings = numRings;
        for (int i = 1; i <= Dax.MAX_NUM_RINGS; i++)
        {   // Toggle the ring on/off based on whether it's below the number of rings on the wheel
            Rings[i].Toggle(i <= numRings ? true : false, i == numRings);
        }

        // set up the camera position based on number of rings    
        // modelete - look into the camera/dax offset
        Camera.main.transform.position = new Vector3(-10f, DaxPuzzleSetup.CAMERA_Y_VALUES[numRings-1], 0f);
    }   
}