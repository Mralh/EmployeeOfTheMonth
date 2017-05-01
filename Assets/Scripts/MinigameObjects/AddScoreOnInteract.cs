using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddScoreOnInteract : MiniGameObject
{
    public int score = 1;
    public override void OnInteractPressed(GameObject hand)
    {
        base.OnInteractPressed(hand);
        MiniGameManager.singleton.currentGame.ChangeScore(0, score);
    }
}
