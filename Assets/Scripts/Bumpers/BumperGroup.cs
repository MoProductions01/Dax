using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BumperGroup.  Holds all of the Bumpers for the current outer ring
/// </summary>
public class BumperGroup : MonoBehaviour
{
    // The Bumpers in this group
    [field: SerializeField] public List<Bumper> Bumpers {get; set;} = new List<Bumper>();
}
