using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace KiyiDongusu.Editor
{
    public static class DomainChecks
    {
        [Serializable] sealed class Result { public string test, status, detail; }
        [Serializable] sealed class Report { public string environment, utc, version; public List<Result> results = new List<Result>(); }
        static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
        static GameSession New() => CheckFixtures.Ready(new GameSession(GameDefinitions.Load(), false));
        // Relocate existing finite world material into raw stock, never mint test material.
        static void Stock(GameSession g, int i, int amount)
        {
            var m = g.State.materials[i];
            Assert(m.reserve >= amount, "Test reserve insufficient"); m.reserve -= amount; m.raw += amount;
        }
        static void Earn(GameSession g, int amount)
        {
            Stock(g, 0, amount); g.Tick(amount * .8 + .01); g.Validate();
        }
        static void Test(Report report, string id, Action action)
        {
            try { action(); report.results.Add(new Result { test = id, status = "PASS", detail = "Assertions completed in actual Unity Editor C# domain." }); }
            catch (Exception e) { report.results.Add(new Result { test = id, status = "FAIL", detail = e.ToString() }); }
        }
        [MenuItem("Kiyi Dongusu/Run Domain Checks")]
        public static void Run()
        {
            var report = new Report { environment = "Unity " + Application.unityVersion + (Application.isBatchMode ? " batchmode" : " Editor") + "; real C# domain + filesystem; no UI/tempo playtest", utc = DateTime.UtcNow.ToString("o"), version = "V" + BuildGame.ArtVersion + "-source" };
            Test(report, "E02-E03 token identity / last pile unit", () =>
            {
                var g = New(); int generation = g.State.materials[0].tokenGeneration;
                int before = g.Collected; Assert(g.TokenAction(0, generation), "First token rejected");
                for (int i = 0; i < 20; i++) g.TokenAction(0, generation);
                Assert(g.Collected == before + 1, "Duplicate token reward");
                g.Tick(.2); g.Supply.RefreshTokens();
                Assert(!g.TokenAction(0, generation), "Old token generation accepted");
                while (g.State.pileRemaining > 1) g.Supply.CollectPileUnit();
                int n = g.Collected; g.Supply.CollectPileUnit(); Assert(!g.Supply.CollectPileUnit(), "Last unit duplicated");
                Assert(g.Collected == n + 1, "Last unit mismatch"); g.Validate();
            });
            Test(report, "E04-E05 partial batch and exact payment", () =>
            {
                var g = New(); Stock(g, 0, 3); g.Tick(.4);
                Assert(g.State.materials[0].raw == 0 && g.State.materials[0].batch == 3 && g.State.money == 0, "Batch not reserved once");
                g.Tick(2.0); Assert(g.State.materials[0].processed == 3 && g.State.money == 300, "Partial duration/payment wrong");
                g.Tick(2); Assert(g.State.money == 300, "Paid twice"); g.Validate();
            });
            Test(report, "E06-E07 parallel shared facility", () =>
            {
                var g = New(); Earn(g, 50); Stock(g, 0, 10); Stock(g, 1, 10); g.Tick(4);
                Assert(Math.Abs(g.State.materials[0].work - 5) < 1e-7, "Paper progress");
                Assert(Math.Abs(g.State.materials[1].work - 10.0 / 3) < 1e-7, "Cardboard blocked by paper");
                g.Tick(4); Assert(g.State.materials[0].batch == 0 && g.State.materials[1].batch == 10, "Parallel finish mismatch"); g.Validate();
            });
            Test(report, "E08-E09 no empty assist / no spill / shared cap", () =>
            {
                var g = New(); while (g.State.pileRemaining > 0) g.Supply.CollectPileUnit();
                g.State.materials[0].reserve += g.State.materials[0].raw; g.State.materials[0].raw = 0;
                Assert(!g.Processing.Assist() && g.State.money == 0, "Empty support paid");
                Stock(g, 0, 20); g.Processing.Tick(7.99);
                int processed = g.Processed; Assert(g.CenterAction(), "Support rejected");
                Assert(g.Processed == processed + 10 && g.State.materials[0].batch == 0 && g.State.materials[0].raw == 10, "Support leaked into next batch");
                long money = g.State.money; for (int i = 0; i < 100; i++) g.CenterAction();
                Assert(g.State.money == money && g.State.materials[0].batch == 0, "Rapid click support leaked"); g.Validate();
            });
            Test(report, "E10-E11 atomic purchase / locked / maximum", () =>
            {
                var g = New(); Assert(!g.Purchase(PurchaseKind.Collector), "Unfunded buy");
                Earn(g, 100); int buys = 0; for (int i = 0; i < 100; i++) if (g.Purchase(PurchaseKind.Collector)) buys++;
                Assert(buys == 1 && g.State.money == 0 && g.State.collectorRank == 1, "Rapid purchase duplicated or negative");
                Assert(!g.Purchase(PurchaseKind.Speed, 3), "Locked facility buy");
                Assert(!g.Purchase((PurchaseKind)99), "Invalid purchase accepted"); g.Validate();
                g.State.facilities[0].speed = 4;
                Assert(g.Cost(PurchaseKind.Speed) == -1 && !g.Purchase(PurchaseKind.Speed), "Maximum rank purchase accepted");
                Assert(g.Definitions.facilities[1].shared_speed_costs_by_rank_credits[1] == 198 && g.Definitions.facilities[1].shared_capacity_costs_by_rank_credits[2] == 726, "Price rounding");
            });
            Test(report, "E12 upgrade preserves active work and quantity", () =>
            {
                var g = New(); Earn(g, 150); Stock(g, 0, 20); g.Tick(2);
                var m = g.State.materials[0]; int quantity = m.batch; double work = m.work;
                Assert(g.Purchase(PurchaseKind.Speed) && g.Purchase(PurchaseKind.Capacity), "Upgrade not purchased");
                Assert(m.batch == quantity && m.work == work && m.raw == 10, "Upgrade reset or duplicated batch");
                double rate = g.Processing.Rate(0); Assert(Math.Abs(rate - 2.4375) < 1e-9, "New capacity rate");
                g.Tick(1); Assert(Math.Abs(m.work - work - rate) < 1e-7, "Wrong remaining work"); g.Validate();
            });
            Test(report, "E15 large time step preserves normal overflow", () =>
            {
                var a = New(); var b = New(); Stock(a, 0, 50); Stock(b, 0, 50);
                a.Tick(30); for (int i = 0; i < 600; i++) b.Tick(.05);
                Assert(a.Processed == b.Processed && a.State.money == b.State.money && Math.Abs(a.State.materials[0].work - b.State.materials[0].work) < 1e-7, "Time lost"); a.Validate(); b.Validate();
            });
            Test(report, "S snapshot / no offline / atomic backup / corruption", () =>
            {
                var g = New(); Earn(g, 100); g.Purchase(PurchaseKind.Collector); g.Tick(.31);
                g.State.muted = true; g.State.reduceMotion = true;
                string directory = Path.GetFullPath("../Evidence/save-check-" + Guid.NewGuid().ToString("N"));
                var save = new SaveService(directory, g.Definitions, false); Assert(save.Load() == null, "Fresh save");
                Assert(save.Save(g), "First write failed"); string snapshot = JsonUtility.ToJson(g.State);
                var loaded = save.Load(); Assert(snapshot == JsonUtility.ToJson(loaded), "Snapshot not exact");
                Assert(loaded.muted && loaded.reduceMotion, "Audio/motion settings lost");
                var legacy = JsonUtility.FromJson<GameState>(snapshot.Replace(",\"reduceMotion\":true", ""));
                new GameSession(g.Definitions, false, legacy).Validate();
                Assert(legacy.muted && !legacy.reduceMotion, "Previous save compatibility failed");
                File.SetLastWriteTimeUtc(save.Path, DateTime.UtcNow.AddDays(-3));
                Assert(snapshot == JsonUtility.ToJson(save.Load()), "Offline time added");
                g.Tick(.4); Assert(save.Save(g), "Atomic replacement failed");
                File.WriteAllText(save.Path, "{broken"); var backup = save.Load();
                Assert(backup != null && save.NeedsRecovery && !save.CanWrite, "Backup recovery silent or absent");
                Assert(JsonUtility.ToJson(backup) == snapshot && !save.Save(g), "Backup not previous valid snapshot / blocked overwrite failed");
                save.ConfirmRecovery(); Assert(save.Save(new GameSession(g.Definitions, false, backup)), "Recovery write failed");
                File.WriteAllText(save.Path, "broken"); File.WriteAllText(save.Path + ".bak", "broken");
                Assert(save.Load() == null && save.NeedsRecovery && !save.CanWrite, "Corrupt saves silently reset");
                Assert(File.ReadAllText(save.Path) == "broken", "Corrupt file overwritten");
            });
            Test(report, "E16 rounded percentage cannot trigger completion", () =>
            {
                var g = New();
                for (int i = 0; i < g.State.materials.Length; i++)
                {
                    var m = g.State.materials[i]; m.reserve = m.scatteredReserve = m.visible = 0;
                    m.processed = g.Definitions.materials[i].reserve_units;
                    g.State.earned += (long)m.processed * g.Definitions.materials[i].reward_cents_per_unit;
                }
                g.State.pileRemaining = 0; g.State.pileMaterial = -1;
                g.State.materials[5].processed--; g.State.materials[5].raw++;
                g.State.earned -= g.Definitions.materials[5].reward_cents_per_unit; g.State.money = g.State.earned;
                g.Tick(.01);
                Assert(Math.Round(100.0 * g.Processed / g.TotalUnits) == 100 && !g.State.complete, "Rounded 100 caused premature finish");
                g.Validate();
            });
            Test(report, "E01-E13-E14-E16-E17 full unattended finite run", () =>
            {
                var g = New(); int actions = 0; double nextAction = 0;
                while (g.State.collectorRank == 0 && g.State.elapsed < 240)
                {
                    if (g.State.elapsed >= nextAction) { if (++actions % 10 == 0) g.TokenAction(0, g.State.materials[0].tokenGeneration); else g.CenterAction(); nextAction += 1.0 / 3; }
                    g.Tick(.05); if (g.State.money >= 10000) g.Purchase(PurchaseKind.Collector); g.Validate();
                }
                Assert(g.State.collectorRank == 1, "Cannot reach automation");
                while (!g.State.complete && g.State.elapsed < 10000) { g.Tick(.25); g.Validate(); }
                Assert(g.State.complete && g.Processed == 6000 && g.WorldRemaining == 0, "Unattended did not finish");
                Assert(g.State.earned == 1056000 && g.State.money == 1046000, "Gross reward changed");
                Assert(g.State.resultCount == 1, "Result did not fire once");
                long money = g.State.money; g.Tick(100); g.CenterAction();
                Assert(g.State.resultCount == 1 && money == g.State.money, "Repeat completion generated reward");
                var restored = new GameSession(g.Definitions, false, JsonUtility.FromJson<GameState>(JsonUtility.ToJson(g.State))); restored.Tick(100); restored.Validate();
                Assert(restored.State.resultCount == 1 && restored.State.money == money, "Completed reload paid again");
            });
            string path = Path.GetFullPath("../Evidence/domain-checks.json"); Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, JsonUtility.ToJson(report, true));
            int failures = report.results.FindAll(r => r.status != "PASS").Count;
            Debug.Log("KIYI_CHECKS tests=" + report.results.Count + " failures=" + failures + " report=" + path);
            if (failures > 0) throw new Exception("Domain checks failed: " + failures);
        }
    }
}
