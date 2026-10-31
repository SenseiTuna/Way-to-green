using System;
namespace KiyiDongusu
{
    // Saved reference-canvas coordinates. Re-spawns never relocate an existing piece.
    public static class ScatterPlacement
    {
        static readonly int[] X={110,350,620,910,1210,1280,1290,1250,1020,740,470,220,100,110,300,1280};
        static readonly int[] Y={190,180,180,180,190,310,490,700,785,780,775,660,490,340,300,795};
        static uint Mix(uint n){unchecked{n^=n>>16;n*=0x7feb352d;n^=n>>15;n*=0x846ca68b;return n^(n>>16);}}
        public static bool InBounds(int x,int y)=>x>=60&&x<=1340&&y>=140&&y<=850;
        public static void Spawn(GameState state,int index)
        {
            var source=state.materials[index];int chosenX=110,chosenY=190;double best=-1;
            for(int attempt=0;attempt<96;attempt++)
            {
                uint h=Mix(unchecked((uint)state.litterSeed+(uint)source.tokenGeneration*747796405u+(uint)index*2891336453u+(uint)attempt*277803737u));
                int zone=(int)(h%X.Length);int x=X[zone]+(int)((h>>8)%51)-25,y=Y[zone]+(int)((h>>16)%37)-18;
                double nearest=source.scatterX==0?double.MaxValue:Distance(x,y,source.scatterX,source.scatterY);
                bool farFromPrevious=nearest>=260;
                for(int j=0;j<state.materials.Length;j++)
                {
                    var other=state.materials[j];if(j==index||other==null||other.visible==0||other.scatterX==0)continue;
                    nearest=Math.Min(nearest,Distance(x,y,other.scatterX,other.scatterY));
                }
                if(nearest>best){best=nearest;chosenX=x;chosenY=y;}
                if(farFromPrevious&&nearest>=155){chosenX=x;chosenY=y;break;}
            }
            source.scatterX=chosenX;source.scatterY=chosenY;
        }
        static double Distance(int x,int y,int a,int b)=>Math.Sqrt((double)(x-a)*(x-a)+(double)(y-b)*(y-b));
    }
}
