using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChannelPiece : MonoBehaviour
{
    public Channel MyChannel;

    public bool Active;

    [Header("Starting State Stuff")]
    public bool StartingState;

    public void InitFromCreation(Channel myChannel)
    {
        MyChannel = myChannel;
        Active = true;               
    }

    public void SetStartState()
    {
        StartingState = Active;
    }
    public void ResetStartState()
    {
        Active = StartingState;
        SetActive(Active);
    }

#if true
    DaxSetup DS = null;
    private void OnDrawGizmos()
    {       
        if (DS == null) DS = FindObjectOfType<DaxSetup>();                   
        if (DS != null && DS.ShowGizmos == true && Active == false)
        {            
            Gizmos.color = Color.red;
            Gizmos.DrawCube(GetComponent<MeshRenderer>().bounds.center, Vector3.one / 15f);
        }        
    }
#endif

    
    public bool IsActive()
    {
        return Active;
    }
    public void Toggle()
    {        
        Active = !Active;        
        GetComponent<MeshRenderer>().enabled = Active;        
        GetComponent<Collider>().enabled = Active;
    }
    public void SetActive( bool isActive)
    {                     
        Active = isActive;
        GetComponent<MeshRenderer>().enabled = isActive;
        GetComponent<Collider>().enabled = isActive;
    }
}
