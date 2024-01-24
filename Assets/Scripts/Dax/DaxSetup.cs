using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using UnityEditor.AnimatedValues;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class DaxSetup : MonoBehaviour
{   // Keep tool specific stuff in here.  Any game data should be in Dax.cs
    //public static float WHEEL_DIST_Y = 8f;       
    public static float[] CAMERA_Y_VALUES = new float[] {3.65f, 5.25f, 6.85f, 8.5f};
    public static string[] NUM_RINGS_NAMES = new string[] { "One", "Two", "Three", "Four" };
    public static int[] NUM_RINGS_TOTALS = { 1, 2, 3, 4 };
    //public static float[] NUM_RINGS_TOTALS = { 1, 2, 3, 4 };
   // public List<string> WheelList = new List<string>(new string[] { "1" });
           
    // node types to view
    public bool ShowDiodes = true;    
    public bool[] DiodesToShow = new bool[Enum.GetValues(typeof(BoardObject.eBoardObjectType)).Length];
        
    public List<Color> DiodeGizmoColors = new List<Color>(new Color[] { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan, Color.black, Color.white,
                                                                        Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan, Color.black, Color.white});
    void Reset()
    {        
        Debug.Log("DaxSetup.Reset()");
        for (int i = 0; i < DiodesToShow.Length; i++) DiodesToShow[i] = true;
    }

    public RifRafDebug RifRafDebugRef;    
    public Dax _Dax;       

    // DEBUG
    public bool ShowGizmos = true;
}