using System;
using System.Collections.Generic;
namespace KiyiDongusu
{
    [Serializable] public sealed class NarrativeState
    {
        // Zero is the migration sentinel: JsonUtility creates missing nested classes.
        // Only initialized story state is written as version one.
        public int version,seenMask,questMask,pinnedQuest;
        public int activeId=-1,page;
        public bool enabled=true;
        public double cooldown,activeSeconds;
        public List<int> pending=new List<int>();
    }
    public static class NarrativeProgress
    {
        public const int Intro=0,Ending=8,Welcome=9;
        public static readonly int[] Targets={6,20,25,30,20,25,20,0};
        static readonly int[] Material={-1,0,1,2,3,4,5,-1};
        public static int Target(GameSession g,int i)=>i==7?g.TotalUnits:Targets[i];
        public static int Current(GameSession g,int i)
        {
            if(i==0){var paper=g.State.materials[0];return paper.raw+paper.batch+paper.processed;}if(i==7)return g.Processed;
            int index=Material[i];if(index>=g.State.materials.Length)return 0;var m=g.State.materials[index];
            return i==2||i==3?m.raw+m.batch+m.processed:m.processed;
        }
        public static bool Done(GameSession g,int i)=>(g.State.story.questMask&(1<<i))!=0;
        public static int CompletedCount(GameSession g){int n=0;for(int i=0;i<8;i++)if(Done(g,i))n++;return n;}
        public static bool Met(GameSession g,int id)
        {
            if(id==0)return true;if(id==1)return g.Processed>0;
            if(id>=2&&id<=6){int index=id-1;return index<g.State.materials.Length&&g.State.materials[index].unlocked;}
            if(id==7)return g.State.collectorRank>0;if(id==8)return g.State.complete;
            if(id==14)return g.State.purchasedBinsMask!=0;
            if(id==13)return g.State.opening.guided&&g.State.opening.complete;
            if(id>=10&&id<=12)return g.Collected*4>=g.TotalUnits*(id-9);return false;
        }
        public static void Initialize(GameSession g,bool legacy)
        {
            if(g.State.story==null||g.State.story.version==0)
            {
                g.State.story=new NarrativeState{version=1};
                if(legacy){for(int i=0;i<15;i++)if(Met(g,i))g.State.story.seenMask|=1<<i;if(!g.State.complete)g.State.story.pending.Add(Welcome);}
                else g.State.story.pending.Add(Intro);
            }
            Sync(g);
        }
        public static bool Sync(GameSession g)
        {
            var s=g.State.story;if(s==null)return false;bool changed=false;
            for(int i=0;i<8;i++)if(Current(g,i)>=Target(g,i)&&!Done(g,i)){s.questMask|=1<<i;changed=true;}
            if(s.pinnedQuest<0||s.pinnedQuest>7)s.pinnedQuest=0;
            if(Done(g,s.pinnedQuest))for(int i=0;i<8;i++)if(!Done(g,i)){s.pinnedQuest=i;break;}
            if(g.State.complete)
            {
                changed|=s.pending.Count>0||s.activeId>=0||(s.seenMask&(1<<Ending))==0;
                s.seenMask|=1<<Ending;s.pending.Clear();s.activeId=-1;s.page=0;s.activeSeconds=0;return changed;
            }
            if(!s.enabled)
            {
                changed|=s.pending.Count>0||s.activeId>=0;
                foreach(int id in s.pending)s.seenMask|=1<<id;if(s.activeId>=0)s.seenMask|=1<<s.activeId;
                s.pending.Clear();s.activeId=-1;s.page=0;s.activeSeconds=0;
                for(int id=0;id<15;id++)if(Met(g,id))s.seenMask|=1<<id;return changed;
            }
            for(int id=0;id<15;id++)if(id!=Ending&&id!=Welcome&&Met(g,id)&&(s.seenMask&(1<<id))==0&&s.activeId!=id&&!s.pending.Contains(id)){if(id==13){s.pending.Insert(0,id);s.cooldown=0;}else s.pending.Add(id);changed=true;}
            return changed;
        }
        public static bool Begin(GameSession g)
        {
            var s=g.State.story;if(!s.enabled||s.activeId>=0||s.cooldown>0||s.pending.Count==0||g.State.complete)return false;
            s.activeId=s.pending[0];s.pending.RemoveAt(0);s.page=0;s.activeSeconds=0;return true;
        }
        public static void Dismiss(GameSession g)
        {
            var s=g.State.story;if(s.activeId>=0)s.seenMask|=1<<s.activeId;s.activeId=-1;s.page=0;s.activeSeconds=0;s.cooldown=45;
        }
        public static void Advance(GameSession g)
        {
            var s=g.State.story;if(s.activeId<0)return;
            if(s.page+1<NarrativeContent.Load().nodes[s.activeId].pages.Length){s.page++;s.activeSeconds=0;}else Dismiss(g);
        }
        public static void Validate(GameSession g)
        {
            var s=g.State.story;if(s==null||s.version!=1||s.seenMask<0||s.seenMask>=32768||s.questMask<0||s.questMask>255||s.pinnedQuest<0||s.pinnedQuest>7||s.activeId< -1||s.activeId>14||s.page<0||s.pending==null||s.pending.Count>15||!Finite(s.cooldown)||s.cooldown<0||s.cooldown>45||!Finite(s.activeSeconds)||s.activeSeconds<0)throw new InvalidOperationException("Invalid narrative state");
            var unique=new HashSet<int>();foreach(int id in s.pending)if(id<0||id>14||id==s.activeId||!unique.Add(id)||(s.seenMask&(1<<id))!=0)throw new InvalidOperationException("Invalid narrative queue");
            if(s.activeId<0?s.page!=0:s.page>=NarrativeContent.Load().nodes[s.activeId].pages.Length)throw new InvalidOperationException("Invalid dialogue page");
            for(int i=0;i<8;i++)if(Done(g,i)!=(Current(g,i)>=Target(g,i)))throw new InvalidOperationException("Invalid quest progress");
        }
        static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
    }
}
