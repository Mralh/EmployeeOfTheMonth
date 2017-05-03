using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MGButton : MonoBehaviour
{
    public Vector3 openPosition, closedPosition;

    public bool localSpace;

    Vector3 initialPos;

    void Start ()
    {
        initialPos = transform.localPosition;
    }
    void FixedUpdate ()
    {
        if (GetComponent<MiniGameObject>().interact)
        {
            if (!localSpace)
                transform.localPosition = Vector3.Lerp(transform.localPosition, closedPosition, 10f * Time.deltaTime);
            else
                transform.localPosition = Vector3.Lerp(transform.localPosition, initialPos + 
                    closedPosition.x*transform.right + closedPosition.y*transform.up + closedPosition.z*transform.forward,
                    10f * Time.deltaTime);
        }
        else
        {
            if (!localSpace)
                transform.localPosition = Vector3.Lerp(transform.localPosition, openPosition, 10f * Time.deltaTime);
            else
                transform.localPosition = Vector3.Lerp(transform.localPosition, initialPos, 10f * Time.deltaTime);
        }
    }
}
