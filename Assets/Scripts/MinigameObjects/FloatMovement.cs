using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatMovement : MonoBehaviour {

    public float amplitude = 1;
    public float rate = 1;
    public Vector3 axis;

    Vector3 pos;

    void Start()
    {
        pos = transform.position;
    }

    void FixedUpdate ()
    {
        transform.position = pos + axis * Mathf.Sin(Time.time * rate) * amplitude;
    }

}
