using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class TopScoresManager : MonoBehaviour
{
    [System.Serializable]
    public class ScoreEntry
    {
        public string playerName;
        public int score;

        public ScoreEntry(string playerName, int score)
        {
            this.playerName = playerName.Length > 6 ? playerName.Substring(0, 6) : playerName;
            this.score = score;
        }
    }

    public int leaderboardSize = 5; // Number of top scores to keep
    private List<ScoreEntry> leaderboard;
    private const string LeaderboardKey = "Leaderboard";

    void Start()
    {
        // Load leaderboard or initialize a new one
        LoadLeaderboard();
        DisplayLeaderboard();
    }

    public void AddScore(string playerName, int score)
    {
        
        LoadLeaderboard();
        // Add the new score
        leaderboard.Add(new ScoreEntry(playerName, score));

        // Sort the leaderboard by score (highest first)
        leaderboard.Sort((entry1, entry2) => entry2.score.CompareTo(entry1.score));

        // Trim the leaderboard to the desired size
        if (leaderboard.Count > leaderboardSize)
            leaderboard.RemoveAt(leaderboard.Count - 1);

        // Save the updated leaderboard
        SaveLeaderboard();
        DisplayLeaderboard();
    }

    private void SaveLeaderboard()
    {
        for (int i = 0; i < leaderboard.Count; i++)
        {
            PlayerPrefs.SetString(LeaderboardKey + "_Name_" + i, leaderboard[i].playerName);
            PlayerPrefs.SetInt(LeaderboardKey + "_Score_" + i, leaderboard[i].score);
        }

        // Clear any leftover entries from previous sessions
        for (int i = leaderboard.Count; PlayerPrefs.HasKey(LeaderboardKey + "_Name_" + i); i++)
        {
            PlayerPrefs.DeleteKey(LeaderboardKey + "_Name_" + i);
            PlayerPrefs.DeleteKey(LeaderboardKey + "_Score_" + i);
        }

        PlayerPrefs.Save();
    }

    private void LoadLeaderboard()
    {
        leaderboard = new List<ScoreEntry>();

        for (int i = 0; ; i++)
        {
            string nameKey = LeaderboardKey + "_Name_" + i;
            string scoreKey = LeaderboardKey + "_Score_" + i;

            if (PlayerPrefs.HasKey(nameKey) && PlayerPrefs.HasKey(scoreKey))
            {
                string playerName = PlayerPrefs.GetString(nameKey);
                int score = PlayerPrefs.GetInt(scoreKey);
                leaderboard.Add(new ScoreEntry(playerName, score));
            }
            else
            {
                break;
            }
        }
    }

    public TextMeshProUGUI leaderboardText; // Add this if using TextMeshPro

    private void DisplayLeaderboard()
    {
        leaderboardText.text = "Top Scores:\n";

        for (int i = 0; i < leaderboard.Count; i++)
        {
            leaderboardText.text += $"{i + 1}. {leaderboard[i].playerName}: {leaderboard[i].score}\n";
        }
    }
}
