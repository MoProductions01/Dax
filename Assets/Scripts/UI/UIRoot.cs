
using System;
using System.Threading;
using TMPro;
using UnityEngine;

/// <summary>
/// Root object for the UI prefab tree
/// </summary>
public class UIRoot : MonoBehaviour
{   
    // Button references for various objects on the UI           
    [field: SerializeField] public GameObject ClickToStartButton {get; set;} // Button clicked to start game
    [field: SerializeField] public UnityEngine.UI.Button FacetCollectActivateButton {get; set;}
    [field: SerializeField] public UnityEngine.UI.Button ShieldActivateButton {get; set;}

    // Various text objects for the on screen data
    [field: SerializeField] public TMP_Text ScoreText {get; set;}
    [field: SerializeField] public TMP_Text TimerText {get; set;}   
    [field: SerializeField] public TMP_Text PointMultTimeText {get; set;}         
    [field: SerializeField] public TMP_Text[] FacetCounterTexts {get; set;} = new TMP_Text[(int)Facet.eFacetColors.ORANGE + 1];        
    
    // Image components on the buttons used to activate either Facet Collect or Shield 
    UnityEngine.UI.Image FacetCollectIcon {get; set;}
    UnityEngine.UI.Image ShieldIcon {get; set;}

    [field: SerializeField] public GameObject EndGameItems {get; set;} // Container for all of the end game UI items
    [field: SerializeField] private TMP_Text EndGameReason {get; set;} // Text for telling you why game ended
       
    // References to the most important objects in the game to tie into the UI
    Dax Dax {get; set;}
    MCP MCP {get; set;}
    Player Player {get; set;}

    /// <summary>
    /// Init the UI
    /// </summary>
    public void Init()
    {
        // First get references to the GameObjects we need
        MCP = FindObjectOfType<MCP>();
        Dax = FindObjectOfType<Dax>(); 
        Player = FindObjectOfType<Player>();    
        FacetCollectIcon = FacetCollectActivateButton.gameObject.GetComponent<UnityEngine.UI.Image>();
        ShieldIcon = ShieldActivateButton.gameObject.GetComponent<UnityEngine.UI.Image>();        
        
        ClickToStartButton.gameObject.SetActive(true); // Turn on the start button
        ResetForGameStart(); // Reset all the UI for the start of a game
    }

    /// <summary>
    /// Sets up the UI for the start of a new game
    /// </summary>
    public void ResetForGameStart()
    {   
        // Reset Timer             
        Dax.LevelTime = Dax.DEFAULT_LEVEL_TIME;                
        SetTimerText(Dax.LevelTime);

        // Reset Score
        Dax.Score = 0;
        ScoreText.SetText(Dax.Score.ToString()); // Reset score
        
        // Reset Facet Counters (the number collected is reset in MCP.ResetWheel since this used to be a multi-wheel game
        for (int i = 0; i < FacetCounterTexts.Length; i++)
        {            
            FacetCounterTexts[i].SetText(0.ToString());
        }

        PointMultTimeText.transform.parent.gameObject.SetActive(false); // Shut off point mod timer
        EndGameItems.SetActive(false); // Shut off the end game container
        // Shut off the action buttons
        FacetCollectActivateButton.gameObject.SetActive(false);
        //FacetCollectIcon.enabled = false; 
       // ShieldIcon.enabled = false;
       ShieldActivateButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Starts the game on the Start Game button press
    /// </summary>
    public void OnClickStartButton()
    {
        Dax.StartGame();
    }

    /// <summary>
    /// Resets and restarts the game from the end game menu
    /// </summary>
    public void OnClickTryAgainButton()
    {        
        ResetForGameStart(); // reset the UI
        Dax.ResetPuzzleFromSave(); // reset the puzzle save data
        Dax.StartGame();
    }            

    /// <summary>
    /// Sets the sprite for the facet collect icon or turns it of
    /// </summary>
    /// <param name="isActive">False turns it off, True sets it up</param>
    /// <param name="facetCollectType">Type of facet collect icon we're showing</param>
    public void ToggleFacetCollectIcon(bool isActive, FacetCollect.eFacetCollectTypes facetCollectType)
    {
        //FacetCollectIcon.enabled = isActive; // Enable or disable the icon itself
        FacetCollectActivateButton.gameObject.SetActive(isActive);
        if(isActive == true)
        {   // If isActive is true, then load up and set the sprite
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
     
    /// <summary>
    /// Sets the sprite of the activate shield icon or turns it off
    /// </summary>
    /// <param name="isActive">False turns it off, True sets it up</param>
    /// <param name="shieldType">Type of facet collect icon we're showing</param>
    public void ToggleShieldIcon(bool isActive, Shield.eShieldTypes shieldType)
    {
        //ShieldIcon.enabled = isActive; // Enable or disable the icon itself
        ShieldActivateButton.gameObject.SetActive(isActive);
        if(isActive == true)
        {   // If isActive is true, then load up and set the sprite
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

    /// <summary>
    /// Callback from the facet collect button
    /// </summary>
    public void OnClickFacetCollectButton()
    {        
        Player.ActivateFacetCollect();
    }

    /// <summary>
    /// Callback from the activate shield button
    /// </summary>
    public void OnClickShieldButton()
    {        
        Player.ActivateShield();

    }   

    
    /// <summary>
    /// Toggles the end game stuff and sets up the text
    /// </summary>
    /// <param name="endGameReason">Reason for game ending</param>
    /// <param name="isActive">True or False</param>
    public void ShowEndGame(string endGameReason, bool isActive)
    {                
        EndGameItems.gameObject.SetActive(isActive);
       // EndGameItems.GetComponentInChildren<TMP_Text>().SetText("Game Over: " + endGameReason);      
       EndGameReason.text = "Game Over: " + endGameReason;
    }

    /// <summary>
    /// Creates a string representing a float time to look like a normal timer
    /// </summary>
    /// <param name="timer">Float time value to create the string from</param>
    /// <returns>The created string</returns>
    string CreateTimerText(float timer)
    {
        int minutes = Mathf.FloorToInt(timer / 60f);     
        int seconds = Mathf.CeilToInt(timer % 60f);                                 
        if(seconds == 60)
        {   // Adjusts the value if the seconds is 60. For example, instead of
            // seeing 1:60 we'd see 2:00.  It's hacky but finding a better way is on the todo list
            minutes += 1;
            seconds = 0;
        }    

        return minutes + ":" + seconds.ToString("00");
    }
            
    /// <summary>
    /// Sets the timer text by string
    /// </summary>
    /// <param name="text">String to set the text to</param>
    public void SetTimerText(string text)
    {
        TimerText.text = text;
    }

    /// <summary>
    /// Sets the timer text by float value.  Calls the text setting version
    /// </summary>
    /// <param name="timeLeft">Time left to display</param>
    public void SetTimerText(float timeLeft)
    {                                                   
        SetTimerText(CreateTimerText(timeLeft));
    }
  
    /// <summary>
    /// Sets the Mod Timer left text 
    /// </summary>
    /// <param name="pointModTimer">Time left on the mod</param>
    public void SetPointModTimerText(float pointModTimer)
    {                   
        PointMultTimeText.text = CreateTimerText(pointModTimer);                        
    }
        
    /// <summary>
    /// Sets the string for a facet counter
    /// </summary>
    /// <param name="color">Color of the facet</param>
    /// <param name="value">Value to convert to text</param>
    public void SetFacetColorText(Facet.eFacetColors color, int value)
    {
        FacetCounterTexts[(int)color].SetText(value.ToString());
    }                 
}
