using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Channel class.  These are the cnannels that the Player and Enemy move through.
/// No functions but still uses a lot of MonoBehaviour functionality
/// </summary>
public class Channel : MonoBehaviour
{
    public Ring MyRing; // The Ring this Channel is on

    // Each Channel has two pieces that can be turned on or off creating paths
    public ChannelPiece InnerChannel;
    public ChannelPiece OuterChannel;

    // Each Channel has 3 nodes.  BoardObjects can only be on the center node.
    // The Start and End nodes are for jumping between channels
    public ChannelNode StartNode;
    public ChannelNode MidNode;
    public ChannelNode EndNode;        
}
