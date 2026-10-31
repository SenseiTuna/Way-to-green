using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace KiyiDongusu.Editor
{
    public static class ProgressionChecks
    {
        [Serializable] sealed class Row{public string name,status,detail;}
        [Serializable] sealed class Report{public string version,environment="Actual Unity Editor C# and isolated save files; not Windows UI or human tempo";public List<Row> checks=new List<Row>();}
        static void Assert(bool value,string message){if(!value)throw new Exception(message);}
        static GameSession New()=>new GameSession(GameDefinitions.Load(),false);
        static void Opening(GameSession g){for(int i=0;i<6;i++){g.Tick(.25);Assert(g.OpeningTokenAction(i),"Opening piece rejected");}}
        static void Earn(GameSession g,long cents){while(g.State.money<cents&&g.State.elapsed<500){g.Tick(.3);g.CenterAction();}Assert(g.State.money>=cents,"Fixture could not earn credits");}
        static void Test(Report r,string name,Action action){try{action();r.checks.Add(new Row{name=name,status="PASS"});}catch(Exception e){r.checks.Add(new Row{name=name,status="FAIL",detail=e.ToString()});}}
        public static void Run()
        {
            var r=new Report{version=BuildGame.ArtVersion};
            Test(r,"Fresh beach contains exactly six finite paper pieces and no pile or locked litter",()=>{var g=New();Assert(g.WorldRemaining==6000&&g.Collected==0&&g.State.pileRemaining==0&&OpeningProgress.Remaining(g.State)==6,"Opening pool wrong");Assert(!g.CenterAction(),"Centre bypassed opening");foreach(var m in g.State.materials)Assert(m.visible==0,"Regular litter spawned during opening");g.Validate();});
            Test(r,"Partial opening mask and deterministic positions resume without duplicate collection",()=>{var g=New();g.OpeningTokenAction(2);Assert(!g.OpeningTokenAction(2),"Duplicate opening piece");var save=new SaveService(Path.GetFullPath("../Evidence/opening-save-"+Guid.NewGuid().ToString("N")),g.Definitions,false);save.Load();Assert(save.Save(g),"Opening save failed");var replay=new GameSession(g.Definitions,false,save.Load());Assert(replay.State.opening.remainingMask==g.State.opening.remainingMask&&replay.State.pileRemaining==0&&replay.Collected==1,"Opening reset");for(int i=0;i<6;i++){OpeningProgress.Position(g.State,i,out int x,out int y);OpeningProgress.Position(replay.State,i,out int a,out int b);Assert(x==a&&y==b,"Positions moved");}g.Validate();replay.Validate();});
            Test(r,"Sixth paper reveals only a paper pile and queues Mira immediately",()=>{var g=New();Opening(g);Assert(g.Collected==6&&g.State.pileMaterial==0&&g.State.pileRemaining==24&&g.State.materials[0].visible==1,"Wrong first pile");Assert(g.State.story.pending[0]==13&&g.State.story.cooldown==0,"Pile guidance late");for(int i=1;i<6;i++)Assert(g.State.materials[i].visible==0,"Locked material visible");g.Validate();});
            Test(r,"Paid containers are optional, atomic and locked to their own material line",()=>{var g=New();Opening(g);Assert(!g.Purchase(PurchaseKind.SortingBin,0)&&!g.Purchase(PurchaseKind.SortingBin,2),"Free/locked purchase accepted");Earn(g,1500);long before=g.State.money,spent=g.State.spent;Assert(g.Purchase(PurchaseKind.SortingBin,0),"Affordable bin rejected");for(int i=0;i<100;i++)Assert(!g.Purchase(PurchaseKind.SortingBin,0),"Duplicate purchase");Assert(g.State.money==before-1500&&g.State.spent==spent+1500&&g.OwnsBin(0),"Non-atomic charge");g.Validate();});
            Test(r,"Wrong or unbought container cannot consume litter, cooldown or pay rewards",()=>{var g=New();Opening(g);int generation=g.State.materials[0].tokenGeneration;string before=JsonUtility.ToJson(g.State);Assert(!g.BinDropAction(0,generation,0)&&!g.BinDropAction(0,generation,2),"Unowned or wrong bin accepted");Assert(before==JsonUtility.ToJson(g.State),"Invalid drop mutated state");Earn(g,1500);g.Purchase(PurchaseKind.SortingBin,0);g.Tick(.3);before=JsonUtility.ToJson(g.State);Assert(!g.BinDropAction(0,g.State.materials[0].tokenGeneration,1)&&before==JsonUtility.ToJson(g.State),"Wrong material consumed");Assert(g.BinDropAction(0,g.State.materials[0].tokenGeneration,0),"Matching purchased bin rejected");g.Validate();});
            Test(r,"Container ownership and tagged partial batch survive save and resume",()=>{var g=New();Opening(g);Earn(g,1500);g.Purchase(PurchaseKind.SortingBin,0);g.Tick(.3);g.BinDropAction(0,g.State.materials[0].tokenGeneration,0);g.Tick(.1);var save=new SaveService(Path.GetFullPath("../Evidence/bin-save-"+Guid.NewGuid().ToString("N")),g.Definitions,false);save.Load();Assert(save.Save(g),"Save failed");var restored=new GameSession(g.Definitions,false,save.Load());Assert(restored.OwnsBin(0)&&restored.NetDelivered==1&&restored.State.opening.complete,"Bin/tag/opening lost");restored.Validate();});
            Test(r,"New materials appear only after their free processing thresholds",()=>{var g=New();Opening(g);Earn(g,6000);Assert(g.State.materials[1].unlocked&&!g.State.materials[2].unlocked,"Wrong unlock order");g.Supply.RefreshTokens();Assert(g.State.materials[1].visible==1&&g.State.materials[2].visible==0,"Wrong visible types");g.Validate();});
            Test(r,"Every credit spent on available containers still leaves a finite route to completion",()=>{var g=New();Opening(g);double next=0;while(!g.State.complete&&g.State.elapsed<12000){if(g.State.elapsed>=next){g.CenterAction();next+=.34;}g.Tick(.1);for(int i=0;i<6;i++)g.Purchase(PurchaseKind.SortingBin,i);if(g.State.collectorRank<3)g.Purchase(PurchaseKind.Collector);g.Validate();}Assert(g.Processed==6000&&g.State.resultCount==1&&g.State.earned==1056000&&g.State.purchasedBinsMask==63,"Container spending softlocked finite game");});
            File.WriteAllText("../Evidence/progression-checks.json",JsonUtility.ToJson(r,true));if(r.checks.Exists(x=>x.status=="FAIL"))throw new Exception("Progression checks failed");Debug.Log("KIYI_PROGRESSION_CHECKS PASS count="+r.checks.Count);
        }
    }
}
