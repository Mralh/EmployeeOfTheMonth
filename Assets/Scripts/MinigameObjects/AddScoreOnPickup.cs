using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddScoreOnPickup : MiniGameObject
{
    public int score = 1;

    public override void OnGrabbed(GameObject hand)
    {
        base.OnGrabbed(hand);
        getMiniGame().ChangeScore(0, score);
    }
}
