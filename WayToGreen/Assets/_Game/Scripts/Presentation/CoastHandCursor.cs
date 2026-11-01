using UnityEngine;
using UnityEngine.UI;
namespace KiyiDongusu
{
    public sealed class CoastHandCursor : MonoBehaviour
    {
        GameBootstrap host;GameArt art;Image image;RectTransform hand;bool focused=true;
        public void Initialize(GameBootstrap game,GameArt artwork)
        {
            host=game;art=artwork;var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=1000;
            var go=new GameObject("Hand",typeof(RectTransform),typeof(Image));go.transform.SetParent(transform,false);image=go.GetComponent<Image>();image.raycastTarget=false;image.preserveAspect=true;
            hand=(RectTransform)go.transform;hand.anchorMin=hand.anchorMax=Vector2.zero;hand.sizeDelta=new Vector2(76,76);hand.pivot=new Vector2(.65f,.84f);image.sprite=art.HandOpen;
        }
        void LateUpdate()
        {
            if(host==null||image==null)return;bool inside=Input.mousePosition.x>=0&&Input.mousePosition.y>=0&&Input.mousePosition.x<Screen.width&&Input.mousePosition.y<Screen.height;
            bool show=Application.isFocused&&focused&&inside&&art.HandOpen!=null;Cursor.visible=!show;image.enabled=show;
            bool held=Input.GetMouseButton(0);image.sprite=held?art.HandPinch:art.HandOpen;
            hand.pivot=held?new Vector2(.72f,.68f):new Vector2(.65f,.84f);hand.anchoredPosition=Input.mousePosition;
        }
        void OnApplicationFocus(bool value){focused=value;if(!value)Cursor.visible=true;}
        void OnDestroy(){Cursor.visible=true;}
    }
}
