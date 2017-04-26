using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager singleton;
    public List<MiniGame> homeGames = new List<MiniGame>();
    public List<MiniGame> officeGames = new List<MiniGame>();
    public List<MiniGame> factoryGames = new List<MiniGame>();
    public List<MiniGame> foodGames = new List<MiniGame>();
    public List<MiniGame> commuteGames = new List<MiniGame>();
    public List<MiniGame> teacherGames = new List<MiniGame>();
    public List<MiniGame> transitions = new List<MiniGame>();

    string currentScene;


    public enum GameType { Home, Office, Factory, Food, Commute, Teacher };
    public GameType currentType = GameType.Home;
    public GameType typeOfTheDay = GameType.Office;

    public enum GameState { Cooldown, Game, Transition };

    public MiniGame currentGame;

    public int totalPlayerScores = 0;
    public int totalPlayerFailures = 0;
    public int totalPlayerSuccesses = 0;

    public int day = 0;
    int days = 5;

    public List<string>[] playlist;

    public Player player;

    public Queue<MiniGame> todaysGames = new Queue<MiniGame>();

    int loadTimer = 0;
    bool loading = false;
    public bool sceneReady = true;
    public float speedModifier = 1;

    void Start()
    {
        //Add home games
        homeGames.Add(new PhoneReception(this));
        homeGames.Add(new BringDogToWorkDay(this));
        SceneManager.sceneLoaded += setLoadedStatus;
        MiniGameManager.singleton = this;

        playlist = new List<string>[days];
        for (int i = 0; i < days; i++)
            playlist[i] = new List<string>();
        playlist[0].Add("infade");
        playlist[0].Add("simpleman");
        playlist[1].Add("creep");
        playlist[1].Add("simpleman");
        playlist[2].Add("autopiliot");
        playlist[3].Add("2p2e5");
        playlist[4].Add("dofearthereaper");
        playlist[4].Add("2p2e5");
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            signalLoad();
            startNewDay();
        }
    }

    void FixedUpdate()
    {
        if (loading)
        {
            if (loadTimer < 120)
                loadTimer++;
            else
            {
                loading = false;
                loadTimer = 0;
            }
            return;
        }

        if (currentGame != null && sceneReady)
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
        if (currentGame != null)
            currentGame.NullState();

        if (todaysGames.Count >= 1)
        {
            currentGame = todaysGames.Dequeue();
            currentGame.ScenePrewarm();
            speedModifier += 0.02f;
        }
        else
        {
            startNewDay();
        }
    }
    public void ForceNextGame(MiniGame mg)
    {
        currentGame.NullState();
        currentGame = mg;
        currentGame.ScenePrewarm();
    }

    public void startNewDay()
    {
        //signalLoad();
        if (day < days)
            day++;
        speedModifier += 0.1f;
        Debug.Log("Start game");
        todaysGames.Clear();
        homeGames = shuffle(homeGames);
        todaysGames.Enqueue(homeGames[0]);
        todaysGames.Enqueue(homeGames[1]);

        SelectNextGame();
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
            int r = i + (int)(_random.NextDouble() * (n - i));
            myGO = aList[r];
            aList[r] = aList[i];
            aList[i] = myGO;
        }

        return aList;
    }

    

    public void signalLoad()
    {
        if (!player.isStaticScreen)
            player.ToggleStatic();
        loading = true;
    }

    public void forceEndMinigame()
    {
        if (currentGame != null && currentGame.timer < currentGame.timeLimit - 1)
        {
            currentGame.timer = currentGame.timeLimit - 1;
        }
    }

    public void RequestSceneChange(string scene)
    {
        if (!scene.Equals(currentScene))
        {
            sceneReady = false;
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
            currentScene = scene;
        }
    }

    public void SetPlayerPosition(Vector3 pos, float yAngle)
    {
        if (player.name.Equals("VR Player"))
        {
            player.transform.FindChild("[CameraRig]").position = pos;
            /*player.transform.position = pos 
                - new Vector3(player.eyeCamera.transform.parent.localPosition.x, 0, player.eyeCamera.transform.parent.localPosition.z);*/
            //player.transform.eulerAngles = new Vector3(0, yAngle - player.eyeCamera.transform.parent.localEulerAngles.y, 0);
        }
        else
        {
            player.transform.position = pos;
            player.transform.eulerAngles = new Vector3(0, yAngle, 0);
        }
    }

    void setLoadedStatus(Scene s, LoadSceneMode lsm)
    {
        sceneReady = true;
    }

    public void RequestNormalBGM()
    {
        if (player.bgm.clip == null || !playlist[day - 1].Contains(player.bgm.clip.name))
            player.playBGM(playlist[day - 1][Random.Range(0, playlist[day - 1].Count - 1)]);
    }
}
