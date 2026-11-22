using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Snowballin;

/// <summary>
/// One-shot authoring pass for the Snowballin core loop: creates materials, the game config,
/// the gameplay prefabs (chunk / obstacle / coin / ramp), wires the Snowball prefab and puts
/// the Game + Track objects into SampleScene. Safe to re-run.
/// </summary>
public static class SnowballinBuilder
{
    const string InputAssetPath = "Assets/InputSystem_Actions.inputactions";
    const string ConfigPath = "Assets/Configs/GameConfig.asset";
    const string SnowballPrefabPath = "Assets/Prefabs/Snowball.prefab";

    public static void Execute()
    {
        EnsureFolders();
        EnsureLayer("Player");
        EnsureLayer("Track");
        EnsureLayer("Obstacle");
        EnsureLayer("Coin");
        EnsureTag("Player");
        EnsureTag("Obstacle");
        EnsureTag("Coin");

        Material trackSnow = CreateMaterial("Assets/Materials/Track_Snow.mat", new Color(0.93f, 0.96f, 1f), 0.32f, null);
        Material wallWhite = CreateMaterial("Assets/Materials/Wall_White.mat", new Color(0.97f, 0.98f, 1f), 0.55f, null);
        Material rock = CreateMaterial("Assets/Materials/Obstacle_Rock.mat", new Color(0.34f, 0.38f, 0.46f), 0.12f, null);
        Material coin = CreateMaterial("Assets/Materials/Coin_Gold.mat", new Color(1f, 0.78f, 0.2f), 0.85f, new Color(1.4f, 0.95f, 0.2f));
        Material ramp = CreateMaterial("Assets/Materials/Ramp_Ice.mat", new Color(0.35f, 0.68f, 1f), 0.8f, new Color(0.05f, 0.3f, 0.7f));

        GameConfig config = LoadOrCreateConfig();

        GameObject chunkPrefab = BuildTrackChunkPrefab(trackSnow, wallWhite, config);
        GameObject obstaclePrefab = BuildObstaclePrefab(rock, config);
        GameObject coinPrefab = BuildCoinPrefab(coin);
        GameObject rampPrefab = BuildRampPrefab(ramp);

        GameObject snowballPrefab = ConfigureSnowballPrefab(config);

        WireScene(config, chunkPrefab, obstaclePrefab, coinPrefab, rampPrefab, snowballPrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Builder] Snowballin core loop assets and scene wiring complete.");
    }

    // ---------------------------------------------------------------- assets

    static void EnsureFolders()
    {
        string[] folders = { "Assets/Materials", "Assets/Prefabs", "Assets/Configs", "Assets/Textures" };
        foreach (string folder in folders)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
                string name = System.IO.Path.GetFileName(folder);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }

    static GameConfig LoadOrCreateConfig()
    {
        GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
        if (config != null) return config;

        config = ScriptableObject.CreateInstance<GameConfig>();
        AssetDatabase.CreateAsset(config, ConfigPath);
        Debug.Log("[Builder] Created GameConfig asset.");
        return config;
    }

    static Material CreateMaterial(string path, Color baseColor, float smoothness, Color? emissive)
    {
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

    // ---------------------------------------------------------------- prefabs

    static GameObject BuildTrackChunkPrefab(Material snow, Material wall, GameConfig config)
    {
        string path = "Assets/Prefabs/TrackChunk.prefab";

        GameObject root = new GameObject("TrackChunk");
        TrackChunk chunk = root.AddComponent<TrackChunk>();

        int trackLayer = LayerMask.NameToLayer("Track");

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform, false);
        ground.transform.localPosition = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(config.trackWidth, 1f, config.chunkLength);
        ground.GetComponent<MeshRenderer>().sharedMaterial = snow;
        if (trackLayer >= 0) ground.layer = trackLayer;

        // Solid white border walls: the ball bounces off them instead of leaving the slab.
        float wallX = config.trackWidth * 0.5f - config.wallThickness * 0.5f;
        CreateWall(root.transform, "WallLeft", new Vector3(-wallX, config.wallHeight * 0.5f, 0f), wall, config, trackLayer);
        CreateWall(root.transform, "WallRight", new Vector3(wallX, config.wallHeight * 0.5f, 0f), wall, config, trackLayer);

        GameObject content = new GameObject("Content");
        content.transform.SetParent(root.transform, false);

        chunk.contentRoot = content.transform;
        chunk.Length = config.chunkLength;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Builder] TrackChunk prefab written to {path} ({config.trackWidth:F1} m wide, walls {config.wallHeight:F1} m)");
        return prefab;
    }

    static void CreateWall(Transform parent, string name, Vector3 localPosition, Material wall, GameConfig config, int layer)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = new Vector3(config.wallThickness, config.wallHeight, config.chunkLength);
        go.GetComponent<MeshRenderer>().sharedMaterial = wall;
        if (layer >= 0) go.layer = layer;
    }

    static GameObject BuildObstaclePrefab(Material rock, GameConfig config)
    {
        string path = "Assets/Prefabs/Obstacle.prefab";

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "Obstacle";
        root.GetComponent<MeshRenderer>().sharedMaterial = rock;
        int obstacleLayer = LayerMask.NameToLayer("Obstacle");
        if (obstacleLayer >= 0) root.layer = obstacleLayer;

        Obstacle obstacle = root.AddComponent<Obstacle>();
        obstacle.config = config;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Builder] Obstacle prefab written to {path}");
        return prefab;
    }

    static GameObject BuildCoinPrefab(Material gold)
    {
        string path = "Assets/Prefabs/Coin.prefab";

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Coin";
        root.transform.localScale = Vector3.one * 0.45f;
        root.GetComponent<MeshRenderer>().sharedMaterial = gold;
        int coinLayer = LayerMask.NameToLayer("Coin");
        if (coinLayer >= 0) root.layer = coinLayer;

        SphereCollider collider = root.GetComponent<SphereCollider>();
        if (collider != null) collider.isTrigger = true;

        root.AddComponent<Coin>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Builder] Coin prefab written to {path}");
        return prefab;
    }

    static GameObject BuildRampPrefab(Material ice)
    {
        string path = "Assets/Prefabs/Ramp.prefab";

        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "Ramp";
        root.transform.localScale = new Vector3(3f, 0.4f, 4f);
        root.GetComponent<MeshRenderer>().sharedMaterial = ice;

        root.AddComponent<Ramp>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Builder] Ramp prefab written to {path}");
        return prefab;
    }

    static GameObject ConfigureSnowballPrefab(GameConfig config)
    {
        InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
        if (inputAsset == null) Debug.LogError($"[Builder] Missing input asset at {InputAssetPath}");

        GameObject contents = PrefabUtility.LoadPrefabContents(SnowballPrefabPath);

        SphereCollider sphere = contents.GetComponent<SphereCollider>();
        if (sphere != null) sphere.radius = 0.5f;

        Rigidbody rigidbody = contents.GetComponent<Rigidbody>();
        if (rigidbody == null) rigidbody = contents.AddComponent<Rigidbody>();
        rigidbody.mass = 1f;
        rigidbody.useGravity = true;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        SnowballBody body = contents.GetComponent<SnowballBody>();
        if (body == null) body = contents.AddComponent<SnowballBody>();
        body.config = config;

        SnowballController controller = contents.GetComponent<SnowballController>();
        if (controller == null) controller = contents.AddComponent<SnowballController>();
        controller.config = config;
        controller.inputActions = inputAsset;

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0) contents.layer = playerLayer;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(contents, SnowballPrefabPath);
        PrefabUtility.UnloadPrefabContents(contents);
        Debug.Log("[Builder] Snowball prefab configured (Rigidbody + SnowballBody + SnowballController).");
        return prefab;
    }

    // ---------------------------------------------------------------- scene

    static void WireScene(GameConfig config, GameObject chunkPrefab, GameObject obstaclePrefab,
        GameObject coinPrefab, GameObject rampPrefab, GameObject snowballPrefab)
    {
        Scene scene = SceneManager.GetActiveScene();
        InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);

        CameraRig rig = Object.FindAnyObjectByType<CameraRig>();
        if (rig == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null) rig = mainCamera.gameObject.AddComponent<CameraRig>();
        }
        if (rig != null) rig.config = config;

        GameObject gameObject = GameObject.Find("Game");
        if (gameObject == null) gameObject = new GameObject("Game");

        GameObject trackObject = GameObject.Find("Track");
        if (trackObject == null) trackObject = new GameObject("Track");
        trackObject.transform.SetParent(gameObject.transform, false);
        trackObject.transform.position = Vector3.zero;

        TrackGenerator generator = trackObject.GetComponent<TrackGenerator>();
        if (generator == null) generator = trackObject.AddComponent<TrackGenerator>();
        generator.config = config;
        generator.chunkPrefab = chunkPrefab;
        generator.obstaclePrefab = obstaclePrefab;
        generator.coinPrefab = coinPrefab;
        generator.rampPrefab = rampPrefab;

        GameManager manager = gameObject.GetComponent<GameManager>();
        if (manager == null) manager = gameObject.AddComponent<GameManager>();
        manager.config = config;
        manager.inputActions = inputAsset;
        manager.snowballPrefab = snowballPrefab;
        manager.trackGenerator = generator;
        manager.cameraRig = rig;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Builder] Scene '{scene.name}' wired and saved.");
    }

    // ---------------------------------------------------------------- tag manager

    static void EnsureLayer(string layerName)
    {
        if (LayerMask.NameToLayer(layerName) >= 0) return;

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty element = layers.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(element.stringValue)) continue;

            element.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Builder] Created layer '{layerName}' at index {i}");
            return;
        }

        Debug.LogWarning($"[Builder] No free user layer slot for '{layerName}'");
    }

    static void EnsureTag(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tagName) return;
        }

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log($"[Builder] Created tag '{tagName}'");
    }
}
