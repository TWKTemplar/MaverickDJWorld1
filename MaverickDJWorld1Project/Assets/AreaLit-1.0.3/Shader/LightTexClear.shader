Shader "AreaLit/LightTexClear" {
Properties {}
CGINCLUDE
#include <UnityCG.cginc>
static const bool enabled = unity_OrthoParams.w == 1;

void vert(float4 vertex : POSITION, out float4 pos : SV_Position) {
	pos = enabled ? float4(vertex.xy*2, UNITY_NEAR_CLIP_VALUE, 1) : 0;
	pos.y *= _ProjectionParams.x;
}
float4 frag() : SV_Target {
	return 0;
}
ENDCG
SubShader {
	Tags { "Queue"="Overlay" "PreviewType"="Plane" "IgnoreProjector"="True" }
	Pass {
		Name "Clear"
		Tags { "LightMode"="ForwardBase" }
		CGPROGRAM
		#pragma target 3.5
		#pragma vertex vert
		#pragma fragment frag
		ENDCG
	}
}
}