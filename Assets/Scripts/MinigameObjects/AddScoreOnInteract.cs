using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddScoreOnInteract : MiniGameObject
{
    public int score = 1;

    public bool effect = false;

    public override void OnInteractPressed(GameObject hand)
    {
        base.OnInteractPressed(hand);
        MiniGameManager.singleton.currentGame.ChangeScore(0, score);
        if (effect &&
            (MiniGameManager.singleton.currentGame.state == MiniGame.GameState.INPROGRESS || MiniGameManager.singleton.currentGame.state == MiniGame.GameState.START))
        {
            GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/winFX"), transform.position, Quaternion.identity);
        }
    }
}
