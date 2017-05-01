using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Utility;

public class CloseElevator : MiniGame
{
    string prefabPath = "Prefabs/Objects/CloseElevator/";

    public CloseElevator(MiniGameManager mgr) : base(mgr) { }

    public override void ScenePrewarm()
    {
        base.ScenePrewarm();

        base.manager.RequestSceneChange("office");
    }
    public override void OnTransitionIn()
    {
        base.OnTransitionIn();
        manager.RequestNormalBGM();
        GameObject.Find("Elevator").GetComponent<ElevatorControls>().OpenDoor();
        base.scoreRequired = 25;

        base.startTimeLimit = 2 * 60;
        base.endTimeLimit = 3 * 60;
        base.timeLimit = (int)((float)(10 * 60) / manager.speedModifier);

        base.introMessages = new string[] { "" };
        base.failureMessages = new string[] { "Too bad" };
        base.successMessages = new string[] { "goo djob" };

        base.loadedObjects.Add(GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/Lightpacks/Office/LP_ELEVATOR")));
        GameObject objectPack = GameObject.Instantiate(Resources.Load<GameObject>(prefabPath + "ObjectPack"));
        objectPack.name = "ObjectPack";
        base.loadedObjects.Add(objectPack);

        objectPack.transform.FindChild("walkinman").gameObject.GetComponent<AutoMoveAndRotate>().moveUnitsPerSecond.value = 
            new Vector3(0, 0, 8 / (10f / manager.speedModifier));


        base.DisplayMessage("Close the Elevator!", 5 * 60, 0);

        manager.SetPlayerPosition(objectPack.transform.FindChild("spawnpoint").position, 180);
        
        
    }
    public override void OnTransitionOut()
    {
        base.OnTransitionOut();
        GameObject.Find("Elevator").GetComponent<ElevatorControls>().CloseDoor();
        manager.signalLoad();
        manager.SelectNextGame();

    }
    public override void Tick()
    {
        if (AllDone())
        {
            manager.forceEndMinigame();
            GameObject.Find("Elevator").GetComponent<ElevatorControls>().CloseDoor();
        }
        if (base.playerScores < 0)
            manager.forceEndMinigame();
    }


}
