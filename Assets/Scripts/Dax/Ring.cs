using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Ring : MonoBehaviour
{
    public float RotateSpeed = 10f;
    public Dax DaxRef = null;
    public BumperGroup BumperGroup = null;

    public void Rotate()
    {
        transform.Rotate(new Vector3(0f, RotateSpeed * Time.deltaTime, 0f));
    }

    public void CollectAllPickupFacets()
    {        
        List<Facet> collectFacets = this.GetComponentsInChildren<Facet>().ToList();
        collectFacets.RemoveAll(x => x._Color != Facet.eFacetColors.WHITE);
        foreach(Facet facet in collectFacets)
        {
            this.DaxRef.CurWheel.CollectPickupFacet(facet);
        }        
    }

    public void Toggle(bool isActive, bool isOutermostRing)
    {
        this.gameObject.SetActive(isActive);
        if (BumperGroup != null)
        {
            if (isActive == true && isOutermostRing == false) BumperGroup.gameObject.SetActive(false);
            else BumperGroup.gameObject.SetActive(isActive);
        }                    
    }    

    public bool IsCenterRing()
    {        
        return DaxRef.CurWheel.Rings[0] == this;
    }   

    
}
