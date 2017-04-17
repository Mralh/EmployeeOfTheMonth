using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MGAlarmClock : MiniGame
{
    public MGAlarmClock (MiniGameManager mgr) : base(mgr) { }

    public override void OnTransitionIn()
    {
        base.OnTransitionIn();
        
    }
    public override void OnGameStart()
    {
        base.OnGameStart();
    }
    public override void Tick()
    {
        base.Tick();
    }
    public override void OnGameEnd()
    {
        base.OnGameEnd();
    }
    public override void OnTransitionOut()
    {
        base.OnTransitionOut();
        manager.SelectNextGame();
        base.NullState();
    }
}
