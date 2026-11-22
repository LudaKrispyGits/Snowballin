using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Polish pass: snowy mountain look. Assigns the generated snow / snowball albedos, builds the
/// panoramic skybox, and sets fog, ambient light and bloom. Safe to re-run.
/// </summary>
public static class SnowballinPolish
{
    const string SnowAlbedoPath = "Assets/Generated_Images/Snow_Albedo.png";
    const string SnowballAlbedoPath = "Assets/Generated_Images/Snowball_Albedo.png";
    const string RockAlbedoPath = "Assets/Textures/Obstacle_Rock_Albedo.png";
    const string PanoramaPath = "Assets/Generated_Images/Mountain_Panorama.png";
    const string SkyboxMaterialPath = "Assets/Materials/Sky_Mountain.mat";

    public static void Execute()
    {
        Texture2D snow = AssetDatabase.LoadAssetAtPath<Texture2D>(SnowAlbedoPath);
        Texture2D ball = AssetDatabase.LoadAssetAtPath<Texture2D>(SnowballAlbedoPath);
        Texture2D rock = AssetDatabase.LoadAssetAtPath<Texture2D>(RockAlbedoPath);

        // 72 m x 40 m chunk at roughly 3 m per tile keeps the grain size constant.
        ApplyMap("Assets/Materials/Track_Snow.mat", snow, new Vector2(24f, 14f), 0.45f);
        ApplyMap("Assets/Materials/Snowball_Snow.mat", ball, new Vector2(2f, 2f), 0.3f);
        ApplyMap("Assets/Materials/Obstacle_Rock.mat", rock, new Vector2(1f, 1f), 0.2f);

        ApplySkybox();

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0075f;
        RenderSettings.fogColor = new Color(0.72f, 0.81f, 0.93f);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.58f, 0.7f, 0.9f);
        RenderSettings.ambientEquatorColor = new Color(0.62f, 0.72f, 0.86f);
        RenderSettings.ambientGroundColor = new Color(0.5f, 0.56f, 0.66f);

        Volume volume = Object.FindAnyObjectByType<Volume>();
        if (volume != null && volume.sharedProfile != null)
        {
            VolumeProfile profile = volume.sharedProfile;
            if (!profile.TryGet(out Bloom bloom)) bloom = profile.Add<Bloom>(true);
            if (bloom != null)
            {
                bloom.active = true;
                bloom.intensity.Override(0.6f);
                bloom.threshold.Override(0.9f);
            }
            EditorUtility.SetDirty(profile);
            Debug.Log("[Polish] Bloom added to the global volume profile.");
        }
        else
        {
            Debug.LogWarning("[Polish] No global Volume found; skipped bloom.");
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[Polish] Snowy mountain pass applied: snow albedos, panoramic skybox, fog/ambient/bloom, scene saved.");
    }

    static void ApplySkybox()
    {
        Shader shader = Shader.Find("Skybox/Panoramic");
        if (shader == null)
        {
            Debug.LogWarning("[Polish] Skybox/Panoramic shader not found; skybox left unchanged.");
            return;
        }

        Texture2D panorama = AssetDatabase.LoadAssetAtPath<Texture2D>(PanoramaPath);
        if (panorama == null)
        {
            Debug.LogWarning($"[Polish] Missing panorama texture at {PanoramaPath}; skybox left unchanged.");
            return;
        }

        Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
        if (sky == null)
        {
            sky = new Material(shader);
            AssetDatabase.CreateAsset(sky, SkyboxMaterialPath);
        }

        sky.shader = shader;
        sky.SetTexture("_MainTex", panorama);
        sky.SetFloat("_Mapping", 1f);    // latitude / longitude panorama
        sky.SetFloat("_ImageType", 0f);  // 360 degrees
        sky.SetFloat("_Exposure", 1.15f);
        sky.SetFloat("_Rotation", 0f);
        if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", new Color(0.9f, 0.95f, 1f, 0.5f));
        EditorUtility.SetDirty(sky);

        RenderSettings.skybox = sky;
        DynamicGI.UpdateEnvironment();
        Debug.Log($"[Polish] Panoramic mountain skybox applied from {PanoramaPath}");
    }

    static void ApplyMap(string materialPath, Texture2D texture, Vector2 tiling, float smoothness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null || texture == null)
        {
            Debug.LogWarning($"[Polish] Missing material '{materialPath}' or its texture.");
            return;
        }

        material.SetTexture("_BaseMap", texture);
        material.SetTexture("_MainTex", texture);
        material.SetTextureScale("_BaseMap", tiling);
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        Debug.Log($"[Polish] Assigned albedo '{texture.name}' to {material.name} (tiling {tiling}, smoothness {smoothness})");
    }
}
