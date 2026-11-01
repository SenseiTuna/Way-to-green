using UnityEngine;
using UnityEngine.UI;
namespace KiyiDongusu
{
    public sealed class CoastBinFeedback:MonoBehaviour
    {
        GameBootstrap host;int material;Image artwork;Outline outline;float pulse;RectTransform rect;
        public void Initialize(GameBootstrap game,int index,Image image){host=game;material=index;artwork=image;rect=(RectTransform)transform;outline=image.gameObject.AddComponent<Outline>();outline.effectDistance=new Vector2(3,-3);outline.enabled=false;host.Feedback.Notice+=Notice;}
        void Notice(FeedbackNotice n){if(n.Kind==FeedbackKind.Net&&n.Facility==material)pulse=.3f;}
        void Update(){int selected=host.View.DraggingMaterial;bool held=selected>=0,match=selected==material;bool hover=held&&RectTransformUtility.RectangleContainsScreenPoint(rect,Input.mousePosition);bool owned=host.Game.OwnsBin(material);outline.enabled=hover||pulse>0;outline.effectColor=match&&owned?CoastTheme.Sun:new Color(.8f,.25f,.18f);artwork.color=!owned?new Color(.65f,.65f,.65f,.6f):hover&&match?CoastTheme.Flash:Color.white;pulse=Mathf.Max(0,pulse-Time.unscaledDeltaTime);}
        void OnDestroy(){if(host!=null&&host.Feedback!=null)host.Feedback.Notice-=Notice;}
    }
}
