using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIRoot : MonoBehaviour
{
    public Camera UICamera;
    public GameObject PreGameButton;
    public TextMeshPro TimerText;
    public TextMeshPro ScoreText;
    public GameObject[] ColorCounters;
    public TextMeshPro[] ColorCounterTexts = new TextMeshPro[(int)Facet.eFacetColors.ORANGE];
    public GameObject ShieldIcon = null;
    public GameObject FacetCollectIcon = null;
    public TextMeshPro EndGameText;
    public GameObject TryAgainButton;
    public GameObject MainMenuButton;
    Dax _Dax;
    MCP _MCP;

    public void ToggleEndGameItems(string endGameReason, bool isActive)
    {
        EndGameText.SetText("Game Over: " + endGameReason);
        EndGameText.gameObject.SetActive(isActive);
        TryAgainButton.SetActive(isActive);
        MainMenuButton.SetActive(isActive);
    }
    public void DestroyShieldIcon()
    {
        if (ShieldIcon != null) DestroyImmediate(ShieldIcon);
    }
    public void ChangeShieldIcon(Shield shield )
    {
        DestroyShieldIcon();
        ShieldIcon = _MCP.CreateShieldIcon(shield.ShieldType);
        ShieldIcon.transform.parent = this.transform;
    }

    public void ChangeFacetCollectIcon(FacetCollect facetCollect)
    {
        DestroyFacetCollectIcon();
        FacetCollectIcon = _MCP.CreateFacetCollectIcon(facetCollect.FacetCollectType);
        FacetCollectIcon.transform.parent = this.transform;
    }

    public void DestroyFacetCollectIcon()
    {
        if (FacetCollectIcon != null) DestroyImmediate(FacetCollectIcon);        
    }
    

    public void Init()
    {
        _MCP = FindObjectOfType<MCP>();
        _Dax = FindObjectOfType<Dax>();
        for (int i = 0; i < ColorCounters.Length; i++)
        {
            ColorCounterTexts[i] = ColorCounters[i].GetComponentInChildren<TextMeshPro>();            
        }
        SetTimerText();
        ScoreText.SetText(0.ToString());
        /*if(_Dax.CurWheel.VictoryCondition == Dax.eVictoryConditions.COLLECTION)
        {
            Material greyMaterial = Instantiate<Material>(Resources.Load<Material>("Dax/Color_Materials/_Grey"));
            Material whiteMaterial = _MCP.GetFacetMaterial(Facet.eFacetColors.WHITE);            
            for (int i = 0; i < ColorCounters.Length; i++)
            {
                ColorCounters[i].GetComponent<MeshRenderer>().material = greyMaterial;
                ColorCounterTexts[i].SetText("");                
            }
            ColorCounters[(int)Facet.eFacetColors.BLUE].GetComponent<MeshRenderer>().material = whiteMaterial;
            ColorCounterTexts[(int)Facet.eFacetColors.BLUE].SetText(0.ToString());
        }
        else
        {
            for (int i = 0; i < ColorCounters.Length; i++)
            {
                ColorCounters[i].GetComponent<MeshRenderer>().material = _MCP.GetFacetMaterial((Facet.eFacetColors)i);
                ColorCounterTexts[i].SetText(0.ToString());
            }
        }*/
        for (int i = 0; i < ColorCounters.Length; i++)
        {
            ColorCounters[i].GetComponent<MeshRenderer>().material = _MCP.GetFacetMaterial((Facet.eFacetColors)i);
            ColorCounterTexts[i].SetText(0.ToString());
        }
        PreGameButton.gameObject.SetActive(true);
    }
    public void SetFacetColorText(Facet.eFacetColors color, int value)
    {
        ColorCounterTexts[(int)color].SetText(value.ToString());
    }
    void SetTimerText()
    {
        int minutes = Mathf.FloorToInt(_Dax.LevelTime / 60f);
        int seconds = Mathf.FloorToInt(_Dax.LevelTime % 60f);
        if (minutes != 0) TimerText.SetText(minutes + ":" + seconds);
        else TimerText.SetText(":" + seconds);
    }

    // Update is called once per frame
    void Update()
    {
        _Dax.LevelTime -= Time.deltaTime;
        SetTimerText();
        
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = UICamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                switch(_Dax.GameState)
                {
                    case Dax.eGameState.RUNNING:
                        if (hit.collider.name.Contains("Shield_HUD"))
                        {
                            Debug.Log("Shield_HUD");
                            FindObjectOfType<Player>().ActivateShield();
                        }
                        else if (hit.collider.name.Contains("Facet_Collect_"))
                        {
                            Debug.Log("Facet_Collect_");
                            FindObjectOfType<Player>().ActivateFacetCollect();
                        }
                        break;
                    case Dax.eGameState.PRE_GAME:
                        if (hit.collider.name.Contains("Pre_Game"))
                        {
                            _Dax.StartGame();
                        }
                        break;
                    case Dax.eGameState.GAME_OVER:
                        if (hit.collider.name.Contains("Try_Again"))
                        {
                            ToggleEndGameItems("ignore", false);
                            _Dax.ResetPuzzleFromSave();
                        }
                        else if (hit.collider.name.Contains("Main_Menu"))
                        {
                            ToggleEndGameItems("ignore", false);
                            _Dax.ResetPuzzleFromSave();
                            _Dax.GameState = Dax.eGameState.PRE_GAME;
                            PreGameButton.SetActive(true);
                        }
                        break;
                }
                //Debug.Log("hit this on UICamera: " + hit.collider.name);
                
            }
        }
    }
}
