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
        RaycastHit rh;
        if (Physics.Raycast(fpcamera.transform.position + 0.1f*fpcamera.transform.forward, fpcamera.transform.forward, out rh, 20f))
        {
            lookLocation = rh.point + (0.01f * rh.normal);
            //reticle.transform.localPosition = new Vector3(0, 0, rh.distance - 0.1f);
            //reticle.transform.localScale = 0.03f * rh.distance * Vector3.one;
        }
        
        //Grab hold behaviour
        if (grabbedObject != null)
        {
            Vector3 targetPos = fpcamera.transform.position
                - (2*fpcamera.transform.right) - (2 * fpcamera.transform.up)
                + (fpcamera.transform.forward * (grabbedObject.GetComponent<MiniGameObject>().holdDistance + 1));
            grabbedObject.GetComponent<Rigidbody>().useGravity = false;
            grabbedObject.transform.eulerAngles = transform.eulerAngles;
            Vector3 targetVel = targetPos - grabbedObject.transform.position;
            float speed = Mathf.Clamp(100 * Mathf.Abs(targetVel.magnitude), 0, 200);
            targetVel = speed * targetVel.normalized;
            grabbedObject.GetComponent<Rigidbody>().velocity = targetVel;
            grabbedObject.GetComponent<MiniGameObject>().OnHeld(gameObject);
        }

        //Interact hold behaviour
        if (selectedObject != null)
        {
            selectedObject.GetComponent<MiniGameObject>().OnInteractHeld(gameObject);

        }
    }

    void OnUsePressed()
    {
        if (grabbedObject != null)
        {
            ReleaseGrab();
            return;
        }

        GameObject selectionTemp = GetObjectScan();
        if (selectionTemp == null)
            return;

        if (selectionTemp.GetComponent<MiniGameObject>().grabbable)
        {
            if (selectionTemp.GetComponent<MiniGameObject>().equippable)
            {
                equippedObject = selectionTemp;
                equippedObject.transform.parent = fpcamera.transform;
                equippedObject.GetComponent<Rigidbody>().isKinematic = true;
                equippedObject.GetComponent<Collider>().isTrigger = true;
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
        useInputPressed = false;
    }
    void OnUseReleased()
    {

    }

    void OnFirePressed()
    {
    }
    void OnFireReleased()
    {

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
            if (hits.transform.parent == null
                    && hits.collider.gameObject.GetComponent<MiniGameObject>() != null
                    && hits.collider.gameObject.GetComponent<Rigidbody>() != null
                    && hits.distance < minDist)
            {
                minDist = hits.distance;
                selectionTemp = hits.collider.gameObject;
            }
        }
        return selectionTemp;
    }
}
