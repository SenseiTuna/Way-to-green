#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace KiyiDongusu
{
    // Explicit development-only validation flag. Never runs in ordinary gameplay,
    // and compiled out of a non-development release.
    public sealed class RuntimeSmokeRunner : MonoBehaviour
    {
        [Serializable] sealed class Report
        {
            public string environment, version, utc, status, error, policy;
            public int processed, total, resultCount, acceptedActions;
            public long earned, spent, money;
            public double simulatedSeconds, wallSeconds;
            public bool snapshotRoundTrip, allUnlocks, parallelObserved;
        }
        GameBootstrap host;
        Report report;
        string directory;
        double nextAction, accumulator;
        float start;
        bool finished;
        public static bool Requested => Array.IndexOf(Environment.GetCommandLineArgs(), "--kiyi-smoke-test") >= 0;
        public static string OutputDirectory
        {
            get
            {
                string[] args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--kiyi-test-output");
                return index >= 0 && index + 1 < args.Length ? Path.GetFullPath(args[index + 1]) : Path.Combine(Application.persistentDataPath, "Validation", Guid.NewGuid().ToString("N"));
            }
        }
        public void Initialize(GameBootstrap bootstrap)
        {
            host = bootstrap; directory = OutputDirectory; Directory.CreateDirectory(directory); start = Time.realtimeSinceStartup;
            report = new Report { environment = "Actual Windows Unity Player, DEVELOPMENT_BUILD, 20x simulation; not normal tempo or manual UI playtest", version = Application.version, utc = DateTime.UtcNow.ToString("o"), policy = "3 domain production actions/s; every tenth loose if available; collector first, then collector ranks through 3; facility speed/capacity through 2; invariants each 50ms; isolated save" };
        }
        void Update()
        {
            if (finished || host == null) return;
            try
            {
                var game = host.Game;
                accumulator += Math.Min(.2, Time.unscaledDeltaTime) * 20;
                while (accumulator >= .05 && !game.State.complete)
                {
                    accumulator -= .05;
                    if (game.State.elapsed >= nextAction)
                    {
                        report.acceptedActions++;
                        bool loose = false;
                        if (report.acceptedActions % 10 == 0)
                            for (int i = 0; i < game.State.materials.Length; i++)
                                if (game.State.materials[i].visible > 0) { loose = game.TokenAction(i, game.State.materials[i].tokenGeneration); break; }
                        if (!loose) game.CenterAction();
                        nextAction += 1.0 / 3;
                    }
                    game.Tick(.05);
                    if (game.State.collectorRank < 3) game.Purchase(PurchaseKind.Collector);
                    if (game.State.collectorRank >= 1)
                        for (int f = 0; f < 4; f++)
                        {
                            if (game.State.facilities[f].speed < 2) game.Purchase(PurchaseKind.Speed, f);
                            if (game.State.facilities[f].capacity < 2) game.Purchase(PurchaseKind.Capacity, f);
                        }
                    game.Validate();
                    report.parallelObserved |= game.State.materials[0].batch > 0 && game.State.materials[1].batch > 0;
                    if (!report.snapshotRoundTrip && game.State.elapsed > 300)
                    {
                        if (!host.Saves.Save(game)) throw new Exception("Runtime snapshot write failed");
                        string before = JsonUtility.ToJson(game.State);
                        var loaded = host.Saves.Load();
                        report.snapshotRoundTrip = loaded != null && before == JsonUtility.ToJson(loaded);
                        if (!report.snapshotRoundTrip) throw new Exception("Runtime snapshot not identical");
                    }
                    if (game.State.elapsed > 12000) throw new Exception("Validation run did not finish in budget");
                }
                if (game.State.complete) Finish(null);
            }
            catch (Exception error) { Finish(error); }
        }
        void Finish(Exception error)
        {
            finished = true;
            var game = host.Game;
            report.total = game.TotalUnits; report.processed = game.Processed; report.earned = game.State.earned; report.spent = game.State.spent; report.money = game.State.money; report.resultCount = game.State.resultCount;
            report.simulatedSeconds = game.State.elapsed; report.wallSeconds = Time.realtimeSinceStartup - start;
            report.allUnlocks = true; foreach (var m in game.State.materials) report.allUnlocks &= m.unlocked;
            report.error = error == null ? "" : error.ToString();
            report.status = error == null && report.processed == 6000 && report.earned == 1056000 && report.resultCount == 1 && report.snapshotRoundTrip && report.allUnlocks && report.parallelObserved ? "PASS" : "FAIL";
            File.WriteAllText(Path.Combine(directory, "runtime-report.json"), JsonUtility.ToJson(report, true));
            Debug.Log("KIYI_WINDOWS_SMOKE " + report.status + " output=" + directory);
            host.Saves.Save(game);
            StartCoroutine(CaptureAndClose());
        }
        IEnumerator CaptureAndClose()
        {
            // Allow the view and canvas to render the completion state before capture.
            yield return new WaitForSecondsRealtime(.5f);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "result.png"));
            yield return new WaitForSecondsRealtime(4);
            Application.Quit(report.status == "PASS" ? 0 : 1);
        }
    }
}
#endif
