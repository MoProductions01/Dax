using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFX : MonoBehaviour
{   // monote - make a generic vfx player like the MCP create board object stuff
    // monote - make ALL vfx play at the player position
    [Header("-------------------Channel Change-------------------")]
    public GameObject ChannelChangeVFXPrefab;       
    public float CC_Y;
    public void PlayChannelChangeVFX(Vector3 worldPos)
    {
        return;
        GameObject instance = Instantiate<GameObject>(ChannelChangeVFXPrefab, this.transform);     
        instance.transform.position = worldPos + new Vector3(0f, CC_Y, 0f);
    }
        
    [Header("-------------------Facet-------------------")]        
    public List<GameObject> FacetVFXPrefabs = new List<GameObject>();
    public float F_Y;
    public void PlayFacetVFX(Facet.eFacetColors color, Vector3 worldPos)
    {
       // Debug.Log("PlayFacetVFX(): " + FacetVFXPrefabs[(int)color].name);
        GameObject instance = Instantiate<GameObject>(FacetVFXPrefabs[(int)color], this.transform);     
        instance.transform.position = worldPos + new Vector3(0f, F_Y, 0f);
    }

    [Header("-------------------Hazard-------------------")]
    public List<GameObject> HazardVFXPrefabs = new List<GameObject>();
    public GameObject ActiveGlueVFX;    
    public void PlayHazardVFX( Hazard.eHazardType type, Vector3 worldPos)
    {
        List<float> yOffsets = new List<float> {.5f, .5f, 0f};
        GameObject instance = Instantiate<GameObject>(HazardVFXPrefabs[(int)type], this.transform);     
        instance.transform.position = worldPos + new Vector3(0f, yOffsets[(int)type], 0f);                           
        Debug.Log("Play Hazard VFX: " + instance.name + ", at: " + instance.transform.position.ToString("F2"));
        if(type == Hazard.eHazardType.GLUE)
        {
            ActiveGlueVFX = instance;
        }
        
        
    }
    
    [Header("-------------------Facet Collect-------------------")]
    public List<GameObject> FacetCollectVFXPrefabs = new List<GameObject>();
    public float FC_Y;
    public void PlayFacetCollectVFX(FacetCollect.eFacetCollectTypes type, Vector3 worldPos)
    {        
        Debug.Log("PlayFacetCollectVFX(): " + FacetCollectVFXPrefabs[(int)type].name);
        GameObject instance = Instantiate<GameObject>(FacetCollectVFXPrefabs[(int)type], this.transform);     
        instance.transform.position = worldPos + new Vector3(0f, FC_Y, 0f);
    }

    [Header("-------------------Shields-------------------")]
    public List<GameObject> ShieldVFXPrefabs = new List<GameObject>();
    public float S_Y;
    public void PlayShieldCollectVFX(Shield.eShieldTypes type, Vector3 worldPos)
    {        
        Debug.Log("PlayShieldCollectVFX(): " + ShieldVFXPrefabs[(int)type].name);
        GameObject instance = Instantiate<GameObject>(ShieldVFXPrefabs[(int)type], this.transform);     
        //Debug.Log("PlayShieldCollectVFX(): " + ShieldVFXPrefabs[dbgIndex].name);
        //GameObject instance = Instantiate<GameObject>(ShieldVFXPrefabs[dbgIndex], this.transform);    
        instance.transform.position = worldPos + new Vector3(0f, S_Y, 0f);
    }

    [Header("-------------------Shield Impacts-------------------")]
    public List<GameObject> ShieldImpactVFXPrefabs = new List<GameObject>();
    //public float SI_Y;
    public void PlayShieldImpactCollectVFX(Shield.eShieldTypes type, Vector3 worldPos)
    { 
        List<float> yOffsets = new List<float> {.2f, .1f};               
        GameObject instance = Instantiate<GameObject>(ShieldImpactVFXPrefabs[(int)type], this.transform);   
        instance.transform.position = worldPos + new Vector3(0f, yOffsets[(int)type], 0f);          
        Debug.Log("PlayShieldImpactCollectVFX(): " + ShieldImpactVFXPrefabs[(int)type].name + ", at: " + instance.transform.position.ToString("F2"));
       // GameObject instance = Instantiate<GameObject>(ShieldImpactVFXPrefabs[dbgIndex], this.transform);    
        //instance.transform.position = worldPos + new Vector3(0f, SI_Y, 0f);
        //Debug.Log("PlayShieldImpactCollectVFX(): " + ShieldImpactVFXPrefabs[dbgIndex].name + ", at: " + instance.transform.position.ToString("F2"));
    }

   /* [Header("-------------------Debug-------------------")]
    public int dbgIndex = 0;
    void OnGUI()
    {
        if(GUI.Button(new Rect(0, 0, 200, 100), "play: " + dbgIndex))
        {
            PlayShieldImpactCollectVFX(Shield.eShieldTypes.HIT, FindObjectOfType<Player>().transform.position);
            dbgIndex = (dbgIndex + 1) % ShieldImpactVFXPrefabs.Count;
        }
    }*/

    public void ShutOffGlueVFX()
    {
        if(ActiveGlueVFX == null) {Debug.LogWarning("Why do we not have an active glue vfx?"); return; }
        
        DestroyImmediate(ActiveGlueVFX);
        ActiveGlueVFX = null;
    } 
}
