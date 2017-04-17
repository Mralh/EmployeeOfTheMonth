using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniGame
{
    public enum GameState { START, INPROGRESS, END, NULL };
    public MiniGameManager manager;
    GameState state = GameState.NULL;
    public int timeLimit;
    public int timer = 0;
    public int scoreRequired = 0;

    int playerScores = 0;

    int startTimer = 0;
    public int startTimeLimit = 240;

    int endTimer;
    public int endTimeLimit;

    public string[] introMessages;
    public string[] successMessages;
    public string[] failureMessages;

    public Player player;

    public bool ready = false;
	
    public MiniGame(MiniGameManager mgr)
    {
        this.manager = mgr;
    }

	// Update is called once per frame
	public void FixedUpdate ()
    {
        player = manager.player;

        Debug.Log(state + ", " + startTimeLimit + ", " + timeLimit + ", " + endTimeLimit);

        if (state != GameState.NULL)
            Tick();
        else if (startTimer == 0)
        {
            OnTransitionIn();
            ready = true;
            return;
        }

        //Start timer
        if (startTimer < startTimeLimit && state == GameState.START)
        {
            if (startTimer == 0)
            {
                DisplayMessage(introMessages[Random.Range(0, introMessages.Length - 1)], startTimeLimit, 0);
            }
            startTimer++;
            return;
        }
        else if (state == GameState.START)
        {
            startTimer = 0;
            OnGameStart();
            state = GameState.INPROGRESS;
            return;
        }

        //In progress timer
        if (timer < timeLimit && state == GameState.INPROGRESS)
        {
            timer++;
            return;
        }
        else if (state == GameState.INPROGRESS)
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
        {
            endTimer++;
            return;
        }
        else if (state == GameState.END)
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
    public virtual void OnTransitionIn() {
        state = GameState.START;
    }

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
        player.displayMessage(s, timer);
    }

    public void NullState()
    {
        startTimer = 0;
        timer = 0;
        endTimer = 0;
        state = GameState.NULL;
        playerScores = 0;
        ready = false;
    }
    
}
