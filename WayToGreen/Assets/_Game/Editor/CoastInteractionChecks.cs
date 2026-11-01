using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace KiyiDongusu.Editor
{
    public static class CoastInteractionChecks
    {
        [Serializable] sealed class Row {public string test,status,detail;}
        [Serializable] sealed class Report {public string environment="Real Unity Editor C# domain, no human playtest",version,utc;public List<Row> results=new List<Row>();}
        static void Assert(bool value,string text){if(!value)throw new Exception(text);}
        static GameSession New()=>CheckFixtures.Ready(new GameSession(GameDefinitions.Load(),false),true);
        static void Check(Report report,string name,Action action){try{action();report.results.Add(new Row{test=name,status="PASS"});}catch(Exception e){report.results.Add(new Row{test=name,status="FAIL",detail=e.ToString()});}}
        [MenuItem("Kiyi Dongusu/Run Coast Interaction Checks")]
        public static void Run()
        {
            var r=new Report{version=BuildGame.ArtVersion,utc=DateTime.UtcNow.ToString("o")};
            Check(r,"Net drop transfers one finite piece; pays only after processing, exactly once",()=>{
                var g=New();int generation=g.State.materials[0].tokenGeneration;int world=g.WorldRemaining;
                Assert(g.NetDropAction(0,generation),"Drop rejected");Assert(g.State.money==0&&g.State.materials[0].netRaw==1&&g.WorldRemaining==world-1,"Drop minted currency or material");
                Assert(!g.NetDropAction(0,generation),"Duplicate token accepted");g.Tick(.7);Assert(g.State.money==0,"Bonus paid early");g.Tick(.2);
                Assert(g.State.money==g.Definitions.materials[0].reward_cents_per_unit+g.NetBonusCents&&g.State.materials[0].netProcessed==1,"Incorrect reward");long money=g.State.money;g.Tick(3);Assert(g.State.money==money,"Duplicate bonus");g.Validate();
            });
            Check(r,"Stale held identity rejected after collector consumes source",()=>{
                var g=New();int generation=g.State.materials[0].tokenGeneration;g.Supply.CollectToken(0,generation);g.Supply.RefreshTokens();
                Assert(!g.NetDropAction(0,generation)&&g.NetDelivered==0,"Stale drag produced bonus");g.Validate();
            });
            Check(r,"Tagged active batch and preferences save and reload losslessly",()=>{
                var g=New();g.NetDropAction(0,g.State.materials[0].tokenGeneration);g.Tick(.2);g.State.language="tr";g.State.musicVolume=.12;g.State.displayWidth=1600;g.State.displayHeight=900;
                var store=new SaveService(Path.GetFullPath("../Evidence/net-save-"+Guid.NewGuid().ToString("N")),g.Definitions,false);store.Load();Assert(store.Save(g),"Write failed");
                var restored=store.Load();string canonical=JsonUtility.ToJson(JsonUtility.FromJson<GameState>(JsonUtility.ToJson(g.State)));Assert(JsonUtility.ToJson(restored)==canonical,"Tagged snapshot differs");var replay=new GameSession(g.Definitions,false,restored);replay.Tick(1);replay.Validate();Assert(replay.State.materials[0].netProcessed==1,"Tag lost");
            });
            Check(r,"Legacy save initializes English and audible defaults without resetting progress",()=>{
                var g=New();g.State.muted=true;g.State.reduceMotion=true;g.CenterAction();g.Tick(.2);
                string text=JsonUtility.ToJson(g.State);int cut=text.IndexOf(",\"settingsVersion\"",StringComparison.Ordinal);Assert(cut>0,"Fixture fields missing");text=text.Substring(0,cut)+"}";
                var restored=new GameSession(g.Definitions,false,JsonUtility.FromJson<GameState>(text));Assert(restored.State.language=="en"&&restored.State.musicVolume>.1&&restored.State.muted&&restored.State.reduceMotion,"Legacy defaults invalid");Assert(restored.Collected==g.Collected&&restored.State.elapsed==g.State.elapsed,"Legacy progress reset");restored.Validate();
            });
            Check(r,"Corrupt preference or net ledger cannot enter a valid save",()=>{
                var g=New();g.State.musicVolume=double.NaN;bool rejected=false;try{g.Validate();}catch{rejected=true;}Assert(rejected,"NaN preference accepted");g.State.musicVolume=.4;g.State.materials[0].netRaw=1;rejected=false;try{g.Validate();}catch{rejected=true;}Assert(rejected,"Unbacked tag accepted");
            });
            Check(r,"All pile stages, net, two hand poses and original audio exist",()=>{
                var a=new GameArt();Assert(a.PileAt(1)!=null&&a.PileAt(.7f)!=null&&a.PileAt(.4f)!=null&&a.PileAt(.2f)!=null,"Missing phase");Assert(a.PileAt(1)!=a.PileAt(.7f)&&a.PileAt(.7f)!=a.PileAt(.4f)&&a.PileAt(.4f)!=a.PileAt(.2f),"Repeated phase");Assert(a.Net!=null&&a.HandOpen!=null&&a.HandPinch!=null,"Missing interaction sprites");
                Assert(Resources.Load<AudioClip>("Audio/Tideglass").length>90&&Resources.Load<AudioClip>("Audio/ShoreWave").length>8,"Missing soundscape");
            });
            Check(r,"Finite demo completes with tagged litter and exact total bonus",()=>{
                var g=New();double next=0;
                while(!g.State.complete&&g.State.elapsed<12000)
                {
                    if(g.State.elapsed>=next)
                    {
                        bool dropped=false;for(int i=0;i<g.State.materials.Length;i++)if(g.State.materials[i].visible>0&&g.State.materials[i].unlocked){dropped=g.NetDropAction(i,g.State.materials[i].tokenGeneration);break;}
                        if(!dropped)g.CenterAction();next+=.34;
                    }
                    g.Tick(.05);if(g.State.collectorRank<3)g.Purchase(PurchaseKind.Collector);
                    for(int f=0;f<4;f++){if(g.State.facilities[f].speed<2)g.Purchase(PurchaseKind.Speed,f);if(g.State.facilities[f].capacity<2)g.Purchase(PurchaseKind.Capacity,f);}g.Validate();
                }
                int delivered=g.NetDelivered;Assert(delivered>0&&g.Processed==6000&&g.State.resultCount==1,"Tagged demo did not finish");
                Assert(g.State.earned==1056000L+delivered*g.NetBonusCents,"Incorrect total tagged reward");foreach(var m in g.State.materials)Assert(m.netRaw==0&&m.netBatch==0,"Unfinished tagged material");
            });
            Directory.CreateDirectory("../Evidence");File.WriteAllText("../Evidence/coast-interaction-checks.json",JsonUtility.ToJson(r,true));
            if(r.results.Exists(x=>x.status=="FAIL"))throw new Exception("Coast interaction checks failed; read report");Debug.Log("KIYI_COAST_CHECKS PASS count="+r.results.Count);
        }
    }
}
