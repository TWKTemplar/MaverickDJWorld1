#ifndef INPUT_INCLUDED
#define INPUT_INCLUDED
int _Cull;
float _LightUVSet;
float4 _LightTex_ST;
float4 _ProjectorColor;

Texture2D<float4> _LightMesh;
float4 _LightMesh_TexelSize;

Texture2D _LightTex0; SamplerState sampler_LightTex0;
Texture2D _LightTex1; SamplerState sampler_LightTex1;
Texture2D _LightTex2; SamplerState sampler_LightTex2;
Texture2DArray _LightTex3; SamplerState sampler_LightTex3;

bool IsStereo() {
#ifdef USING_STEREO_MATRICES
	return true;
#else
	return UNITY_MATRIX_P._13 != 0;
#endif
}
bool IsSpecularOff() {
#if defined(_SPECULARHIGHLIGHTS_OFF)
	return true;
#else
	return false;
#endif
}
#endif