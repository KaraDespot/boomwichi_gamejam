#if UNITY_EDITOR
using System;
using UnityEngine;

/// <summary>Run in Play mode in a disposable gameplay session, via PublishingChecks.Run().</summary>
public static class PublishingChecks
{
    public static string Run()
    {
        if (!Application.isPlaying || GameManager.Instance == null)
            throw new InvalidOperationException("Start Play mode from Bootstrap first.");
        var manager = GameManager.Instance;
        manager.StartGame();
        manager.SetDayState(DayFlowState.Tutorial);
        manager.SetPauseReason(PauseReason.Focus | PauseReason.Application | PauseReason.Visibility | PauseReason.Platform | PauseReason.Advertisement, false);
        int checks = 0;
        void Check(bool value, string label)
        {
            if (!value) throw new Exception("Publishing check failed: " + label);
            checks++;
        }
        manager.Pause();
        Check(manager.IsManuallyPaused && Time.timeScale == 0, "manual pause");
        manager.SetPauseReason(PauseReason.Advertisement, true);
        manager.SetPauseReason(PauseReason.Platform, true);
        manager.SetPauseReason(PauseReason.Advertisement, false);
        Check(manager.IsManuallyPaused && manager.IsPaused, "ad close preserves manual pause");
        manager.Resume();
        Check(manager.IsPaused && manager.CurrentState == GameState.Paused, "manual resume preserves platform pause");
        manager.SetPauseReason(PauseReason.Platform, false);
        Check(!manager.IsPaused && Time.timeScale == 1, "last cause resumes");
        manager.SetPauseReason(PauseReason.Focus, true);
        manager.SetPauseReason(PauseReason.Application, true);
        manager.SetPauseReason(PauseReason.Focus, false);
        Check(manager.IsPaused && Time.timeScale == 0, "independent visibility causes");
        manager.SetPauseReason(PauseReason.Application, false);
        manager.Pause();
        var settings = GameSettings.Load();
        PublishingBridgeService.Instance.HandlePlatformAudio(false);
        Check(AudioListener.pause, "platform mute");
        PublishingBridgeService.Instance.HandlePlatformAudio(true);
        Check(AudioListener.pause, "platform unmute preserves manual pause");
        var after = GameSettings.Load();
        Check(settings.Sound == after.Sound && settings.Music == after.Music, "platform mute preserves player volumes");
        manager.Resume();
        manager.SetDayState(DayFlowState.DayFinished);
        manager.SetPauseReason(PauseReason.Advertisement, true);
        manager.SetPauseReason(PauseReason.Advertisement, false);
        Check(Time.timeScale == 0, "closing ad preserves results freeze");
        manager.SetDayState(DayFlowState.Tutorial);
        manager.SetPauseReason(PauseReason.None, false);
        Check(Time.timeScale == 1, "restored tutorial state");
        manager.GoToMenu();
        return checks + " publishing checks passed";
    }
}
#endif
