using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("内置关卡 (Resources/Levels/ 下的ScriptableObject)")]
    public LevelData[] builtinLevels;

    public static LevelData TestLevel { get; set; }
    public static bool      IsTestMode { get; set; }

    private readonly List<LevelData> _all = new();
    public  int       LevelCount   => _all.Count;
    public  int       CurrentIndex { get; private set; }
    public  LevelData CurrentLevel { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Refresh();
    }

    public void Refresh()
    {
        _all.Clear();
        var seen = new HashSet<string>();
        foreach (var level in Resources.LoadAll<LevelData>("Levels")
                     .Where(level => level != null)
                     .OrderBy(level => level.levelId))
        {
            _all.Add(level);
            seen.Add(level.levelId);
        }

        if (builtinLevels != null)
        {
            foreach (var level in builtinLevels)
                if (level != null && seen.Add(level.levelId)) _all.Add(level);
        }
        LoadCustomLevels();

        if (_all.Count == 0)
        {
            CurrentIndex = 0;
            CurrentLevel = null;
            return;
        }

        if (CurrentLevel == null || CurrentIndex < 0 || CurrentIndex >= _all.Count)
            SetCurrent(0);
        else
            CurrentLevel = GetLevel(CurrentIndex);
    }

    private void LoadCustomLevels()
    {
        string dir = Path.Combine(Application.streamingAssetsPath, "Levels");
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var obj  = JsonUtility.FromJson<LevelDataJson>(json);
                if (obj != null) _all.Add(obj.ToLevelData());
            }
            catch { Debug.LogWarning($"[LevelManager] 无法解析 {file}"); }
        }
    }

    public LevelData GetLevel(int index) =>
        index >= 0 && index < _all.Count ? _all[index] : null;

    public void SetCurrent(int index)
    {
        CurrentIndex = index;
        CurrentLevel = GetLevel(index);
    }
}
