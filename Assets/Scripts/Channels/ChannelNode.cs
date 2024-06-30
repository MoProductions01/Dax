using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// ChannelNode class.  Channel nodes have two functions
/// 1) Middle node is where you place BoardObjects
/// 2) Start and End nodes are there for jumping between channels
/// </summary>
public class ChannelNode : MonoBehaviour
{        
    [field: SerializeField] public Channel MyChannel {get; set;} // The Channel that this node lives on
    [field: SerializeField] public BoardObject SpawnedBoardObject {get; set;} // The BoardObject that is spawned on this node (only applies to Middle)
    
    /// <summary>
    /// Called from the Wheel creation code to assign it's Channel
    /// </summary>
    /// <param name="myChannel"></param>
    public void InitFromCreation(Channel myChannel)
    {
        MyChannel = myChannel;        
    }           

    
    /// <summary>
    /// Helper function to find out if it's the StartNode or not
    /// </summary>
    /// <returns></returns>
    public bool IsStartNode()
    {        
        return MyChannel.StartNode == this;
    }

    /// <summary>
    /// Helper function to find out if this node is the MiddleNode (BoardObjects can only be on MiddleNode)
    /// </summary>
    /// <returns></returns>
    public bool IsMidNode() 
    {
        return MyChannel.MidNode == this;
    }
        
    /// <summary>
    /// Figures out whether and Enemy or the Player can be on this path
    /// </summary>
    /// <returns></returns>
    public bool CanBeOnPath()
    {
        // bool innerChannelOn = MyChannel.InnerChannel.IsActive();
        // bool outerChannelOn = MyChannel.OuterChannel.IsActive(); modelete get
        bool innerChannelOn = MyChannel.InnerChannel.Active;
        bool outerChannelOn = MyChannel.OuterChannel.Active;
        if (MyChannel.StartNode == this && innerChannelOn == false)
        {   // We're the StartNode and the inner channel is off so it's free
            return true;
        }
        else if (MyChannel.EndNode == this && outerChannelOn == false)
        {   // We're the EndNode and the outer channel is off so it's free
            return true;
        }
        else if (MyChannel.MidNode == this && (innerChannelOn == false || outerChannelOn == false))
        {   // We're the MiddleNode and at least one channel piece is off so we're good
            return true;
        }
        
        // If we made it here we can't be on the path so return false
        return false;
    }


    //************************************** Editor Code ***********************************************************//        
    private void OnDrawGizmos()
    {
        DaxPuzzleSetup DS = FindObjectOfType<DaxPuzzleSetup>(); // FindObjectOfType is fine for editor stuff
        if (DS == null || DS.ShowGizmos == false) return;    

        Gizmos.color = new Color(.1f, .1f, .1f, .1f);
        Gizmos.DrawSphere(transform.position, .05f);                               
    }
}
