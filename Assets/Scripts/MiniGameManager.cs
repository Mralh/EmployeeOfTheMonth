using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGameManager : MonoBehaviour
{
    public List<MiniGame> homeGames = new List<MiniGame>();
    public List<MiniGame> officeGames = new List<MiniGame>();
    public List<MiniGame> factoryGames = new List<MiniGame>();
    public List<MiniGame> foodGames = new List<MiniGame>();
    public List<MiniGame> commuteGames = new List<MiniGame>();
    public List<MiniGame> teacherGames = new List<MiniGame>();

    public enum GameType { Home, Office, Factory, Food, Commute, Teacher };
    public GameType currentType = GameType.Home;

    public MiniGame currentGame;

    public int totalPlayerScores = 0;
    public int totalPlayerFailures = 0;
    public int totalPlayerSuccesses = 0;

    public int countMinigamesDay = 0;

    void FixedUpdate()
    {
    }

    public void SelectNextGame ()
    {
        
    }
}
