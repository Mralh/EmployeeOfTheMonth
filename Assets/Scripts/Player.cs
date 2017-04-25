using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Player : MonoBehaviour {

    public GameObject HUD;
    public GameObject clock;
    public GameObject staticScreen;
    public GameObject eyeCamera;
    public AudioSource bgm;

    public LayerMask staticScreenMask;
    public LayerMask regularMask;

    public GameObject handR, handL;

    public bool isStaticScreen = false;

    int tTest = 0;

    void Start ()
    {
        //displayMessage("TEST", 500);
        setClock(0, 1);
    }

    void FixedUpdate ()
    {
        if (MiniGameManager.singleton.currentGame != null && 
            MiniGameManager.singleton.currentGame.state == MiniGame.GameState.INPROGRESS)
            clock.SetActive(true);
        else
            clock.SetActive(false);
    }

    public void displayMessage(string s, int t)
    {
        GameObject hudMessage = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/UI/Message"), HUD.transform);
        hudMessage.GetComponent<RectTransform>().localScale = Vector3.one;
        hudMessage.GetComponent<RectTransform>().localEulerAngles = Vector3.zero;
        hudMessage.GetComponent<Text>().text = s;
        hudMessage.GetComponent<HudMessage>().messageTime = t;
    }

    public void setClock(int timer, int baseTime)
    {
        float cutoff = Mathf.Clamp((float)timer / (float)baseTime, 0.01f, 1.0f);
        clock.GetComponent<Renderer>().sharedMaterial.SetFloat("_Cutoff", cutoff);
    }

    public void ToggleStatic ()
    {
        if (staticScreen.activeSelf)
        {
            eyeCamera.GetComponent<Camera>().cullingMask = regularMask.value;
            staticScreen.SetActive(false);
            isStaticScreen = false;
        }
        else
        {
            eyeCamera.GetComponent<Camera>().cullingMask = staticScreenMask.value;
            staticScreen.SetActive(true);
            if (handR != null)
                handR.GetComponent<ViveHand>().ReleaseGrab();
            if (handL != null)
                handL.GetComponent<ViveHand>().ReleaseGrab();
            isStaticScreen = true;
        }
    }

    public void resumeBGM()
    {
        bgm.UnPause();
    }

    public void pauseBGM()
    {
        bgm.Pause();
    }

    public void playBGM(string source)
    {
        bgm.Stop();
        bgm.clip = Resources.Load<AudioClip>("Sounds/Music/" + source);
        bgm.Play();
    }
}
