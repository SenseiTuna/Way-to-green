using System;

namespace KiyiDongusu
{
    public sealed class SupplySystem
    {
        readonly GameSession game;
        GameState S => game.State;
        public SupplySystem(GameSession game) { this.game = game; }
        public void RefreshTokens()
        {
            for(int i=0;i<S.materials.Length;i++)
            {
                var m=S.materials[i];
                if (m.unlocked && m.visible == 0 && m.scatteredReserve > 0)
                {
                    m.scatteredReserve--;
                    m.visible = 1;
                    m.tokenGeneration++;
                    ScatterPlacement.Spawn(S,i);
                }
            }
        }
        public void SpawnPile()
        {
            if (S.pileRemaining > 0 || S.refillRemaining > 1e-9) return;
            for (int offset = 0; offset < S.materials.Length; offset++)
            {
                int i = (S.centerCursor + offset) % S.materials.Length;
                var m = S.materials[i];
                if (!m.unlocked || m.reserve == 0) continue;
                S.pileMaterial = i;
                S.pileRemaining = Math.Min(m.reserve, game.Definitions.sources.pile_units);
                m.reserve -= S.pileRemaining;
                S.centerCursor = (i + 1) % S.materials.Length;
                return;
            }
        }
        public bool CollectPileUnit()
        {
            if (S.pileRemaining == 0) return false;
            S.materials[S.pileMaterial].raw++;
            S.pileRemaining--;
            game.SyncNarrative();
            if (S.pileRemaining == 0)
            {
                S.refillRemaining = game.Definitions.sources.pile_refill_seconds / game.Definitions.tempo.multiplier;
                S.pileWork = 0;
            }
            return true;
        }
        public void ManualPile()
        {
            S.pileWork += game.Definitions.manual_collection_upgrade.work_per_click_by_rank[S.manualRank] * game.Definitions.tempo.multiplier;
            while (S.pileWork >= 1 && S.pileRemaining > 0)
            {
                S.pileWork--;
                CollectPileUnit();
            }
        }
        public bool CollectToken(int i, int generation)
        {
            if (i < 0 || i >= S.materials.Length) return false;
            var m = S.materials[i];
            if (!m.unlocked || m.visible == 0 || m.tokenGeneration != generation) return false;
            m.visible--;
            m.raw++;
            game.SyncNarrative();
            return true;
        }
        bool CollectLoose()
        {
            for (int n = 0; n < S.materials.Length; n++)
            {
                int i = (S.scatteredCursor + n) % S.materials.Length;
                if (!CollectToken(i, S.materials[i].tokenGeneration)) continue;
                S.scatteredCursor = (i + 1) % S.materials.Length;
                RefreshTokens();
                return true;
            }
            return false;
        }
        public void Tick(double seconds)
        {
            S.refillRemaining = Math.Max(0, S.refillRemaining - seconds);
            SpawnPile();
            RefreshTokens();
            if (S.collectorRank == 0) return;
            S.collectorWork += seconds * game.Definitions.collector.nominal_units_per_second_by_rank[S.collectorRank] * game.Definitions.tempo.multiplier;
            while (S.collectorWork >= 1)
            {
                bool looseFirst = (S.collectorPreference + 1) % game.Definitions.collector.scattered_priority_every_nth_completed_unit == 0;
                bool collected = looseFirst ? CollectLoose() || CollectPileUnit() : CollectPileUnit() || CollectLoose();
                if (!collected)
                {
                    S.collectorWork = Math.Min(S.collectorWork, game.Definitions.collector.fractional_work_carry_cap_if_blocked);
                    break;
                }
                S.collectorWork--;
                S.collectorPreference = (S.collectorPreference + 1) % game.Definitions.collector.scattered_priority_every_nth_completed_unit;
            }
        }
    }
}
