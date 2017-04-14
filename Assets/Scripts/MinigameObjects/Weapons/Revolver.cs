using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Revolver : MiniGameObject
{
    int ammo = 6;
    GameObject handController;
    public override void OnGrabbed(GameObject hand)
    {
        base.OnGrabbed(hand);
        handController = hand;
    }
    public override void OnGrabRelease(GameObject hand)
    {
        base.OnGrabRelease(hand);
        handController = null;
    }
    public override void OnInteractPressed(GameObject hand)
    {
        base.OnInteractPressed(hand);
        if (grabbed)
        {
            Fire();
        }
    }

    void Fire()
    {
        if (ammo <= 0)
        {
            return;
        }
    }
}
