using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorControls : MonoBehaviour {

    bool doorOpen = false;

    public GameObject door1, door2;
    
	void FixedUpdate ()
    {
		if (doorOpen)
        {
            door1.transform.localPosition = Vector3.Lerp(door1.transform.localPosition, new Vector3(0, 0, 0), 3f * Time.deltaTime);
            door2.transform.localPosition = Vector3.Lerp(door2.transform.localPosition, new Vector3(0, 0, 0), 3f * Time.deltaTime);
        }
        else
        {
            door1.transform.localPosition = Vector3.Lerp(door1.transform.localPosition, new Vector3(-1.883f, 0, 0), 2f * Time.deltaTime);
            door2.transform.localPosition = Vector3.Lerp(door2.transform.localPosition, new Vector3(-0.883f, 0, 0), 3f * Time.deltaTime);
        }
	}

    public void OpenDoor()
    {
        doorOpen = true;
    }

    public void CloseDoor()
    {
        doorOpen = false;
    }
}
