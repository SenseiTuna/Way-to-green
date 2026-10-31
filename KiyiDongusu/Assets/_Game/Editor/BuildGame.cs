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
        public const string ArtVersion = "1.0.6.2";
        [MenuItem("Kiyi Dongusu/Build Core Windows")]
        public static void Core() => Build(true);
        [MenuItem("Kiyi Dongusu/Build Full Windows")]
        public static void Full() => Build(false);
        [MenuItem("Kiyi Dongusu/Build Art Preview Windows")]
        public static void ArtPreview() => Build(false, true);
        [MenuItem("Kiyi Dongusu/Build Release Candidate Windows")]
        public static void ReleaseCandidate() => Build(false, true, false);
        static void Build(bool core, bool artPreview = false, bool development = true)
        {
            CoastTypography.Prepare();
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
            string version = core ? "1.0.1.0" : artPreview ? ArtVersion : "1.0.2.0";
            PlayerSettings.bundleVersion = version;
            PlayerSettings.defaultScreenWidth = 1280; PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(scenePath, true) };
            string output = Path.GetFullPath("../Builds/V" + version + (!development ? "-ReleaseCandidate" : core ? "-Core" : artPreview ? "-ArtPreview" : "-Full") + "/KiyiDongusu.exe");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { scenePath }, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = development ? BuildOptions.Development : BuildOptions.None });
            Debug.Log("KIYI_BUILD " + report.summary.result + " development=" + development + " bytes=" + report.summary.totalSize + " path=" + output);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Windows build başarısız.");
            string directory = Path.GetDirectoryName(output);
            foreach (var pair in new[] {
                new[] { "../CREDITS.txt", "CREDITS.txt" },
                new[] { "../Tools/PROTOTYPE_NOTES.txt", "README.txt" },
                new[] { "Assets/TextMesh Pro/LICENSE.md", "TMP-LICENSE.md" },
                new[] { "Assets/_Game/Resources/Typography/OFL.txt", "Lato-OFL.txt" }
            }) if (File.Exists(pair[0])) File.Copy(pair[0], Path.Combine(directory, pair[1]), true);
            string notes = Path.Combine(directory, "README.txt");
            if (!development && File.Exists(notes)) File.WriteAllText(notes, File.ReadAllText(notes).Replace(
                "Development Build ibaresi beklenir. Normal açılışta test otomasyonu etkin değildir.",
                "Bu adayda Development Build ibaresi yoktur; hızlandırılmış test kodu derlemeye dahil değildir."));
        }
    }
}
