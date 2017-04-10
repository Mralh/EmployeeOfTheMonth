using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGame : MonoBehaviour {

    public int timeLimit;
    public int timer = 0;
    public int scoreRequired = 0;

    int playerScores = 0;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		
	}

    public virtual void Tick() { }
    public virtual void OnGameStart()
    {
    }
    public virtual void OnGameEnd()
    {
    }

    public void ChangeScore(int playerID, int score)
    {
        playerScores += score;
    }
    
    public void ResetScore(int playerID)
    {
        playerScores = 0;
    }
}
