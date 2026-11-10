#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 项目里只有一个场景 Assets/Scenes/Main.unity。
/// 打开工程时如果当前没有有效场景，自动打开它；按 Play 时也固定从它启动。
/// 菜单：铁路大亨2 / 打开主场景 Main
/// </summary>
[InitializeOnLoad]
static class DemoSceneTools
{
    const string ScenePath = "Assets/Scenes/Main.unity";
    const string SessionKey = "Railway2.SceneChecked";

    static DemoSceneTools()
    {
        EditorApplication.delayCall += EnsureMainScene;
    }

    static void EnsureMainScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SessionState.GetBool(SessionKey, false)) return;
        SessionState.SetBool(SessionKey, true);

        var main = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (main == null)
        {
            Debug.LogWarning("[Railway2] 找不到 " + ScenePath);
            return;
        }
        EditorSceneManager.playModeStartScene = main;

        var active = SceneManager.GetActiveScene();
        bool hasRealScene = active.IsValid() && active.isLoaded && !string.IsNullOrEmpty(active.path);
        if (!hasRealScene && File.Exists(ScenePath))
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("铁路大亨2/打开主场景 Main", false, 1)]
    static void OpenMainScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    [MenuItem("铁路大亨2/设为 Play 启动场景", false, 2)]
    static void SetPlayScene()
    {
        var main = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (main == null) return;
        EditorSceneManager.playModeStartScene = main;
        Debug.Log("[Railway2] Play 启动场景 = " + ScenePath);
    }
}
#endif
