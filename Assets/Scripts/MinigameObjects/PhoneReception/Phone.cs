using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Phone : MonoBehaviour
{
    public GameObject trigger;

    public GameObject uiCanvas1, uiCanvas2;

    float absDist = 0;

	// Update is called once per frame
	void FixedUpdate ()
    {
        absDist = Mathf.Abs((new Vector2(transform.position.x, transform.position.z) - 
            new Vector2(trigger.transform.position.x, trigger.transform.position.z)).magnitude);
        if (absDist < 1.3f)
        {
            uiCanvas1.transform.FindChild("4").gameObject.SetActive(true);
            uiCanvas1.transform.FindChild("3").gameObject.SetActive(true);
            uiCanvas1.transform.FindChild("2").gameObject.SetActive(true);
            uiCanvas1.transform.FindChild("1").gameObject.SetActive(true);
        }
        else if (absDist < 4f)
        {
            uiCanvas1.transform.FindChild("4").gameObject.SetActive(false);
            uiCanvas1.transform.FindChild("3").gameObject.SetActive(true);
            uiCanvas1.transform.FindChild("2").gameObject.SetActive(true);
            uiCanvas1.transform.FindChild("1").gameObject.SetActive(true);
        }
        else if (absDist < 8f)
        {
            uiCanvas1.transform.FindChild("4").gameObject.SetActive(false);
            uiCanvas1.transform.FindChild("3").gameObject.SetActive(false);
            uiCanvas1.transform.FindChild("2").gameObject.SetActive(true);
            uiCanvas1.transform.FindChild("1").gameObject.SetActive(true);
        }
        else if (absDist < 13f)
        {
            uiCanvas1.transform.FindChild("4").gameObject.SetActive(false);
            uiCanvas1.transform.FindChild("3").gameObject.SetActive(false);
            uiCanvas1.transform.FindChild("2").gameObject.SetActive(false);
            uiCanvas1.transform.FindChild("1").gameObject.SetActive(true);
        }
        else
        {
            uiCanvas1.transform.FindChild("4").gameObject.SetActive(false);
            uiCanvas1.transform.FindChild("3").gameObject.SetActive(false);
            uiCanvas1.transform.FindChild("2").gameObject.SetActive(false);
            uiCanvas1.transform.FindChild("1").gameObject.SetActive(false);
        }
	}
}
