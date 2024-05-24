using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

[ExecuteInEditMode]
public class RadientDebug : MonoBehaviour
{        
    public enum eDebugTextType { PHYSICS, GAME_STATE };
    public Text dbgText;
    public bool KeepOff = true;       
}

[ExecuteInEditMode]
public static class RRDManager
{
    static RadientDebug RadientDebug;

    public static void Init(RadientDebug rrd)
    {
       // Debug.Log("RRDManager.Init(): " + rrd.name);            
        RadientDebug = rrd;
    }

    public static void ResetText()
    {
        if (RadientDebug == null) RRDManager.Init(GameObject.FindObjectOfType<DaxPuzzleSetup>().RadientDebugRef);
        
        //RadientDebug.dbgText.text = "";
    }

    public static void SetText(string s, RadientDebug.eDebugTextType type)
    {
        //Debug.Log("SetText()");
        if (RadientDebug == null) RRDManager.Init(GameObject.FindObjectOfType<DaxPuzzleSetup>().RadientDebugRef);
        
        if (type == RadientDebug.eDebugTextType.PHYSICS) return;
        RadientDebug.dbgText.text = s;
    }
    public static void AppendText(string s, RadientDebug.eDebugTextType type)
    {
       // Debug.Log("AppendText(): " + s);
        if (RadientDebug == null) RRDManager.Init(GameObject.FindObjectOfType<DaxPuzzleSetup>().RadientDebugRef);
        
        if (type == RadientDebug.eDebugTextType.PHYSICS) return;
       // RadientDebug.dbgText.text += s;
    }
}
