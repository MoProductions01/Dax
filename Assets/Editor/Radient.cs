using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;

/// <summary>
/// This class is used to create the menu items to create new puzzles
/// or load up existing serialized save files.
/// </summary>
public class Radient
{    
    /// <summary>
    /// Trash any existing puzzle and create a new one from scratch.
    /// </summary>
    [MenuItem("Radient/Dax/Create Puzzle")]
    public static void CreateDaxPuzzle()
    {        
        MCP.CreateNewPuzzle();
    }

    /// <summary>
    /// Opwns up a file panel so the user can select the puzzle, then
    /// trashes the current one and loads up the new one from the MCP instance
    /// </summary>
    [MenuItem("Radient/Dax/Load Puzzle")]
    public static void LoadPuzzle()
    {
        // Opens up the window for the user to select a puzzle
        string folder = Application.dataPath + "/Resources/Puzzles/";
        string puzzlePath = EditorUtility.OpenFilePanel("Load Dax Puzzle", folder, "");
        if (File.Exists(puzzlePath) == false) { Debug.LogError("Trying to load current puzzle: " + puzzlePath + " but it doesn't exist."); return; }

        MCP.CreateNewPuzzle(); // Trash the old puzzle and re-creates a new one
        GameObject.FindObjectOfType<MCP>().LoadPuzzle(puzzlePath);                  
    }

    /// <summary>
    /// Calls the MCP instance to save the puzzle into binary format
    /// </summary>
    [MenuItem("Radient/Dax/Save Puzzle")]
    public static void SavePuzzle()
    {
        GameObject.FindObjectOfType<MCP>().SavePuzzle();  
    }

}
