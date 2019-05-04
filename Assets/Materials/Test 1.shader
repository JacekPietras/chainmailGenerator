Shader "Custom/Test 1" 
{
Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _MainTex ("Base", 2D) = "white" {}
	_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
	[PowerSlider(5.0)] _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
    _BumpMap ("Normalmap", 2D) = "bump" {}
}

SubShader {
    Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
    LOD 300
        Cull Off

CGPROGRAM
#pragma surface surf BlinnPhong alpha:fade

sampler2D _MainTex;
sampler2D _BumpMap;
fixed4 _Color;
half _Shininess;

struct Input {
    float2 uv_MainTex;
    float2 uv_BumpMap;
};

void surf (Input IN, inout SurfaceOutput o) {

	fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
    o.Albedo = c.rgb* _Color.rgb;
    o.Alpha = c.a * _Color.a;
	o.Gloss = c.a;
    o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
	o.Specular = _Shininess;
}
ENDCG
}

FallBack "Legacy Shaders/Transparent/Diffuse"
}