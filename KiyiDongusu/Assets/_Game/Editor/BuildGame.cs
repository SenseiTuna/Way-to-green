using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KiyiDongusu.Editor
{
    public static class BuildGame
    {
        [MenuItem("Kiyi Dongusu/Build Core Windows")]
        public static void Core() => Build(true);
        [MenuItem("Kiyi Dongusu/Build Full Windows")]
        public static void Full() => Build(false);
        [MenuItem("Kiyi Dongusu/Build Art Preview Windows")]
        public static void ArtPreview() => Build(false, true);
        static void Build(bool core, bool artPreview = false)
        {
            string source = Path.GetFullPath("../ECONOMY_REFERENCE.json");
            const string resource = "Assets/_Game/Resources/Economy.json";
            if (File.ReadAllText(source) != File.ReadAllText(resource))
            {
                File.Copy(source, resource, true);
                AssetDatabase.ImportAsset(resource, ImportAssetOptions.ForceUpdate);
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera)); camera.tag = "MainCamera";
            camera.GetComponent<Camera>().orthographic = true; camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0, 0, -10);
            var bootstrap = new GameObject("GameRoot").AddComponent<GameBootstrap>(); bootstrap.corePrototype = core;
            Directory.CreateDirectory("Assets/_Game/Scenes");
            string scenePath = "Assets/_Game/Scenes/" + (core ? "Core" : "Game") + ".unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            PlayerSettings.companyName = "KiyiDongusuLocal"; PlayerSettings.productName = "KiyiDongusu";
            PlayerSettings.bundleVersion = core ? "1.0.1.0" : artPreview ? "1.0.3.2" : "1.0.2.0";
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            string output = Path.GetFullPath("../Builds/" + (core ? "V1.0.1.0-Core" : artPreview ? "V1.0.3.2-ArtPreview" : "V1.0.2.0-Full") + "/KiyiDongusu.exe");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { scenePath }, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            Debug.Log("KIYI_BUILD " + report.summary.result + " bytes=" + report.summary.totalSize + " path=" + output);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build başarısız.");
        }
    }
}
