using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartButton : MiniGameObject
{
    public override void OnInteractPressed(GameObject hand)
    {
        base.OnInteractPressed(hand);
        MiniGameManager.singleton.signalLoad();
        MiniGameManager.singleton.startNewDay();
    }
}
