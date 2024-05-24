using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Channel : MonoBehaviour
{
    public Ring MyRing;

    public ChannelPiece InnerChannel;
    public ChannelPiece OuterChannel;

    public ChannelNode StartNode;
    public ChannelNode MidNode;
    public ChannelNode EndNode;    

    public void HaveNodesLookAtEachOther()
    {
        StartNode.transform.LookAt(EndNode.transform);
        EndNode.transform.LookAt(StartNode.transform);
    }
    public bool IsChannelFull()
    {
        return InnerChannel.Active == true && OuterChannel.Active == true;
    }
    public bool IsChannelEmpty()
    {
        return InnerChannel.Active == false && OuterChannel.Active == false;
    }
    public bool IsValidStartChannel()
    {
        return InnerChannel.Active == false;// || OuterChannel.Active == false;
    }
    public bool IsOnCenterRing()
    {
        return MyRing.IsCenterRing();
    }  
}
