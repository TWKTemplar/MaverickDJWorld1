[Space(4)]
[ThryToggleUI(true)]_VertexAudioLinkEnabled ("<size=13><b>  Audio Link</b></size>--{condition_showS:(_EnableAudioLink==1)}", Float) = 0

[Vector2]_AudioLinkEmission1StrengthMod ("Emission Strength Add--{ condition_showS:(_AudioLinkEmission1Enabled==1 && _EnableAudioLink==1)}", Vector) = (0, 0, 0, 0)
[Enum(Bass, 0, Low Mid, 1, High Mid, 2, Treble, 3)] Band ("Band--{ condition_showS:(==1 && ==1)}", Int) = 0
[ThryWideEnum(Motion increases as intensity of band increases, 0, Above but Smooth, 1, Motion moves back and forth as a function of intensity, 2, Above but Smoooth, 3, Fixed speed increase when the band is dark Stationary when light, 4, Above but Smooooth, 5, Fixed speed increase when the band is dark Fixed speed decrease when light, 6, Above but Smoooooth, 7)]_VertexLocalRotationCTALType ("Motion Type--{ condition_showS:(_EnableAudioLink==1 && ==1)}", Int) = 0

#ifdef COLOR_GRADING_LOG_VIEW
	if (poiMods.audioLinkAvailable)
	{

	}
#endif