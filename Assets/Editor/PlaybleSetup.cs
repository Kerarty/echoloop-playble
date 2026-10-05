using System.IO;
using Playble.EchoLoop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Playble.EditorTools
{
    /// <summary>
    /// Генератор сцены. Сцена игры намеренно пустая: камера, свет и точка входа,
    /// а весь уровень собирается кодом из данных при запуске. Такой подход даёт
    /// и ноль конфликтов при слиянии, и возможность менять уровни без редактора.
    /// </summary>
    public static class PlaybleSetup
    {
        public const string SceneFolder = "Assets/Scenes";
        public const string ScenePath = SceneFolder + "/Main.unity";
        const string SettingsFolder = "Assets/Settings";
        const string VolumeProfilePath = SettingsFolder + "/EcholoopVolume.asset";

        [MenuItem("Playble/Собрать сцену игры")]
        public static void All()
        {
            EnsureFolders();
            ApplyProjectSettings();
            CreateVolumeProfile();
            CreateScene();
            RegisterSceneInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[PlaybleSetup] Сцена игры собрана: " + ScenePath);
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(SceneFolder))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            if (!AssetDatabase.IsValidFolder(SettingsFolder))
                AssetDatabase.CreateFolder("Assets", "Settings");
        }

        static void ApplyProjectSettings()
        {
            PlayerSettings.companyName = "Yarik";
            PlayerSettings.productName = "Playble ECHOLOOP";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;

            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.allowFullscreenSwitch = true;
        }

        static void CreateVolumeProfile()
        {
            var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (existing != null) return;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.85f;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.92f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.68f;

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.34f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.4f;

            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        }

        static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- камера ---
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraRig>();
            camGo.transform.position = new Vector3(0f, 18f, -16f);

            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            // --- глобальный постпроцессинг ---
            var volumeGo = new GameObject("Global Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);

            // --- свет ---
            var keyGo = new GameObject("KeyLight");
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(0.88f, 0.93f, 1f);
            key.intensity = 1.75f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.7f;
            keyGo.transform.rotation = Quaternion.Euler(52f, -32f, 0f);

            var fillGo = new GameObject("FillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.35f, 0.45f, 0.7f);
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(18f, 145f, 0f);

            // --- точка входа ---
            var gameGo = new GameObject("Game");
            gameGo.AddComponent<GameBootstrap>();

            // --- настройки сцены ---
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.10f, 0.12f, 0.17f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Palette.Backdrop;
            RenderSettings.fogDensity = 0.019f;

            Directory.CreateDirectory(SceneFolder);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        static void RegisterSceneInBuildSettings()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }
    }
}