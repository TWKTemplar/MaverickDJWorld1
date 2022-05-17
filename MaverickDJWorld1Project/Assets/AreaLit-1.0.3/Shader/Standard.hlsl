#include <UnityCG.cginc>
#include <Lighting.cginc>
#include <UnityStandardCore.cginc>
#include <UnityMetaPass.cginc>
#include "Lighting.hlsl"

float4 _OcclusionMap_ST;
float  _OcclusionUVSet;
half4 OcclusionMask(float4 uv01) {
	float2 uv = _OcclusionUVSet == 0 ? uv01.xy : uv01.zw;
	half4 occ = tex2D(_OcclusionMap, TRANSFORM_TEX(uv, _OcclusionMap));
	return lerp(1, occ, _OcclusionStrength);
}

VertexOutputForwardBase vertFwd(VertexInput v) {
	VertexOutputForwardBase o = vertForwardBase(v);
	o.tex = float4(v.uv0, v.uv1);
	return o;
}
FragmentCommonData fragFwdSetup(inout VertexOutputForwardBase i, bool frontFace, out float4 uv01) {
	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
	uv01 = i.tex;
	i.tex.xy = TRANSFORM_TEX(uv01.xy, _MainTex);
	i.tex.zw = TRANSFORM_TEX(((_UVSec == 0) ? uv01.xy : uv01.zw), _DetailAlbedoMap);
	i.tangentToWorldAndPackedData[2].xyz *= frontFace ? +1 : -1;
	FRAGMENT_SETUP(s);
	return s;
}
half4 fragFwd(VertexOutputForwardBase i, bool frontFace : SV_IsFrontFace) : SV_Target {
	float4 uv01;
	FragmentCommonData s = fragFwdSetup(i, frontFace, uv01);

	AreaLightFragInput ai;
	ai.pos = s.posWorld;
	ai.normal = s.normalWorld;
	ai.view = -s.eyeVec;
	ai.roughness = GeometricSpecularAA(SmoothnessToRoughness(s.smoothness), s.normalWorld);
	ai.occlusion = OcclusionMask(uv01);
	ai.screenPos = i.pos.xy;
	half4 diffTerm, specTerm;
	ShadeAreaLights(ai, diffTerm, specTerm, true, !IsSpecularOff(), IsStereo());

	UnityLight mainLight = MainLight();
	UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);
	UnityGI gi = FragmentGI(s, ai.occlusion.g, i.ambientOrLightmapUV, atten, mainLight);

	half4 c = UNITY_BRDF_PBS(s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness,
		s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
	c.rgb += Emission(i.tex.xy);
	c.rgb += s.diffColor * diffTerm + s.specColor * specTerm;

	UNITY_EXTRACT_FOG_FROM_EYE_VEC(i);
	UNITY_APPLY_FOG(_unity_fogCoord, c.rgb);
	return OutputForward(c, s.alpha);
}
half4 fragProj(VertexOutputForwardBase i, bool frontFace : SV_IsFrontFace) : SV_Target {
	float4 uv01;
	FragmentCommonData s = fragFwdSetup(i, frontFace, uv01);

	AreaLightFragInput ai;
	ai.pos = s.posWorld;
	ai.normal = s.normalWorld;
	ai.view = -s.eyeVec;
	ai.roughness = GeometricSpecularAA(SmoothnessToRoughness(s.smoothness), s.normalWorld);
	ai.occlusion = 1;
	ai.screenPos = i.pos.xy;
	half4 diffTerm, specTerm;
	ShadeAreaLights(ai, diffTerm, specTerm, true, !IsSpecularOff(), IsStereo());

	half4 c = half4(0,0,0,1);
	c.rgb += Emission(i.tex.xy);
	c.rgb += (s.diffColor * diffTerm + s.specColor * specTerm);
	c *= _ProjectorColor;

	return c - saturate(min(min(c.r, c.g), min(c.b, c.a))); // reduce clamping in non-HDR blending
}
UnityMetaInput fragMetaSetup(VertexOutputForwardBase i, bool frontFace) {
	float4 uv01;
	FragmentCommonData s = fragFwdSetup(i, frontFace, uv01);

	AreaLightFragInput ai;
	ai.pos = s.posWorld;
	ai.normal = s.normalWorld;
	ai.view = s.normalWorld;
	ai.roughness = GeometricSpecularAA(SmoothnessToRoughness(s.smoothness), s.normalWorld);
	ai.occlusion = OcclusionMask(uv01);
	half4 diffTerm, specTerm;
	ShadeAreaLights(ai, diffTerm, specTerm, true, false);

	UnityMetaInput o;
	UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);
#if defined(UNITY_PASS_FORWARDBASE)
	UnityLight mainLight = MainLight();
	UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);
	UnityGI gi = FragmentGI(s, ai.occlusion.g, i.ambientOrLightmapUV, atten, mainLight);

	gi.indirect.specular = 0;
	o.Albedo = UNITY_BRDF_PBS(s.diffColor + s.specColor, 0, s.oneMinusReflectivity, s.smoothness,
		s.normalWorld, s.normalWorld, gi.light, gi.indirect);
#else
	o.Albedo = s.diffColor + s.specColor * (SmoothnessToRoughness(s.smoothness)/2); // UnityLightmappingAlbedo
#endif
	o.SpecularColor = s.specColor;
	o.Emission = Emission(i.tex.xy);
	o.Emission += (s.diffColor + s.specColor) * diffTerm;
	return o;
}
VertexOutputForwardBase vertTex(VertexInput v) {
	const bool enabled = unity_OrthoParams.w == 1;
	VertexOutputForwardBase o = vertFwd(v);
	float2 uv = _LightUVSet == 0 ? v.uv0 : v.uv1;
	o.pos = enabled ? float4(TRANSFORM_TEX(uv, _LightTex)*2-1, UNITY_NEAR_CLIP_VALUE, 1) : 0;
	o.pos.y *= _ProjectionParams.x;
	return o;
}
half4 fragTex(VertexOutputForwardBase i) : SV_Target {
	UnityMetaInput o = fragMetaSetup(i, _Cull != 1);
	return float4(o.Albedo + o.Emission, 1);
}
#if defined(UNITY_PASS_META)
half4 fragMeta(VertexOutputForwardBase i) : SV_Target {
	UnityMetaInput o = fragMetaSetup(i, _Cull != 1);
	return UnityMetaFragment(o);
}
VertexOutputForwardBase vertMeta(VertexInput v) {
	VertexOutputForwardBase o = vertFwd(v);
	o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
	return o;
}
#endif