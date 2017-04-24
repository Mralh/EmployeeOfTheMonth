using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HudMessage : MonoBehaviour {
    public int messageTime = 350;
    int timer = 0;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
    {
        transform.GetChild(0).GetComponent<Text>().text = GetComponent<Text>().text;
	}

    void FixedUpdate()
    {
        if (timer < messageTime - 20)
            GetComponent<RectTransform>().anchoredPosition3D = Vector3.Lerp(GetComponent<RectTransform>().anchoredPosition3D,
                new Vector3(0, 500, 0),
                10f * Time.deltaTime);
        else
            GetComponent<RectTransform>().anchoredPosition3D = Vector3.Lerp(GetComponent<RectTransform>().anchoredPosition3D,
                new Vector3(0, -200, 0),
                10f * Time.deltaTime);

        if (timer < messageTime)
            timer++;
        else
            Destroy(gameObject);
    }
}
