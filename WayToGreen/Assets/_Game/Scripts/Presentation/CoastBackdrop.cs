using UnityEngine;
using UnityEngine.UI;
namespace KiyiDongusu
{
    public sealed class CoastBackdrop:MonoBehaviour
    {
        GameBootstrap host;RawImage image;RectTransform rect;Material material;float waveTime;Vector2 last;
        public float WaveTime=>waveTime;
        public Rect VisibleTexture=>image.uvRect;
        public void Initialize(GameBootstrap game,Texture texture)
        {
            host=game;image=GetComponent<RawImage>();rect=(RectTransform)transform;image.texture=texture;image.raycastTarget=false;
            var shader=Resources.Load<Shader>("Shaders/CoastWater");
            if(shader==null)throw new System.InvalidOperationException("Missing coastal water shader");
            material=new Material(shader);image.material=material;UpdateCrop();
        }
        void UpdateCrop()
        {
            Vector2 size=rect.rect.size;if(size.x<=0||size.y<=0||image.texture==null)return;last=size;
            float view=size.x/size.y,source=image.texture.width/(float)image.texture.height;
            image.uvRect=view>source?new Rect(0,(1-source/view)*.5f,1,source/view):new Rect((1-view/source)*.5f,0,view/source,1);
        }
        void Update()
        {
            if(host==null)return;if(rect.rect.size!=last)UpdateCrop();
            bool moving=!host.Paused&&!host.Game.State.reduceMotion;if(moving)waveTime+=Time.unscaledDeltaTime;
            material.SetFloat("_CoastTime",waveTime);material.SetFloat("_MotionAmount",host.Game.State.reduceMotion?0:1);
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
