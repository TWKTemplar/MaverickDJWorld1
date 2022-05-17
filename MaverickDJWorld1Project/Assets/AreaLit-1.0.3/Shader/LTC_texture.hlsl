// L1-approximate the distribution 1/(1+P^2)^2 with an average of constant distributions on squares
static const float  squareRadius1 = 1.30019;
static const float3 squareRadius3 = {0.742932, 1.06626, 2.07229};
static const float3 GradScale3 = squareRadius3/squareRadius1;
float4 computeCenterRadius(float3 n, float3 axis) {
	// push the cosine distribution to the plane dot(P,axis)=1 and approx it with 1/(radius^2+(P-center)^2)^2
	float co = dot(axis,n);
	float y  = 0.57735027+co*(0.33333333+co*0.090862607); // (sqrt(3+co*co)+co)/3, rel err 4e-3
	float rx = rsqrt(max(1e-6, (y-2.0/3*co)*co+1));
	return float4((y-rx*rx*co)*n + rx*rx*axis, rx/rsqrt(4./3));
}
float2 computeQuadUV(float3 p, float3 u, float3 v, float3 w) {
	float2x2 m = float2x2(dot(v,v),-dot(u,v),-dot(u,v),dot(u,u));
	float2 mw = mul(m, float2(dot(u,w),dot(v,w)));
	float2 mp = max(0, mul(m, float2(dot(u,p),dot(v,p))));
	float x = mp.x, y = mp.y, a = mw.x, b = mw.y, d = determinant(m);
	return a*b / max(max(a*(b*x+(d-a)*y), b*(a*y+(d-b)*x)), a*b*d) * mp; // clamp to quad by scaling
}
float4 SampleQuad(float3 n, float3 v[4]) {
	float3 axis = normalize(cross(v[0]-v[1], v[2]-v[1]));
	float4 centerRadius = computeCenterRadius(n, dot(axis,v[1])>0 ? +axis : -axis);
	centerRadius.w *= squareRadius1*2; // absorb default gradient scale
	centerRadius *= abs(dot(axis,v[1]));
	return float4(computeQuadUV(centerRadius.xyz-v[1], v[0]-v[1], v[2]-v[1], v[3]-v[1]),
		centerRadius.w*rsqrt(dot(v[0]-v[1],v[0]-v[1])), centerRadius.w*rsqrt(dot(v[2]-v[1],v[2]-v[1])));
}
float SampleQuadDepth(float4 uvrr, float3 v[4]) {
	float3 center = v[1] + uvrr.x*(v[0]-v[1]) + uvrr.y*(v[2]-v[1]);
	return dot(center,center);
}