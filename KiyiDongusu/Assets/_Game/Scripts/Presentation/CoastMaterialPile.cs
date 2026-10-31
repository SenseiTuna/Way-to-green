using UnityEngine;
using UnityEngine.UI;
namespace KiyiDongusu
{
    public sealed class CoastMaterialPile:MonoBehaviour
    {
        GameBootstrap host;GameArt art;Image[] pieces=new Image[24];GameObject layer;
        public int VisiblePieces{get;private set;}
        public int Material{get;private set;}=-1;
        public void Initialize(GameBootstrap game,GameArt assets)
        {
            host=game;art=assets;layer=new GameObject("MaterialPilePieces",typeof(RectTransform));layer.transform.SetParent(transform,false);var root=(RectTransform)layer.transform;root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            for(int i=0;i<pieces.Length;i++){var go=new GameObject("PilePiece_"+i,typeof(RectTransform),typeof(Image));go.transform.SetParent(root,false);var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;float radius=Mathf.Sqrt((i+1)/24f),angle=i*2.399963f;rect.sizeDelta=new Vector2(175,140);rect.anchoredPosition=new Vector2(Mathf.Cos(angle)*radius*280,55+Mathf.Sin(angle)*radius*65);rect.localRotation=Quaternion.Euler(0,0,(i*37%67)-33);pieces[i]=go.GetComponent<Image>();pieces[i].preserveAspect=true;pieces[i].raycastTarget=false;}
        }
        void LateUpdate(){var s=host.Game.State;bool shown=s.opening.complete&&s.pileRemaining>0&&!s.complete;layer.SetActive(shown);VisiblePieces=0;if(!shown)return;Material=s.pileMaterial;float f=s.pileRemaining/(float)host.Game.Definitions.sources.pile_units;int count=f>.75f?24:f>.5f?18:f>.25f?12:6;for(int i=0;i<pieces.Length;i++){pieces[i].gameObject.SetActive(i<count);if(i<count){VisiblePieces++;pieces[i].sprite=art.Waste[Material];}}}
    }
}
