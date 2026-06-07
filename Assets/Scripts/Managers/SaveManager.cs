using UnityEngine;

public static class SaveManager
{
    public static bool IsCompleted(string levelId) =>
        PlayerPrefs.GetInt($"done_{levelId}", 0) == 1;

    public static int GetBestMoves(string levelId) =>
        PlayerPrefs.GetInt($"best_{levelId}", int.MaxValue);

    public static void RecordCompletion(string levelId, int moves)
    {
        PlayerPrefs.SetInt($"done_{levelId}", 1);
        int prev = GetBestMoves(levelId);
        if (moves < prev)
            PlayerPrefs.SetInt($"best_{levelId}", moves);
        PlayerPrefs.Save();
    }

    public static int GetStars(string levelId, int par)
    {
        int best = GetBestMoves(levelId);
        if (best == int.MaxValue) return 0;
        if (best <= par)              return 3;
        if (best <= Mathf.CeilToInt(par * 1.5f)) return 2;
        return 1;
    }

    public static void ResetAll() => PlayerPrefs.DeleteAll();
}
