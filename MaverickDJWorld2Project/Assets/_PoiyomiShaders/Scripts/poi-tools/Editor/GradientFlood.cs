using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

//Made by Dreadrith#3238
//Discord: https://discord.gg/ZsPfrGn
//Github: https://github.com/Dreadrith/DreadScripts
//Gumroad: https://gumroad.com/dreadrith
//Ko-fi: https://ko-fi.com/dreadrith

namespace DreadScripts
{
    public class GradientFlood : EditorWindow
    {

        #region Automated Variables
        private static float modifiedBoundRange;
        private static Texture2D titleTexture;
        #endregion

        #region Input
        public static Texture2D pathTexture;
        public static Color startPixelsColor = Color.red;
        public static Color limitPixelsColor = Color.white;
        public static GradientType gradientType = GradientType.DataGradient;

        public static Color tintColor = Color.white;
        public static Gradient gradientColor = new Gradient();

        public static float gradientDistribution = 1;
        public static float startColorTolerance = 0.05f;
        public static float limitColorTolerance = 0.05f;
        public static float rangeLowerBound;
        public static float rangeUpperBound = 255;
        public static bool invertGradient;
        public static bool loopGradient;
        public static bool applyGradientAlpha;
        #endregion

        public event EventHandler TextureGenerated;
        public class TextureGeneratedEventArgs : EventArgs
        {
            public Texture2D generated_texture;
            public Texture2D original_texture;
        }

        public enum GradientType
        {
            DataGradient,
            TintedGradient,
            GradientGradient
        }
        [MenuItem("Tools/Poi/Gradient Flood")]
        private static void showWindow()
        {
            EditorWindow w = GetWindow<GradientFlood>(false, "Gradient Flood", true);
            if (!titleTexture)
            {
                titleTexture = GetColors((Texture2D)EditorGUIUtility.IconContent("Texture2D Icon").image, 16, 16, out _);
                titleTexture.Apply();
            }

            w.titleContent.image = titleTexture;
            w.minSize = new Vector2(423, 253);
        }

        private void OnGUI()
        {
            GUIStyle centeredTitle = new GUIStyle("boldlabel") {alignment = TextAnchor.MiddleCenter, fontSize = 16};
            using (new GUILayout.VerticalScope("helpbox"))
            {
                GUILayout.Label("Texture", centeredTitle);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    EditorGUIUtility.labelWidth = 1;
                    pathTexture = (Texture2D) EditorGUILayout.ObjectField(string.Empty, pathTexture, typeof(Texture2D), false, GUILayout.Width(80), GUILayout.Height(80));
                    EditorGUIUtility.labelWidth = 0;

                    GUILayout.FlexibleSpace();
                }

                using (new GUILayout.HorizontalScope("box"))
                    gradientType = (GradientType)EditorGUILayout.EnumPopup("Gradient Type", gradientType);



                switch (gradientType)
                {
                    case GradientType.TintedGradient:
                        using (new GUILayout.HorizontalScope("box"))
                            tintColor = EditorGUILayout.ColorField("Tint", tintColor);
                        break;
                    case GradientType.GradientGradient:
                        using (new GUILayout.HorizontalScope("box"))
                            gradientColor = EditorGUILayout.GradientField("Gradient", gradientColor);
                        break;
                }


                if (gradientType > 0)
                {
                    using (new GUILayout.HorizontalScope("box"))
                    {
                        GUILayout.Label("Color Range");

                        EditorGUI.BeginChangeCheck();

                        rangeLowerBound = EditorGUILayout.DelayedIntField((int) rangeLowerBound, GUI.skin.label, GUILayout.Width(28));
                        EditorGUILayout.MinMaxSlider(ref rangeLowerBound, ref rangeUpperBound, 0, 255);

                        rangeUpperBound = EditorGUILayout.DelayedIntField((int) rangeUpperBound, GUI.skin.label, GUILayout.Width(28));

                        if (EditorGUI.EndChangeCheck())
                        {
                            rangeUpperBound = Mathf.Clamp((int) rangeUpperBound, 0, 255);
                            rangeLowerBound = Mathf.Max(0, Mathf.Min(rangeUpperBound, rangeLowerBound));
                        }
                    }
                }
                

                using (new GUILayout.HorizontalScope("box"))
                {
                    using (new GUILayout.HorizontalScope())
                        GUILayout.Label("Tolerance");

                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUIUtility.labelWidth = 40;
                        startColorTolerance = EditorGUILayout.Slider("Start", startColorTolerance, 0, 1);
                        limitColorTolerance = EditorGUILayout.Slider("Limit", limitColorTolerance, 0, 1);
                        EditorGUIUtility.labelWidth = 0;
                    }
                }

                using (new GUILayout.HorizontalScope("box"))
                {
                    invertGradient = EditorGUILayout.Toggle("Invert Gradient", invertGradient);

                    if (gradientType > 0) applyGradientAlpha = EditorGUILayout.Toggle("Apply Gradient Alpha", applyGradientAlpha);
                }

                using (new GUILayout.HorizontalScope("box"))
                {
                    loopGradient = EditorGUILayout.Toggle("Loop Gradient", loopGradient);
                    if (loopGradient) gradientDistribution = EditorGUILayout.FloatField("Distribution", gradientDistribution);
                }

                using (new EditorGUI.DisabledScope(!pathTexture))
                    if (GUILayout.Button("Fill"))
                    {
                        Texture2D generated = GenerateFilledTexture(pathTexture);
                        TextureGeneratedEventArgs args = new TextureGeneratedEventArgs();
                        args.original_texture = pathTexture;
                        args.generated_texture = generated;
                        TextureGenerated?.Invoke(this, args);
                    }
                
            }
            Credit();
        }

        public static Texture2D GenerateFilledTexture(Texture2D texture)
        {
            if (!loopGradient) gradientDistribution = 1;
            if (gradientType == GradientType.DataGradient)
            {
                rangeLowerBound = 1;
                rangeUpperBound = 255;
            }

            int width = texture.width;
            modifiedBoundRange = rangeUpperBound / 255 - rangeLowerBound / 255;
            Texture2D gradientTexture = GetColors(texture, out Color[] ogColors);
            GradientFillPixel[] gradientPixels = new GradientFillPixel[ogColors.Length];

            Queue<GradientFillPixel> pixelsToExpand = new Queue<GradientFillPixel>();

            for (int i = 0; i < ogColors.Length; i++)
            {
                gradientPixels[i] = new GradientFillPixel(ogColors[i], i);
                if (!gradientPixels[i].isLimit && gradientPixels[i].isFilled) pixelsToExpand.Enqueue(gradientPixels[i]);
            }

            if (pixelsToExpand.Count == 0)
                Debug.LogWarning("<color=red>[GPFiller]</color> No start pixels were found! If start pixels exist, try adjusting the color tolerance.");

            bool IsValidFillIndex(int index, out GradientFillPixel pixelToFill)
            {

                if (index >= 0 && index < ogColors.Length)
                {
                    pixelToFill = gradientPixels[index];
                    if (!pixelToFill.isLimit && !pixelToFill.isFilled)
                    {
                        pixelsToExpand.Enqueue(pixelToFill);
                        return true;
                    }
                }

                pixelToFill = null;
                return false;
            }

            void FillIndex(GradientFillPixel fillerPixel, int index)
            {
                if (!IsValidFillIndex(index, out GradientFillPixel nextPixel)) return;
                nextPixel.gradientValue = fillerPixel.gradientValue + gradientDistribution;
                nextPixel.isFilled = true;
            }

            while (pixelsToExpand.Any())
            {
                var pixel = pixelsToExpand.Dequeue();

                bool isTopMost = pixel.arrayIndex < width;
                bool isBottomMost = pixel.arrayIndex >= ogColors.Length - width;
                bool isLeftMost = pixel.arrayIndex % width == 0;
                bool isRightMost = (pixel.arrayIndex + 1) % width == 0;

                if (!isRightMost)
                    FillIndex(pixel, pixel.arrayIndex + 1);

                if (!isLeftMost)
                    FillIndex(pixel, pixel.arrayIndex - 1);

                if (!isBottomMost)
                    FillIndex(pixel, pixel.arrayIndex + width);

                if (!isTopMost)
                    FillIndex(pixel, pixel.arrayIndex - width);

                if (!isBottomMost && !isRightMost)
                    FillIndex(pixel, pixel.arrayIndex + width + 1);

                if (!isBottomMost && !isLeftMost)
                    FillIndex(pixel, pixel.arrayIndex + width - 1);

                if (!isTopMost && !isRightMost)
                    FillIndex(pixel, pixel.arrayIndex - width + 1);

                if (!isTopMost && !isLeftMost)
                    FillIndex(pixel, pixel.arrayIndex - width - 1);
            }


            float maxGradientValue = gradientPixels.Max(p => p.gradientValue);
            if (gradientType == GradientType.DataGradient) maxGradientValue /= 4;

            for (int i = 0; i < ogColors.Length; i++)
            {
                var f = gradientPixels[i].GetFloatValue(maxGradientValue);

                if (f == 0) ogColors[i] = Color.clear;
                else
                {

                    if (gradientType == GradientType.TintedGradient)
                        ogColors[i] = new Color(f * tintColor.r, f * tintColor.g, f * tintColor.b, gradientPixels[i].isLimit ? 1 : (applyGradientAlpha ? f : 1) * tintColor.a);
                    else if (gradientType == GradientType.GradientGradient)
                    {
                        Color gradColor = gradientColor.Evaluate(f);
                        gradColor.a = gradientPixels[i].isLimit ? 1 : (applyGradientAlpha ? f : 1) * gradColor.a;
                        ogColors[i] = gradColor;
                    }
                    else
                    {
                        if (f <= 1) ogColors[i] = new Color(f % 1, 0, 0, 0);

                        else if (f <= 2) ogColors[i] = new Color(0, f%1, 0, 0);

                            else if (f <= 3) ogColors[i] = new Color(0, 0, f % 1, 0);

                        else ogColors[i] = new Color(0, 0, 0, f % 1);
                        
                    }
                }
            }

            gradientTexture.SetPixels(ogColors);
            gradientTexture.Apply();

            string assetPath = AssetDatabase.GetAssetPath(pathTexture);
            string ext = Path.GetExtension(assetPath);
            byte[] data;
            switch (ext)
            {
                case ".jpeg" when !applyGradientAlpha && gradientType != GradientType.DataGradient:
                case ".jpg" when !applyGradientAlpha && gradientType != GradientType.DataGradient:
                    ext = ".jpg";
                    data = gradientTexture.EncodeToJPG(100);
                    break;
                case ".tga":
                    data = gradientTexture.EncodeToTGA();
                    break;
                default:
                    ext = ".png";
                    data = gradientTexture.EncodeToPNG();
                    break;
            }


            DestroyImmediate(gradientTexture);

            string savePath = AssetDatabase.GenerateUniqueAssetPath($"{Path.GetDirectoryName(assetPath)}/{pathTexture.name}{ext}");
            SaveTexture(data, savePath);
            CopyTextureSettings(assetPath, savePath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(savePath);
        }

        public static Texture2D GetColors(Texture2D texture, int width, int height, out Color[] Colors, bool unloadTempTexture = false)
        {
            //Thanks to
            //https://gamedev.stackexchange.com/questions/92285/unity3d-resize-texture-without-corruption
            texture.filterMode = FilterMode.Point;
            RenderTexture rt = RenderTexture.GetTemporary(width, height);

            rt.filterMode = FilterMode.Point;
            RenderTexture.active = rt;
            Graphics.Blit(texture, rt);
            Texture2D newTexture = new Texture2D(width, height);
            newTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            Color[] myColors = newTexture.GetPixels();
            RenderTexture.active = null;
            /////////////////////
            Colors = myColors;
            if (unloadTempTexture)
            {
                DestroyImmediate(newTexture);
                return null;
            }
            return newTexture;
        }

        public static Texture2D GetColors(Texture2D texture, out Color[] Colors, bool unloadTempTexture = false)
        {
            return GetColors(texture, texture.width, texture.height, out Colors, unloadTempTexture);
        }

        private static void CopyTextureSettings(string from, string to)
        {
            TextureImporter source = (TextureImporter)AssetImporter.GetAtPath(from);
            TextureImporterSettings sourceSettings = new TextureImporterSettings();
            source.ReadTextureSettings(sourceSettings);

            TextureImporter destination = (TextureImporter)AssetImporter.GetAtPath(to);
            destination.SetTextureSettings(sourceSettings);
            destination.maxTextureSize = source.maxTextureSize;
            destination.textureCompression = source.textureCompression;
            destination.crunchedCompression = source.crunchedCompression;
            destination.SaveAndReimport();
        }

        private static void SaveTexture(byte[] textureEncoding, string path)
        {
            using (System.IO.FileStream stream = System.IO.File.Create(path))
                stream.Write(textureEncoding, 0, textureEncoding.Length);
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(path));
        }

        public class GradientFillPixel
        {
            public readonly bool isLimit;
            public bool isFilled;
            public float gradientValue;
            public int arrayIndex;

            public GradientFillPixel(Color pixelColor, int index)
            {
                isLimit = Mathf.Abs(pixelColor.r - limitPixelsColor.r) < limitColorTolerance &&
                          Mathf.Abs(pixelColor.g - limitPixelsColor.g) < limitColorTolerance &&
                          Mathf.Abs(pixelColor.b - limitPixelsColor.b) < limitColorTolerance;

                isFilled = Mathf.Abs(pixelColor.r - startPixelsColor.r) < startColorTolerance &&
                           Mathf.Abs(pixelColor.g - startPixelsColor.g) < startColorTolerance &&
                           Mathf.Abs(pixelColor.b - startPixelsColor.b) < startColorTolerance;

                gradientValue = (int)rangeLowerBound;
                arrayIndex = index;
            }

            public float GetFloatValue(float maxGradientValue)
            {
                if (isLimit || !isFilled) return 0;
                float floatValue = !loopGradient ? gradientValue / maxGradientValue : (gradientValue % 255 * (gradientType == GradientType.DataGradient ? 4 : 1)) / 255f;
                float adjustedValue = floatValue * modifiedBoundRange + rangeLowerBound / 255;
                return invertGradient ? (gradientType == GradientType.DataGradient ? 4 : 1) - adjustedValue : adjustedValue;
            }
        }

        private static void Credit()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Made By Dreadrith#3238", "boldlabel"))
                    Application.OpenURL("https://linktr.ee/Dreadrith");
            }
        }
    }


}
