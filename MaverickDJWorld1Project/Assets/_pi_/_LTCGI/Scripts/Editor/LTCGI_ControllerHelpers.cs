#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#endif

namespace pi.LTCGI
{
    #if UNITY_EDITOR
    public partial class LTCGI_Controller
    {
        private float[] GetMaskForRenderer(LTCGI_Screen[] screens, MeshRenderer r)
        {
            Func<bool, float> b = cond => cond ? 1.0f : 0.0f;
            return Enumerable.Range(0, screens.Length)
                .Select(si => {
                    switch (screens[si].RendererMode)
                    {
                        case RendererMode.OnlyListed:
                            return b(!screens[si].RendererList.Contains(r));
                        case RendererMode.ExcludeListed:
                            return b(screens[si].RendererList.Contains(r));
                        case RendererMode.Distance:
                            var screenPos = screens[si].transform.position;
                            var point = r.bounds.ClosestPoint(screenPos);
                            var dist = Vector3.Distance(point, screenPos);
                            return b(dist > screens[si].RendererDistance);
                        default: // RendererMode.All
                            return b(false);
                    }
                })
                .ToArray();
        }

        private void SetTextureImporterToLightmap(Texture2D texture)
        {
            if (null == texture) return;
            string assetPath = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Lightmap)
            {
                importer.textureType = TextureImporterType.Lightmap;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
                AssetDatabase.Refresh();
            }
        }

        private void SetMeshImporterFormat(Mesh texture, bool readable)
        {
            if (null == texture) return;
            string assetPath = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer != null && importer.isReadable != readable)
            {
                importer.isReadable = readable;
                Debug.Log("LTCGI: Read/Write set for Model " + assetPath);
                importer.SaveAndReimport();
                AssetDatabase.Refresh();
            }
        }

        private void SetTextureImporterFormat(Texture2D texture, bool readable)
        {
            if (null == texture) return;
            string assetPath = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && importer.isReadable != readable)
            {
                importer.isReadable = readable;
                Debug.Log("LTCGI: Read/Write set for Texture " + assetPath);
                importer.SaveAndReimport();
                AssetDatabase.Refresh();
            }
        }

        private Texture2D ReadIntoTexture2D(RenderTexture renderTex)
        {
            Texture2D tex = new Texture2D(renderTex.width, renderTex.height, TextureFormat.RGBA32, false, true);
            RenderTexture.active = renderTex;
            tex.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            return tex;
        }

        private bool IsEditorOnly(GameObject obj)
        {
            return obj.tag == "EditorOnly";
        }
    }
    #endif
}