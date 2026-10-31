using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Text=TMPro.TextMeshProUGUI;
namespace KiyiDongusu
{
    public sealed partial class GameView
    {
        RectTransform dialogue;Image mira;Text dialogueName,dialogueText,dialogueSource,dialoguePage,dialogueNext,questDetails,questSource;
        bool questLogOpen;int selectedQuest;string questUrl;
        readonly List<(int id,Text text)> questRows=new List<(int,Text)>();
        public bool DialogueVisible=>dialogue!=null&&dialogue.gameObject.activeSelf;
        public bool AnyLitterDragging{get{foreach(var item in litter)if(item.drag!=null&&item.drag.Dragging)return true;return false;}}
        void InitializeNarrativeUi()
        {
            dialogue=Panel("MiraDialogue",root,0,0,0,300,CoastTheme.SolidPaper,true);
            dialogue.anchorMin=Vector2.zero;dialogue.anchorMax=new Vector2(1,0);dialogue.sizeDelta=new Vector2(-64,300);dialogue.anchoredPosition=new Vector2(32,320);
            dialogue.GetComponent<Image>().raycastTarget=true;
            mira=Panel("MiraPortrait",dialogue,-20,-200,420,500,CoastTheme.White).GetComponent<Image>();mira.preserveAspect=true;mira.raycastTarget=true;
            dialogueName=Label("MiraName",dialogue,420,20,850,48,"",32,null,true);
            dialoguePage=Label("DialoguePage",dialogue,0,20,115,48,"",22);dialoguePage.alignment=TextAlignmentOptions.MidlineRight;DialogueRight(dialoguePage.rectTransform,-140,20);
            dialogueText=Label("DialogueBody",dialogue,420,78,0,122,"",28);dialogueText.rectTransform.anchorMax=new Vector2(1,1);dialogueText.rectTransform.sizeDelta=new Vector2(-460,122);
            dialogueSource=Label("DialogueSource",dialogue,420,232,0,48,"",18);dialogueSource.rectTransform.anchorMax=new Vector2(1,1);dialogueSource.rectTransform.sizeDelta=new Vector2(-880,48);
            var skipButton=Button("DialogueSkip",dialogue,0,232,140,54,"",()=>host.Story.Skip(),out var skip,true);DialogueRight((RectTransform)skipButton.transform,-420,232);words.Add((skip,"Skip","Atla"));
            var nextButton=Button("DialogueNext",dialogue,0,232,220,54,"",()=>host.Story.Advance(),out dialogueNext);DialogueRight((RectTransform)nextButton.transform,-260,232);
            HideDialogue();
        }
        static void DialogueRight(RectTransform rect,float x,float y){rect.anchorMin=rect.anchorMax=Vector2.one;rect.anchoredPosition=new Vector2(x,-y);}
        public void HideDialogue(){if(dialogue!=null)dialogue.gameObject.SetActive(false);}
        public void RenderDialogue(int id,int page,float arrival)
        {
            var node=NarrativeContent.Load().nodes[id];var p=node.pages[page];var fact=NarrativeContent.Load().Fact(p.fact);
            dialogue.gameObject.SetActive(true);dialogue.anchoredPosition=new Vector2(32,320-(1-arrival)*48);
            mira.sprite=art.Mira[Mathf.Clamp(p.pose,0,2)];dialogueName.text=T("Mira / Shore volunteer","Mira / Kıyı gönüllüsü");
            dialogueText.text=NarrativeContent.Resolve(T(p.en,p.tr),G);dialoguePage.text=(page+1)+" / "+node.pages.Length;
            dialogueSource.text=fact==null?T("Shore story","Sahil hikâyesi"):fact.source=="Game mechanics"?T("Game mechanics","Oyun kuralları"):fact.source;
            dialogueNext.text=page+1==node.pages.Length?T("Let's go","Devam edelim"):T("Continue","Devam");
        }
        string QuestProgressText(int i)
        {
            if(NarrativeProgress.Done(G,i))return T("Completed / Read note","Tamamlandı / Notu oku");
            int unlock=QuestUnlock(i);if(G.Processed<unlock)return T("Free line at ","Ücretsiz hat: ")+unlock+T(" recycled"," işlenen");
            return System.Math.Min(NarrativeProgress.Current(G,i),NarrativeProgress.Target(G,i))+" / "+NarrativeProgress.Target(G,i)+(G.State.story.pinnedQuest==i?T(" / Tracking"," / İzleniyor"):T(" / Track"," / İzle"));
        }
        int QuestUnlock(int i){int[] index={-1,0,1,2,3,4,5,-1};return index[i]>=0&&index[i]<G.State.materials.Length?G.Definitions.materials[index[i]].unlock_processed_total_units:0;}
        public void ShowQuestLog(bool completed=false,int selected=-1)
        {
            ClearOverlay(T("Shore tasks","Sahil görevleri"),T("Every collected piece counts, including your crew's work. Recycling keeps running here.","Ekibin topladıkları da sayılır. Bu ekran açıkken işleme sürer."),true);
            questLogOpen=true;host.Paused=false;selectedQuest=selected>=0?selected:G.State.story.pinnedQuest;
            Button("QuestActiveTab",overlay,390,270,550,60,T("Active","Aktif"),()=>ShowQuestLog(false),out _,true);
            Button("QuestCompletedTab",overlay,970,270,550,60,T("Completed","Tamamlanan")+" "+NarrativeProgress.CompletedCount(G)+" / 8",()=>ShowQuestLog(true),out _,true);
            int row=0;
            for(int i=0;i<8;i++)
            {
                if(NarrativeProgress.Done(G,i)!=completed)continue;int id=i;var q=NarrativeContent.Load().quests[i];
                Button("QuestRow_"+i,overlay,390+(row%2)*580,348+(row/2)*74,550,64,"",()=>{if(!NarrativeProgress.Done(G,id)){G.State.story.pinnedQuest=id;host.MarkNarrativeChanged();}ShowQuestLog(completed,id);},out var text,true);
                text.fontSize=22;questRows.Add((i,text));row++;
            }
            if(row==0)Label("QuestEmpty",overlay,390,360,1130,80,T("Your first completed task will appear here.","İlk tamamlanan görevin burada görünecek."),26);
            questDetails=Label("QuestDetails",overlay,390,654,1130,88,"",22);
            questSource=Label("QuestSource",overlay,390,754,800,48,"",18);
            Button("QuestSourceLink",overlay,1240,754,280,48,T("Open source","Kaynağı aç"),()=>{if(!string.IsNullOrEmpty(questUrl))Application.OpenURL(questUrl);},out _,true);
            Button("QuestClose",overlay,390,846,1130,62,T("Back to the shore","Sahile dön"),()=>{if(G.State.complete)ShowResult();else Resume();},out _);
            RefreshQuestLog();
        }
        void RefreshQuestLog()
        {
            if(!questLogOpen)return;var data=NarrativeContent.Load();
            foreach(var row in questRows)if(row.text!=null){var q=data.quests[row.id];row.text.text=T(q.en,q.tr)+"\n"+QuestProgressText(row.id);}
            var selected=data.quests[selectedQuest];var fact=data.Fact(selected.fact);bool done=NarrativeProgress.Done(G,selectedQuest);
            questDetails.text=NarrativeContent.Resolve(T(selected.descriptionEn,selected.descriptionTr),G)+(done&&fact!=null?"\n"+NarrativeContent.Resolve(T(fact.en,fact.tr),G):"\n"+QuestProgressText(selectedQuest));
            questSource.text=done&&fact!=null?(fact.source=="Game mechanics"?T("Game mechanics","Oyun kuralları"):fact.source):T("Complete this task to read Mira's note.","Mira'nın notunu okumak için görevi tamamla.");
            questUrl=done&&fact!=null?fact.url:"";var link=overlay.Find("QuestSourceLink");if(link!=null)link.GetComponent<Button>().interactable=!string.IsNullOrEmpty(questUrl);
        }
        string TrackedQuest()
        {
            int i=G.State.story.pinnedQuest;var q=NarrativeContent.Load().quests[i];int unlock=QuestUnlock(i);
            if(G.Processed<unlock)return T("Next free line: recycle ","Sıradaki ücretsiz hat: ")+G.Processed+" / "+unlock;
            return T(q.en,q.tr)+"  "+System.Math.Min(NarrativeProgress.Current(G,i),NarrativeProgress.Target(G,i))+" / "+NarrativeProgress.Target(G,i);
        }
    }
}
