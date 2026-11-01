Shader "KiyiDongusu/CoastWater"
{
    Properties
    {
        [PerRendererData] _MainTex("Beach",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        _CoastTime("Wave time",Float)=0
        _MotionAmount("Motion",Float)=1
        _StencilComp("Stencil Comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255
        _StencilReadMask("Stencil Read Mask",Float)=255
        _ColorMask("Color Mask",Float)=15
    }
    SubShader
    {
        Tags{"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False"}
        Stencil{Ref[_Stencil] Comp[_StencilComp] Pass[_StencilOp] ReadMask[_StencilReadMask] WriteMask[_StencilWriteMask]}
        Cull Off Lighting Off ZWrite Off ZTest[unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha ColorMask[_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata{float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f{float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float4 world:TEXCOORD1;};
            sampler2D _MainTex;fixed4 _Color;float _CoastTime,_MotionAmount;float4 _ClipRect;
            v2f vert(appdata v){v2f o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 original=tex2D(_MainTex,i.uv);
                float sea=smoothstep(.02,.17,min(original.g-original.r,original.b-original.r));
                float whiteFoam=smoothstep(.79,.94,min(original.r,min(original.g,original.b)))*smoothstep(.60+.28*i.uv.x,.66+.28*i.uv.x,i.uv.y);
                float water=saturate(sea+whiteFoam)*_MotionAmount;
                float t=_CoastTime;
                float2 offset=float2(sin(i.uv.y*82+t*.8)+sin(i.uv.x*31-t*.36)*.4,cos(i.uv.x*68+t*.65)*.55)*.0022*water;
                fixed4 c=tex2D(_MainTex,saturate(i.uv+offset))*i.color;
                c.rgb+=water*(.004+.004*sin(i.uv.x*110+i.uv.y*74-t*1.1))*fixed3(.25,.8,1);
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
