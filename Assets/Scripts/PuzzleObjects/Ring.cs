using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// There's 1-4 rings on the wheel to make up the puzzle
/// </summary>
public class Ring : MonoBehaviour
{
    public Dax DaxRef = null; // Reference to the root gameplay Dax object
    public float RotateSpeed = 10f; // How fast it's rotating    
    public BumperGroup BumperGroup = null; // Rings can have a bumper group only if it's the outer ring

    /// <summary>
    /// Rotates the ring
    /// </summary>
    public void Rotate()
    {
        transform.Rotate(new Vector3(0f, RotateSpeed * Time.deltaTime, 0f));
    }

    /// <summary>
    /// Collect all the facets on this ring.  Used by powerups.
    /// </summary>
    public void CollectAllFacets()
    {        
        // Get all the facets on this ring and collect them
        List<Facet> collectFacets = this.GetComponentsInChildren<Facet>().ToList();        
        foreach(Facet facet in collectFacets)
        {
            this.DaxRef.Wheel.CollectFacet(facet);
        }        
    }

    /// <summary>
    /// Toggles the ring on/off and the bumper group if necessary
    /// </summary>
    /// <param name="isActive">On or off</param>
    /// <param name="isOutermostRing"></param>
    public void Toggle(bool isActive, bool isOutermostRing)
    {
        this.gameObject.SetActive(isActive);
        // Check the bumper group
        if (BumperGroup != null)
        {   // If we're on but not the outermost ring turn off the bumper group
            if (isActive == true && isOutermostRing == false) BumperGroup.gameObject.SetActive(false);
            else BumperGroup.gameObject.SetActive(isActive); // Set on/off based on the isActive variable
        }                    
    }    

    /// <summary>
    /// Quick helper function to know if we're the center ring
    /// </summary>
    /// <returns></returns>
    public bool IsCenterRing()
    {        
        // Rings[0] is the center ring
        return DaxRef.Wheel.Rings[0] == this;
    }       
}
