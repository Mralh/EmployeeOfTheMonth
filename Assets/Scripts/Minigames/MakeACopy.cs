using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakeACopy : MiniGame {

    string prefabPath = "Prefabs/Objects/Copier/";
    List<Transform> spawnPoints;

    List<string> selectedButtonTexts = new List<string> { "Make a Copy" };

    public MakeACopy(MiniGameManager mg) : base(mg) { }

    public override void ScenePrewarm()
    {
        base.ScenePrewarm();
        base.manager.RequestSceneChange("office");
    }

    public override void OnTransitionIn()
    {
        base.OnTransitionIn();
        manager.RequestNormalBGM();
        base.scoreRequired = 1;
        //Timers
        base.startTimeLimit = 5 * 60;
        base.endTimeLimit = 5 * 60;
        base.timeLimit = (int)((float)(20 * 60) / manager.speedModifier);

        //Messages
        base.introMessages = new string[] { "Make a Copy!" };
        base.failureMessages = new string[] { "How could this happen to me?", "Hello darkness my old friend", "No." };
        base.successMessages = new string[] { "Well Done!", "SUCCESS", "Nice!", "I knew you could do it, my sweet summer butterfly!" };

        //Base Game Objects + Lightpacks
        base.loadedObjects.Add(GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Lightpacks/Office/LP_DAY")));
        GameObject objectPack = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "GameObjectCopy"));
        objectPack.name = "GameObjectCopy";
        base.loadedObjects.Add(objectPack);

        Transform spawnList = objectPack.transform.FindChild("SpawnList");
        GameObject copier = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "Copier"));
        base.loadedObjects.Add(copier);

        copier.transform.position = spawnList.GetChild(Random.Range(0, spawnList.childCount - 1)).position;

        List<string> wrongButtonTexts = new List<string> { "Bomb the Russians", "Buy a Car", "Make a Kopy", "Okapi", "Fire Jeff", "Tune Radio", "Sell House on eBay", "Make a Coopy", "Quit Job", "Maek a Copy", "Swear at Copier", "Forgive your wife", "Not Not Not Copy" };

        for (int i = 1; i < 6; i++) {
            int index = Random.Range(0, wrongButtonTexts.Count - 1);
            var name = wrongButtonTexts[index];
            selectedButtonTexts.Add(name);
        }

        //Add the score update to the correct button
        GameObject Button1 = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "Button1"));
        Button1.AddComponent<AddScoreOnInteract>();
        Button1.GetComponent<AddScoreOnInteract>().score = 1;
        base.loadedObjects.Add(Button1);

        GameObject Button2 = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "Button2"));
        Button2.AddComponent<AddScoreOnInteract>();
        Button1.GetComponent<AddScoreOnInteract>().score = -1;
        base.loadedObjects.Add(Button1);

        GameObject Button3 = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "Button3"));
        Button3.AddComponent<AddScoreOnInteract>();
        Button3.GetComponent<AddScoreOnInteract>().score = -1;
        base.loadedObjects.Add(Button1);

        //Need to set all the texts of the buttons here

        GameObject Button1Text = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "Button1Text"));
        //Button1Text.AddComponent<Text>(selectedButtonTexts.Remove());
        base.loadedObjects.Add(Button1Text);

        GameObject Button2Text = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "Button2Text"));
        //Button1Text.AddComponent<Text>(selectedButtonTexts.Remove());
        base.loadedObjects.Add(Button2Text);

        GameObject Button3Text = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "Button3Text"));
        //Button1Text.AddComponent<Text>(selectedButtonTexts.Remove());
        base.loadedObjects.Add(Button3Text);


        manager.SetPlayerPosition(new Vector3(-3.565f, 0f, -6.386f), 0);

    }

    public override void OnGameStart()
    {
        base.OnGameStart();
        base.DisplayMessage("Press the Right Button!", 5 * 60, 0);
    }
    public override void OnTransitionOut()
    {
        base.OnTransitionOut();
        manager.signalLoad();
        manager.SelectNextGame();
    }
    public override void Tick()
    {
        if (AllDone())
            manager.forceEndMinigame();
        if (playerScores < 0)
            manager.forceEndMinigame();
}

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
