Shader "Unlit/BlitCRT" {
Properties {
	_MainTex ("MainTex", 2D) = "black" {}
	[ToggleUI] _Gamma("Gamma", Float) = 0
}
SubShader {
	Pass {
		Lighting Off
CGPROGRAM
#pragma vertex CustomRenderTextureVertexShader
#pragma fragment frag

#include "UnityCustomRenderTexture.cginc"

sampler2D _MainTex;
float4 _MainTex_ST;
float _Gamma;

float4 frag(v2f_customrendertexture i) : SV_Target {
	float4 color = tex2D(_MainTex, i.globalTexcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw);
	if(_Gamma)
		color.rgb = GammaToLinearSpace(color.rgb);
	return color;
}
ENDCG
	}
}
}