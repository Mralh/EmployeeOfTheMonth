using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGameObject : MonoBehaviour
{
    public bool grabbed = false;
    public bool interact = false;
    public bool hover = false;

    public bool equippable = false;

    public float holdDistance = 0.4f;

    public bool grabbable = true;
    public bool interactable = true;

    Vector3 scale;
    public float scaleWhenHeld = 1;

    void Start ()
    {
        scale = transform.localScale;
    }

    void FixedUpdate ()
    {
        if (grabbed)
            transform.localScale = Vector3.Lerp(transform.localScale, scaleWhenHeld * scale, 5f * Time.deltaTime);
        else
            transform.localScale = Vector3.Lerp(transform.localScale, scale, 5f * Time.deltaTime);
    }

	public virtual void OnInteractPressed(GameObject hand)
    {
        interact = true;
    }
    public virtual void OnInteractReleased(GameObject hand)
    {
        interact = false;
    }
    public virtual void OnInteractHeld(GameObject hand) { }
    

    public virtual void OnGrabbed(GameObject hand)
    {
        grabbed = true;
    }
    public virtual void OnGrabRelease(GameObject hand)
    {
        grabbed = false;
    }
    public virtual void OnHeld(GameObject hand) { }

    public virtual void OnHandHover(GameObject hand)
    {
        hover = true;
    }
    public virtual void OnHandExitHover(GameObject hand)
    {
        hover = false;
    }

    public MiniGame getMiniGame()
    {
        GameObject manager = GameObject.Find("MGManager");
        MiniGame g = manager.GetComponent<MiniGameManager>().currentGame;
        return g;
    }

}
