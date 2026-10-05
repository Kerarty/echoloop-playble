using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Playble.EchoLoop
{
    public enum AppState
    {
        Boot,
        Title,
        LevelSelect,
        Playing,
        Paused,
        LevelComplete,
        GameComplete,
    }

    /// <summary>
    /// Точка входа и конечный автомат игры: меню, загрузка уровня, такт петли,
    /// смерть, завершение. Здесь же собирается весь мир, потому что уровень —
    /// это данные, а не сцена.
    ///
    /// Порядок одного тика важен и зафиксирован:
    ///   1. эхо проигрывают прошлое,
    ///   2. игрок делает шаг,
    ///   3. мир реагирует на новое расположение тел,
    ///   4. поза игрока записывается в ленту.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Сразу открыть уровень, минуя меню (удобно для отладки и тестов).")]
        [SerializeField] bool _skipMenu;

        [SerializeField] int _startLevel;

        LoopClock _clock;
        EchoDirector _echoes;
        InputReader _input;
        CameraRig _cameraRig;
        AudioDirector _audio;
        HudView _hud;
        MenuView _menu;
        ProgressStore _progress;

        LevelRuntime _level;
        PlayerController _player;

        Transform _worldRoot;
        Transform _actorRoot;

        readonly List<LoopActor> _actors = new List<LoopActor>(8);
        readonly List<string> _menuLines = new List<string>();

        AppState _state = AppState.Boot;
        Quaternion _spawnRotation = Quaternion.identity;
        int _levelIndex;
        int _menuSelection;
        float _restartCountdown = -1f;
        float _inputLock = -1f;

        // ==============================================================
        // Жизненный цикл
        // ==============================================================

        void Awake()
        {
            Time.timeScale = 1f;

            _clock = gameObject.AddComponent<LoopClock>();
            _echoes = gameObject.AddComponent<EchoDirector>();
            _input = gameObject.AddComponent<InputReader>();
            _progress = gameObject.AddComponent<ProgressStore>();

            _audio = gameObject.AddComponent<AudioDirector>();
            _audio.Initialize();

            _worldRoot = new GameObject("World").transform;
            _worldRoot.SetParent(transform, false);

            _actorRoot = new GameObject("Actors").transform;
            _actorRoot.SetParent(transform, false);

            SetupEnvironment();

            _cameraRig = ResolveCamera();

            _hud = gameObject.AddComponent<HudView>();
            _hud.Initialize(transform);

            _menu = gameObject.AddComponent<MenuView>();
            _menu.Initialize(transform);

            _progress.Initialize(LevelDatabase.Count);

            // Отладочная точка входа: запуск сразу на нужном уровне.
            if (TryReadLevelArgument(out int requestedLevel)) StartLevel(requestedLevel);
            else if (_skipMenu) StartLevel(_startLevel);
            else EnterTitle();
        }

        void SetupEnvironment()
        {
            // Тёмный ambient + сильный ключевой свет: пазл держится на читаемости
            // перепадов высот, а не на декорациях.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.12f, 0.17f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Palette.Backdrop;
            RenderSettings.fogDensity = 0.019f;

            var key = ConfigureLight("KeyLight", new Color(0.88f, 0.93f, 1f), 1.75f, true, Quaternion.Euler(52f, -32f, 0f));
            key.transform.SetParent(transform, false);

            // Заполняющий свет без теней, чтобы боковые грани не проваливались в ноль.
            var fill = ConfigureLight("FillLight", new Color(0.35f, 0.45f, 0.7f), 0.35f, false, Quaternion.Euler(18f, 145f, 0f));
            fill.transform.SetParent(transform, false);
        }

        static Light ConfigureLight(string name, Color color, float intensity, bool shadows, Quaternion rotation)
        {
            var existing = GameObject.Find(name);
            Light light = existing != null ? existing.GetComponent<Light>() : null;

            if (light == null)
            {
                var go = new GameObject(name);
                light = go.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.name = name;
            light.color = color;
            light.intensity = intensity;
            light.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = 0.7f;
            light.transform.rotation = rotation;
            return light;
        }

        CameraRig ResolveCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Backdrop;
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 220f;

            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();

            return cam.GetComponent<CameraRig>() != null ? cam.GetComponent<CameraRig>() : cam.gameObject.AddComponent<CameraRig>();
        }

        // ==============================================================
        // Уровень
        // ==============================================================

        void LoadLevel(int index)
        {
            _levelIndex = Mathf.Clamp(index, 0, LevelDatabase.Count - 1);
            var def = LevelDatabase.Get(_levelIndex);

            UnloadLevel();

            var levelGo = new GameObject("Level_" + (_levelIndex + 1));
            levelGo.transform.SetParent(_worldRoot, false);
            _level = levelGo.AddComponent<LevelRuntime>();
            _level.Build(def, levelGo.transform);
            _level.ShardCollected += OnShardCollected;
            _level.PlayerDied += OnPlayerDied;
            _level.Completed += OnLevelCompleted;

            _spawnRotation = Quaternion.identity;
            _player = PlayerController.Create(_actorRoot, def.PlayerStart);
            _player.Died += OnPlayerDied;
            _player.Jumped += OnPlayerJumped;

            _clock.Configure(def.LoopSeconds);
            _clock.Ticked += OnTick;
            _clock.LoopExpired += OnLoopExpired;
            _clock.LoopRestarting += OnLoopRestarting;

            _echoes.Configure(def.MaxEchoes, def.PlayerStart, _spawnRotation);
            _echoes.SetActorRoot(_actorRoot);
            _echoes.BeginRecording(_clock.TickCount);
            _echoes.RebuildClones();

            _cameraRig.SetTarget(_player.transform);
            _cameraRig.SetBias(def.CameraFocus);
            _cameraRig.Snap();

            _hud.SetLevel(def.Name, def.Subtitle);
            _hud.SetShards(0, def.ShardCount);
            _hud.SetEchoes(0, def.MaxEchoes);
            _hud.SetCycle(0);
            _hud.SetTimer(0f);
            _hud.SetHint(def.Hint);
            _hud.HideOverlay();

            _restartCountdown = -1f;
        }

        void UnloadLevel()
        {
            if (_clock != null)
            {
                _clock.Ticked -= OnTick;
                _clock.LoopExpired -= OnLoopExpired;
                _clock.LoopRestarting -= OnLoopRestarting;
                _clock.Halt();
            }

            if (_level != null)
            {
                _level.ShardCollected -= OnShardCollected;
                _level.PlayerDied -= OnPlayerDied;
                _level.Completed -= OnLevelCompleted;
                _level.gameObject.SetActive(false);
                Destroy(_level.gameObject);
                _level = null;
            }

            if (_player != null)
            {
                _player.Died -= OnPlayerDied;
                _player.Jumped -= OnPlayerJumped;
                _player.gameObject.SetActive(false);
                Destroy(_player.gameObject);
                _player = null;
            }

            _actors.Clear();
            if (_echoes != null) _echoes.Wipe();
        }

        void StartLevel(int index)
        {
            LoadLevel(index);
            _hud.SetVisible(true);
            _menu.Hide();
            _state = AppState.Playing;
            _clock.StartFirstLoop();
        }

        // ==============================================================
        // Такт петли
        // ==============================================================

        void OnTick(int tick)
        {
            if (_state != AppState.Playing || _player == null || _level == null) return;

            float dt = Time.fixedDeltaTime;

            _echoes.PlayTick(tick);
            _player.SimulateStep(dt, _input.Move, _input.JumpPressed, _input.JumpHeld);

            _actors.Clear();
            _actors.Add(_player);
            _echoes.CollectActors(_actors);

            _level.Tick(_actors, _player, dt);

            if (_player.IsAlive) _echoes.Capture(tick, _player.transform.position, _player.transform.rotation);

            _hud.SetTimer(_clock.Normalized);
            _hud.SetEchoes(_echoes.LiveEchoes, _echoes.MaxEchoes);
        }

        void OnLoopExpired()
        {
            if (_state != AppState.Playing) return;
            RestartLoop();
        }

        /// <summary>Архивирует записанную петлю и начинает следующую.</summary>
        void RestartLoop()
        {
            if (_player == null) return;

            _echoes.CommitRecording(_player.transform.position, _player.transform.rotation);
            _echoes.BeginRecording(_clock.TickCount);
            _clock.AdvanceLoop();
        }

        void OnLoopRestarting(int loopIndex)
        {
            if (_level == null || _player == null) return;

            _level.ResetTransient();
            _echoes.RebuildClones();
            _player.Revive(_level.Definition.PlayerStart, _spawnRotation);

            _hud.SetCycle(loopIndex);
            _hud.SetEchoes(_echoes.LiveEchoes, _echoes.MaxEchoes);
            _hud.SetTimer(0f);

            _audio.LoopReset();
            _hud.Flash(Palette.Accent, 0.28f);
        }

        // ==============================================================
        // События уровня
        // ==============================================================

        void OnPlayerJumped() => _audio.Jump();

        void OnPlayerDied()
        {
            if (_state != AppState.Playing) return;

            _clock.Halt();
            _audio.Death();
            _cameraRig.Shake(0.55f);
            _hud.Flash(Palette.Danger, 0.72f);
            _restartCountdown = 0.42f;
        }

        void OnShardCollected(Shard shard)
        {
            _audio.Shard();
            _cameraRig.Shake(0.16f);
            _hud.SetShards(_level.ShardsCollected, _level.ShardsTotal);
            _hud.Flash(Palette.AccentWarm, 0.22f);
        }

        void OnLevelCompleted()
        {
            if (_state != AppState.Playing) return;

            _state = AppState.LevelComplete;
            _clock.Halt();
            _audio.Complete();

            int loops = _clock.LoopIndex + 1;
            bool isRecord = _progress.SubmitResult(_levelIndex, loops);
            _progress.Unlock(_levelIndex);

            bool last = _levelIndex + 1 >= LevelDatabase.Count;

            string subtitle = "Петель: " + loops + "   ·   ориентир: " + _level.Definition.ParLoops;
            if (isRecord) subtitle += "\nНОВЫЙ РЕКОРД";
            subtitle += last ? "\nSPACE — финальный экран" : "\nSPACE — следующий уровень";

            _hud.ShowOverlay("УРОВЕНЬ ПРОЙДЕН", subtitle);
            _inputLock = 0.7f;
        }

        // ==============================================================
        // Экраны
        // ==============================================================

        void EnterTitle()
        {
            LoadLevel(0);
            _clock.Halt();

            _hud.SetVisible(false);
            _menuSelection = 0;
            _state = AppState.Title;

            ShowTitleMenu();
        }

        void ShowTitleMenu()
        {
            _menuLines.Clear();
            _menuLines.Add("НАЧАТЬ ИГРУ");
            _menuLines.Add("ВЫБОР УРОВНЯ");

            _menu.Show(
                "PLAYBLE",
                "ECHOLOOP — головоломка о времени",
                _menuLines,
                _menuSelection,
                "WASD — движение    SPACE — прыжок    ESC — пауза    R — перезапуск уровня");
        }

        void EnterLevelSelect()
        {
            _menuSelection = Mathf.Clamp(_progress.BestLoops(_levelIndex) > 0 ? _levelIndex : 0, 0, LevelDatabase.Count - 1);
            _state = AppState.LevelSelect;
            ShowLevelSelect();
        }

        void ShowLevelSelect()
        {
            _menuLines.Clear();

            for (int i = 0; i < LevelDatabase.Count; i++)
            {
                var def = LevelDatabase.Get(i);
                bool unlocked = _progress.IsUnlocked(i);
                int best = _progress.BestLoops(i);

                if (!unlocked) _menuLines.Add(string.Format("{0:00}. {1}  — закрыт", i + 1, def.Name));
                else if (best > 0) _menuLines.Add(string.Format("{0:00}. {1}  — {2} петель", i + 1, def.Name, best));
                else _menuLines.Add(string.Format("{0:00}. {1}", i + 1, def.Name));
            }

            _menu.Show(
                "УРОВНИ",
                "выбери забег",
                _menuLines,
                _menuSelection,
                "W / S — выбор      SPACE — начать      ESC — назад");
        }

        void EnterPause()
        {
            _state = AppState.Paused;
            _clock.Halt();
            _hud.SetVisible(false);
            _menuSelection = 0;

            _menuLines.Clear();
            _menuLines.Add("ПРОДОЛЖИТЬ");
            _menuLines.Add("НАЧАТЬ УРОВЕНЬ ЗАНОВО");
            _menuLines.Add("В МЕНЮ");

            _menu.Show(
                "ПАУЗА",
                _level.Definition.Name,
                _menuLines,
                _menuSelection,
                "ESC — продолжить");
        }

        void LeavePause()
        {
            _menu.Hide();
            _hud.SetVisible(true);
            _state = AppState.Playing;
            _clock.Resume();
        }

        void EnterGameComplete()
        {
            _state = AppState.GameComplete;
            _hud.HideOverlay();
            _hud.SetVisible(false);
            _menuSelection = 0;

            int total = 0;
            for (int i = 0; i < LevelDatabase.Count; i++) total += _progress.BestLoops(i);

            _menuLines.Clear();
            _menuLines.Add("В МЕНЮ");

            _menu.Show(
                "ЦИКЛ ЗАМКНУТ",
                "все уровни пройдены · сумма петель: " + total,
                _menuLines,
                _menuSelection,
                "SPACE — продолжить");
        }

        // ==============================================================
        // Обновление
        // ==============================================================

        void Update()
        {
            if (_inputLock > 0f) _inputLock -= Time.deltaTime;

            if (_restartCountdown > 0f)
            {
                _restartCountdown -= Time.deltaTime;
                if (_restartCountdown <= 0f && _state == AppState.Playing) RestartLoop();
            }

            switch (_state)
            {
                case AppState.Title: TickTitle(); break;
                case AppState.LevelSelect: TickLevelSelect(); break;
                case AppState.Playing: TickPlaying(); break;
                case AppState.Paused: TickPaused(); break;
                case AppState.LevelComplete: TickLevelComplete(); break;
                case AppState.GameComplete: TickGameComplete(); break;
            }
        }

        void TickTitle()
        {
            Navigate(2);

            if (!InputReader.ConfirmPressed()) return;
            _audio.Ui();

            if (_menuSelection == 0) StartLevel(FirstUnplayedLevel());
            else EnterLevelSelect();
        }

        void TickLevelSelect()
        {
            int max = Mathf.Clamp(_progress.Unlocked, 1, LevelDatabase.Count);
            Navigate(max);

            if (InputReader.CancelPressed())
            {
                _audio.Ui();
                _state = AppState.Title;
                ShowTitleMenu();
                return;
            }

            if (!InputReader.ConfirmPressed()) return;
            _audio.Ui();
            StartLevel(_menuSelection);
        }

        void TickPlaying()
        {
            if (_input.PausePressed)
            {
                _audio.Ui();
                EnterPause();
                return;
            }

            if (_input.RestartPressed)
            {
                _audio.Ui();
                StartLevel(_levelIndex);
            }
        }

        void TickPaused()
        {
            if (InputReader.CancelPressed())
            {
                _audio.Ui();
                LeavePause();
                return;
            }

            Navigate(3);

            if (!InputReader.ConfirmPressed()) return;
            _audio.Ui();

            switch (_menuSelection)
            {
                case 0: LeavePause(); break;
                case 1: StartLevel(_levelIndex); break;
                default:
                    _state = AppState.Title;
                    _menuSelection = 0;
                    ShowTitleMenu();
                    break;
            }
        }

        void TickLevelComplete()
        {
            if (_inputLock > 0f) return;
            if (!InputReader.ConfirmPressed()) return;

            _audio.Ui();
            if (_levelIndex + 1 < LevelDatabase.Count) StartLevel(_levelIndex + 1);
            else EnterGameComplete();
        }

        void TickGameComplete()
        {
            if (_inputLock > 0f) return;
            if (!InputReader.ConfirmPressed()) return;

            _audio.Ui();
            _state = AppState.Title;
            _menuSelection = 0;
            ShowTitleMenu();
        }

        void Navigate(int count)
        {
            if (count <= 0) return;

            bool moved = false;

            if (InputReader.UpPressed())
            {
                _menuSelection = (_menuSelection - 1 + count) % count;
                moved = true;
            }

            if (InputReader.DownPressed())
            {
                _menuSelection = (_menuSelection + 1) % count;
                moved = true;
            }

            if (!moved) return;

            _audio.Ui();
            switch (_state)
            {
                case AppState.Title: ShowTitleMenu(); break;
                case AppState.LevelSelect: ShowLevelSelect(); break;
                case AppState.Paused: EnterPause(); break;
            }
        }

        int FirstUnplayedLevel()
        {
            for (int i = 0; i < LevelDatabase.Count; i++)
            {
                if (_progress.BestLoops(i) == 0) return i;
            }
            return Mathf.Clamp(_levelIndex, 0, LevelDatabase.Count - 1);
        }

        /// <summary>
        /// Отладочная точка входа: запуск сразу на нужном уровне
        /// (например, PlaybleEchoLoop.exe -level 5). Не влияет на обычный запуск.
        /// </summary>
        static bool TryReadLevelArgument(out int levelIndex)
        {
            levelIndex = 0;

            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], "-level", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!int.TryParse(args[i + 1], out int value)) return false;

                levelIndex = Mathf.Clamp(value - 1, 0, LevelDatabase.Count - 1);
                return true;
            }

            return false;
        }
    }
}