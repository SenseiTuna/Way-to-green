using System;

namespace KiyiDongusu
{
    public sealed class ProcessingSystem
    {
        readonly GameSession game;
        public ProcessingSystem(GameSession game) { this.game = game; }
        public int Capacity(int i)
        {
            var d = game.Definitions.processing;
            return d.base_batch_capacity_per_lane + d.capacity_per_facility_rank * game.State.facilities[game.Definitions.FacilityIndex(i)].capacity;
        }
        public double Rate(int i)
        {
            var d = game.Definitions;
            return Capacity(i) / d.materials[i].full_base_batch_seconds *
                (1 + d.processing.speed_multiplier_per_rank * game.State.facilities[d.FacilityIndex(i)].speed) *
                d.tempo.multiplier * (game.State.finalPhase ? d.final_phase.automatic_processing_multiplier : 1);
        }
        void Start(int i)
        {
            var m = game.State.materials[i];
            if (!m.unlocked || m.batch > 0 || m.raw == 0) return;
            m.batch = Math.Min(m.raw, Capacity(i));
            m.raw -= m.batch;
            m.netBatch = Math.Min(m.netRaw, m.batch); m.netRaw -= m.netBatch;
            m.work = 0;
        }
        void Finish(int i)
        {
            var m = game.State.materials[i];
            int quantity = m.batch;
            m.processed += quantity;
            long reward = (long)quantity * game.Definitions.materials[i].reward_cents_per_unit;
            reward += (long)m.netBatch * game.NetBonusCents;
            m.netProcessed += m.netBatch; m.netBatch = 0;
            game.State.money += reward;
            game.State.earned += reward;
            m.batch = 0;
            m.work = 0;
            game.RecordReward(reward);
        }
        public void Tick(double seconds)
        {
            for (int i = 0; i < game.State.materials.Length; i++)
            {
                double remaining = seconds;
                Start(i);
                var m = game.State.materials[i];
                while (m.batch > 0 && remaining > 0)
                {
                    double needed = (m.batch - m.work) / Rate(i);
                    if (remaining + 1e-10 < needed) { m.work += remaining * Rate(i); break; }
                    remaining = Math.Max(0, remaining - needed);
                    Finish(i);
                    Start(i);
                }
            }
        }
        public bool Assist()
        {
            bool active = false;
            for (int i = 0; i < game.State.materials.Length; i++)
            {
                var m = game.State.materials[i];
                if (m.batch == 0) continue;
                active = true;
                m.work = Math.Min(m.batch, m.work + Rate(i) * game.Definitions.assist.equivalent_processing_seconds_per_active_lane_per_click);
                if (m.work >= m.batch - 1e-10) Finish(i);
                // A click never starts or assists the following batch.
            }
            return active;
        }
    }
}
