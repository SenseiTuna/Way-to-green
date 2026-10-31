using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace KiyiDongusu
{
    public sealed partial class GameView
    {
        readonly List<(int slot,Button button,BeachLitterDrag drag)> openingPieces=new List<(int,Button,BeachLitterDrag)>();
        readonly List<(RectTransform target,Button purchase,TextMeshProUGUI text)> bins=new List<(RectTransform,Button,TextMeshProUGUI)>();
        public int DraggingMaterial{get{foreach(var item in litter)if(item.drag.Dragging)return item.i;foreach(var item in openingPieces)if(item.drag.Dragging)return 0;return -1;}}
        public int BinAt(Vector2 point,Camera camera){for(int i=0;i<bins.Count;i++)if(bins[i].target.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(bins[i].target,point,camera))return i;return -1;}
        public RectTransform BinRect(int material)=>bins[material].target;
        void InitializeSortingUi()
        {
            for(int i=0;i<G.State.materials.Length;i++)
            {
                int material=i;var target=Panel("SortingBin_"+i,root,0,0,198,180,CoastTheme.Clear);target.anchorMin=target.anchorMax=new Vector2(0,0);target.anchoredPosition=new Vector2(48+i*218,348);
                var icon=Panel("ContainerArtwork",target,14,0,170,122,CoastTheme.White).GetComponent<Image>();icon.sprite=art.Bins[i];icon.preserveAspect=true;
                var buy=Button("BuyBin_"+i,target,0,118,198,62,"",()=>host.Purchase(PurchaseKind.SortingBin,material),out var caption,true);caption.fontSize=21;buy.gameObject.AddComponent<UpgradeHover>().Initialize(tooltip,PurchaseKind.SortingBin,i);
                target.gameObject.AddComponent<CoastBinFeedback>().Initialize(host,i,icon);
                bins.Add((target,buy,caption));
            }
            NetRect=bins[0].target; // Legacy development checks use the paper target.
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
            for(int i=0;i<bins.Count;i++)
            {
                var entry=bins[i];bool owned=G.OwnsBin(i),unlocked=G.State.materials[i].unlocked&&G.State.opening.complete;entry.target.gameObject.SetActive(unlocked);
                entry.text.text=GameText.Material(i)+"\n"+(owned?T("Ready","Hazır"):Money(G.Cost(PurchaseKind.SortingBin,i))+T(" cr"," kr"));entry.purchase.interactable=!owned&&unlocked&&host.CanInteract&&G.State.money>=G.Cost(PurchaseKind.SortingBin,i);
            }
            foreach(var item in openingPieces){if(item.drag.Dragging)continue;item.button.gameObject.SetActive(OpeningProgress.Visible(G.State,item.slot));item.drag.SetGeneration(0);item.button.interactable=host.CanInteract;}
        }
    }
}
