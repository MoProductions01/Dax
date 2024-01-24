using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : BoardObject
{
    public enum eInteractableType { TOGGLE, SWITCH, WARP_GATE, WORMHOLE };

    [Header("Interactable Data")]
    public eInteractableType InteractableType;

    public void ToggleChannelPieces(eMoveDir playerMoveDir)
    {
        if (playerMoveDir == eMoveDir.OUTWARD)
        {
            CurChannel.InnerChannel.SetActive(true);
            CurChannel.OuterChannel.SetActive(false);
        }
        else
        {
            CurChannel.InnerChannel.SetActive(false);
            CurChannel.OuterChannel.SetActive(true);
        }
    }


    public List<ChannelPiece> PiecesToTurnOff = new List<ChannelPiece>();
    public List<ChannelPiece> PiecesToTurnOn = new List<ChannelPiece>(); // moupdate - maybe combine this with the destgates - make sure to note this in instructional video
    
    public void Activate()
    {
        foreach (ChannelPiece channelPiece in PiecesToTurnOff) if(channelPiece != null) channelPiece.SetActive(false);
        foreach (ChannelPiece channelPiece in PiecesToTurnOn) if (channelPiece != null) channelPiece.SetActive(true);
    }

    public List<Interactable> DestGates = new List<Interactable>();


    private void OnDrawGizmos()
    {
        if (DestGates.Count == 0) return;        
        foreach (Interactable interactable in DestGates)
        {
            if (interactable == null) continue;
            Vector3 yOffset = new Vector3(0f, .2f, 0f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + yOffset);
            Gizmos.DrawLine(transform.position + yOffset, interactable.transform.position + yOffset);
            Gizmos.DrawLine(interactable.transform.position + yOffset, interactable.transform.position);
            Gizmos.color = new Color(1f, 0f, 0f, .3f);
            Gizmos.DrawWireSphere(interactable.transform.position, .06f);
        }
    }
}
