using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallPit : MiniGame
{
    List<Transform> spawnPoints;
    string prefabPath = "Prefabs/Objects/Ballpit/";
    GameObject bomb;

    public BallPit(MiniGameManager mg) : base(mg) { }

    public override void ScenePrewarm()
    {
        base.ScenePrewarm();
        base.manager.RequestSceneChange("darkroom");
    }

    public override void OnTransitionIn()
    {
        base.OnTransitionIn();
        manager.RequestNormalBGM();

        base.scoreRequired = 1;
        //Timers
        base.startTimeLimit = 2 * 60;
        base.endTimeLimit = 3 * 60;
        base.timeLimit = (int)((float)(20 * 60) / manager.speedModifier);

        //Messages
        base.introMessages = new string[] { "It's your son's Birthday!" };
        base.failureMessages = new string[] { "Goodbye, son..." };
        base.successMessages = new string[] { "You saved a day!" };

        //Base Game Objects + Lightpacks
        GameObject objectPack = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "ObjectPack"));
        objectPack.name = "ObjectPack";
        base.loadedObjects.Add(objectPack);

        //Ball spawners
        Transform spawnList = objectPack.transform.FindChild("SpawnList");
        spawnPoints = new List<Transform>(); //Makes a list of possible transformations (Locations)?
        foreach (Transform child in spawnList)
        {
            if (child != spawnList)
            {
                spawnPoints.Add(child);
            }
        }

        int bombIndex = Random.Range(0, spawnPoints.Count - 1);
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (i == bombIndex)
            {
                base.loadedObjects.Add(bomb = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "bomb"),
                    spawnPoints[i].position,
                    Quaternion.Euler(0, Random.Range(-180, 180), 0)));
            }
            else
            {
                
                base.loadedObjects.Add(GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "ball"),
                    spawnPoints[i].position,
                    Quaternion.Euler(0, Random.Range(-180, 180), 0)));
            }
        }

        manager.SetPlayerPosition(new Vector3(0, 0f, 1), 180);
    }

    public override void OnGameStart()
    {
        base.OnGameStart();
        base.DisplayMessage("THERES A BOMB IN THE BALLPIT\n GET IT OUT", 5 * 60, 0);
    }
    public override void OnGameEnd()
    {
        base.OnGameEnd();
        if (base.playerScores < scoreRequired)
            base.loadedObjects.Add(GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Explosion"), bomb.transform.position, Quaternion.identity));

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
    }
}
