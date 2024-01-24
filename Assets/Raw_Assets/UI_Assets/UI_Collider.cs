using UnityEngine;
using UnityEngine.UI;

public class UI_Collider : MonoBehaviour{

    public GameObject Trigger;
    public GameObject Hex;

    public Animator Hex_Animation;

    void Start()
    {
        Hex_Animation = Hex.GetComponent<Animator>();
    }


    void OnTriggerEnter(Collider coll)
    {
        Hex_Scale_Up();
    }
    void OnTriggerExit(Collider coll)
    {
        Hex_Scale_Down();
    }

    public void Hex_Scale_Up()
    {
        Hex_Animation.Play("Hex_Scale_Up");
    }
    public void Hex_Scale_Down()
    {
        Hex_Animation.Play("Hex_Scale_Down");
    }
}