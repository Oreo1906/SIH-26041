Shader "SurakshaXR/SoftSmoke"
{
    Properties { _Color ("Smoke colour", Color) = (.22,.23,.23,.3) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            fixed4 _Color;
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = (i.uv - .5) * 2;
                float radius = length(p);
                float alpha = pow(saturate(1 - radius * radius), 2);
                float variation = .85 + .15 * sin(p.x * 7 + p.y * 4);
                return fixed4(_Color.rgb, _Color.a * alpha * variation);
            }
            ENDCG
        }
    }
}
