using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhoneReception : MiniGame {

    string prefabPath = "Prefabs/Objects/PhoneReception/";
    List<Transform> spawnPoints;

    public PhoneReception(MiniGameManager mg) : base(mg) { }

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
        base.introMessages = new string[] { "Grab the phone" };
        base.failureMessages = new string[] { "Mission Failed, We'll get'em next time.", "You tried", "FAILURE", "You missed the conference call!", "This will come up in your quarterly review." };
        base.successMessages = new string[] { "Well Done my child.", "SUCCESS", "Nice!" };

        //Base Game Objects + Lightpacks
        base.loadedObjects.Add(GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Lightpacks/Office/LP_DAY")));
        GameObject objectPack = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "ObjectPack"));
        objectPack.name = "ObjectPack";
        base.loadedObjects.Add(objectPack);

        Transform spawnList = objectPack.transform.FindChild("SpawnList");
        GameObject trigger = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "ReceptionTrigger"));
        base.loadedObjects.Add(trigger);
        trigger.transform.position = spawnList.GetChild(Random.Range(0, spawnList.childCount - 1)).position;

        manager.SetPlayerPosition(new Vector3(-3.565f, 0f, -6.386f), 0);

        GameObject phone = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "walruS7"), new Vector3(-3.85f, 2f, -2.124f), Quaternion.identity);
        loadedObjects.Add(phone);
        phone.GetComponent<Phone>().trigger = trigger;
    }

    public override void OnGameStart()
    {
        base.OnGameStart();
        base.DisplayMessage("Find 4 Bars!", 5 * 60, 0);
    }
    public override void OnTransitionOut()
    {
        base.OnTransitionOut();
        manager.signalLoad();
        manager.SelectNextGame();
    }
}
