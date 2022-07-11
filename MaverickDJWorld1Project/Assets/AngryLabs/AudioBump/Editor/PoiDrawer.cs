#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System;
using System.Text;
using UnityEngine.Assertions;
using System.Linq;
using Poiyomi.ModularShaderSystem;
using System.Text.RegularExpressions;

//using Poiyomi.ModularShaderSystem;

// PoiyomiPatreon.Scripts.poi_tools.Editor

namespace AngryLabs.AudioBump {
    [ExecuteInEditMode]
    public class PoiDrawer : EditorWindow
    {
        public event EventHandler TextureGenerated;
        public class TextureGeneratedEventArgs: EventArgs{
            public Texture generated_texture;
            public Texture original_texture;
        }

        //[MenuItem("Tools/AngryLabs/Easy Install Audio Bump")]
        static void Init()
        {
            //var window =  GetWindowWithRect<BlendshapeToTextures>(new Rect(0, 0, 200, 500));
            var window = GetWindow(typeof(PoiDrawer));
            window.minSize = new Vector2(250, 300);
            window.Show();
        }


        private static void ShowWindow()
        {
            var window = GetWindow<PoiDrawer>();
            window.titleContent = new GUIContent("Blendshape To Texture");
            window.Show();
        }

        string _fileName = "";
        Texture2D texture;
        SkinnedMeshRenderer _skin;
        List<string> _blendShapeNames;
        Vector2 scrollPos;
        List<int> _shapes = new List<int> { 0 };


        void OnGUI()
        {
            GUILayout.Label("Bake Blendshapes", EditorStyles.boldLabel);
            GUILayout.ExpandWidth(true);

            using (new GUILayout.VerticalScope())
            {
                SkinnedMeshRenderer old = _skin;
                _skin = (SkinnedMeshRenderer) EditorGUILayout.ObjectField("Skinned Mesh", _skin, typeof(SkinnedMeshRenderer), true);
                if (_skin == null)
                {
                    this.ShowNotification(new GUIContent("No Skinned Mesh selected"));
                    return;
                }

                if(old != _skin && _skin != null)
                {
                    _fileName = "Assets/AngryLabs/AudioBump/output/" + _skin.name + "_blendshapes_Baked.asset";
                }

                this.RemoveNotification();

                _blendShapeNames = new List<string>(BlendshapeToTextures.GetBlendShapeNames(_skin.GetComponent<SkinnedMeshRenderer>().sharedMesh));

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos,  GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                if(_shapes.Count < 4)
                {
                    if( GUILayout.Button("Add shape"))
                    {
                        _shapes.Add(0);
                    }
                }
                else
                {
                    GUILayout.Label("Can not add another");
                }

                if (_shapes.Count > 1) {
                    if (GUILayout.Button("Remove shape"))
                    {
                        _shapes.RemoveAt(_shapes.Count - 1);
                    }
                }
                else
                {
                    GUILayout.Label("Can not remove a blendshape");
                }

                var nameArray = _blendShapeNames.ToArray();
                for(int i=0; i<_shapes.Count; i++)
                {
                    EditorGUILayout.LabelField($"Shape {i+1}", EditorStyles.label);
                    _shapes[i] = EditorGUILayout.Popup(_shapes[i], nameArray);
                }

                //For debugging. Undocumented.
                //_dumpCSV = EditorGUILayout.Toggle("Dump CSV?", _dumpCSV);

                GUILayout.Label("Save Path");
                _fileName = GUILayout.TextField(_fileName);

                int verts = _skin.sharedMesh.vertexCount;
                int shapeCount = _shapes.Count;
                int dataPoints = verts * BlendshapeToTextures.Types * shapeCount + 1;
                int sizeRoot = Mathf.CeilToInt(Mathf.Sqrt(dataPoints));
                int textureSideLength = (int)Mathf.Pow(2, Mathf.CeilToInt(Mathf.Log(sizeRoot) / Mathf.Log(2)));

                GUILayout.Label($"Verticies: {_skin.sharedMesh.vertexCount}");
                GUILayout.Label($"Data Points: {dataPoints}");
                GUILayout.Label($"Texture SideLength: {textureSideLength}");
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Bake!") && _skin != null) {
                    var finished = new Action<Texture, Texture2D>((x, y) =>
                   {
                       TextureGeneratedEventArgs ea = new TextureGeneratedEventArgs()
                       {
                           generated_texture = y,
                           original_texture = x,
                       };
                       TextureGenerated?.Invoke(this, ea);
                   });
                    BlendshapeToTextures.Bake(_skin.sharedMesh, _shapes, _fileName, finished);
                }

                GUILayout.Space(20);
            }
        }
    }

    #endif
    /*
            _skin = (SkinnedMeshRenderer) EditorGUILayout.ObjectField("Skinned Mesh", _skin, typeof(SkinnedMeshRenderer), true);
            if (_skin == null)
            {
                this.ShowNotification(new GUIContent("No Skinned Mesh selected"));
            }
            else
            {
                if(old != _skin && _skin != null)
                {
                    SavePath = "Assets/AngryLabs/AudioBump/output/" + _skin.name + "_blendBaked.asset";
                }

                this.RemoveNotification();
                _blendshapeNames = new List<string>(getBlendShapeNames(_skin.GetComponent<SkinnedMeshRenderer>().sharedMesh));

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos,  GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                if(_shapes.Count < 4)
                {
                    if( GUILayout.Button("Add shape"))
                    {
                        _shapes.Add(0);
                    }
                }
                else
                {
                    GUILayout.Label("Can not add another");
                }

                if (ShapeCount > 1) {
                    if (GUILayout.Button("Remove shape"))
                    {
                        _shapes.RemoveAt(_shapes.Count - 1);
                    }
                }
                else
                {
                    GUILayout.Label("Can not remove a blendshape");
                }

                GUILayout.Space(30);

                var nameArray = _blendshapeNames.ToArray();
                for(int i=0; i<ShapeCount; i++)
                {
                    EditorGUILayout.LabelField($"Shape {i+1}", EditorStyles.label);
                    _shapes[i] = EditorGUILayout.Popup(_shapes[i], nameArray);
                }

                //For debugging. Undocumented.
                //_dumpCSV = EditorGUILayout.Toggle("Dump CSV?", _dumpCSV);

                GUILayout.Label("Save Path");
                SavePath = GUILayout.TextField(SavePath);

                GUILayout.Label($"Verticies: {_skin.sharedMesh.vertexCount}");
                GUILayout.Label($"Data Points: {DataPoints}");
                GUILayout.Label($"Texture SideLength: {TextureSideLength}");
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Bake!") && _skin != null) {
                    onBake();
                }
            }
            */


}
