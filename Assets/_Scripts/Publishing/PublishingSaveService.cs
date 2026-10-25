using System;
using UnityEngine;
#if UNITY_WEBGL
using Playgama;
#endif

/// <summary>Settings and completed-shift records only; never resumes an unfinished shift.</summary>
public sealed class PublishingSaveService : MonoBehaviour
{
    public const string StorageKey = "crunchie_munchie_v1";
    public static PublishingSaveService Instance { get; private set; }
    public bool IsLoaded { get; private set; }
    public bool CanWrite { get; private set; }
    public string LastError { get; private set; }

    [Serializable]
    public sealed class SaveData
    {
        public int version;
        public float sound = 1f;
        public float music = 1f;
        public bool tutorialCompleted;
        public int bestTips;
        public int bestCompletedOrders;
    }

    private SaveData data;
    private int revision;
    private int savedRevision;
    private bool writing;
    private float saveAfter;
    private float loadDeadline;
    public SaveData Snapshot => JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
    public GameSettings.Data Settings => new GameSettings.Data { Sound = data.sound, Music = data.music };

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        // Read the old keys once for migration when Bridge has no saved document.
        data = new SaveData
        {
            version = 1,
            sound = Mathf.Clamp01(PlayerPrefs.GetFloat(GameSettings.SoundPrefKey, GameSettings.DefaultSound)),
            music = Mathf.Clamp01(PlayerPrefs.GetFloat(GameSettings.MusicPrefKey, GameSettings.DefaultMusic))
        };
    }

    public void Load()
    {
        loadDeadline = Time.realtimeSinceStartup + 15f;
#if UNITY_WEBGL
        Bridge.storage.Get(StorageKey, HandleLoaded);
#else
        HandleLoaded(true, PlayerPrefs.GetString(StorageKey, ""));
#endif
    }

    private void HandleLoaded(bool success, string json)
    {
        if (IsLoaded) return;
        CanWrite = success;
        if (!success) LastError = "Storage read failed; cloud writes disabled to protect existing data.";
        else if (!string.IsNullOrEmpty(json) && json != "null")
        {
            try
            {
                var loaded = JsonUtility.FromJson<SaveData>(json);
                if (loaded == null || loaded.version != 1 || float.IsNaN(loaded.sound) || float.IsNaN(loaded.music))
                    throw new ArgumentException("Unsupported or invalid save document");
                data = loaded;
                data.sound = Mathf.Clamp01(data.sound);
                data.music = Mathf.Clamp01(data.music);
                data.bestTips = Mathf.Max(0, data.bestTips);
                data.bestCompletedOrders = Mathf.Max(0, data.bestCompletedOrders);
            }
            catch (Exception e)
            {
                CanWrite = false;
                LastError = "Save was preserved: " + e.Message;
            }
        }
        IsLoaded = true;
        GameSettings.Apply(Settings);
        if (LastError != null) Debug.LogWarning(LastError);
    }

    public void SetSettings(GameSettings.Data settings)
    {
        data.sound = Mathf.Clamp01(settings.Sound);
        data.music = Mathf.Clamp01(settings.Music);
        MarkDirty();
    }

    public void CompleteTutorial()
    {
        if (data.tutorialCompleted) return;
        data.tutorialCompleted = true;
        MarkDirty();
    }

    public void RecordShift(int tips, int completedOrders)
    {
        data.bestTips = Mathf.Max(data.bestTips, tips);
        data.bestCompletedOrders = Mathf.Max(data.bestCompletedOrders, completedOrders);
        MarkDirty();
        Flush();
    }

    private void MarkDirty()
    {
        revision++;
        saveAfter = Time.realtimeSinceStartup + 0.6f;
    }

    private void Update()
    {
        if (!IsLoaded && loadDeadline > 0 && Time.realtimeSinceStartup >= loadDeadline)
            HandleLoaded(false, null);
        if (IsLoaded && Time.realtimeSinceStartup >= saveAfter) Flush();
    }

    public void Flush()
    {
        if (!IsLoaded || !CanWrite || writing || revision == savedRevision) return;
        writing = true;
        int submittedRevision = revision;
        string json = JsonUtility.ToJson(data);
        Action<bool> complete = success =>
        {
            writing = false;
            if (success) { savedRevision = submittedRevision; LastError = null; }
            else { LastError = "Storage write failed; retry pending."; Debug.LogWarning(LastError); }
            saveAfter = Time.realtimeSinceStartup + (success ? 0.6f : 5f);
        };
#if UNITY_WEBGL
        Bridge.storage.Set(StorageKey, json, complete);
#else
        PlayerPrefs.SetString(StorageKey, json);
        PlayerPrefs.Save();
        complete(true);
#endif
    }

    private void OnApplicationPause(bool paused) { if (paused) Flush(); }
    private void OnApplicationFocus(bool focused) { if (!focused) Flush(); }
    // Cloud writes must happen while the page is alive. Bridge teardown order is undefined at quit.
    private void OnApplicationQuit() => PlayerPrefs.Save();
}
