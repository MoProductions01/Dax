using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

[ExecuteInEditMode]
public class RifRafDebug : MonoBehaviour
{        
    public enum eDebugTextType { PHYSICS, GAME_STATE };
    public Text dbgText;
    public bool KeepOff = true;       
}

[ExecuteInEditMode]
public static class RRDManager
{
    static RifRafDebug RifRafDebug;

    public static void Init(RifRafDebug rrd)
    {
       // Debug.Log("RRDManager.Init(): " + rrd.name);            
        RifRafDebug = rrd;
    }

    public static void ResetText()
    {
        if (RifRafDebug == null) RRDManager.Init(GameObject.FindObjectOfType<DaxPuzzleSetup>().RifRafDebugRef);
        
        //RifRafDebug.dbgText.text = "";
    }

    public static void SetText(string s, RifRafDebug.eDebugTextType type)
    {
        //Debug.Log("SetText()");
        if (RifRafDebug == null) RRDManager.Init(GameObject.FindObjectOfType<DaxPuzzleSetup>().RifRafDebugRef);
        
        if (type == RifRafDebug.eDebugTextType.PHYSICS) return;
        RifRafDebug.dbgText.text = s;
    }
    public static void AppendText(string s, RifRafDebug.eDebugTextType type)
    {
       // Debug.Log("AppendText(): " + s);
        if (RifRafDebug == null) RRDManager.Init(GameObject.FindObjectOfType<DaxPuzzleSetup>().RifRafDebugRef);
        
        if (type == RifRafDebug.eDebugTextType.PHYSICS) return;
       // RifRafDebug.dbgText.text += s;
    }
}
