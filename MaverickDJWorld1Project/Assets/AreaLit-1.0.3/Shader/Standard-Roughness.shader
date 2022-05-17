Shader "AreaLit/Autodesk Interactive" {
Properties {
	[Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Int) = 2

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

	_Glossiness("Roughness", Range(0.0, 1.0)) = 0.5
	_SpecGlossMap("Roughness Map", 2D) = "white" {}

	_Metallic("Metallic", Range(0.0, 1.0)) = 0.0
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
	[HideInInspector] _SrcBlend ("__src", Float) = 1.0
	[HideInInspector] _DstBlend ("__dst", Float) = 0.0
	[HideInInspector] _ZWrite ("__zw", Float) = 1.0
}
CGINCLUDE
#define UNITY_SETUP_BRDF_INPUT RoughnessSetup
ENDCG
CustomEditor "AreaLit.StandardShaderGUI"
SubShader {
	Tags { "Queue"="Geometry" "RenderType"="Opaque" }
	Pass {
		Name "FORWARD"
		Tags { "LightMode"="ForwardBase" }
		Cull [_Cull]
		Blend [_SrcBlend] [_DstBlend]
		ZWrite [_ZWrite]
		CGPROGRAM
		#pragma target 5.0
		#pragma shader_feature_local _NORMALMAP
		#pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
		#pragma shader_feature _EMISSION
		#pragma shader_feature_local _METALLICGLOSSMAP
		#pragma shader_feature_local _SPECGLOSSMAP
		#pragma shader_feature_local _DETAIL_MULX2
		#pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
		#pragma shader_feature_local _GLOSSYREFLECTIONS_OFF
		#pragma shader_feature_local _PARALLAXMAP
		#pragma shader_feature_local _OPAQUELIGHTS_OFF

		#pragma multi_compile_fwdbase
		#pragma shader_feature_local _ FOG_LINEAR FOG_EXP FOG_EXP2 // shader_feature fog
		#pragma multi_compile_instancing

		#pragma vertex vertFwd
		#pragma fragment fragFwd
		#include "Standard.hlsl"
		ENDCG
	}
	Pass {
		Name "FORWARD_DELTA"
		Tags { "LightMode" = "ForwardAdd" }
		Cull [_Cull]
		Blend [_SrcBlend] One
		Fog { Color (0,0,0,0) } // in additive pass fog should be black
		ZWrite Off
		ZTest LEqual
		CGPROGRAM
		#pragma target 3.5
		#pragma shader_feature_local _NORMALMAP
		#pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
		#pragma shader_feature_local _METALLICGLOSSMAP
		#pragma shader_feature_local _SPECGLOSSMAP
		#pragma shader_feature_local _SPECULARHIGHLIGHTS_OFF
		#pragma shader_feature_local _DETAIL_MULX2
		#pragma shader_feature_local _PARALLAXMAP

		#pragma multi_compile_fwdadd_fullshadows
		#pragma shader_feature_local _ FOG_LINEAR FOG_EXP FOG_EXP2 // shader_feature fog

		#pragma vertex vertAdd
		#pragma fragment fragAdd
		#include "UnityStandardCoreForward.cginc"
		ENDCG
	}
	Pass {
		Name "ShadowCaster"
		Tags { "LightMode" = "ShadowCaster" }
		Cull [_Cull]
		ZWrite On ZTest LEqual
		CGPROGRAM
		#pragma target 3.5
		#pragma shader_feature_local _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
		#pragma shader_feature_local _METALLICGLOSSMAP
		#pragma shader_feature_local _PARALLAXMAP
		#pragma multi_compile_shadowcaster
		#pragma multi_compile_instancing

		#pragma vertex vertShadowCaster
		#pragma fragment fragShadowCaster
		#include "UnityStandardShadow.cginc"
		ENDCG
	}
	Pass {
		Name "META"
		Tags { "LightMode" = "Meta" }
		Cull Off
		CGPROGRAM
		#pragma target 5.0
		#pragma shader_feature _EMISSION
		#pragma shader_feature_local _METALLICGLOSSMAP
		#pragma shader_feature_local _SPECGLOSSMAP
		#pragma shader_feature_local _DETAIL_MULX2
		#pragma shader_feature_local _OPAQUELIGHTS_OFF

		#pragma vertex vertMeta
		#pragma fragment fragMeta
		#include "Standard.hlsl"
		ENDCG
	}
}
}