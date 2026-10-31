using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Text=TMPro.TextMeshProUGUI;

namespace KiyiDongusu
{
    public sealed partial class GameView : MonoBehaviour
    {
        GameBootstrap host; GameSession G=>host.Game;
        GameUiAssets ui; GameArt art;
        CoastUpgradeTooltip tooltip;
        RectTransform root, overlay; public RectTransform NetRect { get; private set; }
        Text credits, goal, totals, pileText, role, notice, netText, saveText;
        Image pileImage, progressFill; Button center;
        CanvasGroup noticeGroup; float noticeTime; bool dirty=true,resultShown; float renderTimer;
        readonly List<Button> buttons=new List<Button>();
        readonly List<(int i,Text name,Text detail,Image bar)> lanes=new List<(int,Text,Text,Image)>();
        readonly List<(int i,Button button,BeachLitterDrag drag)> litter=new List<(int,Button,BeachLitterDrag)>();
        readonly List<(PurchaseKind kind,int facility,Button button,Text text)> upgrades=new List<(PurchaseKind,int,Button,Text)>();
        readonly List<(Text text,string en,string tr)> words=new List<(Text,string,string)>();
        string T(string en,string tr)=>GameText.T(en,tr);
        string Money(long n)=>(n/100.0).ToString("N1",GameText.Culture);
        public void MarkDirty(){dirty=true;}
        RectTransform Panel(string name,Transform parent,float x,float y,float w,float h,Color color,bool rounded=false)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>();PositionUi(r,parent,x,y,w,h);
            var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;
            if(rounded){image.sprite=ui.Surface;image.type=Image.Type.Sliced;}return r;
        }
        Text Label(string name,Transform parent,float x,float y,float w,float h,string value,int size=24,Color? color=null,bool bold=false)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>();PositionUi(r,parent,x,y,w,h);
            var t=go.GetComponent<Text>();t.font=bold?ui.Bold:ui.Regular;t.text=value;t.fontSize=size;t.color=color??CoastTheme.Ink;t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;
            t.enableAutoSizing=false;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Ellipsis;t.richText=false;t.extraPadding=true;
            return t;
        }
        Text Local(string name,Transform p,float x,float y,float w,float h,string en,string tr,int size=24,bool bold=false)
        {var t=Label(name,p,x,y,w,h,T(en,tr),size,null,bold);words.Add((t,en,tr));return t;}
        Button Button(string name,Transform parent,float x,float y,float w,float h,string value,Action action,out Text label,bool quiet=false)
        {
            var r=Panel(name,parent,x,y,w,h,quiet?CoastTheme.Disabled:CoastTheme.Action,true);r.GetComponent<Image>().raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=r.GetComponent<Image>();b.navigation=new Navigation{mode=Navigation.Mode.None};
            var colors=b.colors;colors.normalColor=colors.disabledColor=CoastTheme.White;colors.highlightedColor=CoastTheme.Hover;colors.selectedColor=CoastTheme.Sun;b.colors=colors;
            b.onClick.AddListener(()=>action());buttons.Add(b);
            label=Label(name+"Text",r,12,3,w-24,h-6,value,24,quiet?CoastTheme.Ink:CoastTheme.White,true);label.alignment=TextAlignmentOptions.Midline;
            r.gameObject.AddComponent<CoastPressable>().Initialize(host);
            return b;
        }
        void AddUpgrade(PurchaseKind kind,int f,Transform p,float x,float y,float width)
        {var b=Button(kind+"_"+f,p,x,y,width,64,"",()=>host.Purchase(kind,f),out var t);t.fontSize=22;b.gameObject.AddComponent<UpgradeHover>().Initialize(tooltip,kind,f);upgrades.Add((kind,f,b,t));}
        void Update()
        {
            noticeTime=Mathf.Max(0,noticeTime-Time.unscaledDeltaTime);noticeGroup.alpha=Mathf.Clamp01(noticeTime/.25f);
            if(Input.GetKeyDown(KeyCode.Tab))
            {
                var visible=new List<Selectable>(root.GetComponentsInChildren<Selectable>());visible.RemoveAll(b=>!b.IsInteractable()||!b.gameObject.activeInHierarchy||(overlay.gameObject.activeSelf&&!b.transform.IsChildOf(overlay)));
                int index=visible.FindIndex(b=>EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject==b.gameObject);
                if(visible.Count>0)EventSystem.current.SetSelectedGameObject(visible[(index+1)%visible.Count].gameObject);
            }
            if(Input.GetKeyDown(KeyCode.Return)&&EventSystem.current!=null)
            {var go=EventSystem.current.currentSelectedGameObject;var b=go==null?null:go.GetComponent<Button>();if(b!=null&&b.interactable&&b.gameObject.activeInHierarchy)b.onClick.Invoke();}
            if(EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject!=null)
            {var slider=EventSystem.current.currentSelectedGameObject.GetComponent<Slider>();if(slider!=null){if(Input.GetKeyDown(KeyCode.LeftArrow))slider.value-=.05f;if(Input.GetKeyDown(KeyCode.RightArrow))slider.value+=.05f;}}
            renderTimer+=Time.unscaledDeltaTime;if(dirty&&renderTimer>=.08f){Refresh();dirty=false;renderTimer=0;}
            if(G.State.complete&&!resultShown){resultShown=true;ShowResult();}
        }
        void ShowFeedback(FeedbackNotice message)
        {notice.text=message.Text;noticeTime=1.4f;noticeGroup.alpha=1;}
        void Refresh()
        {
            foreach(var item in words)if(item.text!=null)item.text.text=T(item.en,item.tr);
            credits.text=Money(G.State.money)+T(" credits"," kredi");
            totals.text=T("Cleaned ","Toplandı ")+(100f*G.Collected/G.TotalUnits).ToString("0",GameText.Culture)+"%\n"+T("Recycled ","İşlenen ")+G.Processed+" / "+G.TotalUnits;
            progressFill.rectTransform.sizeDelta=new Vector2(340f*G.Collected/G.TotalUnits,6);
            role.text=G.State.pileRemaining>0?GameText.Material(G.State.pileMaterial)+T(" pile"," yığını"):T("Give the machines a hand","İşlemeye yardım et");
            pileText.text=G.State.pileRemaining>0?G.State.pileRemaining+" / "+G.Definitions.sources.pile_units+T(" remaining"," kaldı"):G.WorldRemaining==0?T("Finishing the last batches","Son partiler işleniyor"):T("Next pile in ","Yeni parti: ")+G.State.refillRemaining.ToString("0.0",GameText.Culture)+T("s"," sn");
            pileImage.gameObject.SetActive(!G.State.complete);
            float fraction=G.State.pileRemaining/(float)G.Definitions.sources.pile_units;
            bool collecting=G.State.pileRemaining>0;
            int facility=0;for(int i=0;i<G.State.materials.Length;i++)if(G.State.materials[i].batch>0){facility=G.Definitions.FacilityIndex(i);break;}
            pileImage.sprite=collecting?art.PileAt(fraction):art.Facilities[facility];
            pileImage.rectTransform.sizeDelta=collecting?new Vector2(840,390):new Vector2(460,300);
            pileImage.rectTransform.anchoredPosition=collecting?Vector2.zero:new Vector2(190,-45);
            center.interactable=host.CanInteract;
            netText.text=T("Sorting net","Ayırma ağı")+"\n"+(G.NetDelivered==0?T("Drag loose litter here","Tekil çöpleri buraya sürükle"):T("Sorted: ","Ayrılan: ")+G.NetDelivered)+"\n"+T("+0.50 after recycling","İşlenince +0,50 kredi");
            foreach(var lane in lanes)
            {
                var m=G.State.materials[lane.i];var d=G.Definitions.materials[lane.i];lane.name.text=GameText.Material(lane.i);
                lane.name.color=m.unlocked?CoastTheme.Ink:CoastTheme.Muted;
                lane.detail.text=!m.unlocked?T("Unlock: ","Açılış: ")+d.unlock_processed_total_units:m.batch>0?m.raw+T(" raw  "," ham  ")+((m.batch-m.work)/G.Processing.Rate(lane.i)).ToString("0.0",GameText.Culture)+T("s"," sn"):m.raw>0?T("Queued: ","Sırada: ")+m.raw:T("Waiting for stock","Malzeme bekliyor");
                lane.bar.rectTransform.sizeDelta=new Vector2(m.batch>0?(float)(374*m.work/m.batch):0,5);
            }
            foreach(var item in litter)
            {
                var m=G.State.materials[item.i];if(item.drag.Dragging)continue;
                item.button.gameObject.SetActive(m.unlocked&&m.visible>0);item.drag.SetGeneration(m.tokenGeneration);item.button.interactable=host.CanInteract;
            }
            foreach(var item in upgrades)
            {
                int rank=G.Rank(item.kind,item.facility);long cost=G.Cost(item.kind,item.facility);
                bool locked=(item.kind==PurchaseKind.Speed||item.kind==PurchaseKind.Capacity)&&!G.FacilityUnlocked(item.facility);
                string name=item.kind==PurchaseKind.Speed?T("Speed","Hız"):item.kind==PurchaseKind.Capacity?T("Batch","Parti"):item.kind==PurchaseKind.Collector?T("Collection crew","Toplama ekibi"):T("Stronger hands","Toplama gücü");
                item.text.text=name+" "+rank+"\n"+(locked?T("Locked","Kilitli"):cost<0?T("Max","Maks."):Money(cost));
                if(item.kind==PurchaseKind.Collector||item.kind==PurchaseKind.Manual)
                {
                    double[] values=item.kind==PurchaseKind.Collector?G.Definitions.collector.nominal_units_per_second_by_rank:G.Definitions.manual_collection_upgrade.work_per_click_by_rank;
                    double next=values[Mathf.Min(rank+1,values.Length-1)]*G.Definitions.tempo.multiplier;
                    string effect=next.ToString("0.#",GameText.Culture)+(item.kind==PurchaseKind.Collector?T("/s","/sn"):T("/click","/tık"));
                    item.text.text=name+" "+rank+"\n"+(cost<0?T("Max: ","Maks: ")+effect:T("Next ","Sonraki ")+effect+"   "+Money(cost)+T(" cr"," kr"));
                }
                bool available=!locked&&cost>=0&&G.State.money>=cost&&!G.State.complete;
                item.button.interactable=available&&host.CanInteract;item.button.GetComponent<Image>().color=available?CoastTheme.Action:CoastTheme.Disabled;item.text.color=available?CoastTheme.White:CoastTheme.Muted;
            }
            goal.text=TrackedQuest();saveText.text=host.Saves.LastMessage;RefreshQuestLog();
        }
        string NextGoal()
        {
            for(int i=0;i<G.State.materials.Length;i++)if(!G.State.materials[i].unlocked)return T("Next free line: ","Sıradaki ücretsiz hat: ")+GameText.Material(i)+"  "+G.Processed+" / "+G.Definitions.materials[i].unlock_processed_total_units;
            return T("Every piece counts. Collected ","Her parça önemli. Toplanan ")+G.Collected+" / "+G.TotalUnits;
        }
        void OnDestroy(){if(host!=null&&host.Feedback!=null)host.Feedback.Notice-=ShowFeedback;if(tooltip!=null)Destroy(tooltip.gameObject);ui?.Dispose();}
    }
}
