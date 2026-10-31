using UnityEngine;
namespace KiyiDongusu
{
    public sealed class CoastShoreReveal:MonoBehaviour
    {
        GameBootstrap host;CanvasGroup before;float elapsed;
        public float BeforeAlpha=>before==null?0:before.alpha;
        public void Initialize(GameBootstrap game,CanvasGroup layer){host=game;before=layer;before.blocksRaycasts=false;before.interactable=false;before.alpha=host.Game.State.reduceMotion?0:1;}
        void Update()
        {
            if(before==null)return;if(host.Game.State.reduceMotion){before.alpha=0;return;}
            elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01((elapsed-.65f)/2.3f);before.alpha=1-t*t*(3-2*t);
        }
    }
}
