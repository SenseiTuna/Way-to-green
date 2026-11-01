using System;
namespace KiyiDongusu
{
    [Serializable] public sealed class OpeningState { public int version,remainingMask; public bool complete,guided; }
    public static class OpeningProgress
    {
        public const int PaperCount=6;
        public static int Remaining(GameState s){int n=0;for(int i=0;i<PaperCount;i++)if((s.opening.remainingMask&(1<<i))!=0)n++;return n;}
        public static bool Visible(GameState s,int slot)=>slot>=0&&slot<PaperCount&&(s.opening.remainingMask&(1<<slot))!=0;
        public static void Initialize(GameSession g,bool restored)
        {
            if(g.State.opening!=null&&g.State.opening.version!=0)return;
            g.State.opening=new OpeningState{version=1,complete=restored,guided=!restored,remainingMask=restored?0:63};
            if(!restored)g.State.materials[0].scatteredReserve-=PaperCount;
        }
        public static bool Collect(GameSession g,int slot)
        {
            var s=g.State;if(s.opening.complete||!Visible(s,slot))return false;
            s.opening.remainingMask&=~(1<<slot);s.materials[0].raw++;
            if(s.opening.remainingMask==0){s.opening.complete=true;g.Supply.SpawnPile();g.Supply.RefreshTokens();}
            g.SyncNarrative();return true;
        }
        public static void Position(GameState s,int slot,out int x,out int y)
        {
            int[] xs={170,440,760,1080,350,1240},ys={150,150,150,150,450,510};
            uint n=unchecked((uint)s.litterSeed+(uint)slot*747796405u);n^=n>>16;n*=0x7feb352du;n^=n>>15;
            x=xs[slot]+(int)(n%61)-30;y=ys[slot]+(int)((n>>8)%41)-20;
        }
        public static void Validate(GameState s)
        {
            var o=s.opening;if(o==null||o.version!=1||o.remainingMask<0||o.remainingMask>63||o.complete!=(o.remainingMask==0)||(!o.complete&&(!o.guided||s.pileRemaining!=0)))throw new InvalidOperationException("Invalid opening state");
            if(s.purchasedBinsMask<0||s.purchasedBinsMask>63)throw new InvalidOperationException("Invalid container ownership");
        }
    }
}
