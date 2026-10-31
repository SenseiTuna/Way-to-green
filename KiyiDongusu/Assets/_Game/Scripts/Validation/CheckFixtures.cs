#if DEVELOPMENT_BUILD || UNITY_EDITOR
namespace KiyiDongusu
{
    public static class CheckFixtures
    {
        // Isolate older economy checks from the separate opening tutorial tests.
        // Return the six finite introductory units to their original reserve, never mint stock.
        public static GameSession Ready(GameSession g,bool bins=false)
        {
            g.State.materials[0].scatteredReserve+=OpeningProgress.Remaining(g.State);g.State.opening.remainingMask=0;g.State.opening.complete=true;g.State.opening.guided=false;
            if(bins)g.State.purchasedBinsMask=(1<<g.State.materials.Length)-1;
            g.Supply.SpawnPile();g.Supply.RefreshTokens();g.SyncNarrative();g.Validate();return g;
        }
    }
}
#endif
