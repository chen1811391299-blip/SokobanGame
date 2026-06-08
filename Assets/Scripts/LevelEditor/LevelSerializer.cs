using System.IO;
using UnityEngine;

public static class LevelSerializer
{
    private static string LevelsDir =>
        Path.Combine(Application.streamingAssetsPath, "Levels");

    public static void Save(LevelData data)
    {
        if (!Directory.Exists(LevelsDir))
            Directory.CreateDirectory(LevelsDir);

        if (string.IsNullOrEmpty(data.levelId))
            data.levelId = System.Guid.NewGuid().ToString("N")[..8];

        var json = JsonUtility.ToJson(LevelDataJson.From(data), prettyPrint: true);
        var path = Path.Combine(LevelsDir, $"{data.levelId}.json");
        File.WriteAllText(path, json);
        Debug.Log($"[LevelSerializer] Saved: {path}");
    }

    public static LevelData Load(string levelId)
    {
        var path = Path.Combine(LevelsDir, $"{levelId}.json");
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonUtility.FromJson<LevelDataJson>(json).ToLevelData();
    }

    public static LevelData LoadMostRecent()
    {
        if (!Directory.Exists(LevelsDir)) return null;

        string latest = null;
        var latestWrite = System.DateTime.MinValue;
        foreach (var file in Directory.GetFiles(LevelsDir, "*.json"))
        {
            var write = File.GetLastWriteTimeUtc(file);
            if (write <= latestWrite) continue;
            latestWrite = write;
            latest = file;
        }

        if (latest == null) return null;
        var json = File.ReadAllText(latest);
        return JsonUtility.FromJson<LevelDataJson>(json).ToLevelData();
    }

    public static string[] GetAllLevelIds()
    {
        if (!Directory.Exists(LevelsDir)) return System.Array.Empty<string>();
        var files = Directory.GetFiles(LevelsDir, "*.json");
        var ids   = new string[files.Length];
        for (int i = 0; i < files.Length; i++)
            ids[i] = Path.GetFileNameWithoutExtension(files[i]);
        return ids;
    }

    public static void Delete(string levelId)
    {
        var path = Path.Combine(LevelsDir, $"{levelId}.json");
        if (File.Exists(path))
            File.Delete(path);
    }

    public static (string id, string name, System.DateTime modified)[] GetAllLevelMeta()
    {
        if (!Directory.Exists(LevelsDir))
            return System.Array.Empty<(string, string, System.DateTime)>();

        var files = Directory.GetFiles(LevelsDir, "*.json");
        var result = new System.Collections.Generic.List<(string, string, System.DateTime)>();
        foreach (var file in files)
        {
            try
            {
                var id       = Path.GetFileNameWithoutExtension(file);
                var json     = File.ReadAllText(file);
                var dto      = JsonUtility.FromJson<LevelDataJson>(json);
                var modified = File.GetLastWriteTime(file);
                result.Add((id, dto?.name ?? id, modified));
            }
            catch { /* skip malformed files */ }
        }
        result.Sort((a, b) => b.Item3.CompareTo(a.Item3)); // newest first
        return result.ToArray();
    }
}
