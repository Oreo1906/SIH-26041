Shader "SurakshaXR/CaveRock"
{
    Properties {
        _Color ("Rock tint", Color) = (0.35,0.28,0.2,1)
        _MainTex ("Bundled procedural mineral texture", 2D) = "white" {}
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        struct Input { float3 rockPosition; float3 rockNormal; };
        void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 scale = float3(length(unity_ObjectToWorld._m00_m10_m20), length(unity_ObjectToWorld._m01_m11_m21), length(unity_ObjectToWorld._m02_m12_m22));
            o.rockPosition = v.vertex.xyz * scale;
            o.rockNormal = v.normal;
        }
        void surf(Input IN, inout SurfaceOutputStandard o) {
            float3 weights = abs(IN.rockNormal);
            weights /= max(weights.x + weights.y + weights.z, 0.001);
            float3 p = IN.rockPosition * 0.65;
            fixed grain = tex2D(_MainTex, p.yz).r * weights.x
                + tex2D(_MainTex, p.xz).r * weights.y
                + tex2D(_MainTex, p.xy).r * weights.z;
            o.Albedo = _Color.rgb * (0.55 + grain * 0.9);
            o.Metallic = 0;
            o.Smoothness = 0.08;
            o.Occlusion = 0.8 + grain * 0.2;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
