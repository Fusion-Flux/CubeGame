using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UniversalValues : MonoBehaviour
{
    
    public bool paused = false;
    public bool levelComplete = false;

    public int finalScore;
    public Canvas LevelComplete;
    public Canvas Scoreboard;
    public Canvas HighScores;
    public Canvas Paused;
    public Canvas Leaderboard;
    
    public TopScoresManager topScoresManager;
    public TMP_InputField  nameInputField;
    
    public TMP_Text FinalScore;
    public TMP_Text HighScoreFinalScore;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Time.timeScale = 1;
    }
    public void SubmitScoreScreen()
    {
        HighScores.gameObject.SetActive(true);
        LevelComplete.gameObject.SetActive(false);
    }
    
    public void LeaderboardScreen()
    {
        Leaderboard.gameObject.SetActive(true);
        HighScores.gameObject.SetActive(false);
    }
    
    public void SubmitScore()
    {
        int score = finalScore;
        string playerName = nameInputField.text;
        topScoresManager.AddScore(playerName, score);
    }
}
