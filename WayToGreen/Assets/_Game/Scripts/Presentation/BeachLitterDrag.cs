using UnityEngine;
using UnityEngine.EventSystems;
namespace KiyiDongusu
{
    public sealed class BeachLitterDrag : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        GameBootstrap host;GameView view;RectTransform rect,artwork;int material,generation,heldGeneration;Vector2 home;float suppressedUntil,visualTime;
        public bool Dragging {get;private set;}
        int homeSibling;
        public int Material=>material;
        int openingSlot=-1;
        public bool AllowClick=>!Dragging&&Time.unscaledTime>suppressedUntil;
        public float DisplayedScale=>artwork==null?1:artwork.localScale.x;
        public void Initialize(GameBootstrap game,GameView gameView,int index,RectTransform visual,int introSlot=-1){host=game;view=gameView;material=index;artwork=visual;rect=(RectTransform)transform;openingSlot=introSlot;}
        public void SetGeneration(int value)
        {
            generation=value;if(Dragging)return;
            // Placement is derived from the saved identity, not unsaved randomness.
            var m=host.Game.State.materials[material];int x=m.scatterX,y=m.scatterY;if(openingSlot>=0)OpeningProgress.Position(host.Game.State,openingSlot,out x,out y);else if(x<340&&y>280)x=360+(material%2)*110;home=new Vector2(x-960,540-Mathf.Min(y,550));rect.anchoredPosition=home;
        }
        public void OnBeginDrag(PointerEventData e)
        {
            if(!host.CanInteract||(openingSlot>=0?!OpeningProgress.Visible(host.Game.State,openingSlot):host.Game.State.materials[material].visible==0))return;
            Dragging=true;heldGeneration=generation;homeSibling=transform.GetSiblingIndex();rect.pivot=Vector2.one*.5f;transform.SetAsLastSibling();OnDrag(e);
        }
        public void OnDrag(PointerEventData e){if(Dragging)rect.position=e.position;}
        void LateUpdate()
        {
            if(artwork==null)return;float scale=view.PerspectiveScaleAt(rect.TransformPoint(rect.rect.center));artwork.localScale=Vector3.one*scale;
            if(!host.Paused&&!host.Game.State.reduceMotion)visualTime+=Time.unscaledDeltaTime;
            bool afloat=!Dragging&&home.y>220&&!host.Game.State.reduceMotion;
            artwork.anchoredPosition=afloat?new Vector2(0,Mathf.Sin(visualTime*1.4f+material)*2):Vector2.zero;
            artwork.localRotation=Quaternion.Euler(0,0,afloat?Mathf.Sin(visualTime+material)*2:0);
        }
        public void OnEndDrag(PointerEventData e)
        {
            if(!Dragging)return;Dragging=false;suppressedUntil=Time.unscaledTime+.25f;
            int target=view.BinAt(e.position,e.pressEventCamera);if(!host.Paused){if(target>=0)host.BinAction(material,heldGeneration,target,openingSlot);else if(RectTransformUtility.RectangleContainsScreenPoint(view.NetRect,e.position,e.pressEventCamera))host.FreeNetAction(material,heldGeneration,openingSlot);}
            transform.SetSiblingIndex(homeSibling);rect.pivot=new Vector2(0,1);rect.anchoredPosition=home;view.MarkDirty();
        }
        void OnDisable(){Dragging=false;if(rect!=null){rect.pivot=new Vector2(0,1);rect.anchoredPosition=home;}}
    }
}
