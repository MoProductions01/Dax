using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ChannelNode : MonoBehaviour
{
    DaxSetup DS = null;
    public Channel MyChannel = null;
    public BoardObject SpawnedBoardObject = null;    

    /*public bool IsNodeAvailable()
    {           
        return SpawnedBoardObject == null;
    }*/
    public void InitFromCreation(Channel myChannel)
    {
        MyChannel = myChannel;        
    }   
    
    public bool IsOnCenterRing()
    {
        return MyChannel.MyRing.IsCenterRing();        
    }
    public bool IsStartNode()
    {
        return MyChannel.StartNode == this;
    }
    public bool IsEndNode()
    {
        return MyChannel.EndNode == this;
    }
    public bool IsMidNode()
    {
        return MyChannel.MidNode == this;
    }
        
    public bool IsNodeAvailableForRandomFacet()
    {        
        return false;        
    }
    public bool CanBeOnPath()
    {
        bool innerChannelOn = MyChannel.InnerChannel.IsActive();
        bool outerChannelOn = MyChannel.OuterChannel.IsActive();
        if (MyChannel.StartNode == this && innerChannelOn == false)
        {
            //Debug.Log("valid start node: " + MyChannel.StartNode.name + ", : " + Time.time);
            return true;
        }
        else if (MyChannel.EndNode == this && outerChannelOn == false)
        {
           // Debug.Log("valid end node: " + MyChannel.EndNode.name + ", : " + Time.time);
            return true;
        }
        else if (MyChannel.MidNode == this && (innerChannelOn == false || outerChannelOn == false))
        {
           // Debug.Log("valid MidNode: " + MyChannel.MidNode.name + ", : " + Time.time);
            return true;
        }
        
        return false;
    }

    
    private void OnDrawGizmos()
    {
        if (DS == null) DS = FindObjectOfType<DaxSetup>();
        if (DS != null && DS.ShowGizmos == true )      
        {
            Gizmos.color = new Color(.1f, .1f, .1f, .1f);
            Gizmos.DrawSphere(transform.position, .05f);            
        }        

        if(SpawnedBoardObject != null)
        {                        
            Gizmos.color = DS.DiodeGizmoColors[(int)(SpawnedBoardObject.BoardObjectType)];
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, .5f);
            Gizmos.DrawWireSphere(transform.position, .02f);
        }
    }
}
