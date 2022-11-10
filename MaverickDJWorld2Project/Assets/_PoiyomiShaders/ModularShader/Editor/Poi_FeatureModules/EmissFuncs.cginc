#ifndef EMISSFUNCS_DEFINED
#define EMISSFUNCS_DEFINED
float calculateGlowInTheDark(in float minLight, in float maxLight, in float minEmissionMultiplier, in float maxEmissionMultiplier, in float enabled, in float worldOrMesh, in PoiLight poiLight)
{
	float glowInTheDarkMultiplier = 1;
	//UNITY_BRANCH
	if (enabled)
	{
		#ifdef POI_LIGHTING
			float3 lightValue = worldOrMesh ? calculateluminance(poiLight.finalLighting.rgb) : calculateluminance(poiLight.directLighting.rgb);
			float gitdeAlpha = saturate(inverseLerp(minLight, maxLight, lightValue));
			glowInTheDarkMultiplier = lerp(minEmissionMultiplier, maxEmissionMultiplier, gitdeAlpha);
		#endif
	}
	return glowInTheDarkMultiplier;
}

float calculateScrollingEmission(in float3 direction, in float velocity, in float interval, in float scrollWidth, float offset, float3 position)
{
	float phase = 0;
	phase = dot(position, direction);
	phase -= (_Time.x + offset) * velocity;
	phase /= interval;
	phase -= floor(phase);
	phase = saturate(phase);
	return(pow(phase, scrollWidth) + pow(1 - phase, scrollWidth * 4)) * 0.5;
}

float calculateBlinkingEmission(in float blinkMin, in float blinkMax, in float blinkVelocity, float offset)
{
	float amplitude = (blinkMax - blinkMin) * 0.5f;
	float base = blinkMin + amplitude;
	return sin((_Time.x + offset) * blinkVelocity) * amplitude + base;
}
#endif