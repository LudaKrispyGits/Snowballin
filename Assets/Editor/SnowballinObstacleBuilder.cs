using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Snowballin;

/// <summary>
/// Builds the seven themed obstacle prefabs (snow mound -> skyscraper) out of primitives and
/// generated cone meshes, fills GameConfig.obstacleLevels with their gameplay definitions and
/// re-saves the scene wiring. Safe to re-run: prefabs and meshes are overwritten in place.
/// </summary>
public static class SnowballinObstacleBuilder
{
    const string ConfigPath = "Assets/Configs/GameConfig.asset";
    const string PrefabFolder = "Assets/Prefabs/Obstacles";
    const string MeshFolder = "Assets/Meshes";
    const string MaterialFolder = "Assets/Materials";

    public static void Execute()
    {
        EnsureFolder("Assets/Prefabs", "Obstacles");
        EnsureFolder("Assets", "Meshes");

        Material snow = CreateMaterial("Obstacle_Snow", new Color(0.93f, 0.96f, 1f), 0.35f, null);
        Material granite = CreateMaterial("Obstacle_Granite", new Color(0.36f, 0.4f, 0.48f), 0.12f, null);
        Material pine = CreateMaterial("Obstacle_Pine", new Color(0.13f, 0.29f, 0.2f), 0.1f, null);
        Material wood = CreateMaterial("Obstacle_Wood", new Color(0.42f, 0.28f, 0.19f), 0.15f, null);
        Material glass = CreateMaterial("Obstacle_Glass", new Color(0.24f, 0.44f, 0.66f), 0.9f, new Color(0.06f, 0.14f, 0.22f));
        Material lamp = CreateMaterial("Obstacle_Lamp", new Color(1f, 0.86f, 0.5f), 0.5f, new Color(1.2f, 0.95f, 0.45f));
        Material jacket = CreateMaterial("Npc_Jacket", new Color(0.82f, 0.27f, 0.2f), 0.2f, null);
        Material trousers = CreateMaterial("Npc_Trousers", new Color(0.16f, 0.19f, 0.26f), 0.15f, null);
        Material skin = CreateMaterial("Npc_Skin", new Color(0.88f, 0.7f, 0.58f), 0.2f, null);
        Material pack = CreateMaterial("Npc_Pack", new Color(0.2f, 0.31f, 0.42f), 0.2f, null);

        // ---- level 1: snow mound ------------------------------------------------------------------
        GameObject snowMound = BuildObstacle("Obstacle_SnowMound", "Obstacle_SnowMound", 0.4f, visual =>
        {
            AddPrim(visual, PrimitiveType.Sphere, "Drift", new Vector3(0f, 0.3f, 0f), new Vector3(1.15f, 0.7f, 1.15f), snow);
            AddPrim(visual, PrimitiveType.Sphere, "DriftTip", new Vector3(0.36f, 0.18f, 0.22f), new Vector3(0.7f, 0.45f, 0.7f), snow);
        });

        // ---- level 2: small rock ------------------------------------------------------------------
        GameObject smallRock = BuildObstacle("Obstacle_Rock", "Obstacle_Rock", 0.9f, visual =>
        {
            AddPrim(visual, PrimitiveType.Cube, "Boulder", new Vector3(0f, 0.52f, 0f), new Vector3(1.35f, 1.1f, 1.2f), granite,
                new Vector3(10f, 24f, 7f));
            AddPrim(visual, PrimitiveType.Sphere, "SnowCap", new Vector3(0f, 1.02f, 0f), new Vector3(1.1f, 0.3f, 0.95f), snow);
        });

        // ---- level 3: walking NPC -----------------------------------------------------------------
        GameObject npc = BuildObstacle("Obstacle_Npc", "Obstacle_Npc", 1.3f, visual =>
        {
            AddPrim(visual, PrimitiveType.Cylinder, "LegL", new Vector3(0.22f, 0.36f, 0f), new Vector3(0.22f, 0.36f, 0.22f), trousers);
            AddPrim(visual, PrimitiveType.Cylinder, "LegR", new Vector3(-0.22f, 0.36f, 0f), new Vector3(0.22f, 0.36f, 0.22f), trousers);

            GameObject body = new GameObject("Body");
            body.transform.SetParent(visual, false);

            AddPrim(body.transform, PrimitiveType.Capsule, "Torso", new Vector3(0f, 1.05f, 0f), new Vector3(0.85f, 0.62f, 0.85f), jacket);
            AddPrim(body.transform, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.84f, 0f), Vector3.one * 0.44f, skin);
            AddPrim(body.transform, PrimitiveType.Cube, "Backpack", new Vector3(0f, 1.15f, -0.45f), new Vector3(0.55f, 0.7f, 0.3f), pack);
        }, prefabRoot =>
        {
            NpcWalkAnim walk = prefabRoot.AddComponent<NpcWalkAnim>();
            walk.body = prefabRoot.transform.Find("Visual/Body");
            walk.legLeft = prefabRoot.transform.Find("Visual/LegL");
            walk.legRight = prefabRoot.transform.Find("Visual/LegR");
        });

        // ---- level 4a: tall tree ------------------------------------------------------------------
        Mesh cone = CreateConeMesh("Cone_Seg8", 1f, 1f, 8);
        GameObject tallTree = BuildObstacle("Obstacle_Tree", "Obstacle_Tree", 1.7f, visual =>
        {
            AddPrim(visual, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, 1.2f, 0f), new Vector3(0.3f, 1.2f, 0.3f), wood);
            AddMesh(visual, "Tier1", cone, new Vector3(0f, 1.6f, 0f), new Vector3(1.7f, 2f, 1.7f), pine);
            AddMesh(visual, "Tier2", cone, new Vector3(0f, 2.9f, 0f), new Vector3(1.35f, 1.8f, 1.35f), pine);
            AddMesh(visual, "Tier3", cone, new Vector3(0f, 4f, 0f), new Vector3(1f, 1.6f, 1f), pine);
            AddMesh(visual, "SnowCap", cone, new Vector3(0f, 5f, 0f), new Vector3(0.7f, 1f, 0.7f), snow);
        });

        // ---- level 4b: big snow mound -------------------------------------------------------------
        GameObject snowMound2 = BuildObstacle("Obstacle_SnowMound2", "Obstacle_SnowMound2", 2.2f, visual =>
        {
            AddPrim(visual, PrimitiveType.Sphere, "Bank", new Vector3(0f, 0.9f, 0f), new Vector3(4.4f, 2f, 4.4f), snow);
            AddPrim(visual, PrimitiveType.Sphere, "BankPeak", new Vector3(0.8f, 0.75f, 0.5f), new Vector3(2.6f, 1.6f, 2.6f), snow);
        });

        // ---- level 5: house -----------------------------------------------------------------------
        Mesh pyramid = CreateConeMesh("Cone_Seg4", 1f, 1f, 4);
        GameObject house = BuildObstacle("Obstacle_House", "Obstacle_House", 3f, visual =>
        {
            AddPrim(visual, PrimitiveType.Cube, "Walls", new Vector3(0f, 1.6f, 0f), new Vector3(5.6f, 3.2f, 5f), wood);
            AddMesh(visual, "Roof", pyramid, new Vector3(0f, 3.2f, 0f), new Vector3(4f, 2.6f, 4f), snow, new Vector3(0f, 45f, 0f));
            AddPrim(visual, PrimitiveType.Cube, "Chimney", new Vector3(1.7f, 4.6f, 1f), new Vector3(0.6f, 1.6f, 0.6f), granite);
            AddPrim(visual, PrimitiveType.Cube, "Door", new Vector3(0f, 0.9f, 2.52f), new Vector3(1f, 1.8f, 0.12f), pack);
            AddPrim(visual, PrimitiveType.Cube, "WindowL", new Vector3(-1.8f, 2.2f, 2.52f), new Vector3(0.9f, 0.9f, 0.1f), lamp);
            AddPrim(visual, PrimitiveType.Cube, "WindowR", new Vector3(1.8f, 2.2f, 2.52f), new Vector3(0.9f, 0.9f, 0.1f), lamp);
        });

        // ---- level 6: skyscraper ------------------------------------------------------------------
        GameObject skyscraper = BuildObstacle("Obstacle_Skyscraper", "Obstacle_Skyscraper", 4.5f, visual =>
        {
            AddPrim(visual, PrimitiveType.Cube, "Tower", new Vector3(0f, 8f, 0f), new Vector3(8f, 16f, 8f), glass);
            AddPrim(visual, PrimitiveType.Cube, "Annex", new Vector3(4.4f, 3.5f, 1.5f), new Vector3(4.4f, 7f, 4.4f), glass);
            AddPrim(visual, PrimitiveType.Cube, "Roof", new Vector3(0f, 16.15f, 0f), new Vector3(8.2f, 0.3f, 8.2f), granite);
            for (int i = 0; i < 4; i++)
            {
                AddPrim(visual, PrimitiveType.Cube, $"Band{i}", new Vector3(0f, 3f + i * 3f, 0f), new Vector3(8.1f, 0.2f, 8.1f), lamp);
            }
        });

        ApplyConfig(snowMound, smallRock, npc, tallTree, snowMound2, house, skyscraper);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        Debug.Log("[Builder] 7 obstacle level prefabs built, GameConfig.obstacleLevels filled, scene saved.");
    }

    // ------------------------------------------------------------------ gameplay definitions

    static void ApplyConfig(GameObject snowMound, GameObject rock, GameObject npc, GameObject tree,
        GameObject snowMound2, GameObject house, GameObject skyscraper)
    {
        GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
        if (config == null)
        {
            Debug.LogError($"[Builder] Missing GameConfig at {ConfigPath}");
            return;
        }

        // Longer hang time: lower gravity, same authored jump height conversion (v = sqrt(2gh)).
        config.gravityY = -3.2f;
        config.baseJumpHeight = 3.2f;

        // Size comparison bands for the three obstacle outcomes.
        config.sameSizeBand = 0.1f;
        config.slowSpeedFactor = 0.5f;
        config.slowDuration = 0.6f;

        // Store effect rates.
        config.growthMultiplierPerLevel = 0.15f;
        config.goldMultiplierPerLevel = 0.25f;
        config.luckyChancePerLevel = 0.05f;

        config.obstacleLevels = new[]
        {
            Level("Snow Mound", 1, 0.4f, 0f, 1f, 1f, 25, true, 0.35f, snowMound),
            Level("Small Rock", 2, 0.9f, 40f, 400f, 0.5f, 50, false, 0f, rock),
            Level("Hiker", 3, 1.3f, 150f, 500f, 0.45f, 75, false, 0f, npc),
            Level("Tall Tree", 4, 1.7f, 300f, 600f, 0.35f, 100, false, 0f, tree),
            Level("Snow Mound 2", 4, 2.2f, 350f, 600f, 0.3f, 120, true, 0.6f, snowMound2),
            Level("House", 5, 3f, 450f, 800f, 0.2f, 160, false, 0f, house),
            Level("Skyscraper", 6, 4.5f, 600f, 1200f, 0.12f, 250, false, 0f, skyscraper)
        };

        // The fallback prefab on the generator is always the softest level.
        TrackGenerator generator = Object.FindAnyObjectByType<TrackGenerator>();
        if (generator != null)
        {
            generator.obstaclePrefab = snowMound;
            generator.config = config;
            EditorUtility.SetDirty(generator);
        }

        EditorUtility.SetDirty(config);
        Debug.Log($"[Builder] GameConfig: gravity {config.gravityY} m/s^2, jump {config.baseJumpHeight} m, " +
                  $"{config.obstacleLevels.Length} obstacle levels (skyscrapers unlock at {config.obstacleLevels[6].unlockDistance} m)");
    }

    static ObstacleLevelDef Level(string label, int level, float radius, float unlock, float ramp, float maxWeight,
        int points, bool grantsGrowth, float growthPerHit, GameObject prefab)
    {
        return new ObstacleLevelDef
        {
            label = label,
            level = level,
            radius = radius,
            unlockDistance = unlock,
            weightRampDistance = ramp,
            maxWeight = maxWeight,
            points = points,
            grantsGrowth = grantsGrowth,
            growthPerHit = growthPerHit,
            prefab = prefab
        };
    }

    // ------------------------------------------------------------------ prefab assembly

    static GameObject BuildObstacle(string prefabName, string rootName, float radius,
        System.Action<Transform> buildVisual, System.Action<GameObject> configureRoot = null)
    {
        GameObject root = new GameObject(rootName);

        SphereCollider trigger = root.AddComponent<SphereCollider>();
        trigger.radius = radius;
        trigger.center = Vector3.zero;
        trigger.isTrigger = true;

        Obstacle obstacle = root.AddComponent<Obstacle>();
        obstacle.config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        // The root sits at collider height, so the visual is dropped to stand on the ground.
        visual.transform.localPosition = new Vector3(0f, -radius, 0f);

        buildVisual(visual.transform);
        configureRoot?.Invoke(root);

        SetLayerRecursive(root, LayerMask.NameToLayer("Obstacle"));

        string path = $"{PrefabFolder}/{prefabName}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Debug.Log($"[Builder] Obstacle prefab {path} (level radius {radius:F2} m)");
        return prefab;
    }

    static GameObject AddPrim(Transform parent, PrimitiveType type, string name, Vector3 localPos,
        Vector3 localScale, Material material, Vector3 euler = default)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = localScale;

        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;

        return go;
    }

    static GameObject AddMesh(Transform parent, string name, Mesh mesh, Vector3 localPos,
        Vector3 localScale, Material material, Vector3 euler = default)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = localScale;

        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        MeshRenderer renderer = go.AddComponent<MeshRenderer>();
        if (material != null) renderer.sharedMaterial = material;

        return go;
    }

    // ------------------------------------------------------------------ assets

    /// <summary>Unit cone (radius 1 at the base, height 1) scaled per use; cached as an asset.</summary>
    static Mesh CreateConeMesh(string name, float radius, float height, int segments)
    {
        string path = $"{MeshFolder}/{name}.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null) return existing;

        var vertices = new System.Collections.Generic.List<Vector3>();
        var triangles = new System.Collections.Generic.List<int>();

        int apex = 0;
        vertices.Add(new Vector3(0f, height, 0f));

        for (int i = 0; i < segments; i++)
        {
            float a = Mathf.PI * 2f * i / segments;
            vertices.Add(new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }

        int baseCenter = vertices.Count;
        vertices.Add(Vector3.zero);

        for (int i = 0; i < segments; i++)
        {
            int a = 1 + i;
            int b = 1 + (i + 1) % segments;

            triangles.Add(apex); triangles.Add(b); triangles.Add(a);
            triangles.Add(baseCenter); triangles.Add(a); triangles.Add(b);
        }

        Mesh mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    static Material CreateMaterial(string name, Color baseColor, float smoothness, Color? emissive)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", baseColor);
        material.SetColor("_Color", baseColor);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);

        if (emissive.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissive.Value);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        if (layer < 0) return;
        go.layer = layer;
        foreach (Transform child in go.transform) SetLayerRecursive(child.gameObject, layer);
    }

    static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
    }
}
