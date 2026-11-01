using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace KiyiDongusu
{
    public sealed class CoastPressable : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        GameBootstrap host;bool down,hover,focused;Outline ring;Image focusLine;RectTransform rect;Selectable selectable;
        public void Initialize(GameBootstrap h)
        {
            host=h;rect=(RectTransform)transform;selectable=GetComponent<Selectable>();ring=gameObject.AddComponent<Outline>();ring.effectDistance=new Vector2(3,-3);ring.useGraphicAlpha=false;ring.enabled=false;
            if(GetComponent<Image>().color.a>.05f){var shadow=gameObject.AddComponent<Shadow>();shadow.effectColor=CoastTheme.Shadow;shadow.effectDistance=new Vector2(0,-3);shadow.useGraphicAlpha=true;}
            if(GetComponent<Image>().color.a<.05f)
            {
                var go=new GameObject("FocusLine",typeof(RectTransform),typeof(Image));go.transform.SetParent(transform,false);
                var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=new Vector2(1,0);rect.offsetMin=new Vector2(10,0);rect.offsetMax=new Vector2(-10,4);
                focusLine=go.GetComponent<Image>();focusLine.color=CoastTheme.Ink;focusLine.raycastTarget=false;focusLine.enabled=false;
            }
        }
        public void OnPointerDown(PointerEventData e){if((host.Paused&&name=="CenterAction")||(selectable!=null&&!selectable.IsInteractable()))return;down=true;host.Feedback.Tap();}
        public void OnPointerUp(PointerEventData e){down=false;}
        public void OnPointerEnter(PointerEventData e){hover=true;}
        public void OnPointerExit(PointerEventData e){hover=false;down=false;}
        public void OnSelect(BaseEventData e){focused=true;Focus();}
        public void OnDeselect(BaseEventData e){focused=false;Focus();}
        void Focus(){if(ring==null)return;ring.effectColor=GetComponent<Image>().color==CoastTheme.Action?CoastTheme.Sun:CoastTheme.Ink;ring.enabled=focused&&focusLine==null;if(focusLine!=null)focusLine.enabled=focused;}
        void Update()
        {
            if(host==null)return;bool enabledControl=selectable==null||selectable.IsInteractable();float target=host.Game.State.reduceMotion||!enabledControl?1:down?.96f:hover?1.025f:1;
            SetScaleKeepingCenter(Vector3.Lerp(transform.localScale,Vector3.one*target,1-Mathf.Exp(-30*Time.unscaledDeltaTime)));
            Focus();
        }
        void SetScaleKeepingCenter(Vector3 scale){if(rect==null){transform.localScale=scale;return;}Vector2 local=rect.rect.center+(name=="CenterAction"?new Vector2(0,55):Vector2.zero);Vector3 center=rect.TransformPoint(local);rect.localScale=scale;rect.position+=center-rect.TransformPoint(local);}
        void OnDisable(){down=hover=focused=false;SetScaleKeepingCenter(Vector3.one);Focus();}
    }
}
