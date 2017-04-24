using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddScoreOnTrigger : MonoBehaviour {

    public string targetLayer, targetObjectName, targetTag;
    public int score = 1;

	void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == null)
            return;
        if (targetLayer != null && other.gameObject.layer == 1 << LayerMask.NameToLayer("targetLayer"))
            OnActivate();
        else if (targetObjectName != null && other.gameObject.name.Equals(targetObjectName))
            OnActivate();
        else if (targetTag != null && other.gameObject.tag.Equals(targetTag))
            OnActivate();
    }

    void OnActivate()
    {
        MiniGameManager.singleton.currentGame.ChangeScore(0, score);
    }
}
