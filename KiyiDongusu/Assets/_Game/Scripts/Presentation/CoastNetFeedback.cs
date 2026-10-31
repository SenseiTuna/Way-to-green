using UnityEngine;
using UnityEngine.UI;
namespace KiyiDongusu
{
    public sealed class CoastNetFeedback:MonoBehaviour
    {
        GameBootstrap host;BeachLitterDrag[] litter;RectTransform rect;Outline outline;Image image;float pulse;
        public void Initialize(GameBootstrap game,BeachLitterDrag[] sources)
        {
            host=game;litter=sources;rect=(RectTransform)transform;image=GetComponent<Image>();
            outline=gameObject.AddComponent<Outline>();outline.effectColor=CoastTheme.Sun;outline.effectDistance=new Vector2(4,-4);outline.enabled=false;
            host.Feedback.Notice+=OnNotice;
        }
        void OnNotice(FeedbackNotice notice){if(notice.Kind==FeedbackKind.Net)pulse=.35f;}
        void Update()
        {
            if(host==null)return;bool dragging=false;foreach(var item in litter)dragging|=item!=null&&item.Dragging;
            bool target=dragging&&!host.Paused&&RectTransformUtility.RectangleContainsScreenPoint(rect,Input.mousePosition);
            outline.enabled=target||pulse>0;image.color=target||pulse>0?CoastTheme.Flash:CoastTheme.White;
            pulse=Mathf.Max(0,pulse-Time.unscaledDeltaTime);
            float scale=host.Game.State.reduceMotion?1:1+.06f*Mathf.Sin(Mathf.PI*pulse/.35f);transform.localScale=Vector3.one*scale;
        }
        void OnDestroy(){if(host!=null&&host.Feedback!=null)host.Feedback.Notice-=OnNotice;}
    }
}
