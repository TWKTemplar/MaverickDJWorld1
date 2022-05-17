AreaLit is a high performance realtime area lighting system with support for arbitrary meshes and multiple lights. It is designed for forward rendering in Unity 2019's built-in render pipeline, and compatible with VRChat (PC/VR/Quest) and Cluster (PC/VR/Mac). Area lights are created by assigning a shader, and the result can be viewed in scene without entering play mode.

# Installation

- The only dependency is Unity (tested on 2019.4).
- Import this folder into your Unity project.
- Open the demo scene under Example folder.

If you are using the trial version, it might not contain all features and it won't work in Unity build.

# Overview

AreaLit consists of four parts:
- A family of AreaLit/Standard shaders receive area lights and does Unity Standard shading.
- An AreaLit/LightMesh shader emits area light.
- An AreaLit/Projector shader projects area lights onto non-AreaLit materials like avatars.
- A LightCam prefab registers the positions and textures of area lights in the scene.

Please refer to the end of this document for a breakdown of the demo scene.

# Quick instruction

- Drag the LightCam.prefab into your scene.
- Create a material with AreaLit/LightMesh shader. The intensity is adjustable in its "Color" field picker.
- Add a material slot to your video screen MeshRenderer, and drag the newly created material to the slot.
- Set the layer of your video screen to "TransparentFX" so its light is registered in LightCam system.
- For every meshRenderer that needs to be affected by video light, set its material to AreaLit/Standard shader.
- In AreaLit/Standard inspector, drag LightMesh.renderTexture into "Mesh" slot, and drag your video renderTexture into "Texture 0" slot. If you are not sure about the video renderTexture, please try my free prefab VideoRT: <https://drive.google.com/file/d/1XQBybXg2D87AueLI87UuujA3jDN4fU33/view?usp=sharing>

# Features

## Arbitrary light shape

Area lights support any mesh, not limited to quad. It *generates a quad light for each polygon* (triangle or rectangle) in the mesh.

Note: A complex mesh like cylinder generates lots of quads. Please try a low-poly version for the light source.

Note for Cluster user: due to Metal limitation, only quad and cube meshes are supported on Mac build.

## Easy light toggle

An area light is enabled when an AreaLit/LightMesh material is seen by LightMeshCam camera. Adjust camera properties "Depth" and "Clipping plane" to accommodate lights in a bigger area. Adjust camera's culling mask to other layer if you reserve "TransparentFX" for other purpose.

Note: The poly count of lights is limited to 16 because it's *very expensive*. Increase the height of LightMesh renderTexture to raise this limit (up to 63).

## Opaque light

Area lights occlude each other by default. For example, a black light casts shadow if there is an area light behind it. If additive lighting is desired, decrease the alpha value of light color in AreaLit/LightMesh.

Note: The implementation of occlusion is not perfect. It can be turned off in "Opaque Lights" checkbox.

## Light texture

The AreaLit/Standard shader supports three dynamic light textures "Texture 0" ~ "Texture 2", and unlimited static light texture through texture array "Texture 3+". Its i-th texture in the array corresponds to "Texture Index" i+3. Mip maps and trilinear filtering are required to perform blurring. Anisotropic filtering is strongly recommended.

## Shadow mask

The AreaLit/Standard shader supports shadow mask through occlusion map. It attenuates every area light by a channel value specified in AreaLit/LightMesh.

## Dynamic lightmap

Area lights can be configured to emit indirect light. When "Dynamic Lightmap" is checked in AreaLit/LightMesh, the camera LightTexCam will render the light mesh into a lightmap renderTexture. Make sure the correct lightmap uv channel is selected to avoid overlapping results. If you are not using this feature, please disable LightTexCam for better performance.

Note: Due to a GPU limitation (render target can't be used as a texture input at the same time), the renderTexture needs to be copied to work in dynamic lightmap rendering. Failure to do so will result in one-bounce instead of multi-bounce.

## Light projector

A non-AreaLit material can be affected by area lights using a projector with AreaLit/Projector shader. Adjust the RGB value of projector color to change the intensity, and the alpha value to change the base light level.

Note: Make sure player avatars are affected by some ambient light, because projector works by attenuating the existing color.

## Quest support

AreaLit has been optimized and tested on Oculus Quest 2. To achieve acceptable framerate, please refrain from using more than 3 quad lights. Disabling specular and opaque lights improves performance too.

# How does the demo scene work?

- LightMeshCam saves the positions of area lights in the scene into LightMesh renderTexture. Area lights should stay on a layer seen by this camera (in this case, TransparentFX layer).
- Screen and Text have two materials: Unlit and AreaLit/LightMesh. They emit lights and are not affected by lights.
- Room has a AreaLit/Standard material. It is affected by area lights.
- Cube is similar to Screen and Text except that its emission part is moved into a separate object CubeLight. If you disable CubeLight, it no longer emits light.
- In CubeLight's material, "Texture Index" field is set to -1, so it displays pure color rather than video texture.
- Room has a AreaLit/Standard material. It is affected by area lights.
- In Room's material, "Mesh" slot is set to LightMesh, "Texture 0" is set to video renderTexture and "Texture 1" is set to text texture, so the shader knows the positions and textures of area lights in the scene.
- RoomLight is a special light emitter for indirect light reflected from other sources. If you disable RoomLight, it loses global illumination.
- In RoomLight's material, "Texture Index" field is set to 2 and "Dynamic Lightmap" is checked, so LightTexCam saves its lighting into Lightmap renderTexture. "Texture 2" slots need to set to Lightmap renderTexture to match texture index 2.
- Player has a non-AreaLit shader, but it is still affected by area lights due to Projector.
- The scene has a gray ambient color, so Player isn't completely black without Projector.