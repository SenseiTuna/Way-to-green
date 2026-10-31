using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace KiyiDongusu.Editor
{
    public static class ScatterChecks
    {
        [Serializable] sealed class Row{public string name,status,detail;}
        [Serializable] sealed class Report{public string environment="Real Unity Editor C# + filesystem; not a Windows visual or human test",version,utc;public List<Row> checks=new List<Row>();}
        static void Assert(bool value,string text){if(!value)throw new Exception(text);}
        static void Test(Report r,string name,Action action){try{action();r.checks.Add(new Row{name=name,status="PASS"});}catch(Exception e){r.checks.Add(new Row{name=name,status="FAIL",detail=e.ToString()});}}
        public static void Run()
        {
            var report=new Report{version=BuildGame.ArtVersion,utc=DateTime.UtcNow.ToString("o")};
            Test(report,"Random replacement moves away and preserves every finite unit",()=>{
                var game=new GameSession(GameDefinitions.Load(),false);var m=game.State.materials[0];var positions=new HashSet<string>();
                int count=Math.Min(100,m.scatteredReserve);for(int i=0;i<count;i++)
                {
                    int x=m.scatterX,y=m.scatterY,generation=m.tokenGeneration;Assert(game.Supply.CollectToken(0,generation),"Collect rejected");game.Supply.RefreshTokens();
                    Assert(ScatterPlacement.InBounds(m.scatterX,m.scatterY),"Out of bounds");double distance=Math.Sqrt((double)(m.scatterX-x)*(m.scatterX-x)+(double)(m.scatterY-y)*(m.scatterY-y));Assert(distance>=260,"Replacement too close: "+distance);positions.Add(m.scatterX+","+m.scatterY);game.Validate();
                }
                Assert(positions.Count>count*.9,"Positions repeat too often");
            });
            Test(report,"Layout, seed and subsequent spawn survive save reload",()=>{
                var g=new GameSession(GameDefinitions.Load(),false);var save=new SaveService(Path.GetFullPath("../Evidence/scatter-save-"+Guid.NewGuid().ToString("N")),g.Definitions,false);save.Load();Assert(save.Save(g),"Save rejected");
                var replay=new GameSession(g.Definitions,false,save.Load());Assert(JsonUtility.ToJson(g.State)==JsonUtility.ToJson(replay.State),"Positions changed on reload");
                foreach(var session in new[]{g,replay}){session.Supply.CollectToken(0,session.State.materials[0].tokenGeneration);session.Supply.RefreshTokens();session.Validate();}
                Assert(JsonUtility.ToJson(g.State)==JsonUtility.ToJson(replay.State),"Future spawn diverges after reload");
            });
            Test(report,"Simultaneous visible pieces keep their positions and separation",()=>{
                var g=new GameSession(GameDefinitions.Load(),false);
                for(int i=0;i<g.State.materials.Length;i++)g.State.materials[i].unlocked=true;
                g.Supply.RefreshTokens();
                for(int round=0;round<80;round++)
                {
                    int i=round%6;int[] x=new int[6],y=new int[6];for(int j=0;j<6;j++){x[j]=g.State.materials[j].scatterX;y[j]=g.State.materials[j].scatterY;}
                    g.Supply.CollectToken(i,g.State.materials[i].tokenGeneration);g.Supply.RefreshTokens();
                    for(int j=0;j<6;j++)if(j!=i){var a=g.State.materials[i];var b=g.State.materials[j];Assert(b.scatterX==x[j]&&b.scatterY==y[j],"Other piece relocated");Assert(Math.Sqrt((double)(a.scatterX-b.scatterX)*(a.scatterX-b.scatterX)+(double)(a.scatterY-b.scatterY)*(a.scatterY-b.scatterY))>=155,"Pieces overlap");}
                }
                // This fixture forces unlocks only to exercise visual placement; it is not a progression test.
            });
            File.WriteAllText("../Evidence/scatter-checks.json",JsonUtility.ToJson(report,true));if(report.checks.Exists(x=>x.status=="FAIL"))throw new Exception("Scatter checks failed");Debug.Log("KIYI_SCATTER_CHECKS PASS count="+report.checks.Count);
        }
    }
}
