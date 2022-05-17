#include <UnityCG.cginc>
#include "Input.hlsl"

UNITY_INSTANCING_BUFFER_START(Props)
	UNITY_DEFINE_INSTANCED_PROP(float4, _LightColor)
	UNITY_DEFINE_INSTANCED_PROP(int, _LightTexIndex)
	UNITY_DEFINE_INSTANCED_PROP(int, _LightTopology)
	UNITY_DEFINE_INSTANCED_PROP(int, _LightChannel)
UNITY_INSTANCING_BUFFER_END(Props)

struct VertInputMesh {
	float2 uv0 : TEXCOORD0;
	float2 uv1 : TEXCOORD1;
	float3 vertex : POSITION;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct GeomInputMesh {
	float2 uv : TEXCOORD0;
	float3 vertex : TEXCOORD1;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};
struct FragInputMesh {
	float2 coord : TEXCOORD0;
	nointerpolation float4 data[6] : TEXCOORD1;
	float4 pos : SV_Position;
	UNITY_VERTEX_OUTPUT_STEREO
};
void vertMesh(VertInputMesh v, out GeomInputMesh o) {
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_TRANSFER_INSTANCE_ID(v, o);
	float2 uv = _LightUVSet == 0 ? v.uv0 : v.uv1;
	o.vertex = mul(unity_ObjectToWorld, float4(v.vertex, 0)).xyz;
	o.uv = TRANSFORM_TEX(uv, _LightTex);
}
void geomMeshSetup(GeomInputMesh i[3], uint pid, inout FragInputMesh o, out float4 coordAABB, out float4 posAABB) {
	coordAABB = 0;
	posAABB = 0;

	const float epsCross = 1e-5;
	if(length(cross(i[2].vertex-i[0].vertex, i[1].vertex-i[0].vertex)) < epsCross)
		return;

	float3 origin = mul(unity_ObjectToWorld, float4(0,0,0,1));
	if(determinant(unity_ObjectToWorld) * (_Cull == 1 ? -1 : +1) > 0) {
		GeomInputMesh t=i[0]; i[0]=i[1]; i[1]=t;
	}

	float3 extraVertex = (i[1].vertex+i[2].vertex)/2;
	int topology = UNITY_ACCESS_INSTANCED_PROP(Props, _LightTopology);
	if(topology != 3) {
		float3 dir0 = normalize(i[1].vertex-i[2].vertex);
		float3 dir1 = normalize(i[2].vertex-i[0].vertex);
		float3 dir2 = normalize(i[0].vertex-i[1].vertex);
		float3 absDots = abs(float3(dot(dir1,dir2), dot(dir2,dir0), dot(dir0,dir1)));
		bool3  minDots = absDots <= min(absDots.zxy, absDots.yzx);
		if(minDots.y) {
			absDots = absDots.yzx;
			GeomInputMesh t=i[0]; i[0]=i[1]; i[1]=i[2]; i[2]=t;
		} else if(minDots.z) {
			absDots = absDots.zxy;
			GeomInputMesh t=i[2]; i[2]=i[1]; i[1]=i[0]; i[0]=t;
		}
		extraVertex = (i[1].vertex+i[2].vertex)/2; // recompute after swap
		if(absDots.x < 1e-5) { // orthogonal triangle
			if(pid % 2 != 0)
				return;
			extraVertex = (i[1].vertex+i[2].vertex)-i[0].vertex;
		} else if(topology == 4)
			return;
	}

	float4 color = UNITY_ACCESS_INSTANCED_PROP(Props, _LightColor);
	int texId = UNITY_ACCESS_INSTANCED_PROP(Props, _LightTexIndex);
	int flags = UNITY_ACCESS_INSTANCED_PROP(Props, _LightChannel); // TODO
	if(dot(color,color) == 0)
		return;

	coordAABB = float4(0,0,_ScreenParams.xy);
	posAABB = float4(-1,-1,1,1);
	posAABB.yw *= _ProjectionParams.x;

	o.data[0] = float4(origin+i[2].vertex, (origin+extraVertex).x);
	o.data[1] = float4(origin+i[0].vertex, (origin+extraVertex).y);
	o.data[2] = float4(origin+i[1].vertex, (origin+extraVertex).z);
	o.data[3] = float4(i[0].uv, texId, flags);
	o.data[4] = float4(i[2].uv-i[0].uv, i[1].uv-i[0].uv);
	o.data[5] = color;
}
[maxvertexcount(8)]
void geomMesh(triangle GeomInputMesh i[3], uint pid : SV_PrimitiveID, inout TriangleStream<FragInputMesh> stream) {
	FragInputMesh o;
	UNITY_SETUP_INSTANCE_ID(i[0]);
	UNITY_INITIALIZE_OUTPUT(FragInputMesh, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	static const bool enabled = unity_OrthoParams.w == 1;
	if(!enabled)
		return;
	if(pid >= 256)
		return; // avoid overdraw just in case

	float4 coordAABB, posAABB;
	geomMeshSetup(i, pid, o, coordAABB, posAABB);
	if(posAABB.x == 0)
		return;

	[unroll] for(int I=0; I<2; I++) {
		o.coord = coordAABB.xw; o.pos = float4(posAABB.xw,1,1); stream.Append(o);
		o.coord = coordAABB.xy; o.pos = float4(posAABB.xy,1,1); stream.Append(o);
		o.coord = coordAABB.zw; o.pos = float4(posAABB.zw,1,1); stream.Append(o);
		o.coord = coordAABB.zy; o.pos = float4(posAABB.zy,1,1); stream.Append(o);
		stream.RestartStrip();
		if(_Cull != 0)
			return;
		float3 t = o.data[0].xyz; o.data[0].xyz = o.data[2].xyz; o.data[2].xyz = t;
		float2 T = o.data[4].xy;  o.data[4].xy  = o.data[4].zw;  o.data[4].zw = T;
	}
}
float4 fragMesh(FragInputMesh i) : SV_Target {
	switch(int(floor(i.coord.x))) { // use switch because dynamic indexing on varying is broken on AMD
	case 0: return i.data[0];
	case 1: return i.data[1];
	case 2: return i.data[2];
	case 3: return i.data[3];
	case 4: return i.data[4];
	case 5: return i.data[5];
	default: return 0;
	}
}
void vertMeshNoGeom(VertInputMesh v, out FragInputMesh o) {
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_OUTPUT(FragInputMesh, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	static const bool enabled = unity_OrthoParams.w == 1;
	if(!enabled)
		return;

	float3 axisX = normalize(v.tangent.xyz);
	float3 axisY = normalize(cross(v.normal, v.tangent.xyz) * v.tangent.w);
	float2 axisV = float2(dot(v.vertex, axisX), dot(v.vertex, axisY));
	float3x3 posMat = transpose(float3x3(2*abs(axisV.x)*axisX, 2*abs(axisV.y)*axisY,
		v.vertex - axisV[0]*axisX - axisV[1]*axisY));

	float4 uvXY = float4(1,0,0,1);
	if(_LightUVSet == 1)
		switch(int(round(dot(transpose(posMat)[2], float3(2,4,6))))) {
		case +1: uvXY = float4(0,+0.3056716, -0.3056716,0); break;
		case -1: uvXY = float4(0,-0.3056716, +0.3056716,0); break;
		case +2: uvXY = float4(-0.3056716,0, 0,-0.3056716); break;
		case -2: uvXY = float4(+0.3056716,0, 0,+0.3056716); break;
		case +3: uvXY = float4(+0.3056716,0, 0,+0.3056716); break;
		case -3: uvXY = float4(+0.3056716,0, 0,+0.3056716); break;
		}

	float3x3 uvMat = transpose(float3x3(uvXY.xy,0, uvXY.zw,0,
		(_LightUVSet == 0 ? v.uv0 : v.uv1) - (axisV>0).x*uvXY.xy - (axisV>0).y*uvXY.zw, 0));

	GeomInputMesh i[3];
	i[0].vertex = mul(unity_ObjectToWorld, mul(posMat, float3(-0.5, -0.5, 1)));
	i[1].vertex = mul(unity_ObjectToWorld, mul(posMat, float3(+0.5, +0.5, 1)));
	i[2].vertex = mul(unity_ObjectToWorld, mul(posMat, float3(+0.5, -0.5, 1)));
	i[0].uv = TRANSFORM_TEX((mul(uvMat, float3(0,0,1)).xy), _LightTex);
	i[1].uv = TRANSFORM_TEX((mul(uvMat, float3(1,1,1)).xy), _LightTex);
	i[2].uv = TRANSFORM_TEX((mul(uvMat, float3(1,0,1)).xy), _LightTex);

	float4 coordAABB, posAABB;
	geomMeshSetup(i, 0, o, coordAABB, posAABB);
	if(posAABB.x == 0)
		return;

	o.coord = axisV < 0 ? coordAABB.xy : coordAABB.zw;
	o.pos = float4(axisV < 0 ? posAABB.xy : posAABB.zw, 1,1);
}