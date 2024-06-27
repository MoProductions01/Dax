using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Static visual effects class that can be called from anywhere and then calls a
/// MonoBehavior to get access to necessary functions that you can't access in a static class.
/// I used a static class because the VFX are stateless and there's only one instance of everything
/// Some of the calls are redundant but there's a few exceptions and I wanted to leave it
/// open for future modification
/// </summary>
public static class VFXPlayer
{
    private static VFX vfx; // Reference to the MonoBehavior instance

    /// <summary>
    /// Sets up the VFX class reference
    /// </summary>
    /// <param name="vfxInstance"></param>
    public static void Init(VFX vfxInstance)
    {
        vfx = vfxInstance;
    }
    
    /// <summary>
    /// Plays a VFX for collecting a facet
    /// </summary>
    /// <param name="color">Color of facet</param>
    /// <param name="worldPos">Spawn point</param>
    public static void PlayFacetVFX(Facet.eFacetColors color, Vector3 worldPos)
    {     
        string prefabName = "Dax/Prefabs/VFX/Facets/" + color.ToString() + "FacetPickupVFX";        
        worldPos += new Vector3(0f, .1f, 0f);     
        vfx.PlayVFX(prefabName, worldPos);
    }    

    /// <summary>
    /// Plays a VFX for collecting a facet
    /// </summary>
    /// <param name="type">type of Hazard</param>
    /// <param name="worldPos">Spawn point</param>
    public static void PlayHazardVFX(Hazard.eHazardType type, Vector3 worldPos)
    {
        List<float> yOffsets = new List<float> {.5f, .5f, 0f};
        string prefabName = "Dax/Prefabs/VFX/Hazards/" + type.ToString() + "EnemyVFX";        
        worldPos += new Vector3(0f, yOffsets[(int)type], 0f);          
        GameObject instance = vfx.PlayVFX(prefabName, worldPos); 
        
        // Keep a reference to the Glue instance we created so that the code 
        // can get rid of it when the Glue timer is out
        if(type == Hazard.eHazardType.GLUE)
        {
            vfx.ActiveGlueVFX = instance;
        }
    }

    /// <summary>
    /// If we have a Glue Hazard on screen this will get called
    /// from another spat to get rid of it once the timer is done
    /// </summary>
    public static void ShutOffGlueVFX()
    {
        vfx.ShutOffGlueVFX();
    }

    /// <summary>
    /// Play FacetCollect VFX
    /// </summary>
    /// <param name="type">Type of FacetCollect</param>
    /// <param name="worldPos">Spawn point</param>
    public static void PlayFacetCollectVFX(FacetCollect.eFacetCollectTypes type, Vector3 worldPos)
    {
        string prefabName = "Dax/Prefabs/VFX/Pickups/Facet Collects/" + type.ToString() + "FacetCollectVFX";
        worldPos += new Vector3(0f, .2f, 0f);
        vfx.PlayVFX(prefabName, worldPos);        
    }

    /// <summary>
    /// Play VFX for picking up and activating a shield
    /// </summary>
    /// <param name="type">Type of shield</param>
    /// <param name="worldPos">Spawn point</param>
    public static void PlayShieldCollectActivateVFX(Shield.eShieldTypes type, Vector3 worldPos)
    {
        string prefabName = "Dax/Prefabs/VFX/Pickups/Shields/PickupOrActivate/" + type.ToString() + "ShieldCollectActivateVFX";
        worldPos += new Vector3(0f, .2f, 0f);
        vfx.PlayVFX(prefabName, worldPos);        
    }

    /// <summary>
    /// Play VFX for when a shield impacts with an object
    /// </summary>
    /// <param name="type">Type of shield</param>
    /// <param name="worldPos">Spawn point</param>
    public static void PlayShieldImpactVFX(Shield.eShieldTypes type, Vector3 worldPos)
    {
        List<float> yOffsets = new List<float> {.2f, .1f};      
        string prefabName = "Dax/Prefabs/VFX/Pickups/Shields/Impact/" + type.ToString() + "ShieldImpactVFX";
        worldPos += new Vector3(0f, yOffsets[(int)type], 0f);   
        vfx.PlayVFX(prefabName, worldPos);
    }

    /// <summary>
    /// Play PointMod VFX
    /// </summary>
    /// <param name="type">Type of PointMod</param>
    /// <param name="worldPos">SpawnPoint</param>
    public static void PlayPointModVFX(PointMod.ePointModType type, Vector3 worldPos)
    {
        string prefabName = "Dax/Prefabs/VFX/Pickups/Point Mods/" + type.ToString() + "PointModVFX";
        worldPos += new Vector3(0f, .2f, 0f);
        vfx.PlayVFX(prefabName, worldPos);     
    }

    /// <summary>
    /// Play a SpeedMod VFX
    /// </summary>
    /// <param name="type">Type of SpeedMod</param>
    /// <param name="worldPos">Spawn Point</param>
    public static void PlaySpeedModVFX(SpeedMod.eSpeedModType type, Vector3 worldPos)
    {
        string prefabName = "Dax/Prefabs/VFX/Pickups/Speed Mods/" + type.ToString() + "SpeedModVFX";
        worldPos += new Vector3(0f, .2f, 0f);
        vfx.PlayVFX(prefabName, worldPos);     
    }

    /// <summary>
    /// Play ChannelChange VFX
    /// </summary>
    /// <param name="worldPos">Spawn point</param>
    public static void PlayChannelChangeVFX(Vector3 worldPos)
    {
        string prefabName = "Dax/Prefabs/VFX/Misc/ChannelChangeVFX";
        worldPos += new Vector3(0f, .1f, 0f);
        vfx.PlayVFX(prefabName, worldPos);     
    }
}

/// <summary>
/// This is the class that's called from VFXPlayer to play a VFX.
/// I can't do everything I need to do in a static class (like Resources.Load)
/// so I use this to handle the actually playing.
/// </summary>
public class VFX : MonoBehaviour
{   
    public GameObject ActiveGlueVFX {get; set;}  // Ref to the GlueVFX that we get rid of once the timer is gone

    private void Awake() 
    {
        VFXPlayer.Init(this);
    }

    /// <summary>
    /// The function that actually handles the resource loading and playing of a VFX
    /// Can't put stuff like Resources.Load in a static class so this is used for evertying
    /// once the static class sets it up
    /// </summary>
    /// <param name="name">Name of the prefab</param>
    /// <param name="pos">Spawn point</param>
    /// <returns></returns>
    public GameObject PlayVFX(string name, Vector3 pos)
    {        
//        Debug.Log("PlayVFX(): " + name);
        GameObject prefab = Resources.Load<GameObject>(name);
        GameObject instance = GameObject.Instantiate<GameObject>(prefab, this.transform);
        instance.transform.position = pos;
        return instance;
    }                                            

    /// <summary>
    /// Destroys the ActiveGlueVFX object
    /// </summary>
    public void ShutOffGlueVFX()
    {
        if(ActiveGlueVFX == null) {Debug.LogWarning("Why do we not have an active glue vfx?"); return; }
        
        DestroyImmediate(ActiveGlueVFX);
        ActiveGlueVFX = null;
    }        
}

    