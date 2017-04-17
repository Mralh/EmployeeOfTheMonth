using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupToWin : MiniGame {

	public PickupToWin (MiniGameManager m) : base(m) { }

    public override void OnTransitionIn()
    {
        base.OnTransitionIn();
        base.scoreRequired = 1;
        base.timeLimit = 5 * 60;
        base.startTimeLimit = 5 * 60;
        base.endTimeLimit = 5 * 60;
        base.introMessages = new string[] { "Pick up that cube!" };
        base.failureMessages = new string[] { "Cmon it wasnt that hard", "Ya dun goofed", "u tried" };
        base.successMessages = new string[] { "I'm proud of you, son." };
    }
    public override void OnTransitionOut()
    {
        base.OnTransitionOut();
        manager.signalLoad();
        manager.SelectNextGame();
    }

}
