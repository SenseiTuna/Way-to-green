#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace KiyiDongusu
{
    // Explicit development-only UI test. Never reads or replaces the normal player's files.
    public sealed partial class RuntimeRecoveryChecks : MonoBehaviour
    {
        [Serializable] sealed class Check { public string name, status, detail; }
        [Serializable] sealed class Report
        {
            public string environment = "Actual Unity Player UI callbacks and filesystem; isolated fixture; not physical mouse input or human tempo";
            public string version, utc, status;
            public List<Check> checks = new List<Check>();
        }
        public static bool Requested => Array.IndexOf(Environment.GetCommandLineArgs(), "--kiyi-recovery-test") >= 0;
        static string output;
        GameBootstrap host;
        readonly Report report = new Report();
        static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
        void Test(string name, Action action)
        {
            try { action(); report.checks.Add(new Check { name = name, status = "PASS", detail = "Assertions completed in real Player" }); }
            catch (Exception e) { report.checks.Add(new Check { name = name, status = "FAIL", detail = e.ToString() }); }
        }
        public static string Prepare(GameDefinitions definitions)
        {
            output = Path.Combine(RuntimeSmokeRunner.OutputDirectory, "recovery-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(output);
            var saves = new SaveService(output, definitions, false); saves.Load();
            var game = new GameSession(definitions, false); game.State.muted = true; game.State.reduceMotion = true;
            Assert(saves.Save(game), "Cannot create fixture backup");
            game.CenterAction(); Assert(saves.Save(game), "Cannot rotate fixture");
            File.WriteAllText(saves.Path, "deliberately corrupt isolated primary");
            return output;
        }
        public void Initialize(GameBootstrap bootstrap) { host = bootstrap; StartCoroutine(Run()); }
        static Button Button(string name) { var go = GameObject.Find(name); Assert(go != null, "Missing UI: " + name); return go.GetComponent<Button>(); }
        IEnumerator Run()
        {
            report.version = Application.version; report.utc = DateTime.UtcNow.ToString("o");
            yield return new WaitForSecondsRealtime(.3f);
            Test("corrupt primary shows recovery and blocks writes", () => {
                Assert(host.Saves.NeedsRecovery && !host.Saves.CanWrite && host.Paused, "Recovery gate missing");
                Assert(Button("RestoreBackup").interactable, "Restore not available");
            });
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "recovery.png"));
            Test("open New Game confirmation", () => Button("NewGame").onClick.Invoke());
            yield return null;
            Test("cancel preserves corrupt files and recovery gate", () => {
                Button("CancelNew").onClick.Invoke();
                Assert(host.Saves.NeedsRecovery && !host.Saves.CanWrite, "Cancel bypassed recovery");
                Assert(File.ReadAllText(host.Saves.Path).StartsWith("deliberately"), "Primary overwritten without approval");
            });
            yield return null;
            Test("restore UI explicitly recovers valid backup", () => {
                Button("RestoreBackup").onClick.Invoke(); host.Paused = true;
                Assert(!host.Saves.NeedsRecovery && host.Saves.CanWrite, "Restore failed");
                Assert(host.Game.State.muted && host.Game.State.reduceMotion, "Backup settings lost");
                Assert(Directory.GetFiles(output, "*.preserved-*").Length == 1, "Corrupt primary not preserved");
            });
            yield return null;
            string before = File.ReadAllText(host.Saves.Path);
            string obstruction = host.Saves.Path + ".tmp";
            Test("failed exit is blocked and primary remains valid", () => {
                Directory.CreateDirectory(obstruction);
                host.SetMuted(false);
                Assert(!host.TryPrepareQuit(), "Exit accepted unsaved state");
                Assert(host.Paused && Button("RetrySave").interactable, "Save failure UI missing");
                Assert(File.ReadAllText(host.Saves.Path) == before, "Failed write changed primary");
            });
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "save-failure.png"));
            Test("retry button remains safe while disk write blocked", () => {
                Button("RetrySave").onClick.Invoke();
                Assert(File.ReadAllText(host.Saves.Path) == before && host.Paused, "Retry lost state");
            });
            // Only remove our own empty obstruction directory, never a player's save.
            if (Directory.Exists(obstruction)) Directory.Delete(obstruction);
            yield return new WaitForSecondsRealtime(2.5f);
            Test("failed save retries automatically even while paused", () => {
                Assert(!host.Saves.Load().muted, "Pending preference was not retried");
                Assert(File.ReadAllText(host.Saves.Path + ".bak") == before, "Valid backup not rotated");
            });
            Test("exit preparation succeeds once storage recovers", () => Assert(host.TryPrepareQuit(), "Still blocking saved exit"));
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--kiyi-feature-check") >= 0) yield return Features();
            report.status = report.checks.TrueForAll(c => c.status == "PASS") ? "PASS" : "FAIL";
            File.WriteAllText(Path.Combine(output, "recovery-report.json"), JsonUtility.ToJson(report, true));
            Debug.Log("KIYI_RECOVERY_CHECKS " + report.status + " path=" + output);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--kiyi-manual-check") >= 0)
            {
                if(host.Paused)host.View.TogglePause();
                yield return new WaitForSecondsRealtime(600);
            }
            yield return new WaitForSecondsRealtime(1);
            Application.Quit(report.status == "PASS" ? 0 : 1);
        }
    }
}
#endif
