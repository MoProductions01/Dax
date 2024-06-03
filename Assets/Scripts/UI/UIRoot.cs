using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.PackageManager;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class UIRoot : MonoBehaviour
{
    public Camera UICamera;
    public GameObject ClickToStartButton;

   // public TextMeshPro TimerText;
   // public TextMeshPro ScoreText;
    public TMP_Text ScoreText;
    public TMP_Text TimerText;    
    public TMP_Text PointMultTimeText;    
    //public GameObject[] ColorCounters;
    public TMP_Text[] FacetCounterTexts = new TMP_Text[(int)Facet.eFacetColors.ORANGE + 1];
    public GameObject EndGameItems;
    public UnityEngine.UI.Button FacetCollectActivateButton;
    UnityEngine.UI.Image FacetCollectIcon;
    public UnityEngine.UI.Button ShieldActivateButton;
    UnityEngine.UI.Image ShieldIcon;
    
   // public GameObject TryAgainButton;
    //public GameObject ShieldIcon = null;
    //public GameObject FacetCollectIcon = null;        
//    public GameObject MainMenuButton;
    Dax Dax;
    MCP MCP;
    Player Player;

    // mopowerup
   // public void ChangeFacetCollectIcon(FacetCollect.eFacetCollectTypes facetCollectType)
   // {         // modelete update - make the points for items const
        
   //     FacetCollectIcon.enabled = true;
              

        /*
        Debug.Log("UIRoot.ChangeFacetCollectIcon()");
        DestroyFacetCollectIcon();
        FacetCollectIcon = MCP.CreateFacetCollectButton(facetCollectType);
        Debug.Log("name: " + FacetCollectIcon.name);
        FacetCollectIcon.transform.parent = this.transform;*/
 //   }

    public void ToggleFacetCollectIcon(bool isActive, FacetCollect.eFacetCollectTypes facetCollectType)
    {
        Debug.Log("UIRoot.ToggleFacetCollectIcon(). isActive: " + isActive + ", type: " + facetCollectType.ToString());
        FacetCollectIcon.enabled = isActive;
        if(isActive == true)
        {
            if(facetCollectType == FacetCollect.eFacetCollectTypes.RING)
            {
                FacetCollectIcon.sprite = Resources.Load<Sprite>("Dax/Powerup Icons/Icon Lightning Yellow");
            }
            else
            {
                FacetCollectIcon.sprite = Resources.Load<Sprite>("Dax/Powerup Icons/Icon Lightning Blue");
            }  
        }
    }
     

    public void OnClickFacetCollectButton()
    {
        Debug.Log("UIRoot.OnClickFacetCollectButton()");
        Player.ActivateFacetCollect();
    }

    public void OnClickShieldButton()
    {
        Debug.Log("UIRoot.OnClickShieldButton()");
        Player.ActivateShield();

    }

    /*public void DestroyFacetCollectIcon()
    {
        Debug.LogWarning("Implement UIRoot.DestroyFacetCollectIcon()");
        //Debug.Log("UIRoot.DestroyFacetCollectIcon()");
       */ //if (FacetCollectIcon != null) DestroyImmediate(FacetCollectIcon);        
   // }
  /*  public void DestroyShieldIcon()
    {
        Debug.LogWarning("Implement UIRoot.DestroyShieldIcon()");
        //if (ShieldIcon != null) DestroyImmediate(ShieldIcon);
    }*/
    public void ToggleShieldIcon(bool isActive, Shield.eShieldTypes shieldType)
    {
        Debug.Log("UIRoot.ToggleShieldIcon(). isActive: " + isActive + ", type: " + shieldType.ToString());
        ShieldIcon.enabled = isActive;
        if(isActive == true)
        {
            if(shieldType == Shield.eShieldTypes.HIT)
            {
                ShieldIcon.sprite = Resources.Load<Sprite>("Dax/Powerup Icons/Icon Shield Wood");
            }
            else
            {
                ShieldIcon.sprite = Resources.Load<Sprite>("Dax/Powerup Icons/Icon Shield Metal");
            }  
        }        
    }   

    public void ShowEndGame(string endGameReason, bool isActive)
    {                
        EndGameItems.gameObject.SetActive(isActive);
        EndGameItems.GetComponentInChildren<TMP_Text>().SetText("Game Over: " + endGameReason);
      //  TryAgainButton.SetActive(isActive);
       // MainMenuButton.SetActive(isActive);
    }
    

    public void OnClickStartButton()
    {
        Dax.StartGame();
    }

    public void OnClickTryAgainButton()
    {
        Debug.Log("UIRoot.OnClickTryAgainButton()");
        EndGameItems.gameObject.SetActive(false);
        Dax.ResetPuzzleFromSave();
    }       

    void SetTimerText()
    {
        int minutes = Mathf.FloorToInt(Dax.LevelTime / 60f); // moui timer/score
        int seconds = Mathf.FloorToInt(Dax.LevelTime % 60f);
        if (minutes != 0) TimerText.SetText(minutes + ":" + seconds);
        else TimerText.SetText(":" + seconds);
    }

    public void SetPointModTime(float pointModTimer)
    {
        int minutes = Mathf.FloorToInt(pointModTimer / 60f); // moui timer/score
        float seconds = pointModTimer % 60f;
        if (minutes != 0) PointMultTimeText.SetText(minutes + ":" + Mathf.FloorToInt(seconds));
        else PointMultTimeText.SetText(":" + seconds.ToString("F2"));
    }
    
    public void ResetForGameStart()
    {        
        Dax.LevelTime = Dax.DEFAULT_LEVEL_TIME; // moui timer/score
        SetTimerText();
        ScoreText.SetText(0.ToString());
        
        for (int i = 0; i < FacetCounterTexts.Length; i++)
        {            
            FacetCounterTexts[i].SetText(0.ToString()); // tie this into resetting the facet counters
        }

        PointMultTimeText.transform.parent.gameObject.SetActive(false);

        EndGameItems.SetActive(false);
        ClickToStartButton.gameObject.SetActive(true);
        FacetCollectIcon.enabled = false;
        ShieldIcon.enabled = false;
    }

    

    public void Init()
    {
        MCP = FindObjectOfType<MCP>();
        Dax = FindObjectOfType<Dax>(); // moui 
        Player = FindObjectOfType<Player>();    
        FacetCollectIcon = FacetCollectActivateButton.gameObject.GetComponent<UnityEngine.UI.Image>();
        ShieldIcon = ShieldActivateButton.gameObject.GetComponent<UnityEngine.UI.Image>();        
        ResetForGameStart();                
    }
    public void SetFacetColorText(Facet.eFacetColors color, int value)
    {
        FacetCounterTexts[(int)color].SetText(value.ToString());
    }
       
    // Update is called once per frame
    void Update()
    {
        Dax.LevelTime -= Time.deltaTime; // moui timer/score - move this to Dax.cs
        SetTimerText();                
    }

    
   
}
