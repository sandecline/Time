using System;
using TimeMetroidvania.GameTime;
using TimeMetroidvania.Level;
using TimeMetroidvania.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TimeMetroidvania.Flow
{
    /// <summary>
    /// 管理测试 Demo 的标题、暂停、地图、死亡遮罩和结尾流程。
    /// </summary>
    public sealed class StageSevenFlowController : MonoBehaviour
    {
        [SerializeField] private GameObject player;
        [SerializeField] private HealthSystem playerHealth;
        [SerializeField] private TimeGauge timeGauge;
        [SerializeField] private WorldTimeService worldTime;
        [SerializeField] private StageFiveLevelController level;
        [SerializeField] private DemoProgressService progress;
        [SerializeField] private bool startInMenu = true;

        private Behaviour[] playerControls;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private bool deathVisible;

        public event Action<GameFlowState> StateChanged;

        public GameFlowState State { get; private set; } = GameFlowState.StartMenu;
        public bool IsDeathVisible => deathVisible;
        public bool PersistenceEnabled => progress != null && progress.PersistenceEnabled;

        public void Configure(
            GameObject playerObject, HealthSystem health, TimeGauge gauge,
            WorldTimeService time, StageFiveLevelController controller,
            DemoProgressService progressService)
        {
            player = playerObject;
            playerHealth = health;
            timeGauge = gauge;
            worldTime = time;
            level = controller;
            progress = progressService;
            CachePlayerControls();
        }

        private void Awake()
        {
            CachePlayerControls();
            if (startInMenu)
            {
                SetState(GameFlowState.StartMenu);
            }
            else
            {
                StartNewGame();
            }
        }

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.Died += HandlePlayerDied;
                playerHealth.Respawned += HandlePlayerRespawned;
            }
            if (level != null)
            {
                level.BossDefeatedConfirmed += HandleBossDefeated;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.Died -= HandlePlayerDied;
                playerHealth.Respawned -= HandlePlayerRespawned;
            }
            if (level != null)
            {
                level.BossDefeatedConfirmed -= HandleBossDefeated;
            }
            Time.timeScale = 1f;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (State == GameFlowState.StartMenu
                && (keyboard.enterKey.wasPressedThisFrame
                    || keyboard.spaceKey.wasPressedThisFrame))
            {
                StartNewGame();
                return;
            }

            if (State == GameFlowState.Ending)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (State == GameFlowState.Map || State == GameFlowState.Paused)
                {
                    ResumeGame();
                }
                else if (State == GameFlowState.Playing && !deathVisible)
                {
                    PauseGame();
                }
                return;
            }

            if (keyboard.mKey.wasPressedThisFrame && !deathVisible)
            {
                if (State == GameFlowState.Playing)
                {
                    OpenMap();
                }
                else if (State == GameFlowState.Map)
                {
                    ResumeGame();
                }
            }
        }

        public void StartNewGame()
        {
            progress?.StartNewSession();
            worldTime?.ResetToNormal();
            timeGauge?.Clear();
            playerHealth?.ResetForEncounter();
            deathVisible = false;
            SetState(GameFlowState.Playing);
            level?.ShowMessage("试玩版：本次运行不读取或写入存档。M 查看地图，Esc 暂停。");
        }

        public void PauseGame()
        {
            if (State == GameFlowState.Playing && !deathVisible)
            {
                SetState(GameFlowState.Paused);
            }
        }

        public void OpenMap()
        {
            if (State == GameFlowState.Playing && !deathVisible)
            {
                SetState(GameFlowState.Map);
            }
        }

        public void ResumeGame()
        {
            if (State is GameFlowState.Paused or GameFlowState.Map)
            {
                SetState(GameFlowState.Playing);
            }
        }

        public void ReturnToTitle()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void QuitGame()
        {
            Application.Quit();
        }

        private void SetState(GameFlowState next)
        {
            State = next;
            bool playing = next == GameFlowState.Playing;
            Time.timeScale = playing ? 1f : 0f;
            SetPlayerControlsEnabled(playing);
            StateChanged?.Invoke(next);
        }

        private void CachePlayerControls()
        {
            if (player == null)
            {
                return;
            }

            playerControls = new Behaviour[]
            {
                player.GetComponent<PlayerMovement>(),
                player.GetComponent<PlayerCombat>(),
                player.GetComponent<PlayerDash>(),
                player.GetComponent<PlayerHealing>(),
                player.GetComponent<PlayerTimeAbilities>()
            };
        }

        private void SetPlayerControlsEnabled(bool enabled)
        {
            if (playerControls == null)
            {
                CachePlayerControls();
            }
            if (playerControls == null)
            {
                return;
            }

            foreach (Behaviour control in playerControls)
            {
                if (control != null)
                {
                    control.enabled = enabled;
                }
            }
        }

        private void HandlePlayerDied()
        {
            deathVisible = true;
        }

        private void HandlePlayerRespawned()
        {
            deathVisible = false;
        }

        private void HandleBossDefeated()
        {
            worldTime?.ResetToNormal();
            deathVisible = false;
            SetState(GameFlowState.Ending);
        }

        private void OnGUI()
        {
            GUI.depth = -100;
            titleStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 34,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            bodyStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                normal = { textColor = Color.white }
            };

            switch (State)
            {
                case GameFlowState.StartMenu:
                    DrawStartMenu();
                    break;
                case GameFlowState.Paused:
                    DrawPauseMenu();
                    break;
                case GameFlowState.Map:
                    DrawMap();
                    break;
                case GameFlowState.Ending:
                    DrawEnding();
                    break;
                case GameFlowState.Playing when deathVisible:
                    DrawDeathOverlay();
                    break;
            }
        }

        private void DrawStartMenu()
        {
            Rect panel = CenteredRect(600f, 390f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x, panel.y + 35f, panel.width, 55f),
                "时序旅人 · 可玩 Demo", titleStyle);
            GUI.Label(new Rect(panel.x + 30f, panel.y + 100f, panel.width - 60f, 55f),
                "当前版本不会读取或写入存档\n每次启动游戏都从房间 1 开始", bodyStyle);
            if (GUI.Button(new Rect(panel.x + 150f, panel.y + 185f, 300f, 52f),
                    "开始游戏（Enter）"))
            {
                StartNewGame();
            }
            GUI.enabled = false;
            GUI.Button(new Rect(panel.x + 150f, panel.y + 248f, 300f, 44f),
                "继续游戏（当前版本不可用）");
            GUI.enabled = true;
            if (GUI.Button(new Rect(panel.x + 150f, panel.y + 303f, 300f, 44f),
                    "退出"))
            {
                QuitGame();
            }
        }

        private void DrawPauseMenu()
        {
            Rect panel = CenteredRect(460f, 280f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x, panel.y + 25f, panel.width, 50f),
                "暂停", titleStyle);
            if (GUI.Button(new Rect(panel.x + 90f, panel.y + 95f, 280f, 48f), "继续"))
            {
                ResumeGame();
            }
            if (GUI.Button(new Rect(panel.x + 90f, panel.y + 155f, 280f, 48f),
                    "返回标题（清空本次进度）"))
            {
                ReturnToTitle();
            }
            GUI.Label(new Rect(panel.x, panel.y + 220f, panel.width, 32f),
                "Esc 返回游戏", bodyStyle);
        }

        private void DrawMap()
        {
            Rect panel = CenteredRect(760f, 520f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x, panel.y + 18f, panel.width, 48f),
                "区域地图", titleStyle);
            GUI.Label(new Rect(panel.x, panel.y + 65f, panel.width, 28f),
                "仅显示本次运行已发现的房间 · M / Esc 关闭", bodyStyle);

            Vector2 origin = new(panel.x + 155f, panel.y + 145f);
            DrawMapNode("R1", origin + new Vector2(0f, 0f));
            DrawMapNode("R2", origin + new Vector2(155f, 0f));
            DrawMapNode("R3", origin + new Vector2(310f, 0f));
            DrawMapNode("R4", origin + new Vector2(0f, 130f));
            DrawMapNode("R5", origin + new Vector2(155f, 130f));
            DrawMapNode("R6", origin + new Vector2(155f, 260f));
            DrawMapNode("R7", origin + new Vector2(310f, 130f));
        }

        private void DrawMapNode(string roomId, Vector2 position)
        {
            bool discovered = progress != null && progress.IsRoomDiscovered(roomId);
            bool current = level != null && level.CurrentRoomId == roomId;
            Color previous = GUI.color;
            GUI.color = current
                ? new Color(0.25f, 0.9f, 0.65f)
                : discovered ? new Color(0.3f, 0.68f, 0.85f) : new Color(0.25f, 0.25f, 0.28f);
            GUI.Box(new Rect(position.x, position.y, 110f, 64f), GUIContent.none);
            GUI.color = previous;
            GUI.Label(new Rect(position.x, position.y + 16f, 110f, 30f),
                discovered ? $"房间 {roomId[1..]}" : "?", bodyStyle);
        }

        private void DrawEnding()
        {
            Rect panel = CenteredRect(640f, 360f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x, panel.y + 38f, panel.width, 58f),
                "时间回廊恢复了流动", titleStyle);
            GUI.Label(new Rect(panel.x + 40f, panel.y + 110f, panel.width - 80f, 60f),
                "Boss 已击败，试玩流程完成。\n本次进度不会写入存档。", bodyStyle);
            if (GUI.Button(new Rect(panel.x + 160f, panel.y + 205f, 320f, 50f),
                    "重新开始"))
            {
                ReturnToTitle();
            }
            if (GUI.Button(new Rect(panel.x + 160f, panel.y + 270f, 320f, 44f), "退出"))
            {
                QuitGame();
            }
        }

        private void DrawDeathOverlay()
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.45f, 0.08f, 0.1f, 0.9f);
            GUI.Box(new Rect(0f, Screen.height * 0.38f, Screen.width, 110f), GUIContent.none);
            GUI.color = previous;
            GUI.Label(new Rect(0f, Screen.height * 0.4f, Screen.width, 58f),
                "时间回溯中……", titleStyle);
        }

        private static Rect CenteredRect(float width, float height)
        {
            return new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);
        }
    }
}
