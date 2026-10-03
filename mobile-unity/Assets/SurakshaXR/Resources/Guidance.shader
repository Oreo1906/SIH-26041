Shader "SurakshaXR/Guidance"
{
    SubShader
    {
        Tags { "Queue"="Geometry+10" "RenderType"="Opaque" }
        Cull Off
        ZWrite On
        ZTest LEqual
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; };
            v2f vert(appdata v)
            {
                v2f o; o.vertex = UnityObjectToClipPos(v.vertex); o.color = v.color; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Gentle 0.6 Hz brightness cycle; alpha is a pulse mask, not transparency.
                // The dark outline stays steady, with no extra lights or Update callbacks.
                fixed pulse = .9 + .1 * sin(_Time.y * 3.769911);
                return fixed4(i.color.rgb * lerp(1, pulse, i.color.a), 1);
            }
            ENDCG
        }
    }
    FallBack Off
}
