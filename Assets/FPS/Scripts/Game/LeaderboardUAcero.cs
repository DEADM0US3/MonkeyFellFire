using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;

public class LeaderboardUIAcero : MonoBehaviour
{
    public LeaderboardManagerAcero leaderboard;

    void Start()
    {
        leaderboard.ShowTopTimes(LeaderboardManagerAcero.GameMode.ModoB);
    }
}
