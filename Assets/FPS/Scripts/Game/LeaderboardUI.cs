using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    public LeaderboardManager leaderboard;

    void Start()
    {
        leaderboard.ShowTopTimes(LeaderboardManager.GameMode.ModoA);
    }
}
