Shader "Schulte/Glow"
{
    Properties { _Color ("Color", Color) = (0.77,0.97,0.48,1) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 vertex:SV_POSITION; };
            fixed4 _Color;
            v2f vert(float4 vertex:POSITION) { v2f o; o.vertex=UnityObjectToClipPos(vertex); return o; }
            fixed4 frag(v2f i):SV_Target { return _Color; }
            ENDCG
        }
    }
}
