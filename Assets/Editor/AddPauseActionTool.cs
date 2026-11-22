using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class AddPauseActionTool
{
    public static void Execute()
    {
        const string path = "Assets/InputSystem_Actions.inputactions";

        InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        if (asset == null)
        {
            Debug.LogError($"[Setup] Missing input asset at {path}");
            return;
        }

        asset.Disable();

        InputActionMap map = asset.FindActionMap("Player", true);
        InputAction existing = map.FindAction("Pause", false);
        if (existing == null)
        {
            InputAction pause = map.AddAction("Pause", InputActionType.Button);
            pause.AddBinding("<Keyboard>/p");
            Debug.Log("[Setup] Added Player/Pause bound to <Keyboard>/p");
        }
        else
        {
            Debug.Log("[Setup] Player/Pause already present");
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
