using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFX : MonoBehaviour
{
    public List<GameObject> Prefabs = new List<GameObject>();
    

    public void OnClickPrefabPreview(int index)
    {
       GameObject preafab = Instantiate<GameObject>(Prefabs[index], this.transform);
    }
}
