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
    public List<MiniGame> transitions = new List<MiniGame>();


    public enum GameType { Home, Office, Factory, Food, Commute, Teacher };
    public GameType currentType = GameType.Home;
    public GameType typeOfTheDay = GameType.Office;

    public enum GameState { Cooldown, Game, Transition };

    public MiniGame currentGame;

    public int totalPlayerScores = 0;
    public int totalPlayerFailures = 0;
    public int totalPlayerSuccesses = 0;

    public int countMinigamesDay = 0;

    public Player player;

    public Queue<MiniGame> todaysGames = new Queue<MiniGame>();

    int loadTimer = 0;
    bool loading = false;

    void Start()
    {
        //Add home games
        homeGames.Add(new PickupToWin(this));

        startNewDay();
    }

    void FixedUpdate()
    {
        if (loading)
        {
            if (loadTimer < 60)
                loadTimer++;
            else
            {
                loading = false;
            }
            return;
        }

        if (currentGame != null)
        {
            currentGame.FixedUpdate();
            //Debug.Log("update");
            player.setClock(currentGame.timer, currentGame.timeLimit);
            if (player.isStaticScreen && currentGame.ready)
                player.ToggleStatic();
        }
    }

    public void SelectNextGame ()
    {
        currentGame.NullState();
        currentGame = todaysGames.Dequeue();
        if (currentGame == null)
        {
            startNewDay();
        }
    }

    public void startNewDay()
    {
        Debug.Log("Start game");
        todaysGames.Clear();
        todaysGames.Enqueue(homeGames[0]);
        currentGame = todaysGames.Dequeue();
        return;

        int numHomeGamesPre = Random.Range(1, 2);
        int numHomeGamesPost = Random.Range(0, 1);

        int numCommuteGames = Random.Range(0, 1);

        int numWorkGames = 7 - numCommuteGames - numHomeGamesPre - numHomeGamesPost;

        int random = Random.Range(0, 100);
        if (random < 101) //75
            typeOfTheDay = GameType.Office;
        else if (random < 85)
            typeOfTheDay = GameType.Factory;
        else if (random < 95)
            typeOfTheDay = GameType.Food;
        else
            typeOfTheDay = GameType.Teacher;

        homeGames = shuffle(homeGames);
        officeGames = shuffle(officeGames);
        commuteGames = shuffle(commuteGames);

        todaysGames.Enqueue(transitions[Random.Range(0, transitions.Count - 1)]);
        int i = 0;
        for (int j = 0; j < numHomeGamesPre; j++)
            todaysGames.Enqueue(homeGames[i++ % homeGames.Count]);

        i = 0;
        for (int j = 0; j < numCommuteGames; j++)
            todaysGames.Enqueue(commuteGames[i++ % commuteGames.Count]);

        i = 0;
        for (int j = 0; j < numWorkGames; j++)
            todaysGames.Enqueue(officeGames[i++ % officeGames.Count]);

        i = numHomeGamesPre;
        for (int j = 0; j < numHomeGamesPost; j++)
            todaysGames.Enqueue(homeGames[i++ % homeGames.Count]);

        currentGame = todaysGames.Dequeue();
    }

    public static List<MiniGame> shuffle(List<MiniGame> aList)
    {

        System.Random _random = new System.Random();

        MiniGame myGO;

        int n = aList.Count;
        for (int i = 0; i < n; i++)
        {
            // NextDouble returns a random number between 0 and 1.
            // ... It is equivalent to Math.random() in Java.
            int r = i + (int)(_random.NextDouble() * (n - i));
            myGO = aList[r];
            aList[r] = aList[i];
            aList[i] = myGO;
        }

        return aList;
    }

    public void signalLoad()
    {
        player.ToggleStatic();
    }
}
