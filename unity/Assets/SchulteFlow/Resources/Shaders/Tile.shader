Shader "Schulte/Tile"
{
    Properties { _Color ("Color", Color) = (0.10,0.19,0.21,1) _Accent ("Accent", Float) = 0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 vertex:SV_POSITION; float3 normal:TEXCOORD0; float3 world:TEXCOORD1; };
            fixed4 _Color; float _Accent;
            v2f vert(appdata v)
            {
                v2f o; o.vertex=UnityObjectToClipPos(v.vertex);
                o.normal=UnityObjectToWorldNormal(v.normal); o.world=mul(unity_ObjectToWorld,v.vertex).xyz; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float light=0.48+0.52*saturate(dot(normalize(i.normal),normalize(float3(-0.4,0.7,-1))));
                float rim=pow(1-saturate(dot(normalize(i.normal),normalize(_WorldSpaceCameraPos-i.world))),3);
                float3 color=_Color.rgb*light + rim*float3(0.18,0.28,0.22) + _Accent*float3(0.22,0.35,0.08);
                return fixed4(color,1);
            }
            ENDCG
        }
    }
}
