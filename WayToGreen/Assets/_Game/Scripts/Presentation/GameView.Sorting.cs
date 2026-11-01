using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace KiyiDongusu
{
    public sealed partial class GameView
    {
        readonly List<(int slot,Button button,BeachLitterDrag drag)> openingPieces=new List<(int,Button,BeachLitterDrag)>();
        readonly List<(RectTransform target,Button purchase,TextMeshProUGUI text,TextMeshProUGUI name)> bins=new List<(RectTransform,Button,TextMeshProUGUI,TextMeshProUGUI)>();
        RectTransform sortingRow;
        public int DraggingMaterial{get{foreach(var item in litter)if(item.drag.Dragging)return item.i;foreach(var item in openingPieces)if(item.drag.Dragging)return 0;return -1;}}
        public int BinAt(Vector2 point,Camera camera){for(int i=0;i<bins.Count;i++)if(bins[i].target.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(bins[i].target,point,camera))return i;return -1;}
        public RectTransform BinRect(int material)=>bins[material].target;
        void InitializeSortingUi()
        {
            NetRect=Panel("SortingNet",root,52,416,260,180,CoastTheme.White);NetRect.GetComponent<Image>().sprite=art.Net;NetRect.GetComponent<Image>().preserveAspect=true;
            Panel("NetSign",root,52,602,260,62,CoastTheme.Paper,true);netText=Label("NetLabel",root,52,602,260,62,"",22);netText.alignment=TextAlignmentOptions.Center;
            sortingRow=Panel("SortingRow",root,0,0,0,244,CoastTheme.Clear);sortingRow.anchorMin=Vector2.zero;sortingRow.anchorMax=new Vector2(1,0);sortingRow.sizeDelta=new Vector2(-512,244);sortingRow.anchoredPosition=new Vector2(32,400);
            for(int i=0;i<G.State.materials.Length;i++)
            {
                int material=i;var target=Panel("SortingBin_"+i,sortingRow,0,0,210,244,CoastTheme.Clear);target.anchorMin=target.anchorMax=new Vector2(.5f,1);
                var icon=Panel("ContainerArtwork",target,10,0,190,170,CoastTheme.White).GetComponent<Image>();icon.sprite=art.Bins[i];icon.preserveAspect=true;
                icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=new Vector2(.5f,1);icon.rectTransform.pivot=new Vector2(.5f,1);icon.rectTransform.anchoredPosition=Vector2.zero;
                var name=Label("BinName_"+i,target,0,164,210,32,"",22,null,true);name.alignment=TextAlignmentOptions.Center;
                var buy=Button("BuyBin_"+i,target,0,200,210,44,"",()=>host.Purchase(PurchaseKind.SortingBin,material),out var caption,true);caption.fontSize=23;caption.alignment=TextAlignmentOptions.Center;buy.gameObject.AddComponent<UpgradeHover>().Initialize(tooltip,PurchaseKind.SortingBin,i);
                target.gameObject.AddComponent<CoastBinFeedback>().Initialize(host,i,icon);
                bins.Add((target,buy,caption,name));
            }
            for(int slot=0;slot<OpeningProgress.PaperCount;slot++)
            {
                int id=slot;var b=Button("OpeningPaper_"+slot,root,0,0,110,106,"",()=>{var item=openingPieces.Find(v=>v.slot==id);if(item.drag.AllowClick)host.OpeningAction(id);},out var label);label.gameObject.SetActive(false);b.GetComponent<Image>().color=CoastTheme.Clear;
                var visual=Panel("OpeningPaperArt",b.transform,0,0,110,106,CoastTheme.White);visual.anchorMin=visual.anchorMax=visual.pivot=Vector2.one*.5f;visual.anchoredPosition=Vector2.zero;var image=visual.GetComponent<Image>();image.sprite=art.Waste[0];image.preserveAspect=true;
                var rect=(RectTransform)b.transform;rect.anchorMin=rect.anchorMax=Vector2.one*.5f;var drag=b.gameObject.AddComponent<BeachLitterDrag>();drag.Initialize(host,this,0,visual,slot);openingPieces.Add((slot,b,drag));
            }
            center.gameObject.AddComponent<CoastMaterialPile>().Initialize(host,art);
        }
        void RefreshSortingUi()
        {
            netText.text=T("Collection net\nFree collection","Toplama ağı\nÜcretsiz toplama");
            bool arranging=DraggingMaterial<0;int count=0,index=0;foreach(var material in G.State.materials)if(material.unlocked&&G.State.opening.complete)count++;float start=-(count*222-12)*.5f;
            for(int i=0;i<bins.Count;i++)
            {
                var entry=bins[i];bool owned=G.OwnsBin(i),unlocked=G.State.materials[i].unlocked&&G.State.opening.complete;if(arranging){entry.target.gameObject.SetActive(unlocked);if(unlocked)entry.target.anchoredPosition=new Vector2(start+index++*222,0);}
                entry.name.text=GameText.Material(i);entry.text.text=owned?T("Ready","Hazır"):Money(G.Cost(PurchaseKind.SortingBin,i))+T(" cr"," kr");entry.purchase.interactable=!owned&&unlocked&&host.CanInteract&&G.State.money>=G.Cost(PurchaseKind.SortingBin,i);
                bool ready=entry.purchase.interactable;entry.purchase.GetComponent<Image>().color=ready?CoastTheme.Action:CoastTheme.Disabled;entry.text.color=ready?CoastTheme.White:CoastTheme.Ink;
            }
            foreach(var item in openingPieces){if(item.drag.Dragging)continue;item.button.gameObject.SetActive(OpeningProgress.Visible(G.State,item.slot));item.drag.SetGeneration(0);item.button.interactable=host.CanInteract;}
        }
    }
}
