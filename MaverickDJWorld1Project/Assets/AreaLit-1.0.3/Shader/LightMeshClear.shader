Shader "AreaLit/LightMeshClear" {
Properties {}
CGINCLUDE
#pragma target 3.5
#pragma vertex vert
#pragma fragment frag

#include <UnityCG.cginc>
static const bool enabled = unity_OrthoParams.w == 1;

#if defined(UNITY_REVERSED_Z)
#define NEAR_Z_VALUE 1
#else
#define NEAR_Z_VALUE 0
#endif

void vert(float4 vertex : POSITION, out float2 coord : TEXCOORD0, out float4 pos : SV_Position) {
	coord = (vertex.xy + 0.5) * _ScreenParams.xy;
	pos = enabled ? float4(vertex.xy*2, 0, 1) : 0;
	pos.y *= _ProjectionParams.x;
}
uint getLayer(float2 coord) {
	return uint(floor(coord.y+1));
}
ENDCG
SubShader {
	Tags { "LightMode"="Vertex" "Queue"="Overlay" "PreviewType"="Plane" "IgnoreProjector"="True" }
	Stencil {
		Comp Always Ref 255
		Pass Replace ZFail Keep
	}
	Cull Off
	ZTest Less ZWrite Off
	ColorMask 0
	Pass {
		Name "Clear"
		ColorMask RGBA
		ZTest Always ZWrite On
		Stencil { Ref 0 }
		CGPROGRAM
		float4 frag() : SV_Target {
			return 0;
		}
		ENDCG
	}
	Pass {
		Stencil { Ref 1 ZFail Zero }
		CGPROGRAM
		float frag(float2 coord : TEXCOORD0) : SV_Depth {
			return (getLayer(coord) &  1) ? NEAR_Z_VALUE : 1-NEAR_Z_VALUE;
		}
		ENDCG
	}
	Pass {
		Stencil { WriteMask 2 }
		CGPROGRAM
		float frag(float2 coord : TEXCOORD0) : SV_Depth {
			return (getLayer(coord) &  2) ? NEAR_Z_VALUE : 1-NEAR_Z_VALUE;
		}
		ENDCG
	}
	Pass {
		Stencil { WriteMask 4 }
		CGPROGRAM
		float frag(float2 coord : TEXCOORD0) : SV_Depth {
			return (getLayer(coord) &  4) ? NEAR_Z_VALUE : 1-NEAR_Z_VALUE;
		}
		ENDCG
	}
	Pass {
		Stencil { WriteMask 8 }
		CGPROGRAM
		float frag(float2 coord : TEXCOORD0) : SV_Depth {
			return (getLayer(coord) &  8) ? NEAR_Z_VALUE : 1-NEAR_Z_VALUE;
		}
		ENDCG
	}
	Pass {
		Stencil { WriteMask 16 }
		CGPROGRAM
		float frag(float2 coord : TEXCOORD0) : SV_Depth {
			return (getLayer(coord) & 16) ? NEAR_Z_VALUE : 1-NEAR_Z_VALUE;
		}
		ENDCG
	}
	Pass {
		Stencil { WriteMask 32 }
		CGPROGRAM
		float frag(float2 coord : TEXCOORD0) : SV_Depth {
			return (getLayer(coord) & 32) ? NEAR_Z_VALUE : 1-NEAR_Z_VALUE;
		}
		ENDCG
	}
	Pass {
		Name "Noop"
		Tags { "LightMode"="ForwardBase" }
		Stencil { Ref 0 }
		CGPROGRAM
		float4 frag() : SV_Target {
			return 0;
		}
		ENDCG
	}
}
}