using System;
using System.Collections.Generic;

namespace KiyiDongusu
{
    public enum PurchaseKind { Collector, Manual, Speed, Capacity }
    public sealed class GameSession
    {
        public GameDefinitions Definitions { get; }
        public GameState State { get; }
        public SupplySystem Supply { get; }
        public ProcessingSystem Processing { get; }
        public event Action Changed;
        public event Action ImportantChange;
        readonly Queue<(double time, long amount)> rewards = new Queue<(double, long)>();
        public int TotalUnits { get; }
        public int Processed { get { int n = 0; foreach (var m in State.materials) n += m.processed; return n; } }
        public int WorldRemaining { get { int n = State.pileRemaining; foreach (var m in State.materials) n += m.reserve + m.scatteredReserve + m.visible; return n; } }
        public int Collected => TotalUnits - WorldRemaining;

        public GameSession(GameDefinitions definitions, bool core, GameState restored = null)
        {
            Definitions = definitions;
            int count = core ? 1 : definitions.materials.Length;
            for (int i = 0; i < count; i++) TotalUnits += definitions.materials[i].reserve_units;
            State = restored ?? new GameState { balanceId = definitions.balance_id, corePrototype = core, materials = new MaterialState[count], facilities = new FacilityState[definitions.facilities.Length] };
            if (restored == null)
            {
                for (int i = 0; i < count; i++) State.materials[i] = new MaterialState { id = definitions.materials[i].id, reserve = definitions.materials[i].center_reserve_units, scatteredReserve = definitions.materials[i].scattered_reserve_units };
                for (int i = 0; i < State.facilities.Length; i++) State.facilities[i] = new FacilityState();
            }
            Supply = new SupplySystem(this);
            Processing = new ProcessingSystem(this);
            if (restored == null) { UpdateProgress(); Supply.SpawnPile(); Supply.RefreshTokens(); }
            Validate();
        }
        public void RecordReward(long reward) { rewards.Enqueue((State.elapsed, reward)); }
        public double RecentIncome()
        {
            while (rewards.Count > 0 && State.elapsed - rewards.Peek().time > 10) rewards.Dequeue();
            long amount = 0; foreach (var r in rewards) amount += r.amount;
            return amount / 100.0 / Math.Max(0.05, Math.Min(10, State.elapsed));
        }
        bool AcceptAction()
        {
            if (State.complete || State.actionCooldown > 1e-9) return false;
            State.actionCooldown = Definitions.assist.minimum_accepted_input_interval_seconds;
            return true;
        }
        public bool CenterAction()
        {
            if (!AcceptAction()) return false;
            if (State.pileRemaining > 0) Supply.ManualPile(); else Processing.Assist();
            UpdateProgress(); Changed?.Invoke(); return true;
        }
        public bool TokenAction(int i, int generation)
        {
            if (i < 0 || i >= State.materials.Length || State.materials[i].visible == 0 || State.materials[i].tokenGeneration != generation || !AcceptAction()) return false;
            bool collected = Supply.CollectToken(i, generation);
            UpdateProgress(); Changed?.Invoke(); return collected;
        }
        public void Tick(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            // Fixed steps preserve refill/collector ordering even after a long frame.
            while (seconds > 1e-9 && !State.complete)
            {
                double dt = Math.Min(0.05, seconds); seconds -= dt;
                State.elapsed += dt;
                State.actionCooldown = Math.Max(0, State.actionCooldown - dt);
                UpdateProgress(); Supply.Tick(dt); UpdateProgress(); Processing.Tick(dt); UpdateProgress();
            }
            Changed?.Invoke();
        }
        public bool FacilityUnlocked(int facility)
        {
            for (int i = 0; i < State.materials.Length; i++) if (Definitions.FacilityIndex(i) == facility && State.materials[i].unlocked) return true;
            return false;
        }
        public int Rank(PurchaseKind kind, int facility = 0) => kind == PurchaseKind.Collector ? State.collectorRank : kind == PurchaseKind.Manual ? State.manualRank : kind == PurchaseKind.Speed ? State.facilities[facility].speed : State.facilities[facility].capacity;
        public int[] Costs(PurchaseKind kind, int facility = 0) => kind == PurchaseKind.Collector ? Definitions.collector.purchase_cost_credits : kind == PurchaseKind.Manual ? Definitions.manual_collection_upgrade.purchase_cost_credits : kind == PurchaseKind.Speed ? Definitions.facilities[facility].shared_speed_costs_by_rank_credits : Definitions.facilities[facility].shared_capacity_costs_by_rank_credits;
        public long Cost(PurchaseKind kind, int facility = 0)
        {
            var costs = Costs(kind, facility); int rank = Rank(kind, facility);
            return rank >= costs.Length ? -1 : costs[rank] * 100L;
        }
        public bool Purchase(PurchaseKind kind, int facility = 0)
        {
            if (!Enum.IsDefined(typeof(PurchaseKind), kind) || facility < 0 || facility >= State.facilities.Length || State.complete) return false;
            if ((kind == PurchaseKind.Speed || kind == PurchaseKind.Capacity) && !FacilityUnlocked(facility)) return false;
            long cost = Cost(kind, facility);
            if (cost < 0 || State.money < cost) return false;
            State.money -= cost; State.spent += cost;
            switch (kind)
            {
                case PurchaseKind.Collector: State.collectorRank++; break;
                case PurchaseKind.Manual: State.manualRank++; break;
                case PurchaseKind.Speed: State.facilities[facility].speed++; break;
                case PurchaseKind.Capacity: State.facilities[facility].capacity++; break;
            }
            Changed?.Invoke(); ImportantChange?.Invoke(); return true;
        }
        void UpdateProgress()
        {
            bool important = false;
            for (int i = 0; i < State.materials.Length; i++)
                if (!State.materials[i].unlocked && Processed >= Definitions.materials[i].unlock_processed_total_units) { State.materials[i].unlocked = true; important = true; }
            if (WorldRemaining == 0 && !State.finalPhase) { State.finalPhase = true; important = true; }
            if (!State.complete && WorldRemaining == 0 && Processed == TotalUnits)
            {
                bool empty = true; foreach (var m in State.materials) empty &= m.raw == 0 && m.batch == 0;
                if (empty) { State.complete = true; State.resultCount++; important = true; }
            }
            if (important) ImportantChange?.Invoke();
        }
        public void Validate()
        {
            var s = State;
            if (s.schemaVersion != 1 || s.balanceId != Definitions.balance_id || s.materials == null || s.materials.Length != (s.corePrototype ? 1 : Definitions.materials.Length) || s.facilities == null || s.facilities.Length != Definitions.facilities.Length) throw new InvalidOperationException("Kayıt şeması veya denge kimliği uyumsuz.");
            if (s.pileRemaining < 0 || s.pileRemaining > Definitions.sources.pile_units || s.pileMaterial < -1 || s.pileMaterial >= s.materials.Length || (s.pileRemaining > 0 && s.pileMaterial < 0) || s.money < 0 || s.earned < 0 || s.spent < 0 || s.earned - s.spent != s.money) throw new InvalidOperationException("Geçersiz kaynak veya cüzdan.");
            long earned = 0;
            for (int i = 0; i < s.materials.Length; i++)
            {
                var m = s.materials[i]; var d = Definitions.materials[i];
                if (m == null || m.id != d.id || m.reserve < 0 || m.scatteredReserve < 0 || m.visible < 0 || m.visible > 1 || m.raw < 0 || m.batch < 0 || m.processed < 0 || !Finite(m.work) || m.work < 0 || m.work > m.batch || (m.batch == 0 && m.work != 0)) throw new InvalidOperationException("Geçersiz malzeme veya işlem ilerlemesi.");
                long sum = (long)m.reserve + m.scatteredReserve + m.visible + m.raw + m.batch + m.processed + (s.pileMaterial == i ? s.pileRemaining : 0);
                if (sum != d.reserve_units) throw new InvalidOperationException("Kaynak korunumu ihlali: " + d.id);
                earned += (long)m.processed * d.reward_cents_per_unit;
            }
            if (earned != s.earned || s.collectorRank < 0 || s.collectorRank >= Definitions.collector.nominal_units_per_second_by_rank.Length || s.manualRank < 0 || s.manualRank >= Definitions.manual_collection_upgrade.work_per_click_by_rank.Length) throw new InvalidOperationException("Geçersiz ödül veya geliştirme.");
            foreach (var f in s.facilities) if (f == null || f.speed < 0 || f.speed > 4 || f.capacity < 0 || f.capacity > 3) throw new InvalidOperationException("Geçersiz tesis seviyesi.");
            if (!Finite(s.elapsed) || s.elapsed < 0 || !Finite(s.refillRemaining) || s.refillRemaining < 0 || !Finite(s.pileWork) || s.pileWork < 0 || s.pileWork >= 1 || !Finite(s.collectorWork) || s.collectorWork < 0 || !Finite(s.actionCooldown) || s.actionCooldown < 0 || s.actionCooldown > Definitions.assist.minimum_accepted_input_interval_seconds + 1e-8 || s.centerCursor < 0 || s.centerCursor >= s.materials.Length || s.scatteredCursor < 0 || s.scatteredCursor >= s.materials.Length || s.collectorPreference < 0 || s.collectorPreference >= 5) throw new InvalidOperationException("Geçersiz zaman veya kaynak seçim imleci.");
            if (s.finalPhase != (WorldRemaining == 0) || s.complete != (Processed == TotalUnits && WorldRemaining == 0) || s.resultCount != (s.complete ? 1 : 0)) throw new InvalidOperationException("Geçersiz sonuç durumu.");
            for (int i = 0; i < s.materials.Length; i++) if (s.materials[i].unlocked != (Processed >= Definitions.materials[i].unlock_processed_total_units)) throw new InvalidOperationException("Geçersiz açılım durumu.");
        }
        static bool Finite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);
    }
}
