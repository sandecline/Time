#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeMetroidvania.Editor
{
    /// <summary>
    /// 编辑器首次打开默认空场景时，自动呈现当前阶段的七房间试玩场景。
    /// </summary>
    [InitializeOnLoad]
    internal static class PrototypeSceneStartup
    {
        private const string EncounterScenePath =
            "Assets/_Project/Scenes/Prototype/EnemyEncounter.unity";
        private const string LatestScenePath =
            "Assets/_Project/Scenes/Prototype/SevenRoomSlice.unity";
        private const string SessionKey = "TimeMetroidvania.PrototypeSceneStartupChecked";

        static PrototypeSceneStartup()
        {
            if (!Application.isBatchMode)
            {
                EditorApplication.delayCall += OpenLatestWhenDefaultSceneIsEmpty;
            }
        }

        [MenuItem("Tools/Time Metroidvania/Open Enemy Encounter")]
        private static void OpenEncounterFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode
                || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(EncounterScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/Time Metroidvania/Open Seven Room Slice")]
        private static void OpenLatestFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode
                || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(LatestScenePath, OpenSceneMode.Single);
        }

        private static void OpenLatestWhenDefaultSceneIsEmpty()
        {
            if (SessionState.GetBool(SessionKey, false)
                || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid()
                || SceneManager.sceneCount != 1
                || !string.IsNullOrEmpty(activeScene.path))
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

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(LatestScenePath) == null)
            {
                Debug.LogError($"找不到默认试玩场景：{LatestScenePath}");
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EditorSceneManager.OpenScene(LatestScenePath, OpenSceneMode.Single);
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}
#endif
