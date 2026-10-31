using UnityEngine;
namespace KiyiDongusu
{
    public sealed partial class GameView
    {
        public RectTransform FrameRect=>root;
        public bool ModalOpen=>overlay!=null&&overlay.gameObject.activeSelf;
        public float PerspectiveScaleAt(Vector2 screenPoint)
        {
            Vector2 local=root.InverseTransformPoint(screenPoint);
            Vector2 pile=root.InverseTransformPoint(center.transform.TransformPoint(((RectTransform)center.transform).rect.center));
            return Mathf.Lerp(.50f,1.32f,Mathf.InverseLerp(360,-240,local.y-pile.y));
        }
        void Anchor(string name,Vector2 anchor,float x,float y)
        {
            var child=root.Find(name) as RectTransform;if(child==null)return;child.anchorMin=child.anchorMax=anchor;child.anchoredPosition=new Vector2(x,y);
        }
        void ArrangeGameplay()
        {
            var header=(RectTransform)root.Find("Header");header.anchorMin=new Vector2(0,1);header.anchorMax=Vector2.one;header.sizeDelta=new Vector2(-64,96);
            Anchor("Pause",Vector2.one,-270,-43);Anchor("QuestLog",Vector2.one,-480,-43);Anchor("Credits",Vector2.one,-940,-40);
            Anchor("Processed",new Vector2(.5f,1),-400,-36);Anchor("WorldProgressTrack",new Vector2(.5f,1),-400,-102);Anchor("WorldProgress",new Vector2(.5f,1),-400,-102);
            Anchor("FacilityPanel",Vector2.one,-470,-150);Anchor("SaveSign",new Vector2(1,0),-470,130);Anchor("SaveStatus",new Vector2(1,0),-454,122);
            Anchor("CenterRole",Vector2.one*.5f,-624,278);Anchor("CenterAction",Vector2.one*.5f,-644,202);Anchor("PileRemaining",Vector2.one*.5f,-529,-98);
            Anchor("Collector_0",new Vector2(.5f,0),-669,146);Anchor("Manual_0",new Vector2(.5f,0),-209,146);
            Anchor("GoalSign",new Vector2(.5f,0),-691,74);Anchor("NextGoal",new Vector2(.5f,0),-675,74);Anchor("FeedbackText",new Vector2(.5f,1),-724,-130);
            Anchor("SortingNet",new Vector2(0,.5f),52,124);Anchor("NetSign",new Vector2(0,.5f),52,-62);Anchor("NetLabel",new Vector2(0,.5f),52,-62);
            overlay.anchorMin=Vector2.zero;overlay.anchorMax=Vector2.one;overlay.offsetMin=overlay.offsetMax=Vector2.zero;
        }
        void PositionUi(RectTransform rect,Transform parent,float x,float y,float w,float h)
        {
            bool modal=parent==overlay;rect.anchorMin=rect.anchorMax=modal?Vector2.one*.5f:new Vector2(0,1);rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=modal?new Vector2(x-960,540-y):new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);
        }
    }
}
