using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallpitBall : MonoBehaviour
{
	void Start ()
    {
        GetComponent<MeshRenderer>().sharedMaterial = new Material(GetComponent<MeshRenderer>().sharedMaterial);
        float h = Random.Range(0, 1.0f);
        Color c = Color.HSVToRGB(h, 1, 1);

        GetComponent<MeshRenderer>().sharedMaterial.SetColor("_EmissionColor", Color.HSVToRGB(h, 0.8f, 0.5f));
        GetComponent<MeshRenderer>().sharedMaterial.color = c;
    }
}
