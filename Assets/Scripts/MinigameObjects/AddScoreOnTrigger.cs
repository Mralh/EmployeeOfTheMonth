using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddScoreOnTrigger : MonoBehaviour {

    public string targetLayer, targetObjectName, targetTag;
    public int score = 1;
    public bool effect = false;

	void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == null)
            return;
        if (targetLayer != null && other.gameObject.layer == 1 << LayerMask.NameToLayer("targetLayer"))
            OnActivate(other.gameObject);
        else if (targetObjectName != null && other.gameObject.name.Equals(targetObjectName))
            OnActivate(other.gameObject);
        else if (targetTag != null && other.gameObject.tag.Equals(targetTag))
            OnActivate(other.gameObject);
    }

    void OnActivate(GameObject g)
    {
        MiniGameManager.singleton.currentGame.ChangeScore(0, score);
        if (effect && 
            (MiniGameManager.singleton.currentGame.state == MiniGame.GameState.INPROGRESS || MiniGameManager.singleton.currentGame.state == MiniGame.GameState.START))
        {
            GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/winFX"), g.transform.position, Quaternion.identity);
        }
    }
}
