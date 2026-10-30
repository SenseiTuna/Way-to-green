using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KiyiDongusu.Editor
{
    public static class FeedbackChecks
    {
        [Serializable] sealed class Check { public string name, status, detail; }
        [Serializable] sealed class Report
        {
            public string environment = "Real Unity Editor Play Mode; PCM signal, AudioSource playback/mute and UI notices; not a human listening test";
            public string utc, status;
            public List<Check> checks = new List<Check>();
        }
        static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
        static void Test(Report report, string name, Action action)
        {
            try { action(); report.checks.Add(new Check { name = name, status = "PASS", detail = "Assertions completed in Play Mode" }); }
            catch (Exception e) { report.checks.Add(new Check { name = name, status = "FAIL", detail = e.Message }); }
        }
        [MenuItem("Kiyi Dongusu/Run Feedback Checks (Play Mode)")]
        public static void Begin()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Feedback checks require Play Mode.");
            var host = UnityEngine.Object.FindAnyObjectByType<GameBootstrap>();
            if (host == null || host.Feedback == null) throw new InvalidOperationException("GameRoot is not ready.");
            host.StartCoroutine(Run(host));
        }
        static IEnumerator Run(GameBootstrap host)
        {
            var report = new Report { utc = DateTime.UtcNow.ToString("o") };
            bool oldPause = host.Paused, oldMute = host.Game.State.muted, oldMotion = host.Game.State.reduceMotion;
            host.Paused = true;
            host.Feedback.BindSession(host.Game);
            string snapshot = JsonUtility.ToJson(host.Game.State);
            int collects = 0; string lastNotice = "";
            Action<FeedbackNotice> listener = notice => { if (notice.Kind == FeedbackKind.Collect) { collects++; lastNotice = notice.Text; } };
            host.Feedback.Notice += listener;
            try
            {
                Test(report, "one listener / two bounded UI audio sources", () => {
                    Assert(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length == 1, "Expected one AudioListener");
                    Assert(host.Feedback.GetComponents<AudioSource>().Length == 2, "Audio sources grew");
                });
                Test(report, "six non-silent clipped-free PCM cues", () => {
                    Assert(host.Feedback.Clips.Length == 6, "Missing cues");
                    foreach (var clip in host.Feedback.Clips)
                    {
                        var samples = new float[clip.samples * clip.channels]; Assert(clip.GetData(samples, 0), "Cannot read clip PCM");
                        double energy = 0; float peak = 0;
                        foreach (float value in samples) { energy += value * value; peak = Mathf.Max(peak, Mathf.Abs(value)); }
                        Assert(Math.Sqrt(energy / samples.Length) > .03 && peak <= 1, "Silent/clipped cue: " + clip.name);
                    }
                });
                host.SetMuted(false);
                host.Feedback.Collection(0, 1);
                host.Feedback.Collection(0, 1);
                Test(report, "same-frame collection notices coalesce", () => Assert(collects == 2 && lastNotice.StartsWith("+2 ", StringComparison.Ordinal), "Collection count=" + collects + ", notice=" + lastNotice));
                yield return new WaitForSecondsRealtime(.01f);
                Test(report, "unmuted AudioSource playback", () => Assert(host.Feedback.ActionSource.isPlaying && !host.Feedback.ActionSource.mute, "Source not playing"));
                host.SetMuted(true);
                Test(report, "mute stops both sources immediately", () => Assert(host.Feedback.ActionSource.mute && host.Feedback.ProductionSource.mute && !host.Feedback.ActionSource.isPlaying && !host.Feedback.ProductionSource.isPlaying, "Mute failed"));
                host.SetReducedMotion(true);
                yield return new WaitForSecondsRealtime(.03f);
                Test(report, "reduced motion keeps pile scale stable", () => {
                    var artwork = GameObject.Find("PileArtwork");
                    if (artwork != null) Assert(Vector3.Distance(artwork.transform.localScale, Vector3.one) < .0001f, "Pile still animates in reduced-motion mode");
                });
                Test(report, "new settings round-trip / old save defaults", () => {
                    string directory = Path.GetFullPath("../Evidence/feedback-save-" + Guid.NewGuid().ToString("N"));
                    var store = new SaveService(directory, host.Game.Definitions, host.Game.State.corePrototype); store.Load();
                    Assert(store.Save(host.Game), "Settings save failed"); var restored = store.Load();
                    Assert(restored.muted && restored.reduceMotion, "Settings did not restore");
                    var legacy = JsonUtility.FromJson<GameState>("{\"schemaVersion\":1,\"muted\":false}"); Assert(!legacy.reduceMotion, "Old save did not default reduced motion to false");
                });
            }
            finally
            {
                host.Feedback.Notice -= listener; host.SetMuted(oldMute); host.SetReducedMotion(oldMotion); host.Paused = oldPause;
            }
            Test(report, "presentation cues do not change economy", () => Assert(snapshot == JsonUtility.ToJson(host.Game.State), "Feedback mutated simulation state"));
            report.status = report.checks.TrueForAll(check => check.status == "PASS") ? "PASS" : "FAIL";
            string path = Path.GetFullPath("../Evidence/feedback-checks.json"); Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log("KIYI_FEEDBACK_CHECKS " + report.status + " report=" + path);
        }
    }
}
