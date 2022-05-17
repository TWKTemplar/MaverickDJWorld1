float3 integrateEdge(float3 p, float3 q) {
	float4 pq = float4(cross(p,q), dot(p,q));
	float ppqq = dot(p,p)*dot(q,q), ipq = rsqrt(ppqq), co = abs(pq.w)*ipq;
	float f = 0.25+co*(-0.13972066+co*0.048875605); // acos(co)*rsqrt(1-co*co)/(2*PI), rel err 7e-3
	return (pq.w >= 0 ? f*ipq : rsqrt(ppqq-pq.w*pq.w)/2 - f*ipq) * pq.xyz;
}
float3 integrateEdgeClipped(float3 p, float3 q, float np, float nq, inout float3 P, inout float3 Q) {
	UNITY_BRANCH if((np<0) != (nq<0)) {
		float3 m = q*np - p*nq;
		if(np<0) Q = p = -m;
		else     P = q = +m;
	}
	return (np<0) && (nq<0) ? 0 : integrateEdge(p, q);
}
float3 integrateQuadClipped(float3 p[4], float3 n) {
#if 0
	float4 np = float4(dot(n,p[0]), dot(n,p[1]), dot(n,p[2]), dot(n,p[3]));
	float3 area = 0, P = p[0], Q = p[0];
	area += integrateEdgeClipped(p[0], p[1], np[0], np[1], P, Q);
	area += integrateEdgeClipped(p[1], p[2], np[1], np[2], P, Q);
	area += integrateEdgeClipped(p[2], p[3], np[2], np[3], P, Q);
	area += integrateEdgeClipped(p[3], p[0], np[3], np[0], P, Q);
	if(any(P-Q))
		area += integrateEdge(P, Q);
	return area;
#else
	float4 np = float4(dot(n,p[0]), dot(n,p[1]), dot(n,p[2]), dot(n,p[3]));
	float clips = dot(np<0, float4(1,2,4,8)), N;
	float3 q0,q1,q2,q3,q4;
	switch(int(clips)) {
	default:
	case  0: N=4; q0=p[0]; q1=p[1]; q2=p[2]; q3=p[3]; q4=q0;   break;
	case  1: N=5; q0=p[0]*np[3]-p[3]*np[0]; q1=p[0]*np[1]-p[1]*np[0]; q2=p[1]; q3=p[2]; q4=p[3]; break;
	case  2: N=5; q0=p[1]*np[0]-p[0]*np[1]; q1=p[1]*np[2]-p[2]*np[1]; q2=p[2]; q3=p[3]; q4=p[0]; break;
	case  3: N=4; q0=p[0]*np[3]-p[3]*np[0]; q1=p[1]*np[2]-p[2]*np[1]; q2=p[2]; q3=p[3]; q4=q0;   break;
	case  4: N=5; q0=p[2]*np[1]-p[1]*np[2]; q1=p[2]*np[3]-p[3]*np[2]; q2=p[3]; q3=p[0]; q4=p[1]; break;
	case  6: N=4; q0=p[1]*np[0]-p[0]*np[1]; q1=p[2]*np[3]-p[3]*np[2]; q2=p[3]; q3=p[0]; q4=q0;   break;
	case  7: N=3; q0=p[0]*np[3]-p[3]*np[0]; q1=p[2]*np[3]-p[3]*np[2]; q2=p[3]; q3=q0;   q4=q0;   break;
	case  8: N=5; q0=p[3]*np[2]-p[2]*np[3]; q1=p[3]*np[0]-p[0]*np[3]; q2=p[0]; q3=p[1]; q4=p[2]; break;
	case  9: N=4; q0=p[3]*np[2]-p[2]*np[3]; q1=p[0]*np[1]-p[1]*np[0]; q2=p[1]; q3=p[2]; q4=q0;   break;
	case 11: N=3; q0=p[3]*np[2]-p[2]*np[3]; q1=p[1]*np[2]-p[2]*np[1]; q2=p[2]; q3=q0;   q4=q0;   break;
	case 12: N=4; q0=p[2]*np[1]-p[1]*np[2]; q1=p[3]*np[0]-p[0]*np[3]; q2=p[0]; q3=p[1]; q4=q0;   break;
	case 13: N=3; q0=p[2]*np[1]-p[1]*np[2]; q1=p[0]*np[1]-p[1]*np[0]; q2=p[1]; q3=q0;   q4=q0;   break;
	case 14: N=3; q0=p[1]*np[0]-p[0]*np[1]; q1=p[3]*np[0]-p[0]*np[3]; q2=p[0]; q3=q0;   q4=q0;   break;
	}
	float3 area = 0;
	if(clips != 15) {
		area += integrateEdge(q0, q1);
		area += integrateEdge(q1, q2);
		area += integrateEdge(q2, q3);
		area += integrateEdge(q3, q4);
		[branch] if(N > 4)
			area += integrateEdge(q4, q0);
	}
	return area;
#endif
}
float MeasureQuad(float3 n, float3 p[4]) {
	// NOTE: test shows integrateQuad + disk clipping is only 5% faster and leaks light
	return dot(n, integrateQuadClipped(p, n));
}
float MeasurePlane(float3 n, float4 p) {
	return dot(n,p.xyz) * (p.w>0 ? +0.5 : -0.5) + 0.5;
}
void TransformQuad(float3x3 M, float3 p[4], out float3 q[4]) {
	[unroll] for(uint i=0; i<4; i++)
		q[i] = mul(M, p[i]);
}