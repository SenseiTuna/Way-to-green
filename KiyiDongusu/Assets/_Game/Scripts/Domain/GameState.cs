using System;

namespace KiyiDongusu
{
    [Serializable] public sealed class MaterialState
    {
        public string id;
        public int reserve, scatteredReserve, visible, raw, batch, processed, tokenGeneration;
        public double work;
        public bool unlocked;
    }
    [Serializable] public sealed class FacilityState { public int speed, capacity; }
    [Serializable] public sealed class GameState
    {
        public int schemaVersion = 1;
        public string balanceId;
        public bool corePrototype, complete, finalPhase, muted, reduceMotion;
        public long money, earned, spent;
        public MaterialState[] materials;
        public FacilityState[] facilities;
        public int pileMaterial = -1, pileRemaining, centerCursor, scatteredCursor;
        public int collectorRank, manualRank, collectorPreference, resultCount;
        public double pileWork, refillRemaining, collectorWork, actionCooldown, elapsed;
    }
}
