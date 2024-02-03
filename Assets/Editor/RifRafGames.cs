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
public class RifRafGames
{    
    [MenuItem("RifRaf Games/Dax/Choose Puzzle")]
    public static void LoadPuzzle()
    {
        string folder = Application.dataPath + "/Resources/Puzzles/";
        string puzzlePath = EditorUtility.OpenFilePanel("Load Dax Puzzle", folder, "");
        if (File.Exists(puzzlePath) == false) { Debug.LogError("Trying to load current profile: " + puzzlePath + " but it doesn't exist."); return; }

        MCP.CreateNewPuzzle(); // Trash the old puzzle and re-creates a new one
        GameObject.FindObjectOfType<MCP>().LoadPuzzle(puzzlePath);       
    }

    /// <summary>
    /// Trash any existing puzzle and create a new one from scratch.
    /// </summary>
    [MenuItem("RifRaf Games/Dax/Create Puzzle")]
    public static void CreateDaxPuzzle()
    {        
        MCP.CreateNewPuzzle();
    }
}
