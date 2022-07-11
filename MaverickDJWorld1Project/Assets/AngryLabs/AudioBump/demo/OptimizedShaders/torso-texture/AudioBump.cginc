#ifndef AngryLabsAudioBumpCGINC
#define AngryLabsAudioBumpCGINC
float2 AngryLabsAudioBumpIndexToUV(uint index, uint textureSize) {
	int x = index % textureSize;
	int y = index / textureSize;
	float2 raw = float2(x + 0.5, y + 0.5) / textureSize;
	return raw;
}
float3 AngryLabsAudioBumpVertexDisplace (sampler2D_float blendTexture, int index, float amount, float3 position, inout float3 normal, float3 tangent, float3 binormal, int textureSize, float blendScale) {
	float2 one = float2(1.0 / textureSize, 0);
	float2 blendSampleUV;
	float3 sampledDelta;
	float3 delta;
	blendSampleUV = AngryLabsAudioBumpIndexToUV(index, textureSize);
	sampledDelta = tex2Dlod( blendTexture, float4( blendSampleUV, 0, 0.0) ).rgb;
	delta = sampledDelta;
	delta *= amount * blendScale;
	position += ( delta.x * normal ) + ( delta.y * tangent ) + ( delta.z * binormal );
	blendSampleUV = AngryLabsAudioBumpIndexToUV(index + 1, textureSize);
	sampledDelta = tex2Dlod( blendTexture, float4( blendSampleUV, 0, 0.0) ).rgb;
	delta = sampledDelta * blendScale;
	delta *= amount;
	normal += ( delta.x * normal ) + ( delta.y * tangent ) + ( delta.z * binormal );
	normal = normalize(normal);
	return position;
}
float3 AngryLabsAudioBumpApplyBlendshape(float4 audio, int vertex_id, float textureSize, sampler2D blendShapes, 
	float4 blend1, float4 blend2, float4 blend3, float4 blend4, 
	float3 normal, float4 tangent, float blendScale) {
	float4 audioMod[4] = { blend1, blend2, blend3, blend4 };
	float2 vcolor_uv = float2(0.5, 0.5) / textureSize;
	float4 vcolor = tex2Dlod( blendShapes , float4( vcolor_uv, 0, 0.0) ).rgba;
	int verticies = vcolor.r;
	int shapes = vcolor.g;
	float3 ret = float3(0,0,0);
	normal = normalize(normal);
	float3 tangent3 = normalize(tangent.xyz);	
	float3 biNormal = cross( normal , tangent3 ) * tangent.w;
	for(int i=0; i<shapes; i++) 
	{
		float4 audioMuled = audio * audioMod[i];
		switch(i){
			case 0: audioMuled = audio * blend1; break;
			case 1: audioMuled = audio * blend2; break;
			case 2: audioMuled = audio * blend3; break;
			case 3: audioMuled = audio * blend4; break;
		}
		float audioLen = length(audioMuled);
        int offset = 1 + vertex_id * 2;
        int shape = verticies * 2 * i;
		int vert = offset + shape;
		ret += AngryLabsAudioBumpVertexDisplace(blendShapes, vert, audioLen, ret, normal, tangent3, biNormal, textureSize, blendScale);
	}
	return ret;
}
#endif
