using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// These are the pieces that can be turned on or off to create paths on the Rings
/// </summary>
public class ChannelPiece : MonoBehaviour
{
    [field: SerializeField] public Channel MyChannel {get; set;} // The Channel this piece is on} // The Channel this piece is on
    [SerializeField] private bool active2; 
    [SerializeField] public bool Active  
    {
        get { return this.active2; }       // monote - rename to active             
        set
        {                        
            this.active2 = value;
            GetComponent<MeshRenderer>().enabled = this.active2;
            GetComponent<Collider>().enabled = this.active2;
        }
    }   
       
    /// <summary>
    /// Called from the Wheel creation code
    /// </summary>
    /// <param name="myChannel">The channel this piece is on</param>
    public void InitFromCreation(Channel myChannel)
    {
        MyChannel = myChannel;
        Active = true;               
    }       
        

    //************************************** Editor Code ***********************************************************//    
    private void OnDrawGizmos()
    {       
        if(Active == true) return;
        DaxPuzzleSetup DS = FindObjectOfType<DaxPuzzleSetup>(); // FindObjectOfType is fine for editor stuff
        if (DS == null || DS.ShowGizmos == false) return;    

        Gizmos.color = Color.red;
        Gizmos.DrawCube(GetComponent<MeshRenderer>().bounds.center, Vector3.one / 15f);                                 
    }
}