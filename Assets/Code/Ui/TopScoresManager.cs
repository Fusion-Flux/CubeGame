using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class TopScoresManager : MonoBehaviour
{
    [System.Serializable]
    public class TimeEntry
    {
        public string playerName;
        public float time;

        public TimeEntry(string playerName, float time)
        {
            this.playerName = playerName.Length > 6 ? playerName.Substring(0, 6) : playerName;
            this.time = time;
        }
    }

    public int leaderboardSize = 5; // Number of top times to keep
    private List<TimeEntry> leaderboard;
    private const string LeaderboardKey = "Leaderboard";

    void Start()
    {
        // Load leaderboard or initialize a new one
        LoadLeaderboard();
        DisplayLeaderboard();
    }

    public void AddTime(string playerName, float time)
    {
        LoadLeaderboard();

        // Check for duplicate entries
        foreach (var entry in leaderboard)
        {
            if (entry.playerName == playerName && entry.time == time)
            {
                // Duplicate found, do not add the new entry
                DisplayLeaderboard();
                return;
            }
        }

        // Add the new time
        leaderboard.Add(new TimeEntry(playerName, time));

        // Sort the leaderboard by time (lowest first)
        leaderboard.Sort((entry1, entry2) => entry1.time.CompareTo(entry2.time));

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
            PlayerPrefs.SetFloat(LeaderboardKey + "_Time_" + i, leaderboard[i].time);
        }

        // Clear any leftover entries from previous sessions
        for (int i = leaderboard.Count; PlayerPrefs.HasKey(LeaderboardKey + "_Name_" + i); i++)
        {
            PlayerPrefs.DeleteKey(LeaderboardKey + "_Name_" + i);
            PlayerPrefs.DeleteKey(LeaderboardKey + "_Time_" + i);
        }

        PlayerPrefs.Save();
    }

    private void LoadLeaderboard()
    {
        leaderboard = new List<TimeEntry>();

        for (int i = 0; ; i++)
        {
            string nameKey = LeaderboardKey + "_Name_" + i;
            string timeKey = LeaderboardKey + "_Time_" + i;

            if (PlayerPrefs.HasKey(nameKey) && PlayerPrefs.HasKey(timeKey))
            {
                string playerName = PlayerPrefs.GetString(nameKey);
                float time = PlayerPrefs.GetFloat(timeKey);
                leaderboard.Add(new TimeEntry(playerName, time));
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
        leaderboardText.text = "Top Times:\n";

        for (int i = 0; i < leaderboard.Count; i++)
        {
            leaderboardText.text += $"{i + 1}. {leaderboard[i].playerName}: {FormatTime(leaderboard[i].time)}\n";
        }
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        int milliseconds = Mathf.FloorToInt((time * 100) % 100);

        return string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }
}
