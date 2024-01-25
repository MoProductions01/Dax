using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Player : BoardObject
{
    [Header("Player Specific")] // moupdate - separate code that's saved and that's reset
    public Shield ActiveShield = null;
    public List<Shield> Shields = new List<Shield>();
    public List<Magnet.eMagnetTypes> MagnetTypes = new List<Magnet.eMagnetTypes>();
    public Hazard TempEnemyIgnore = null; // ignore this enemy after a shield collision until you're not collided
    public BoardObject CarriedColorFacet = null;
    public float EMPTime; // moupdate
    public Hazard.eHazardType EffectType; // moupdate - this is sloppy, maybe a new enum
    public float SpeedSave; // moupdate

    

    public void EMPHit(float effectTime)
    {
        EffectType = Hazard.eHazardType.EMP;
        EMPTime = effectTime;
        SpeedSave = Speed;
        Speed = 0f;
    }
    public void ClearInventory()
    {
       // for(int i=0; i < Shields.Count; i++) DestroyImmediate(Shields[i].gameObject);        
        Shields.Clear();
        MagnetTypes.Clear();
    }
    public bool AddMagnet(Magnet.eMagnetTypes magnetType)
    {
        if (MagnetTypes.Count == 24) return false;

        MagnetTypes.Add(magnetType);
        if (MagnetTypes.Count == 1) _Dax._UIRoot.ChangeMagnetIcon(magnetType);
        return true;
    }
    public void ActivateMagnet()
    {
        if (MagnetTypes.Count == 0) return;

        Magnet.eMagnetTypes magnetType = MagnetTypes[0];
        MagnetTypes.RemoveAt(0);
        _Dax._UIRoot.DestroyMagnetIcon();
        if (MagnetTypes.Count > 0) _Dax._UIRoot.ChangeMagnetIcon(MagnetTypes[0]);

        switch(magnetType)
        {
            case Magnet.eMagnetTypes.REGULAR:                
                CurChannel.MyRing.CollectAllPickupFacets();
                break;
            case Magnet.eMagnetTypes.SUPER:                
                foreach(Ring ring in _Dax.CurWheel.Rings)
                {
                    ring.CollectAllPickupFacets();
                }
                break;
        }
        
    }
    
    
    public bool AddShield(Shield shield)
    {
        if (Shields.Count == 24) return false;

        Destroy(shield.GetComponent<Collider>());        
        shield.SpawningNode.SpawnedBoardObject = null;
        shield.transform.parent = this.transform;
        shield.gameObject.SetActive(false);

        Shields.Add(shield);
        if(Shields.Count == 1) _Dax._UIRoot.ChangeShieldIcon(shield);
        return true;        
    }
    public void ActivateShield()
    {
        if (Shields.Count == 0 || ActiveShield != null) return;

        ActiveShield = Shields[0];
        ActiveShield.gameObject.SetActive(true);
        Shields.RemoveAt(0);
        _Dax._UIRoot.DestroyShieldIcon();
        if (Shields.Count > 0) _Dax._UIRoot.ChangeShieldIcon(Shields[0]);                
    }
    
    private void LateUpdate()
    {        
       // string s = "pos: " + transform.position + ", localPos: " + transform.localPosition + "\n";
        //s += "forward: " + transform.forward + ", CurChannel: " + CurChannel.name;
         //RRDManager.SetText(s, RifRafDebug.eDebugTextType.GAME_STATE);   
        
        if(CarriedColorFacet != null)
        {
            CarriedColorFacet.transform.position = this.transform.position + (this.transform.up * .1f);
            /*if(MoveDir == eMoveDir.OUTWARD)
            {
                CarriedColorFacet.transform.position = this.transform.position - (this.transform.forward * .1f);
            }            
            else
            {
                CarriedColorFacet.transform.position = this.transform.position + (this.transform.forward * .1f);
            }*/
        }
        if (ActiveShield != null)
        {
            ActiveShield.transform.position = this.transform.position;            
        }
        
    }

    private void OnCollisionExit(Collision collision)
    {
        //string s = "Player.OnCollisionExit() collision name: " + collision.collider.name + ", collision parent name: " + collision.collider.transform.parent.name + ", Dest Gate: ";
        //s += (DestGateJustWapredTo == null ? "no dest gate" : DestGateJustWapredTo.name);
        //Debug.Log(s);        
        if(DestGateJustWarpedTo != null && collision.collider.name.Equals(DestGateJustWarpedTo.name))
        {   // this is so we don't warp back 'n forth between gates after a warp            
            DestGateJustWarpedTo = null;
        }    
        if (collision.collider.gameObject.GetComponentInParent<Hazard>() != null)
        {            
            if(TempEnemyIgnore == collision.collider.gameObject.GetComponentInParent<Hazard>())
            {                
                TempEnemyIgnore = null;
            }
        }
    }
    
    private void Update()
    {   
        /*if(ActiveShield != null && (ActiveShield.ShieldType == Shield.eShieldTypes.TIMED || ActiveShield.ShieldType == Shield.eShieldTypes.TIMED_KILL))
        {
            ActiveShield.Timer -= Time.deltaTime;
            if (ActiveShield.Timer <= 0f)
            {
                DestroyImmediate(ActiveShield.gameObject);
                ActiveShield = null;
            }
        } */
           
        if(EffectType == Hazard.eHazardType.EMP)
        {
            EMPTime -= Time.deltaTime;
            if(EMPTime <= 0f)
            {
                EffectType = Hazard.eHazardType.ENEMY; // moupdate
                Speed = SpeedSave;
            }
        }
    }   

    public void ResetForPuzzleRestart(Dax.BoardObjectSave playerSave = null)
    {
        // clear out player inventory
        ClearInventory();
        if (_Dax._UIRoot == null)
        {
            Debug.LogWarning("Update UIRoot on new scene");
        }
        else
        {
            _Dax._UIRoot.DestroyMagnetIcon();
            _Dax._UIRoot.DestroyShieldIcon();
        }
        
        transform.position = Vector3.zero;
        if(playerSave != null)
        {
            //this.MoveDir = playerSave.MoveDir; monewsave
            this.Speed = playerSave.Speed;
            CurChannel = GameObject.Find(playerSave.StartChannel).GetComponent<Channel>();
        }
        ActiveShield = null;
        TempEnemyIgnore = null;
        CarriedColorFacet = null;               
    }

    public void SetStartChannel(int channelIndex)
    {
        Debug.Log("Player.SetStartChannel: channelIndex: " + channelIndex);
        CurChannel = _Dax.CurWheel.Rings[0].transform.GetComponentsInChildren<Channel>().ToList()[channelIndex];
        SpawningNode = CurChannel.StartNode;

        transform.LookAt(SpawningNode.transform);
    }



#if true
    DaxSetup DS = null;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position + transform.forward * .1f, .04f);
        if (DS == null) DS = FindObjectOfType<DaxSetup>();        
        if (DS != null && DS.ShowGizmos == true)            
        {           
            if (CurChannel != null)
            {
                Gizmos.color = Color.yellow / 1.5f;
                Gizmos.DrawWireSphere(CurChannel.StartNode.transform.position, .08f);
                Gizmos.DrawWireSphere(CurChannel.MidNode.transform.position, .08f);
                Gizmos.DrawWireSphere(CurChannel.EndNode.transform.position, .08f);
            }       
        }        
    }
#endif
}
