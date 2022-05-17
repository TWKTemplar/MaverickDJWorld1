Shader "AreaLit/LightMesh" {
Properties {
	[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Int) = 2

	[HDR] _LightColor("Light Color", Color) = (1,1,1,1)
	_LightTexIndex("Texture Index", Int) = 0
	_LightTex("Texture UV", 2D) = "white" {}
	[Enum(UV0,0,UV1,1)] _LightUVSet("UV Set", Int) = 0

	[Enum(Auto,0,Triangle,3,Quad,4)] _LightTopology("Topology", Int) = 0
	[Enum(Red,0,Green,1,Blue,2,Alpha,3)] _LightChannel("Occlusion Channel", Int) = 1
	
	[Toggle(_LIGHTTEX_PASS)] _LightTexPass("Indirect Light", Float) = 0.0

	[NoScaleOffset] _LightMesh("Light Mesh", 2D) = "black" {}
	[NoScaleOffset] _LightTex0("Light Texture 0", 2D) = "white" {}
	[NoScaleOffset] _LightTex1("Light Texture 1", 2D) = "black" {}
	[NoScaleOffset] _LightTex2("Light Texture 2", 2D) = "black" {}
	[NoScaleOffset] _LightTex3("Light Texture 3", 2DArray) = "black" {}
	[ToggleOff] _OpaqueLights("Opaque Lights", Float) = 1.0

	[Enum(UV0,0,UV1,1)] _OcclusionUVSet ("UV Set for occlusion map", Float) = 0

	//// Standard properties ////

	_Color("Color", Color) = (1,1,1,1)
	_MainTex("Albedo", 2D) = "white" {}

	_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

	_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
	_GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
	[Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel ("Smoothness texture channel", Float) = 0

	[Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
	_MetallicGlossMap("Metallic", 2D) = "white" {}

	[ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
	[ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

	_BumpScale("Scale", Float) = 1.0
	[Normal] _BumpMap("Normal Map", 2D) = "bump" {}

	_Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
	_ParallaxMap ("Height Map", 2D) = "black" {}

	_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
	_OcclusionMap("Occlusion", 2D) = "white" {}

	_EmissionColor("Color", Color) = (0,0,0)
	_EmissionMap("Emission", 2D) = "white" {}

	_DetailMask("Detail Mask", 2D) = "white" {}

	_DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
	_DetailNormalMapScale("Scale", Float) = 1.0
	[Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}

	[Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0


	// Blending state
	[HideInInspector] _Mode ("__mode", Float) = 0.0
	// [HideInInspector] _SrcBlend ("__src", Float) = 1.0
	// [HideInInspector] _DstBlend ("__dst", Float) = 0.0
	// [HideInInspector] _ZWrite ("__zw", Float) = 1.0
}
CGINCLUDE
#define UNITY_SETUP_BRDF_INPUT MetallicSetup
ENDCG
CustomEditor "AreaLit.StandardShaderGUI"
SubShader {
	Tags { "Queue"="Overlay+1" "PreviewType"="Plane" "IgnoreProjector"="True" }
	Pass {
		Name "Mesh"
		Tags { "LightMode"="Vertex" }
		ZTest Always ZWrite Off
		Cull Off
		Stencil {
			Comp Equal Ref 1
			Pass DecrSat Fail DecrSat
		}
		CGPROGRAM
		#pragma exclude_renderers metal
		#pragma target 4.0
		#pragma vertex vertMesh
		#pragma geometry geomMesh
		#pragma fragment fragMesh
		#pragma multi_compile_instancing
		#include "Standard.hlsl"
		#include "LightMesh.hlsl"
		ENDCG
	}
	Pass {
		Name "Tex"
		Tags { "LightMode"="ForwardBase" }
		Cull Off
		CGPROGRAM
		#pragma exclude_renderers metal
		#pragma target 5.0
		#pragma shader_feature _EMISSION
		#pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
		#pragma shader_feature_local _GLOSSYREFLECTIONS_OFF
		#pragma shader_feature_local _OPAQUELIGHTS_OFF
		#pragma shader_feature_local _LIGHTTEX_PASS
		#pragma multi_compile_instancing
		#pragma multi_compile_fwdbase

		#pragma vertex vert
		#pragma fragment frag
		#include "Standard.hlsl"
		#include "LightMesh.hlsl"
		
		#ifdef _LIGHTTEX_PASS
		VertexOutputForwardBase vert(VertexInput v) { return vertTex(v); }
		half4 frag(VertexOutputForwardBase i) : SV_Target { return fragTex(i); }
		#else
		float4 vert() : SV_Position { return 0; }
		float4 frag() : SV_Target { return 0; }
		#endif
		ENDCG
	}
}
SubShader {
	Tags { "Queue"="Overlay+1" "PreviewType"="Plane" "IgnoreProjector"="True" }
	Pass {
		Name "Mesh"
		Tags { "LightMode"="Vertex" }
		ZTest Always ZWrite Off
		Cull Off
		Stencil {
			Comp Equal Ref 1
			Pass DecrSat Fail DecrSat
		}
		CGPROGRAM
		#pragma only_renderers metal
		#pragma target 4.0
		#pragma vertex vertMeshNoGeom
		#pragma fragment fragMesh
		#pragma multi_compile_instancing
		#include "Standard.hlsl"
		#include "LightMesh.hlsl"
		ENDCG
	}
	Pass {
		Name "Tex"
		Tags { "LightMode"="ForwardBase" }
		Cull Off
		CGPROGRAM
		#pragma only_renderers metal
		#pragma target 5.0
		#pragma shader_feature _EMISSION
		#pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
		#pragma shader_feature_local _GLOSSYREFLECTIONS_OFF
		#pragma shader_feature_local _OPAQUELIGHTS_OFF
		#pragma shader_feature_local _LIGHTTEX_PASS
		#pragma multi_compile_instancing
		#pragma multi_compile_fwdbase

		#pragma vertex vert
		#pragma fragment frag
		#include "Standard.hlsl"
		#include "LightMesh.hlsl"
		
		#ifdef _LIGHTTEX_PASS
		VertexOutputForwardBase vert(VertexInput v) { return vertTex(v); }
		half4 frag(VertexOutputForwardBase i) : SV_Target { return fragTex(i); }
		#else
		float4 vert() : SV_Position { return 0; }
		float4 frag() : SV_Target { return 0; }
		#endif
		ENDCG
	}
}
}