#if UNITY_EDITOR
using System;
using System.IO;
using TimeMetroidvania.Boss;
using TimeMetroidvania.Player;
using TimeMetroidvania.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace TimeMetroidvania.Editor
{
    /// <summary>在用户当前地图上补充镜头与 Boss 表现组件，不重建墙体和敌人。</summary>
    public static class V2ActionRevision
    {
        [MenuItem("Tools/时序旅人/V2/Apply Action And Boss Revision")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("请先退出运行模式。");
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != "EightRoomGreybox")
                throw new InvalidOperationException("请打开当前 EightRoomGreybox 地图。");
            if (scene.isDirty) EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Logs/V2ActionRevision");
            string backup = "Logs/V2ActionRevision/Scene_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
            File.Copy(scene.path, backup, false);

            CameraFollow2D camera = Object.FindFirstObjectByType<CameraFollow2D>();
            if (camera == null) throw new InvalidOperationException("场景缺少跟随镜头。");
            if (camera.GetComponent<RoomViewportMask>() == null)
                Undo.AddComponent<RoomViewportMask>(camera.gameObject);

            foreach (BossBrain boss in Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None))
            {
                if (boss.GetComponent<BossAttackTelegraphVisual>() == null)
                    Undo.AddComponent<BossAttackTelegraphVisual>(boss.gameObject);
                if (boss.GetComponent<BossGreyboxPoseVisual>() == null)
                    Undo.AddComponent<BossGreyboxPoseVisual>(boss.gameObject);
                EnsureBossHitbox(boss, "SweepHitbox");
                EnsureBossHitbox(boss, "SlamHitbox");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"V2 动作与 Boss 场景组件已更新；修改前地图备份：{backup}");
        }

        private static void EnsureBossHitbox(BossBrain boss, string name)
        {
            Transform child = boss.transform.Find(name);
            if (child == null)
            {
                GameObject go = new(name);
                Undo.RegisterCreatedObjectUndo(go, "创建 Boss 独立攻击框");
                go.transform.SetParent(boss.transform, false);
                child = go.transform;
            }
            BoxCollider2D collider = child.GetComponent<BoxCollider2D>();
            if (collider == null) collider = Undo.AddComponent<BoxCollider2D>(child.gameObject);
            collider.isTrigger = true;
            collider.enabled = false;
            if (child.GetComponent<BossAttackHitbox2D>() == null)
                Undo.AddComponent<BossAttackHitbox2D>(child.gameObject);
        }
    }
}
#endif
