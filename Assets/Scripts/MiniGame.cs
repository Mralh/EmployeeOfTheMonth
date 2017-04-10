using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGame : MonoBehaviour {
    public enum GameState { START, INPROGRESS, END, NULL };

    GameState state = GameState.NULL;
    public int timeLimit;
    public int timer = 0;
    public int scoreRequired = 0;

    int playerScores = 0;

    int startTimer;
    int startTimeLimit;

    int endTimer;
    int endTimeLimit;

    public string[] introMessages;
    public string[] successMessages;
    public string[] failureMessages;
	
	// Update is called once per frame
	void FixedUpdate () {
        if (state != GameState.NULL)
            Tick();

        //Start timer
        if (startTimer < startTimeLimit && state == GameState.START)
        {
            if (startTimer == 0)
            {
                OnTransitionIn();
                DisplayMessage(introMessages[Random.Range(0, introMessages.Length - 1)], startTimeLimit, 0);
            }
            startTimer++;
        }
        else
        {
            startTimer = 0;
            OnGameStart();
            state = GameState.INPROGRESS;
            return;
        }

        //In progress timer
        if (timer < timeLimit && state == GameState.INPROGRESS)
            timer++;
        else
        {
            timer = 0;
            OnGameEnd();
            state = GameState.END;
            if (playerScores >= scoreRequired)
                DisplayMessage(successMessages[Random.Range(0, successMessages.Length - 1)], endTimeLimit, 0);
            else
                DisplayMessage(failureMessages[Random.Range(0, failureMessages.Length - 1)], endTimeLimit, 0);
            return;
        }

        //End timer
        if (endTimer < endTimeLimit && state == GameState.END)
            endTimer++;
        else
        {
            endTimer = 0;
            OnTransitionOut();
            state = GameState.NULL;
            return;
        }
    }

    public virtual void Tick() { }
    public virtual void OnGameStart() { }
    public virtual void OnGameEnd() { }

    public virtual void OnTransitionOut() { }
    public virtual void OnTransitionIn() { }

    public void ChangeScore(int playerID, int score)
    {
        playerScores += score;
    }
    
    public void ResetScore(int playerID)
    {
        playerScores = 0;
    }

    public void DisplayMessage(string s, int timer, int dismissButton)
    {

    }

    public void NullState()
    {
        startTimer = 0;
        timer = 0;
        endTimer = 0;
        state = GameState.NULL;
    }
    
}
