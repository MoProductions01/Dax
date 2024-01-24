using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Hazard : BoardObject
{
    public enum eHazardType { ENEMY, EMP, /*BOMB,*/ DYNAMITE, /*PROXIMITY_MINE, *//*TIMED_MINE */};
    public static float DEFAULT_EFFECT_TIME = 2f; // moupdate -  look into using this to init the objects instead of the prefab
    public static float MAX_EFFECT_TIME = 10f;

    [Header("Hazard Data")]
    public eHazardType HazardType;
    public float EffectTime = DEFAULT_EFFECT_TIME;    
    public float EffectRadius = .05f;
    // non saved
    public bool TimerActive = false; // moupdate
    public float EffectTimer;

    public override void InitForChannelNode(ChannelNode spawnNode, Dax dax)
    {
        //name = spawnNode.name + "--Magnet";
        // BoardObjectType = eBoardObjectType.MAGNET;
        /*if(this.HazardType == eHazardType.PROXIMITY_MINE)
        {
            Debug.Log("update collider");
            this.GetComponent<SphereCollider>().radius = EffectRadius;
        }*/
        base.InitForChannelNode(spawnNode, dax);
    }

    public void ActivateTimer()
    {
        TimerActive = true;
        EffectTimer = EffectTime;
        this.GetComponent<SphereCollider>().enabled = false;
    }

    private void Update()
    {
        if(TimerActive == true)
        {
            EffectTimer -= Time.deltaTime;
            if(EffectTimer <= 0f)
            {
                // right now only thing is timed mine but we'll moupdate this
                Explode();
            }
        }
    }

    void Explode()
    {
        List<Collider> curSphereColliders = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius).ToList();
        curSphereColliders.RemoveAll(x => x.GetComponent<Player>() == null);
        if(curSphereColliders.Count != 0)
        {
            //Debug.Log("kill player");
            TimerActive = false;
            FindObjectOfType<Dax>().EndGame("Killed By Timed Mine");
        }
        else
        {
           // Debug.Log("nothing");
            DestroyImmediate(this.gameObject);
        }
    }

}
