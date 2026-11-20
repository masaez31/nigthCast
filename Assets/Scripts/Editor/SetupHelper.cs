#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class SetupHelper
{
    [MenuItem("CLI/Setup And Play")]
    public static void SetupAndPlay()
    {
        var type = System.Type.GetType("NightCast.Runtime.GameBootstrapper, Assembly-CSharp");
        if (type == null)
        {
            Debug.LogError("Could not find GameBootstrapper. Compilation might have failed.");
            return;
        }

        var obj = Object.FindFirstObjectByType(type) as MonoBehaviour;
        if (obj == null)
        {
            var go = new GameObject("GameBootstrapper");
            go.AddComponent(type);
            Debug.Log("Created GameBootstrapper object.");
        }

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        EditorApplication.isPlaying = true;
    }
}
#endif
