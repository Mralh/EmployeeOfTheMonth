using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BringDogToWorkDay : MiniGame
{
    List<Transform> spawnPoints;
    string prefabPath = "Prefabs/Objects/BYDTWD/";

    public BringDogToWorkDay(MiniGameManager mg) : base(mg) { }

    public override void ScenePrewarm()
    {
        base.ScenePrewarm();
        base.manager.RequestSceneChange("office");
    }

    public override void OnTransitionIn()
    {
        base.OnTransitionIn();
        manager.player.playBGM("sh2dog");
        
        base.scoreRequired = 1;
        //Timers
        base.startTimeLimit = 5*60;
        base.endTimeLimit = 5 * 60;
        base.timeLimit = (int)((float)(25 * 60) / manager.speedModifier);

        //Messages
        base.introMessages = new string[] { "Bring your dog to work day !! !" };
        base.failureMessages = new string[] { "The bad boy is still at large", "You tried", "FAILURE", "Mission failed, returning to base", "You have forsaken us all." };
        base.successMessages = new string[] { "The good dogs can rest easy", "SUCCESS", "Good Boy" };

        //Base Game Objects + Lightpacks
        base.loadedObjects.Add(GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Lightpacks/Office/LP_DAY")));
        GameObject objectPath = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "ObjectPack"));
        objectPath.name = "ObjectPack";
        base.loadedObjects.Add(objectPath);

        //Dog spawners
        Transform spawnList = objectPath.transform.FindChild("SpawnList");
        spawnPoints = new List<Transform>(); //Makes a list of possible transformations (Locations)?
        foreach (Transform child in spawnList)
        {
            if (child != spawnList)
            {
                spawnPoints.Add(child);
            }
        }

        int imposterIndex = Random.Range(0, spawnPoints.Count - 1);
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (i == imposterIndex)
            {
                base.loadedObjects.Add(GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "dogboy"),
                    spawnPoints[i].position,
                    Quaternion.Euler(0, Random.Range(-180, 180), 0)));
            }
            else
            {
                int dogChance = Random.Range(0, 100);
                if (dogChance < 50)
                {
                    base.loadedObjects.Add(GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "japdog_sit"),
                        spawnPoints[i].position,
                        Quaternion.Euler(0, Random.Range(-180, 180), 0)));
                }
                else if (dogChance < 95)
                {
                    base.loadedObjects.Add(GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "japdog"),
                        spawnPoints[i].position,
                        Quaternion.Euler(0, Random.Range(-180, 180), 0)));
                }
                else
                {
                    base.loadedObjects.Add(GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "mira"),
                        spawnPoints[i].position,
                        Quaternion.Euler(0, Random.Range(-180, 180), 0)));
                }
            }
        }

        manager.SetPlayerPosition(new Vector3(-3.565f, 0f, -6.386f), 0);
    }

    public override void OnGameStart()
    {
        base.OnGameStart();
        base.DisplayMessage("Find the imposter and put him in the trash!", 5 * 60, 0);
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
