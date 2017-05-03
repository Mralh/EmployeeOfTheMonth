using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopQuiz : MiniGame {

    List<Transform> spawnPoints;
    string prefabPath = "Prefabs/Objects/PopQuiz/";
    GameObject bomb;

    public PopQuiz(MiniGameManager mg) : base(mg) { }

    public override void ScenePrewarm()
    {
        base.ScenePrewarm();
        base.manager.RequestSceneChange("darkroom");
    }

    public override void OnTransitionIn()
    {
        base.OnTransitionIn();
        float r = Random.Range(0f, 1f);
        if (r <= 0.5f)
            manager.player.playBGM("quiztime");
        else
            manager.player.playBGM("eb");

        base.scoreRequired = 1;
        //Timers
        base.startTimeLimit = 2 * 60;
        base.endTimeLimit = 3 * 60;
        base.timeLimit = (int)((float)(20 * 60) / manager.speedModifier);

        //Messages
        base.introMessages = new string[] { "POP QUIZ" };
        base.failureMessages = new string[] { "Wrong you dummy", "Don't you go to Purdue?", "Not even close" };
        base.successMessages = new string[] { "WOW good answer!", "You can did it", "CORRECT" };

        //Base Game Objects + Lightpacks
        GameObject objectPack = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "ObjectPack"));
        objectPack.name = "ObjectPack";
        base.loadedObjects.Add(objectPack);

        GameObject quiz = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "QUIZ" + Random.Range(1, 7)));
        base.loadedObjects.Add(quiz);


        manager.SetPlayerPosition(new Vector3(0.36f, 0.0564f, 3f), 180);
    }

    public override void OnGameStart()
    {
        base.OnGameStart();
    }
    public override void OnGameEnd()
    {
        base.OnGameEnd();
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
}
