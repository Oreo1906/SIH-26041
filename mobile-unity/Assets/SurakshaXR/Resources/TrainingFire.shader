Shader "SurakshaXR/TrainingFire"
{
    Properties { _EffectTime ("Local animation time", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent-5" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            float _EffectTime;
            v2f vert(appdata v) { v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p) {
                float2 i = floor(p); float2 f = frac(p); f = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x),
                    lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float y = i.uv.y; float x = (i.uv.x - .5) * 2;
                float flow = noise(float2(x * 4, y * 4 - _EffectTime * 1.9));
                float detail = noise(float2(x * 9 + 4, y * 8 - _EffectTime * 3.2));
                float bend = sin(y * 7 - _EffectTime * 2.4) * .13 * y;
                float width = (.78 * pow(saturate(1 - y), .62) + (flow - .5) * .45 * y);
                float edge = width - abs(x + bend);
                float tongues = saturate((flow * .70 + detail * .30) * 1.5 - y * .65);
                float alpha = smoothstep(-.05, .11, edge) * smoothstep(0, .045, y)
                    * (1 - smoothstep(.82, 1, y)) * lerp(.68, 1, tongues);
                float core = saturate(edge * 2.7) * saturate(1 - y * 1.3);
                float3 orange = float3(1, .22, .015);
                float3 yellow = float3(1, .76, .12);
                float3 color = lerp(orange, yellow, saturate(core + tongues * .35));
                color = lerp(color, float3(1, .98, .65), pow(core, 2.5));
                return fixed4(color, alpha * .92);
            }
            ENDCG
        }
    }
    FallBack Off
}
