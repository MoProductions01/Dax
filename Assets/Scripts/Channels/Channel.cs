using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Channel class.  These are the cnannels that the Player and Enemy move through.
/// No functions but still uses a lot of MonoBehaviour functionality
/// </summary>
public class Channel : MonoBehaviour
{
    [field: SerializeField] public Ring MyRing {get; set;} // The Ring this Channel is on

    // Each Channel has two pieces that can be turned on or off creating paths
    [field: SerializeField] public ChannelPiece InnerChannel {get; set;}
    [field: SerializeField] public ChannelPiece OuterChannel;

    // Each Channel has 3 nodes.  BoardObjects can only be on the center node.
    // The Start and End nodes are for jumping between channels
    [field: SerializeField] public ChannelNode StartNode {get; set;}
    [field: SerializeField] public ChannelNode MidNode;
    [field: SerializeField] public ChannelNode EndNode;    
}
