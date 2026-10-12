#if UNITY_EDITOR
using System.Linq;
using TimeMetroidvania.Boss;
using TimeMetroidvania.Enemy;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TimeMetroidvania.Editor
{
    /// <summary>列出试玩场景中的角色视觉尺寸，以便按原碰撞框绑定新图。</summary>
    internal static class V2ActorArtAudit
    {
        public static void Print()
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/V2/EightRoomArtPreview.unity");
            foreach (var e in Object.FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None))
            {
                var visual = e.transform.Find("ArtVisual");
                var collider = e.GetComponent<Collider2D>();
                Debug.Log($"V2_ACTOR: {e.name} kind={e.Kind} visualScale={visual?.localScale} visualPosition={visual?.localPosition} sprite={visual?.GetComponent<SpriteRenderer>()?.sprite?.name} collider={collider?.bounds.size}");
            }
            foreach (var e in Object.FindObjectsByType<V2FlyingEnemy>(FindObjectsSortMode.None))
            {
                var visual = e.transform.Find("ArtVisual");
                Debug.Log($"V2_ACTOR: {e.name} flying visualScale={visual?.localScale} visualPosition={visual?.localPosition} collider={e.GetComponent<Collider2D>()?.bounds.size}");
            }
            foreach (var e in Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None))
            {
                var visual = e.transform.Find("ArtVisual");
                Debug.Log($"V2_ACTOR: {e.name} boss rootRotation={e.transform.rotation.eulerAngles.z} visualRotation={visual?.rotation.eulerAngles.z} visualScale={visual?.localScale} visualPosition={visual?.localPosition} sprite={visual?.GetComponent<SpriteRenderer>()?.sprite?.name} collider={e.GetComponent<Collider2D>()?.bounds.size}");
                foreach (var renderer in e.GetComponentsInChildren<SpriteRenderer>(true))
                    Debug.Log($"V2_BOSS_RENDERER: {renderer.name}; enabled={renderer.enabled}; sprite={renderer.sprite?.name}; rotation={renderer.transform.rotation.eulerAngles.z}; rect={renderer.sprite?.rect}");
                var sprite = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/V2/Preview/Boss_TimeWarden_BodyActions_v03.png")
                    .OfType<Sprite>().FirstOrDefault(s => s.name == "BossAction3");
                var camera = GameObject.Find("Main Camera")?.GetComponent<Camera>();
                if (visual != null && sprite != null && camera != null)
                {
                    visual.GetComponent<SpriteRenderer>().sprite = sprite;
                    camera.orthographicSize = 3f;
                    camera.transform.position = new Vector3(e.transform.position.x, e.transform.position.y, camera.transform.position.z);
                    V2ArtPreviewSmoke.Capture(camera, "ArtSource/V2/Previews/Boss_Action3_OrientationCheck_v03.png");
                    Debug.Log($"V2_BOSS_CAPTURE: sprite={sprite.name}; rect={sprite.rect}; rotation={visual.rotation.eulerAngles.z}");
                }
            }
        }
    }
}
#endif
