using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
namespace KiyiDongusu
{
    public sealed partial class GameView
    {
        public void Initialize(GameBootstrap bootstrap)
        {
            host=bootstrap;ui=new GameUiAssets();art=new GameArt();
            var canvasGo=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasGo.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var canvasRoot=canvasGo.GetComponent<RectTransform>();
            var ground=Panel("OuterBackground",canvasRoot,0,0,0,0,CoastTheme.Paper);ground.anchorMin=Vector2.zero;ground.anchorMax=Vector2.one;ground.sizeDelta=Vector2.zero;
            root=Panel("GameFrame",canvasRoot,0,0,1920,1080,CoastTheme.Clear);root.anchorMin=root.anchorMax=root.pivot=Vector2.one*.5f;root.anchoredPosition=Vector2.zero;
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));events.GetComponent<EventSystem>().sendNavigationEvents=false;
            var scene=Panel("BeachArtwork",root,0,0,1920,1080,CoastTheme.White).GetComponent<Image>();scene.sprite=art.Beach;
            Panel("Header",root,32,24,1856,96,CoastTheme.Paper,true);
            Local("Title",root,58,40,450,64,"Coastal Loop","Kıyı Döngüsü",42,true);
            totals=Label("Processed",root,610,36,430,60,"",22);
            Panel("WorldProgressTrack",root,610,102,340,6,CoastTheme.Track);
            progressFill=Panel("WorldProgress",root,610,102,0,6,CoastTheme.Action).GetComponent<Image>();
            credits=Label("Credits",root,1120,40,490,64,"",42,null,true);
            Button("Pause",root,1650,43,208,58,T("Settings","Ayarlar"),TogglePause,out var settingsText,true);words.Add((settingsText,"Settings","Ayarlar"));

            var dock=Panel("FacilityPanel",root,1450,150,430,790,CoastTheme.Paper,true);
            Local("DockTitle",dock,28,12,374,56,"Recycling","Geri dönüşüm",32,true);
            float top=82;
            for(int f=0;f<(host.corePrototype?1:4);f++)
            {
                int facility=f;float height=f<2?188:150;
                Local("FacilityName"+f,dock,28,top,374,36,new[]{"Paper mill","Plastic workshop","Metal works","Glass studio"}[f],new[]{"Kâğıt merkezi","Plastik merkezi","Metal merkezi","Cam merkezi"}[f],26,true);
                int lane=0;
                for(int i=0;i<G.State.materials.Length;i++)
                {
                    if(G.Definitions.FacilityIndex(i)!=f)continue;
                    var icon=Panel("LaneIcon_"+i,dock,28,top+41+lane*44,28,28,CoastTheme.White).GetComponent<Image>();icon.sprite=art.Waste[i];icon.preserveAspect=true;
                    var name=Label("Lane_"+i,dock,64,top+34+lane*44,168,34,"",22);
                    var detail=Label("LaneDetails_"+i,dock,232,top+34+lane*44,174,34,"",21);
                    Panel("Track_"+i,dock,28,top+70+lane*44,374,5,CoastTheme.Track);
                    var bar=Panel("Progress_"+i,dock,28,top+70+lane*44,0,5,CoastTheme.Action).GetComponent<Image>();
                    lanes.Add((i,name,detail,bar));lane++;
                }
                AddUpgrade(PurchaseKind.Speed,facility,dock,28,top+height-66,178);
                AddUpgrade(PurchaseKind.Capacity,facility,dock,222,top+height-66,180);
                if(f<3)Panel("Rule"+f,dock,28,top+height+1,374,1,CoastTheme.Track);
                top+=height+5;
            }
            role=Label("CenterRole",root,480,262,800,64,"",42,null,true);role.alignment=TextAlignmentOptions.Midline;
            center=Button("CenterAction",root,450,338,840,390,"",host.CenterAction,out var unused);
            center.GetComponent<Image>().color=CoastTheme.Clear;unused.gameObject.SetActive(false);
            pileImage=Panel("PileArtwork",center.transform,0,0,840,390,CoastTheme.White).GetComponent<Image>();pileImage.sprite=art.Pile;pileImage.preserveAspect=true;
            pileText=Label("PileRemaining",root,560,750,610,50,"",27);pileText.alignment=TextAlignmentOptions.Midline;

            NetRect=Panel("SortingNet",root,90,740,270,220,CoastTheme.White);NetRect.GetComponent<Image>().sprite=art.Net;NetRect.GetComponent<Image>().preserveAspect=true;
            Panel("NetSign",root,62,949,360,112,CoastTheme.Paper,true);
            netText=Label("NetLabel",root,62,949,360,112,"",22);netText.alignment=TextAlignmentOptions.Midline;
            float[] px={150,1230,250,1170,990,410},py={305,330,560,614,190,200};
            for(int i=0;i<G.State.materials.Length;i++)
            {
                int index=i;
                var b=Button("WasteToken_"+i,root,px[i],py[i],110,106,"",()=>{var d=litter.Find(v=>v.i==index).drag;if(d!=null&&d.AllowClick)host.TokenAction(index,G.State.materials[index].tokenGeneration);},out var label);
                label.gameObject.SetActive(false);var img=b.GetComponent<Image>();img.sprite=art.Waste[i];img.type=Image.Type.Simple;img.preserveAspect=true;img.color=CoastTheme.White;
                var drag=b.gameObject.AddComponent<BeachLitterDrag>();drag.Initialize(host,this,i,new Vector2(px[i],-py[i]));litter.Add((i,b,drag));
            }
            NetRect.gameObject.AddComponent<CoastNetFeedback>().Initialize(host,root.GetComponentsInChildren<BeachLitterDrag>(true));
            AddUpgrade(PurchaseKind.Collector,0,root,470,934,430);
            AddUpgrade(PurchaseKind.Manual,0,root,930,934,430);
            Panel("GoalSign",root,466,1006,934,50,CoastTheme.Paper,true);
            goal=Label("NextGoal",root,482,1006,904,50,"",22);
            Panel("SaveSign",root,1450,950,430,108,CoastTheme.Paper,true);
            saveText=Label("SaveStatus",root,1466,958,398,96,"",20);
            notice=Label("FeedbackText",root,400,818,1000,70,"",29,CoastTheme.Ink,true);notice.alignment=TextAlignmentOptions.Midline;
            noticeGroup=notice.gameObject.AddComponent<CanvasGroup>();noticeGroup.blocksRaycasts=false;noticeGroup.alpha=0;
            host.Feedback.Notice+=ShowFeedback;
            overlay=Panel("Overlay",root,0,0,1920,1080,CoastTheme.Scrim);overlay.GetComponent<Image>().raycastTarget=true;overlay.gameObject.SetActive(false);
            var cursor=new GameObject("CoastalHandCursor").AddComponent<CoastHandCursor>();cursor.Initialize(host,art);
            Refresh();
        }
    }
}
