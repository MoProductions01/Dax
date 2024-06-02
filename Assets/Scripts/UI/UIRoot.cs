using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIRoot : MonoBehaviour
{
    public Camera UICamera;
    public GameObject ClickToStartButton;

   // public TextMeshPro TimerText;
   // public TextMeshPro ScoreText;
    public TMP_Text ScoreText;
    public TMP_Text TimerText;    
    //public GameObject[] ColorCounters;
    public TMP_Text[] FacetCounterTexts = new TMP_Text[(int)Facet.eFacetColors.ORANGE + 1];
    public GameObject EndGameItems;
   // public GameObject TryAgainButton;
    public GameObject ShieldIcon = null;
    public GameObject FacetCollectIcon = null;        
//    public GameObject MainMenuButton;
    Dax Dax;
    MCP MCP;

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
    
    public void ResetForGameStart()
    {        
        Dax.LevelTime = Dax.DEFAULT_LEVEL_TIME; // moui timer/score
        SetTimerText();
        ScoreText.SetText(0.ToString());
        
        for (int i = 0; i < FacetCounterTexts.Length; i++)
        {            
            FacetCounterTexts[i].SetText(0.ToString()); // tie this into resetting the facet counters
        }

        EndGameItems.SetActive(false);
        ClickToStartButton.gameObject.SetActive(true);
    }

    public void Init()
    {
        MCP = FindObjectOfType<MCP>();
        Dax = FindObjectOfType<Dax>(); // moui        
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

    public void DestroyShieldIcon()
    {
        if (ShieldIcon != null) DestroyImmediate(ShieldIcon);
    }
    public void ChangeShieldIcon(Shield shield )
    {
        DestroyShieldIcon();
        ShieldIcon = MCP.CreateShieldIcon(shield.ShieldType);
        ShieldIcon.transform.parent = this.transform;
    }   

    public void ChangeFacetCollectIcon(FacetCollect facetCollect)
    {        
        DestroyFacetCollectIcon(); // modelete update - make the points for items const
        FacetCollectIcon = MCP.CreateFacetCollectIcon(facetCollect.FacetCollectType);
        FacetCollectIcon.transform.parent = this.transform;
    }

    public void DestroyFacetCollectIcon()
    {
        if (FacetCollectIcon != null) DestroyImmediate(FacetCollectIcon);        
    }
}
