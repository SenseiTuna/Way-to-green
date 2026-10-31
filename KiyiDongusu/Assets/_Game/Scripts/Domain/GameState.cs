using System;

namespace KiyiDongusu
{
    [Serializable] public sealed class MaterialState
    {
        public string id;
        public int reserve, scatteredReserve, visible, raw, batch, processed, tokenGeneration;
        public double work;
        public bool unlocked;
        public int netRaw, netBatch, netProcessed;
        public int scatterX, scatterY;
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
        public int settingsVersion, displayWidth, displayHeight;
        public string language;
        public double masterVolume, musicVolume, effectsVolume, ambienceVolume;
        public bool fullscreen;
        public int litterSeed;
        public NarrativeState story;
    }
}
