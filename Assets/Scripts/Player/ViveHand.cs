using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ViveHand : MonoBehaviour
{
    /* --------------------
           GLOBALS
   ----------------------- */

    public GameObject VRrig;
    private SteamVR_TrackedController device;

    //Grabbing variables
    bool grabInput = false;
    GameObject grabbedObject;
    Vector3 grabbedObjectRot;

    //Selection variables
    GameObject selectedObject;


    /* --------------------
      MONOBEHAVIOUR STUFF
   ----------------------- */
    void Start ()
    {
        device = GetComponent<SteamVR_TrackedController>();
        device.Gripped += OnGripPressed;
        device.Ungripped += OnGripReleased;
        device.TriggerClicked += OnTriggerPressed;
        device.TriggerUnclicked += OnTriggerReleased;
    }
	
	void FixedUpdate ()
    {
        //Grab hold behaviour
		if (grabbedObject != null)
        {
            Vector3 targetPos = transform.position + (transform.forward * grabbedObject.GetComponent<MiniGameObject>().holdDistance);
            grabbedObject.GetComponent<Rigidbody>().useGravity = false;
            grabbedObject.transform.eulerAngles = transform.eulerAngles;
            Vector3 targetVel = targetPos - grabbedObject.transform.position;
            float speed = Mathf.Clamp(100 * Mathf.Abs(targetVel.magnitude), 0, 200);
            targetVel = speed * targetVel.normalized;
            grabbedObject.GetComponent<Rigidbody>().velocity = targetVel;
            grabbedObject.GetComponent<MiniGameObject>().OnHeld(gameObject);

            if (grabbedObject.GetComponent<MiniGameObject>().equippable)
            {
                transform.FindChild("Model").gameObject.SetActive(false);


            }
        }

        //Interact hold behaviour
        if (selectedObject != null)
        {
            selectedObject.GetComponent<MiniGameObject>().OnInteractHeld(gameObject);
            
        }
	}

    /* --------------------
         INPUT EVENTS
    ----------------------- */
    void OnGripPressed(object sender, ClickedEventArgs e)
    {
        if (grabbedObject != null)
        {
            ReleaseGrab();
            return;
        }

        GameObject selectionTemp = GetObjectNearHand();
        if (selectionTemp == null)
            return;
        grabbedObject = selectionTemp;
        grabbedObject.GetComponent<MiniGameObject>().OnGrabbed(gameObject);

        grabbedObjectRot = grabbedObject.transform.eulerAngles - transform.eulerAngles;
        grabInput = true;
    }
    void OnGripReleased(object sender, ClickedEventArgs e)
    {
        if (grabbedObject != null)
        {
            if (!grabbedObject.GetComponent<MiniGameObject>().equippable)
            {
                ReleaseGrab();
            }
        }
    }

    void OnTriggerPressed(object sender, ClickedEventArgs e)
    {
        if (grabbedObject != null)
        {
            grabbedObject.GetComponent<MiniGameObject>().OnInteractPressed(gameObject);
            selectedObject = grabbedObject;
            return;
        }

        GameObject selectionTemp = GetObjectNearHand();
        if (selectionTemp == null)
            return;
        selectedObject = selectionTemp;
        selectedObject.GetComponent<MiniGameObject>().OnInteractPressed(gameObject);

    }
    void OnTriggerReleased(object sender, ClickedEventArgs e)
    {
        if (selectedObject != null)
        {
            selectedObject.GetComponent<MiniGameObject>().OnInteractReleased(gameObject);
            selectedObject = null;
        }
    }

    /* --------------------
         HELPER METHODS
    ----------------------- */
    public void ReleaseGrab()
    {
        if (grabbedObject != null)
        {
            grabbedObject.GetComponent<MiniGameObject>().OnGrabRelease(gameObject);
            grabbedObject.GetComponent<Rigidbody>().useGravity = true;
            if (grabbedObject.GetComponent<MiniGameObject>().equippable)
                transform.FindChild("Model").gameObject.SetActive(true);
        }

        grabbedObject = null;
    }

    GameObject GetObjectNearHand()
    {
        RaycastHit[] objects = Physics.SphereCastAll(transform.position, 0.14f, transform.forward, 0.3f, 1 << LayerMask.NameToLayer("Object"));
        float minDist = float.MaxValue;
        GameObject selectionTemp = null;
        foreach (RaycastHit hits in objects)
        {
            if (hits.collider.gameObject.GetComponent<MiniGameObject>() != null
                    && ((hits.transform.parent == null && hits.collider.gameObject.GetComponent<Rigidbody>() != null)
                        || !hits.collider.gameObject.GetComponent<MiniGameObject>().grabbable)
                    && hits.distance < minDist)
            {
                minDist = hits.distance;
                selectionTemp = hits.collider.gameObject;
            }
        }
        return selectionTemp;
    }
}
