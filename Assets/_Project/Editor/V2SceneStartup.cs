#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeMetroidvania.Editor
{
    /// <summary>
    /// 编辑器启动时默认打开已接入美术的 V2 八房间试玩场景。
    /// </summary>
    [InitializeOnLoad]
    internal static class V2SceneStartup
    {
        private const string LatestScenePath =
            "Assets/_Project/Scenes/V2/EightRoomArtPreview.unity";
        private const string GreyboxScenePath =
            "Assets/_Project/Scenes/V2/EightRoomGreybox.unity";
        private const string SessionKey =
            "TimeMetroidvania.V2SceneStartupChecked";

        static V2SceneStartup()
        {
            if (!Application.isBatchMode)
            {
                EditorApplication.delayCall += OpenCurrentSceneOnStartup;
            }
        }

        [MenuItem("Tools/时序旅人/Open V2 Eight Room Greybox", priority = 1)]
        private static void OpenLatestFromMenu()
        {
            OpenSceneFromMenu(GreyboxScenePath);
        }

        private static void OpenSceneFromMenu(string scenePath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode
                || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                Debug.LogError($"找不到场景：{scenePath}");
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        private static void OpenCurrentSceneOnStartup()
        {
            if (SessionState.GetBool(SessionKey, false)
                || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || SceneManager.sceneCount != 1)
            {
                SessionState.SetBool(SessionKey, true);
                return;
            }

            if (!string.IsNullOrEmpty(activeScene.path))
            {
                SessionState.SetBool(SessionKey, true);
                return;
            }

            GameObject[] roots = activeScene.GetRootGameObjects();
            bool isDefaultEmptyScene = !activeScene.isDirty
                && roots.Length == 1
                && roots[0].name == "Main Camera"
                && roots[0].GetComponent<Camera>() != null;
            if (!isDefaultEmptyScene)
            {
                SessionState.SetBool(SessionKey, true);
                return;
            }

            OpenLatestScene();
        }

        private static void OpenLatestScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LatestScenePath) == null)
            {
                Debug.LogError($"找不到默认 V2 试玩场景：{LatestScenePath}");
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EditorSceneManager.OpenScene(LatestScenePath, OpenSceneMode.Single);
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}
#endif
