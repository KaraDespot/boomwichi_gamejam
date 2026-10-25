using System;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_WEBGL
using Playgama;
using Playgama.Modules.Advertisement;
using Playgama.Modules.Platform;
#endif

/// <summary>The official Bridge template initializes JS before starting Unity.</summary>
public sealed class PublishingBridgeService : MonoBehaviour
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void PublishingWatchVisibility();
#endif
    public static PublishingBridgeService Instance { get; private set; }
    public bool IsReady { get; private set; }
    public bool AdInProgress { get; private set; }
    public string PlatformId { get; private set; } = "local";
    private bool platformMuted;
    private bool interstitialActive;
    private bool rewardedActive;
    private bool rewardConfirmed;
    private Action adContinuation;
    private Action<bool> rewardContinuation;
    private bool gameplayReported;
    private bool levelStarted;
    private bool levelCompleted;
    private bool readyReported;

    public void Initialize()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
#if UNITY_WEBGL
        PlatformId = Bridge.platform.id;
        platformMuted = !Bridge.platform.isAudioEnabled;
        Bridge.platform.pauseStateChanged += HandlePlatformPause;
        Bridge.platform.audioStateChanged += HandlePlatformAudio;
        Bridge.advertisement.interstitialStateChanged += HandleInterstitial;
        Bridge.advertisement.rewardedStateChanged += HandleRewarded;
#endif
        SceneManager.sceneLoaded += HandleSceneLoaded;
        IsReady = true;
#if UNITY_WEBGL && !UNITY_EDITOR
        PublishingWatchVisibility();
#endif
        Debug.Log("Publishing Bridge ready: " + PlatformId);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneNames.MainMenu && !readyReported)
        {
            readyReported = true;
#if UNITY_WEBGL
            Bridge.platform.SendMessage(PlatformMessage.GameReady);
#endif
        }
        if (scene.name == SceneNames.GameScene) { levelStarted = false; levelCompleted = false; gameplayReported = false; }
    }

    private void LateUpdate()
    {
        if (!IsReady) return;
        var manager = GameManager.Instance;
        RefreshAudio();
        if (manager != null && manager.CurrentDayState == DayFlowState.DayFinished && levelStarted && !levelCompleted)
        {
            levelCompleted = true;
            gameplayReported = false;
#if UNITY_WEBGL
            Bridge.platform.SendMessage(PlatformMessage.LevelCompleted);
#endif
            return;
        }
        bool playing = manager != null && manager.CurrentState == GameState.Playing &&
            manager.CurrentDayState != DayFlowState.None && manager.CurrentDayState != DayFlowState.Tutorial &&
            manager.CurrentDayState != DayFlowState.DayFinished;
        if (playing == gameplayReported) return;
        gameplayReported = playing;
#if UNITY_WEBGL
        Bridge.platform.SendMessage(playing
            ? (levelStarted ? PlatformMessage.LevelResumed : PlatformMessage.LevelStarted)
            : (manager != null && manager.CurrentDayState == DayFlowState.DayFinished
                ? PlatformMessage.LevelCompleted : PlatformMessage.LevelPaused));
#endif
        if (playing) levelStarted = true;
    }

    public void HandlePlatformPause(bool paused) => GameManager.Instance?.SetPauseReason(PauseReason.Platform, paused);
    [UnityEngine.Scripting.Preserve]
    public void OnBrowserVisibility(string state)
    {
        GameManager.Instance?.SetPauseReason(PauseReason.Visibility, state == "hidden");
        if (state == "hidden") PublishingSaveService.Instance?.Flush();
    }
    public void HandlePlatformAudio(bool enabled) { platformMuted = !enabled; RefreshAudio(); }
    public void RefreshAudio() => AudioListener.pause = platformMuted || (GameManager.Instance != null && GameManager.Instance.IsPaused);

    // Invoked only by the local HTTP test harness; no diagnostic UI ships in the game.
    [UnityEngine.Scripting.Preserve]
    public void RunLocalDiagnostic(string command)
    {
        if (!Application.isEditor && !Application.absoluteURL.StartsWith("http://127.0.0.1:", StringComparison.Ordinal)) return;
        switch (command)
        {
            case "start": GameManager.Instance?.StartGame(); break;
            case "results": GameManager.Instance?.FinishDay(); break;
            case "interstitial": GameManager.Instance?.RestartGameScene(); break;
            case "rewarded": TryShowRewarded("technical_test", confirmed => Debug.Log("Diagnostic rewarded confirmed=" + confirmed)); break;
            case "pause": GameManager.Instance?.Pause(); break;
            case "resume": GameManager.Instance?.Resume(); break;
            case "platformPause": HandlePlatformPause(true); break;
            case "platformResume": HandlePlatformPause(false); break;
            case "mute": HandlePlatformAudio(false); break;
            case "unmute": HandlePlatformAudio(true); break;
        }
        var manager = GameManager.Instance;
        Debug.Log("Publishing state: " + (manager != null ? manager.PauseReasons.ToString() : "none") +
            "; timeScale=" + Time.timeScale + "; audioPaused=" + AudioListener.pause + "; ad=" + AdInProgress);
    }

    private void OnApplicationFocus(bool focused)
    {
        GameManager.Instance?.SetPauseReason(PauseReason.Focus, !focused);
        if (!focused) PublishingSaveService.Instance?.Flush();
    }

    private void OnApplicationPause(bool paused) => GameManager.Instance?.SetPauseReason(PauseReason.Application, paused);

    /// <summary>Only call from the completed-shift results transition.</summary>
    public bool TryShowBetweenShifts(Action continuation)
    {
        if (AdInProgress) return false;
        if (!IsReady || GameManager.Instance == null ||
            GameManager.Instance.CurrentDayState != DayFlowState.DayFinished) return false;
#if UNITY_WEBGL
        if (!Bridge.advertisement.isInterstitialSupported) { continuation?.Invoke(); return true; }
        interstitialActive = true;
        AdInProgress = true;
        adContinuation = continuation;
        GameManager.Instance.SetPauseReason(PauseReason.Advertisement, true);
        try { Bridge.advertisement.ShowInterstitial("between_shifts"); }
        catch (Exception e) { Debug.LogWarning(e.Message); FinishInterstitial(); }
#else
        continuation?.Invoke();
#endif
        return true;
    }

    /// <summary>Technical hook only: product UI and reward remain pending economy design.</summary>
    public bool TryShowRewarded(string placement, Action<bool> confirmed)
    {
        if (!IsReady || AdInProgress || confirmed == null || string.IsNullOrWhiteSpace(placement)) return false;
#if UNITY_WEBGL
        if (!Bridge.advertisement.isRewardedSupported) return false;
        AdInProgress = true;
        rewardedActive = true;
        rewardConfirmed = false;
        rewardContinuation = confirmed;
        GameManager.Instance?.SetPauseReason(PauseReason.Advertisement, true);
        try { Bridge.advertisement.ShowRewarded(placement); }
        catch (Exception e) { Debug.LogWarning(e.Message); FinishRewarded(false); }
        return true;
#else
        return false;
#endif
    }

#if UNITY_WEBGL
    private void HandleInterstitial(InterstitialState state)
    {
        Debug.Log("Bridge interstitial: " + state);
        if (!interstitialActive) return;
        if (state == InterstitialState.Closed || state == InterstitialState.Failed) FinishInterstitial();
    }

    private void HandleRewarded(RewardedState state)
    {
        Debug.Log("Bridge rewarded: " + state);
        if (!rewardedActive) return;
        if (state == RewardedState.Rewarded) rewardConfirmed = true;
        if (state == RewardedState.Closed) FinishRewarded(rewardConfirmed);
        if (state == RewardedState.Failed) FinishRewarded(false);
    }
#endif

    private void FinishInterstitial()
    {
        var continuation = adContinuation;
        adContinuation = null;
        interstitialActive = false;
        AdInProgress = false;
        GameManager.Instance?.SetPauseReason(PauseReason.Advertisement, false);
        continuation?.Invoke();
    }

    private void FinishRewarded(bool confirmed)
    {
        var continuation = rewardContinuation;
        rewardContinuation = null;
        rewardedActive = false;
        rewardConfirmed = false;
        AdInProgress = false;
        GameManager.Instance?.SetPauseReason(PauseReason.Advertisement, false);
        continuation?.Invoke(confirmed);
    }

    private void OnApplicationQuit() => IsReady = false;

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
#if UNITY_WEBGL
        if (IsReady)
        {
            Bridge.platform.pauseStateChanged -= HandlePlatformPause;
            Bridge.platform.audioStateChanged -= HandlePlatformAudio;
            Bridge.advertisement.interstitialStateChanged -= HandleInterstitial;
            Bridge.advertisement.rewardedStateChanged -= HandleRewarded;
        }
#endif
        AudioListener.pause = false;
        if (Instance == this) Instance = null;
    }
}
