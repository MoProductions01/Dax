using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// These are the pieces that can be turned on or off to create paths on the Rings
/// </summary>
public class ChannelPiece : MonoBehaviour
{
    public Channel MyChannel; // The Channel this piece is on
    public bool Active; // Are we on or off
       
    /// <summary>
    /// Called from the Wheel creation code
    /// </summary>
    /// <param name="myChannel">The channel this piece is on</param>
    public void InitFromCreation(Channel myChannel)
    {
        MyChannel = myChannel;
        Active = true;               
    }

    /// <summary>
    /// Returns whether or not this piece is on
    /// </summary>
    /// <returns></returns>
    public bool IsActive()
    {
        return Active;
    }

    /// <summary>
    /// Tells it specifically if it's on or off
    /// </summary>
    /// <param name="isActive"></param>
    public void SetActive( bool isActive)
    {                     
        Active = isActive;
        GetComponent<MeshRenderer>().enabled = isActive;
        GetComponent<Collider>().enabled = isActive;
    }

    /// <summary>
    /// Called from the DaxEditor code to toggle the piece on or off
    /// </summary>
    public void Toggle()
    {        
        Active = !Active;        
        GetComponent<MeshRenderer>().enabled = Active;        
        GetComponent<Collider>().enabled = Active;
    }
        
    DaxPuzzleSetup DS = null; // This is so we can use DrawGizmos when setting up the puzzle
    private void OnDrawGizmos()
    {       
        if (DS == null) DS = FindObjectOfType<DaxPuzzleSetup>();                   
        if (DS != null && DS.ShowGizmos == true && Active == false)
        {            
            Gizmos.color = Color.red;
            Gizmos.DrawCube(GetComponent<MeshRenderer>().bounds.center, Vector3.one / 15f);
        }        
    }
}
