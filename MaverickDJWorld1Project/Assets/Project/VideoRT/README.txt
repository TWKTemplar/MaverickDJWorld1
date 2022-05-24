This project shows how to feed video player output into a custom render texture without using a camera. The custom render texture can be shared by both AVPro and Unity video players. It is based on the UdonSyncPlayer example in VRCSDK3.

Requirement:
VRChat world SDK3

Note:
If you have multiple video players in your world, please duplicate "Video" and "VideoBlit" for each video player.

How it works:
- VRCUnityVideoPlayer has a "render texture" render mode, so it can output to the render texture "Video" directly.
- VRCAVProVideoScreen can't output to render texture directly, but it can override the texture slot in a material. A special shader "BlitCRT" is put on the material "VideoBlit" to copy the video texture to the render texture "Video".