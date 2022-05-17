#include "LTC.hlsl"
#include "Input.hlsl"

float2 fragParity(float2 screenPos) {
	return round(frac(screenPos.xy / float2(ddx(screenPos.x), ddy(screenPos.y)) / 2));
}

struct MSBuffer {
	float4 sum;
	float4 sumW;
	float2 stat;
	void add(float4 color, float depth) {
		float w = rcp(depth) * saturate(pow(color.a*4,2)); // hack: square opacity to counter depth
		sum  += color;
		sumW += color * w;
		stat.x = max(stat.x, w);
		stat.y += w;
	}
	void mergeNeighbor(float2 screenPos) {
		float2 sgn = lerp(+1, -1, fragParity(screenPos)), stat_;
#if !defined(SHADER_API_D3D11) // ddx_fine not available
		sum  += (sgn.x*sgn.y*ddx(sgn.y*sum )-ddy(sgn.y*sum ))/2;
		sumW += (sgn.x*sgn.y*ddx(sgn.y*sumW)-ddy(sgn.y*sumW))/2;
		stat_ = (sgn.x*sgn.y*ddx(sgn.y*stat)-ddy(sgn.y*stat))/2;
#else
		sum  += sum  + sgn.x/2*ddx_fine(sum ) + sgn.y/2*ddy_fine(sum );
		sumW += sumW + sgn.x/2*ddx_fine(sumW) + sgn.y/2*ddy_fine(sumW);
		stat_ = stat + sgn.x/2*ddx_fine(stat) + sgn.y/2*ddy_fine(stat);
#endif
		stat.x = max(stat.x, stat_.x);
		stat.y += stat_.y;
	}
	float4 resolve() {
#if defined(_OPAQUELIGHTS_OFF)
		return sum;
#else
		float w = lerp(stat.x, stat.y, 0.5); // hack
		float4 front = w>0 ? sumW/w : 0;
		return lerp(sum, front, saturate(front.w));
#endif
	}
};

SamplerState sampler_trilinear_clamp;
half4 sampleLightTex(float4 distr, float3x3 mat, bool specular) {
	float3 uvw = mul(mat, float3(distr.xy,1));
	float2 dx = mul(mat, float3(distr.z,0,0)).xy;
	float2 dy = mul(mat, float3(0,distr.w,0)).xy;
	switch((int)(uvw.z*2+(specular?1:0))) {
	case 0:
		return _LightTex0.SampleGrad(sampler_trilinear_clamp, uvw.xy, dx, dy); // aniso looks aliased on diffuse
	case 1:
		return (
			+ _LightTex0.SampleGrad(sampler_LightTex0, uvw.xy, LTC::GradScale3[0]*dx, LTC::GradScale3[2]*dy)
			+ _LightTex0.SampleGrad(sampler_LightTex0, uvw.xy, LTC::GradScale3[1]*dx, LTC::GradScale3[1]*dy)
			+ _LightTex0.SampleGrad(sampler_LightTex0, uvw.xy, LTC::GradScale3[2]*dx, LTC::GradScale3[0]*dy)
		)/3;
	case 2:
		return _LightTex1.SampleGrad(sampler_trilinear_clamp, uvw.xy, dx, dy); // aniso looks aliased on diffuse
	case 3:
		return (
			+ _LightTex1.SampleGrad(sampler_LightTex1, uvw.xy, LTC::GradScale3[0]*dx, LTC::GradScale3[2]*dy)
			+ _LightTex1.SampleGrad(sampler_LightTex1, uvw.xy, LTC::GradScale3[1]*dx, LTC::GradScale3[1]*dy)
			+ _LightTex1.SampleGrad(sampler_LightTex1, uvw.xy, LTC::GradScale3[2]*dx, LTC::GradScale3[0]*dy)
		)/3;
	case 4:
	case 5: // indirect light
	{
		half4 c = _LightTex2.SampleGrad(sampler_trilinear_clamp, uvw.xy, dx, dy);
		return c * rcp(max(c.a, 1e-2)); // skip black border
	}
	case -1:
	case -2:
		return 1;
	default: // TODO: static lights
		return _LightTex3.SampleGrad(sampler_trilinear_clamp, float3(uvw.xy, uvw.z-3), dx, dy);
	}
}
float3x3 standardizeNormal(inout float3 normal) {
	// optimization: standardize normal to reduce computation e.g. we only need cross(p,q).y
	const float3 standard = float3(0,1,0);
	bool flip = dot(normal, standard) < -0.9; // flip to handle singularity
	float3x3 mat = LTC::RotationFromTo(flip ? -normal : normal, standard);
	if(flip)
		mat = transpose(float3x3(-transpose(mat)[0], -transpose(mat)[1], +transpose(mat)[2]));
	normal = standard;
	return mat;
}
struct AreaLightFragInput {
	float3 pos, normal, view;
	float roughness;
	float4 occlusion;
	float2 screenPos;
};
void ShadeAreaLights(AreaLightFragInput i, out half4 diffColor, out half4 specColor, const bool diffuse=true, const bool specular=true, bool checker=false) {
#if 1 
	float3x3 mat0 = standardizeNormal(i.normal);
	i.view = mul(mat0, i.view);
#else
	float3x3 mat0 = float3x3(1,0,0, 0,1,0, 0,0,1);
#endif
	const float minRoughness = 1e-4; // Quest breaks on 1e-5
	float3x3 mat1 = LTC::MatrixSmithGGX(i.normal, i.view, clamp(i.roughness, minRoughness, 1));

	// Quest: distinct light jobs will corrupt ddx by non-uniform control flow
#if defined(SHADER_API_GLES3)
	if(!(diffuse&&specular))
		checker = false;
#endif

	bool parity = frac(dot(0.5, float3(fragParity(i.screenPos), unity_StereoEyeIndex))) != 0;
	bool spec0 = !(diffuse&&specular) ? specular : checker && parity;
	uint start = 0, step = 1;
	if(!(diffuse&&specular) && checker)
		start = parity, step = 2;
	mat0 = transpose(float3x3( // NOTE: force column-major copy in assembly
		spec0 ? mul(mat1, transpose(mat0)[0]) : transpose(mat0)[0],
		spec0 ? mul(mat1, transpose(mat0)[1]) : transpose(mat0)[1],
		spec0 ? mul(mat1, transpose(mat0)[2]) : transpose(mat0)[2]
	));

	const uint maxLights = 64;
	const float epsDist = 1e-4;
	const float epsCov = 1e-4;
	MSBuffer buf0 = (MSBuffer)0;
	MSBuffer buf1 = (MSBuffer)0;
	[loop] for(uint I=start; I<maxLights; I+=step) {
#if defined(UNITY_COMPILER_HLSLCC) // hlslcc bug
		float4 data0 = _LightMesh.Load(uint3(0,I,0));
		float4 data1 = _LightMesh.Load(uint3(1,I,0));
		float4 data2 = _LightMesh.Load(uint3(2,I,0));
#else
		float4 data0 = _LightMesh.Load(uint3(0,I,0), uint2(0,0));
		float4 data1 = _LightMesh.Load(uint3(0,I,0), uint2(1,0));
		float4 data2 = _LightMesh.Load(uint3(0,I,0), uint2(2,0));
#endif
		if(dot(data0,data0) + dot(data1,data1) == 0)
			break;
		float3 quad[4] = {data0.xyz-i.pos, data1.xyz-i.pos, data2.xyz-i.pos, float3(data0.w, data1.w, data2.w)-i.pos};
		float3 axis = cross(quad[0]-quad[2], quad[1]-quad[2]);
		if(pow(max(dot(axis,quad[2]),0),2) <= pow(epsDist,2) * dot(axis,axis)) // less glitch by using plane distance
			continue;

#if defined(UNITY_COMPILER_HLSLCC) // hlslcc bug
		float4 data3 = _LightMesh.Load(uint3(3,I,0));
		float4 data4 = _LightMesh.Load(uint3(4,I,0));
		float4 tint  = _LightMesh.Load(uint3(5,I,0));
#else
		float4 data3 = _LightMesh.Load(uint3(0,I,0), uint2(3,0));
		float4 data4 = _LightMesh.Load(uint3(0,I,0), uint2(4,0));
		float4 tint  = _LightMesh.Load(uint3(0,I,0), uint2(5,0));
#endif
		float3x3 uvMat = transpose(float3x3(data4.xy, 0, data4.zw, 0, data3.xyz));
		uint flags = data3.w;
		uint chan = flags & 3; // use of occlusion is delayed to reduce texture stall

		float cov; float4 distr, color;
		LTC::TransformQuad(mat0, quad, quad);
		cov = max(0, LTC::MeasureQuad(i.normal, quad));
		// Quest: branching before tex read will corrupt ddx by non-uniform control flow
#if !defined(SHADER_API_GLES3)
		if(cov < epsCov)
			continue;
#endif
		distr = LTC::SampleQuad(i.normal, quad);
		color = sampleLightTex(distr, uvMat, spec0) * tint;
#if defined(SHADER_API_GLES3)
		if(cov < epsCov)
			continue;
#endif
		buf0.add(cov * i.occlusion[chan] * color, LTC::SampleQuadDepth(distr, quad));

		if(!(diffuse&&specular) || checker)
			continue;

		LTC::TransformQuad(mat1, quad, quad);
		cov = max(0, LTC::MeasureQuad(i.normal, quad));
		if(cov < epsCov)
			continue;
		distr = LTC::SampleQuad(i.normal, quad);
		color = sampleLightTex(distr, uvMat, true) * tint;
		buf1.add(cov * i.occlusion[chan] * color, LTC::SampleQuadDepth(distr, quad));
	}
	if(spec0) {
		buf1 = buf0;
		buf0 = (MSBuffer)0;
	}
	if(checker) {
		buf0.mergeNeighbor(i.screenPos);
		buf1.mergeNeighbor(i.screenPos);
	}
	diffColor = max(0, buf0.resolve());
	specColor = max(0, buf1.resolve());
}

float GeometricSpecularAA(float roughness, float3 normal) {
	const float variance = 0.2;
	const float threshold = 0.2;
	float3 du = ddx(normal), dv = ddy(normal);
	float kernelRoughness2 = min(threshold, 2*variance * (dot(du,du) + dot(dv,dv)));
#if defined(SHADER_API_GLES3) // TODO: precision issue
	return roughness + sqrt(kernelRoughness2);
#else
	return sqrt(roughness * roughness + kernelRoughness2);
#endif
}