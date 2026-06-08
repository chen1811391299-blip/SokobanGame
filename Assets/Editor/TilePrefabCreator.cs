using UnityEngine;
using UnityEditor;

public static class TilePrefabCreator
{
    [MenuItem("SokobanGame/1. Create Materials & Prefabs")]
    public static void CreateAll()
    {
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Prefabs");

        var matWall    = Mat("Mat_Wall",          0.25f, 0.25f, 0.25f);
        var matFloor   = Mat("Mat_Floor",         0.78f, 0.76f, 0.70f);
        var matGoal    = Mat("Mat_Goal",          1.00f, 0.85f, 0.10f);
        var matBox     = Mat("Mat_Box",           0.70f, 0.40f, 0.10f);
        var matBoxG    = Mat("Mat_BoxOnGoal",     0.15f, 0.72f, 0.20f);
        var matPlayer  = Mat("Mat_Player",        0.10f, 0.40f, 0.90f);
        var matIce     = Mat("Mat_Ice",           0.70f, 0.90f, 1.00f);
        var matPlate   = Mat("Mat_PressurePlate", 0.80f, 0.25f, 0.90f);
        var matDoorC   = Mat("Mat_DoorClosed",    0.80f, 0.12f, 0.12f);
        var matDoorO   = Mat("Mat_DoorOpen",      0.12f, 0.80f, 0.25f);
        var matPortalB = Mat("Mat_PortalBlue",    0.00f, 0.50f, 1.00f);
        var matPortalO = Mat("Mat_PortalOrange",  1.00f, 0.50f, 0.00f);
        var matPortalG = Mat("Mat_PortalGreen",   0.00f, 0.85f, 0.40f);

        Prefab("Prefab_Wall",          matWall,    1.0f, 1.0f,   1.0f);
        Prefab("Prefab_Floor",         matFloor,   1.0f, 0.1f,   1.0f);
        Prefab("Prefab_Goal",          matGoal,    1.0f, 0.1f,   1.0f);
        Prefab("Prefab_Box",           matBox,     0.9f, 0.9f,   0.9f);
        Prefab("Prefab_BoxOnGoal",     matBoxG,    0.9f, 0.9f,   0.9f);
        Prefab("Prefab_Player",        matPlayer,  0.55f, 0.45f, 0.55f, PrimitiveType.Capsule, true);
        Prefab("Prefab_Ice",           matIce,     1.0f, 0.1f,   1.0f);
        Prefab("Prefab_PressurePlate", matPlate,   1.0f, 0.05f,  1.0f);
        Prefab("Prefab_DoorClosed",    matDoorC,   1.0f, 0.8f,   0.2f);
        Prefab("Prefab_DoorOpen",      matDoorO,   1.0f, 0.1f,   0.2f);
        Prefab("Prefab_PortalBlue",    matPortalB, 0.9f, 0.1f,   0.9f);
        Prefab("Prefab_PortalOrange",  matPortalO, 0.9f, 0.1f,   0.9f);
        Prefab("Prefab_PortalGreen",   matPortalG, 0.9f, 0.1f,   0.9f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TilePrefabCreator] Done — 13 materials, 13 prefabs created.");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts  = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    static Material Mat(string name, float r, float g, float b)
    {
        string path = $"Assets/Materials/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(r, g, b);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static void Prefab(
        string name,
        Material mat,
        float sx,
        float sy,
        float sz,
        PrimitiveType primitive = PrimitiveType.Cube,
        bool overwriteExisting = false)
    {
        string path = $"Assets/Prefabs/{name}.prefab";
        if (!overwriteExisting && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

        var go = GameObject.CreatePrimitive(primitive);
        go.name = name;
        go.transform.localScale = new Vector3(sx, sy, sz);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        var collider = go.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }
}
