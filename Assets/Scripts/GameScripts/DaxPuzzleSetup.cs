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

    // DEBUG
    public bool ShowGizmos = true;
}