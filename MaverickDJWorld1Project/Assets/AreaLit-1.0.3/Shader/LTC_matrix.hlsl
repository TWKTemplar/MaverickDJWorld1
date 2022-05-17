float densityOverCosine(float3 n, float3 w, float3x3 M) {
	return determinant(M) * dot(n,mul(M,w)) / pow(length(mul(M,w)), 4) / dot(n,w);
}
float3x3 MatrixAxisScale(float3 n, float3 axis, float s, float t) {
	float3 Y = n, X = axis-dot(axis,Y)*Y;
	X *= rsqrt(max(dot(X,X), 1e-20)); // safe normalize
	float sinT = dot(axis,X), cosT = dot(axis,Y);
	float d = (s*s-1)*sinT*sinT + 1;
	float a = (s*s-1)*sinT*cosT / d, b = s / d, c = s/t * rsqrt(d);
	float3 P = (1-c)*X+a*Y, Q = (b-c)*Y;
	return transpose(float3x3(
		float3(c,0,0)+X*P.x+Y*Q.x, float3(0,c,0)+X*P.y+Y*Q.y, float3(0,0,c)+X*P.z+Y*Q.z));
}
float3x3 MatrixAxisPeak(float3 n, float3 axis, float a) {
	float c2 = dot(axis,n)*dot(axis,n), s2 = 1-c2, a2 = a*a, k2 = a2;
	k2 = (s2*(k2*k2)+(2*a2)*sqrt(c2+s2*k2)) / ((c2+s2*k2)*3-c2); // Newton's method
	return MatrixAxisScale(n, axis, sqrt(k2), sqrt(k2));
}
float3x3 MatrixSmithGGX(float3 n, float3 v, float roughness) {
	// let f(a) = D_GGX(1, a) * V_SmithGGXCorrelated(nv, nv, a) * PI be peak density over cosine
	// note rsqrt(f(a)) = 2*nv*a
	// pick scale(a,nv) = 2*nv*a + o(a) and scale(1,nv)=1 and scale(a,1)<=1
	float a = roughness, nv = dot(n,v);
	return MatrixAxisPeak(n, -reflect(v,n), (2-a)*a*lerp(nv,1,a));
}
float3x3 RotationFromTo(float3 u, float3 v) {
	float co = dot(u,v);
	float3 cu = (v+u)*rcp(1+co), cv = 2*u-cu;
	return transpose(float3x3(
		float3(1,0,0) + cv[0]*v - cu[0]*u,
		float3(0,1,0) + cv[1]*v - cu[1]*u,
		float3(0,0,1) + cv[2]*v - cu[2]*u));
}