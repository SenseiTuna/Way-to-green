using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
namespace KiyiDongusu
{
    public sealed class CoastUpgradeTooltip:MonoBehaviour
    {
        GameBootstrap host;RectTransform canvas,surface;TextMeshProUGUI heading,body;UpgradeHover source;
        public bool Visible=>surface!=null&&surface.gameObject.activeSelf;
        public string Description=>body==null?"":body.text;
        public void Initialize(GameBootstrap game,RectTransform parent,GameUiAssets assets)
        {
            host=game;canvas=parent;surface=(RectTransform)transform;surface.SetParent(canvas,false);surface.anchorMin=surface.anchorMax=Vector2.one*.5f;surface.pivot=new Vector2(0,1);surface.sizeDelta=new Vector2(540,360);
            var image=gameObject.AddComponent<Image>();image.sprite=assets.Surface;image.type=Image.Type.Sliced;image.color=CoastTheme.SolidPaper;image.raycastTarget=false;
            var shadow=gameObject.AddComponent<Shadow>();shadow.effectColor=CoastTheme.Shadow;shadow.effectDistance=new Vector2(0,-6);
            heading=Text("UpgradeHintTitle",assets.Bold,26,20,14,500,48);body=Text("UpgradeHintBody",assets.Regular,23,20,70,500,310);surface.gameObject.SetActive(false);
        }
        TextMeshProUGUI Text(string name,TMP_FontAsset font,int size,int x,int y,int width,int height)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(surface,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);
            var text=go.GetComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.color=CoastTheme.Ink;text.raycastTarget=false;text.richText=false;text.textWrappingMode=TextWrappingModes.Normal;return text;
        }
        public void Show(UpgradeHover item){source=item;surface.gameObject.SetActive(true);surface.SetAsLastSibling();Update();}
        public void Hide(UpgradeHover item){if(source!=item)return;source=null;surface.gameObject.SetActive(false);}
        void Update()
        {
            if(source==null||host.Paused||host.View.ModalOpen||!source.gameObject.activeInHierarchy){if(surface!=null)surface.gameObject.SetActive(false);return;}
            heading.text=UpgradeDescription.Title(source.Kind,source.Facility);body.text=UpgradeDescription.Body(host.Game,source.Kind,source.Facility);
            float height=Mathf.Clamp(body.GetPreferredValues(body.text,500,1000).y+92,200,470);surface.sizeDelta=new Vector2(540,height);body.rectTransform.sizeDelta=new Vector2(500,height-84);
            var r=(RectTransform)source.transform;Vector2 point=canvas.InverseTransformPoint(r.TransformPoint(r.rect.center));
            float x=point.x>0?point.x-r.rect.width*.5f-560:point.x+r.rect.width*.5f+20;
            float y=point.y<canvas.rect.yMin+280?point.y+height+r.rect.height*.5f+20:point.y+r.rect.height*.5f;surface.anchoredPosition=new Vector2(Mathf.Clamp(x,canvas.rect.xMin+20,canvas.rect.xMax-560),Mathf.Clamp(y,canvas.rect.yMin+height+20,canvas.rect.yMax-20));
        }
    }
}
