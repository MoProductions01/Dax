using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using UnityEditor.AnimatedValues;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Various data for setting up the puzzles in the level editor
/// </summary>
[ExecuteInEditMode]
public class DaxPuzzleSetup : MonoBehaviour
{   // Keep tool specific stuff in here.  Any game data should be in Dax.cs          
    public static float[] CAMERA_Y_VALUES = new float[] {3.65f, 5.25f, 6.85f, 8.5f};
    public static string[] NUM_RINGS_NAMES = new string[] { "One", "Two", "Three", "Four" };
    public static int[] NUM_RINGS_TOTALS = { 1, 2, 3, 4 };    
           
    // node types to view
    public bool ShowDiodes = true;    
    public bool[] DiodesToShow = new bool[Enum.GetValues(typeof(BoardObject.eBoardObjectType)).Length];
        
    public List<Color> DiodeGizmoColors = new List<Color>(new Color[] { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan, Color.black, Color.white,
                                                                        Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan, Color.black, Color.white});
    void Reset()
    {                
        for (int i = 0; i < DiodesToShow.Length; i++) DiodesToShow[i] = true;
    }

    public RadientDebug RadientDebugRef;    
    public Dax _Dax;       

    // DEBUG
    public bool ShowGizmos = true;
}