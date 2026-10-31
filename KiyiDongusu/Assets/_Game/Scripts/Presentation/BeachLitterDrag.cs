using UnityEngine;
using UnityEngine.EventSystems;
namespace KiyiDongusu
{
    public sealed class BeachLitterDrag : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        GameBootstrap host;GameView view;RectTransform rect;int material,generation,heldGeneration;Vector2 home,baseHome;float suppressedUntil;
        public bool Dragging {get;private set;}
        int homeSibling;
        public bool AllowClick=>!Dragging&&Time.unscaledTime>suppressedUntil;
        public void Initialize(GameBootstrap game,GameView gameView,int index,Vector2 origin){host=game;view=gameView;material=index;baseHome=home=origin;rect=(RectTransform)transform;}
        public void SetGeneration(int value)
        {
            generation=value;if(Dragging)return;
            // Placement is derived from the saved identity, not unsaved randomness.
            home=baseHome+new Vector2(((value*17+material*7)%41)-20,((value*11+material*3)%31)-15);rect.anchoredPosition=home;
        }
        public void OnBeginDrag(PointerEventData e)
        {
            if(host.Paused||host.Game.State.complete||host.Game.State.materials[material].visible==0)return;
            Dragging=true;heldGeneration=generation;homeSibling=transform.GetSiblingIndex();rect.pivot=Vector2.one*.5f;transform.SetAsLastSibling();OnDrag(e);
        }
        public void OnDrag(PointerEventData e){if(Dragging)rect.position=e.position;}
        public void OnEndDrag(PointerEventData e)
        {
            if(!Dragging)return;Dragging=false;suppressedUntil=Time.unscaledTime+.25f;
            if(!host.Paused&&RectTransformUtility.RectangleContainsScreenPoint(view.NetRect,e.position,e.pressEventCamera))host.NetAction(material,heldGeneration);
            transform.SetSiblingIndex(homeSibling);rect.pivot=new Vector2(0,1);rect.anchoredPosition=home;view.MarkDirty();
        }
        void OnDisable(){Dragging=false;if(rect!=null){rect.pivot=new Vector2(0,1);rect.anchoredPosition=home;}}
    }
}
