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


public class RifRafGames
{

    static bool CheckSelection<T>()
    {
        if (Selection.activeGameObject != null)
        {
            Debug.LogError("Error: must have no GameObject selected to create puzzle.");
            return false;
        }

        if (Selection.activeGameObject == null)
        {
            Debug.LogError("RifRaf Error: No GameObject selected.");
            return false;
        }
        if (Selection.gameObjects.Length != 1)
        {
            Debug.LogError("RifRaf Error: You must have only 1 GameObject selected.  You have " + Selection.gameObjects.Length + " selected.");
            return false;
        }
        if (Selection.activeGameObject.GetComponent<T>() == null)
        {
            Debug.LogError("RifRafError: This object is not the correct type.");
            return false;
        }
        return true;
    }

    [MenuItem("RifRaf Games/Dax/Choose Puzzle")]
    public static void LoadPuzzle()
    {
        string folder = Application.dataPath + "/Resources/Puzzles/";
        string puzzlePath = EditorUtility.OpenFilePanel("Load Dax Puzzle", folder, "");
        if (File.Exists(puzzlePath) == false) { Debug.LogError("Trying to load current profile: " + puzzlePath + " but it doesn't exist."); return; }

        MCP.CreateNewPuzzle(); // Trash the old puzzle and re-creates a new one
        GameObject.FindObjectOfType<MCP>().LoadPuzzle(puzzlePath);       
    }

    [MenuItem("RifRaf Games/Dax/Create Puzzle")]
    public static void CreateDaxPuzzle()
    {        
        MCP.CreateNewPuzzle();
    }
}
