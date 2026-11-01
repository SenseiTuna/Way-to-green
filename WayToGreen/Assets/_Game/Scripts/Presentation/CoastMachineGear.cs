using UnityEngine;
using UnityEngine.UI;
namespace KiyiDongusu
{
    public sealed class CoastMachineGear:MaskableGraphic
    {
        GameBootstrap host;int facility;
        public bool Working{get;private set;}
        public float Angle=>transform.localEulerAngles.z;
        public void Initialize(GameBootstrap game,int index){host=game;facility=index;raycastTarget=false;color=CoastTheme.Muted;}
        void Update()
        {
            if(host==null)return;Working=false;
            for(int i=0;i<host.Game.State.materials.Length;i++)if(host.Game.State.materials[i].batch>0&&(facility<0||host.Game.Definitions.FacilityIndex(i)==facility)){Working=true;break;}
            color=Working?CoastTheme.Action:CoastTheme.Muted;
            if(host.Game.State.reduceMotion)transform.localRotation=Quaternion.identity;
            else if(Working&&!host.Paused)transform.Rotate(0,0,-(70+Mathf.Max(0,facility)*11)*Time.unscaledDeltaTime);
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.47f;
            for(int i=0;i<32;i++)
            {
                float a=i*Mathf.PI/16,b=(i+1)*Mathf.PI/16;
                float ra=i%4<2?radius:radius*.79f,rb=(i+1)%4<2?radius:radius*.79f;
                int index=mesh.currentVertCount;mesh.AddVert(new Vector3(Mathf.Cos(a)*ra,Mathf.Sin(a)*ra),color,Vector2.zero);mesh.AddVert(new Vector3(Mathf.Cos(b)*rb,Mathf.Sin(b)*rb),color,Vector2.zero);
                mesh.AddVert(new Vector3(Mathf.Cos(b)*radius*.38f,Mathf.Sin(b)*radius*.38f),color,Vector2.zero);mesh.AddVert(new Vector3(Mathf.Cos(a)*radius*.38f,Mathf.Sin(a)*radius*.38f),color,Vector2.zero);
                mesh.AddTriangle(index,index+1,index+2);mesh.AddTriangle(index+2,index+3,index);
            }
        }
    }
}
