using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bumper class.  These are only along the outer ring and server various functions (see below)
/// </summary>
public class Bumper : MonoBehaviour
{
    // Bumper types
    // REGULAR - no functionality just there to bounce the player or enemy back in the other direction
    // COLOR_MATCH - for the COLOR_MATCH victory condition.  If it matches the facet color the player is carrying it get consumed
    // DEATH - kills player
    public enum eBumperType { REGULAR, COLOR_MATCH, DEATH };
    [field: SerializeField] public eBumperType BumperType {get; set;} // Type of this bumper
    
    [field: SerializeField] public Facet.eFacetColors BumperColor;// Color of the bumper
}