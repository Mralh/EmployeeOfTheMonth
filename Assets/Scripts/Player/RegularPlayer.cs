using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;


public class RegularPlayer : MonoBehaviour
{
    public GameObject fpcamera;

    //Grabbing variables
    bool grabInput = false;
    GameObject grabbedObject;
    GameObject equippedObject;
    Vector3 grabbedObjectRot;

    //Selection variables
    GameObject selectedObject;

    bool fireInputPressed, fireInputReleased;
    bool useInputPressed, useInputReleased, releaseInputPressed;

    Vector3 lookLocation;
    public GameObject reticle;

    void Update()
    {
        if (Input.GetButtonDown("Use")) useInputPressed = true;
        if (Input.GetButtonUp("Use")) useInputReleased = true;

        if (Input.GetButtonDown("Fire1")) fireInputPressed = true;
        if (Input.GetButtonUp("Fire1")) fireInputReleased = true;

        if (Input.GetButtonDown("Throw")) releaseInputPressed = true;
    }

    void FixedUpdate()
    {
        if (useInputPressed)
            OnUsePressed();
        if (useInputReleased)
            OnUseReleased();

        if (fireInputPressed)
            OnFirePressed();
        if (fireInputReleased)
            OnFireReleased();

        if (releaseInputPressed)
            OnDisarmPressed();

        RaycastHit rh;
        if (Physics.Raycast(fpcamera.transform.position + 0.1f*fpcamera.transform.forward, fpcamera.transform.forward, out rh, 200f))
        {
            lookLocation = rh.point;
            //reticle.transform.localPosition = new Vector3(0, 0, rh.distance - 0.01f);
            //reticle.transform.localScale = 0.03f * rh.distance * Vector3.one;
        }
        else
        {
            lookLocation = transform.position + fpcamera.transform.position + (fpcamera.transform.forward * 5);
        }
        
        //Grab hold behaviour
        if (grabbedObject != null)
        {
            Vector3 targetPos = fpcamera.transform.position
                + (fpcamera.transform.forward * (grabbedObject.GetComponent<MiniGameObject>().holdDistance + 0.4f));
            //Debug.Log(targetPos);
            grabbedObject.GetComponent<Rigidbody>().useGravity = false;
            grabbedObject.transform.eulerAngles = transform.eulerAngles;
            Vector3 targetVel = targetPos - grabbedObject.transform.position;
            float speed = Mathf.Clamp(20 * Mathf.Abs(targetVel.magnitude), 0, 100);
            targetVel = speed * targetVel.normalized;
            grabbedObject.GetComponent<Rigidbody>().velocity = targetVel;
            grabbedObject.GetComponent<MiniGameObject>().OnHeld(gameObject);
        }
        
        if (equippedObject != null)
        {
            equippedObject.transform.localEulerAngles = new Vector3(-50, 0, 0);
        }

        //Interact hold behaviour
        if (selectedObject != null)
        {
            selectedObject.GetComponent<MiniGameObject>().OnInteractHeld(gameObject);

        }
    }

    void OnUsePressed()
    {
        Debug.Log("USE");
        useInputPressed = false;
        if (grabbedObject != null)
        {
            ReleaseGrab();
            return;
        }

        GameObject selectionTemp = GetObjectScan();
        if (selectionTemp == null)
            return;
        Debug.Log(selectionTemp.name);
        if (selectionTemp.GetComponent<MiniGameObject>().grabbable)
        {
            if (selectionTemp.GetComponent<MiniGameObject>().equippable)
            {
                equippedObject = selectionTemp;
                equippedObject.transform.parent = fpcamera.transform;
                equippedObject.transform.localPosition = new Vector3(0, -0.15f, 0.25f);
                equippedObject.GetComponent<Rigidbody>().isKinematic = true;
                equippedObject.GetComponent<Collider>().enabled = false;
                
                foreach (Transform t in equippedObject.transform)
                {
                    if (t.GetComponent<Collider>() != null && !t.GetComponent<Collider>().isTrigger)
                    {
                        t.GetComponent<Collider>().enabled = false;
                    }
                }
            }
            else
            {
                grabbedObject = selectionTemp;
                grabbedObject.GetComponent<MiniGameObject>().OnGrabbed(gameObject);

                grabbedObjectRot = grabbedObject.transform.eulerAngles - transform.eulerAngles;
            }
        }
        if (selectionTemp.GetComponent<MiniGameObject>().interactable)
        {
            selectedObject = selectionTemp;
            selectedObject.GetComponent<MiniGameObject>().OnInteractPressed(gameObject);
        }
        
    }
    void OnUseReleased()
    {
        useInputReleased = false;
        if (selectedObject != null)
        {
            selectedObject.GetComponent<MiniGameObject>().OnInteractReleased(gameObject);
            selectedObject = null;
        }
        
    }

    void OnFirePressed()
    {
        fireInputPressed = false;
        if (grabbedObject != null)
        {
            grabbedObject.GetComponent<MiniGameObject>().OnGrabRelease(gameObject);
            grabbedObject.GetComponent<Rigidbody>().useGravity = true;
            grabbedObject.GetComponent<Rigidbody>().velocity = fpcamera.transform.forward * 15;

            grabbedObject = null;
            return;
        }

        if (equippedObject != null)
        {
            equippedObject.GetComponent<MiniGameObject>().OnInteractPressed(gameObject);
        }

    }
    void OnFireReleased()
    {

    }

    void OnDisarmPressed()
    {
        releaseInputPressed = false;
        if (equippedObject != null)
        {
            equippedObject.transform.parent = null;
            equippedObject.GetComponent<MiniGameObject>().OnGrabRelease(gameObject);
            equippedObject.GetComponent<Rigidbody>().useGravity = true;
            equippedObject.GetComponent<Rigidbody>().velocity = fpcamera.transform.forward * 6;
            equippedObject.GetComponent<Rigidbody>().isKinematic = false;
            equippedObject.GetComponent<Collider>().enabled = true;
            foreach (Transform t in equippedObject.transform)
            {
                if (t.GetComponent<Collider>() != null && !t.GetComponent<Collider>().isTrigger)
                {
                    t.GetComponent<Collider>().enabled = true;
                }
            }
            equippedObject = null;
        }
    }

    void ReleaseGrab()
    {
        grabbedObject.GetComponent<MiniGameObject>().OnGrabRelease(gameObject);
        grabbedObject.GetComponent<Rigidbody>().useGravity = true;

        grabbedObject = null;
    }

    void ReleaseWeapon()
    {
        equippedObject.transform.parent = null;
        equippedObject.GetComponent<Rigidbody>().isKinematic = false;
        equippedObject.GetComponent<Collider>().isTrigger = false;
        equippedObject = null;

    }

    GameObject GetObjectScan()
    {
        RaycastHit[] objects = Physics.RaycastAll(fpcamera.transform.position, fpcamera.transform.forward, 3f, 1 << LayerMask.NameToLayer("Object"));
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
