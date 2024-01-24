using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Facet : BoardObject
{
    public enum eFacetColors { RED, GREEN, BLUE, YELLOW, PURPLE, PINK, ORANGE, WHITE };
    public eFacetColors _Color;
    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {
        name = spawnNode.name + "--Facet";
        BoardObjectType = eBoardObjectType.FACET;       
        base.InitForChannelNode(spawnNode, dax);
    }
}