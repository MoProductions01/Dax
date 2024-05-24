using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bumper : MonoBehaviour
{
    public enum eBumperType { REGULAR, COLOR_MATCH, DEATH };

    public eBumperType BumperType = eBumperType.REGULAR;
    public Facet.eFacetColors BumperColor;// = Facet.eFacetColors.WHITE;    
}