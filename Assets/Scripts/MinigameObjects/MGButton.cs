using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MGButton : MonoBehaviour
{
    public Vector3 openPosition, closedPosition;

    void FixedUpdate ()
    {
        if (GetComponent<MiniGameObject>().interact)
            transform.localPosition = Vector3.Lerp(transform.localPosition, closedPosition, 10f * Time.deltaTime);
        else
            transform.localPosition = Vector3.Lerp(transform.localPosition, openPosition, 10f * Time.deltaTime);
    }
}
