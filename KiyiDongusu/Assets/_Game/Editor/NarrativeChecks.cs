using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace KiyiDongusu.Editor
{
    public static class NarrativeChecks
    {
        [Serializable] sealed class Row{public string name,status,detail;}
        [Serializable] sealed class Report{public string environment="Real Unity Editor C# + isolated files; not Windows or human play",version,utc;public List<Row> checks=new List<Row>();}
        static void Assert(bool v,string message){if(!v)throw new Exception(message);}
        static GameSession New()=>new GameSession(GameDefinitions.Load(),false);
        static void Test(Report r,string name,Action action){try{action();r.checks.Add(new Row{name=name,status="PASS"});}catch(Exception e){r.checks.Add(new Row{name=name,status="FAIL",detail=e.ToString()});}}
        public static void Run()
        {
            var r=new Report{version=BuildGame.ArtVersion,utc=DateTime.UtcNow.ToString("o")};
            Test(r,"Content has eight finite tasks, three portraits and cited real facts",()=>{
                var c=NarrativeContent.Load();Assert(c.quests.Length==8&&c.nodes.Length==13,"Content incomplete");for(int i=0;i<c.nodes.Length;i++)Assert(c.nodes[i].id==i&&c.nodes[i].pages.Length>0,"Node id mismatch");
                foreach(var f in c.facts)Assert(!string.IsNullOrWhiteSpace(f.en)&&!string.IsNullOrWhiteSpace(f.tr)&&(!string.IsNullOrEmpty(f.url)||f.source=="Game mechanics"),"Missing translation or attribution");
                var art=new GameArt();foreach(var image in art.Mira)Assert(image!=null,"Mira pose missing");
            });
            Test(r,"Partial intro page and queue save exactly, without economy rewards",()=>{
                var g=New();Assert(NarrativeProgress.Begin(g),"Intro not queued");NarrativeProgress.Advance(g);Assert(g.State.story.activeId==0&&g.State.story.page==1,"Page not advanced");
                var save=new SaveService(Path.GetFullPath("../Evidence/story-save-"+Guid.NewGuid().ToString("N")),g.Definitions,false);save.Load();Assert(save.Save(g),"Write failed");
                var loaded=save.Load();Assert(JsonUtility.ToJson(loaded)==JsonUtility.ToJson(g.State),"Partial dialogue lost");Assert(g.State.money==0&&g.Collected==0,"Story minted a reward");
                var replay=new GameSession(g.Definitions,false,loaded);NarrativeProgress.Dismiss(replay);NarrativeProgress.Sync(replay);Assert(!replay.State.story.pending.Contains(0),"Intro repeated");replay.Validate();
            });
            Test(r,"Legacy save backfills tasks and queues one welcome without resetting progress",()=>{
                var g=New();for(int i=0;i<30;i++){g.CenterAction();g.Tick(.8);}string before=JsonUtility.ToJson(g.State);var state=JsonUtility.FromJson<GameState>(before);state.story=null;
                var old=new GameSession(g.Definitions,false,state);Assert(old.State.story.pending.Count==1&&old.State.story.pending[0]==NarrativeProgress.Welcome,"Legacy tutorial flood");
                Assert(old.State.story.questMask!=0&&old.Collected==g.Collected&&old.State.money==g.State.money&&Math.Abs(old.State.elapsed-g.State.elapsed)<1e-9,"Legacy progress reset");old.Validate();
            });
            Test(r,"Auto collection counts; enabling guide after disable cannot flood old milestones",()=>{
                var g=New();g.State.story.enabled=false;NarrativeProgress.Sync(g);while(g.State.money<g.Cost(PurchaseKind.Collector)&&g.State.elapsed<240){g.CenterAction();g.Tick(1);}Assert(g.Purchase(PurchaseKind.Collector),"Could not afford test crew");
                int before=g.Collected;g.Tick(30);Assert(g.Collected>before&&NarrativeProgress.Done(g,0),"Crew ignored by task");g.State.story.enabled=true;NarrativeProgress.Sync(g);Assert(g.State.story.pending.Count==0,"Disabled history replayed");g.Validate();
            });
            Test(r,"Actual legacy JSON through SaveService preserves every economy field and queues only welcome",()=>{
                var g=New();while(g.Collected<35){g.CenterAction();g.Tick(.5);}string json=JsonUtility.ToJson(g.State);json=json.Substring(0,json.LastIndexOf(",\"story\":",StringComparison.Ordinal))+"}";
                var parsed=JsonUtility.FromJson<GameState>(json);Assert(parsed.story!=null&&parsed.story.version==0,"Missing-story serializer sentinel changed");string expected=JsonUtility.ToJson(parsed);expected=expected.Substring(0,expected.LastIndexOf(",\"story\":",StringComparison.Ordinal));
                string directory=Path.GetFullPath("../Evidence/legacy81-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);string hash;using(var sha=System.Security.Cryptography.SHA256.Create())hash=Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json)));
                File.WriteAllText(Path.Combine(directory,"beach-v1.json"),JsonUtility.ToJson(new SaveEnvelope{payload=json,sha256=hash}));var saves=new SaveService(directory,g.Definitions,false);var restored=saves.Load();Assert(restored!=null&&!saves.NeedsRecovery,"Legacy envelope failed to load");string actual=JsonUtility.ToJson(restored);actual=actual.Substring(0,actual.LastIndexOf(",\"story\":",StringComparison.Ordinal));Assert(expected==actual,"Economy changed during migration");Assert(restored.story.version==1&&restored.story.pending.Count==1&&restored.story.pending[0]==9,"Legacy dialogue flood");var loaded=new GameSession(g.Definitions,false,restored);Assert(saves.Save(loaded),"Migration write failed");var again=saves.Load();Assert(JsonUtility.ToJson(again)==JsonUtility.ToJson(restored),"Migration repeated on reopen");
            });
            Test(r,"Duplicate story sync changes no stock or currency; bad page rejected",()=>{
                var g=New();NarrativeProgress.Sync(g);string before=JsonUtility.ToJson(g.State);for(int i=0;i<100;i++)NarrativeProgress.Sync(g);Assert(before==JsonUtility.ToJson(g.State),"Duplicate sync changed state");
                NarrativeProgress.Begin(g);g.State.story.page=99;bool rejected=false;try{g.Validate();}catch{rejected=true;}Assert(rejected,"Bad page accepted");
            });
            Test(r,"Malformed QA flags cannot fall back to the normal player profile",()=>{
                foreach(var args in new[]{new[]{"--kiyi-smoke-test--kiyi-test-output","somewhere"},new[]{"--kiyi-test-output","somewhere"},new[]{"--kiyi-story-test"}}){bool rejected=false;try{RuntimeSmokeRunner.ValidateTestArguments(args);}catch(ArgumentException){rejected=true;}Assert(rejected,"Unsafe QA argument accepted");}RuntimeSmokeRunner.ValidateTestArguments(new[]{"game.exe","--kiyi-smoke-test","--kiyi-test-output","somewhere"});RuntimeSmokeRunner.ValidateTestArguments(new[]{"game.exe"});
            });
            Test(r,"All eight tasks finish in finite passive run and ending clears queued tutorials",()=>{
                var g=New();while(g.State.collectorRank==0&&g.State.elapsed<240){g.CenterAction();g.Tick(.3);g.Purchase(PurchaseKind.Collector);}while(!g.State.complete&&g.State.elapsed<20000){g.Tick(1);if(g.State.collectorRank<4)g.Purchase(PurchaseKind.Collector);g.Validate();}
                Assert(g.Processed==6000&&g.State.resultCount==1&&g.State.story.questMask==255,"Tasks or demo unfinished");Assert(g.State.story.pending.Count==0&&g.State.story.activeId<0,"Ending blocked by tutorials");Assert(g.State.earned==1056000,"Tasks changed credits");
            });
            File.WriteAllText("../Evidence/narrative-checks.json",JsonUtility.ToJson(r,true));if(r.checks.Exists(x=>x.status=="FAIL"))throw new Exception("Narrative checks failed");Debug.Log("KIYI_NARRATIVE_CHECKS PASS count="+r.checks.Count);
        }
    }
}
