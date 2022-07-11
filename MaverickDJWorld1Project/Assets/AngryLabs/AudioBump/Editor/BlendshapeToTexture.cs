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

namespace AngryLabs.AudioBump
{
    [ExecuteInEditMode]
    public class BlendshapeToTextures : EditorWindow
    {


        public event EventHandler TextureGenerated;
        public class TextureGeneratedEventArgs: EventArgs{
            public Texture generated_texture;
            public Texture original_texture;
        }

        int ShapeCount
        {
            get { return _shapes.Count; }
        }

        string SavePath { get; set; }

        SkinnedMeshRenderer _skin;
        List<string> _blendshapeNames = new List<string>();
        Vector2 scrollPos;
        List<int> _shapes = new List<int>() { 0 };

        private static void ShowWindow()
        {
            var window = GetWindow<BlendshapeToTextures>();
            window.titleContent = new GUIContent("Blendshape To Texture");
            window.Show();
        }

        private GameObject _target;
        private GUIStyle _style;

        [MenuItem("Tools/AngryLabs/Bake Blendshape To Texture")]
        static void Init()
        {
            //var window =  GetWindowWithRect<BlendshapeToTextures>(new Rect(0, 0, 200, 500));
            var window = GetWindow(typeof(BlendshapeToTextures));
            window.minSize = new Vector2(250, 300);
            window.Show();
        }

        public static string [] GetBlendShapeNames (Mesh m)
        {
            string[] arr;
            arr = new string [m.blendShapeCount];
            for (int i= 0; i < m.blendShapeCount; i++)
            {
                string s = m.GetBlendShapeName(i);
                arr[i] = s;
            }
            return arr;
        }

        static void writePixel(Texture2D tex, int index, int value) {
            writePixel(tex, index, new Color(value, 0, 0, 0));
        }

        static void writePixel(Texture2D tex, int index, Vector3 value)
        {
            writePixel(tex, index, new Vector4(value.x, value.y, value.z, 0.0f));
        }

        static void writePixel(Texture2D tex, int index, Vector4 value)
        {
            writePixel(tex, index, new Color(value.x, value.y, value.z, value.w));
        }

        static void writePixel(Texture2D tex, int index, Color col)
        {
            var location = IndexToLocation(tex, index);

            int x = location.Item1;
            int y = location.Item2;

            tex.SetPixel(x, y, col);
        }

        // Floats per Axis.
        public const int Types = 2;

        int DataPoints
        {
            get
            {
                int verts = _skin.sharedMesh?.vertexCount ?? 0;
                return verts * Types * ShapeCount + 1;
            }
        }

        int TextureSideLength
        {
            get
            {
                int sizeRoot = Mathf.CeilToInt(Mathf.Sqrt(DataPoints));
                return (int)Mathf.Pow(2, Mathf.CeilToInt(Mathf.Log(sizeRoot) / Mathf.Log(2)));
            }
        }

        static Vector3 BiTangnent(Vector3 norm, Vector4 tangnet)
        {
            Vector3 lt = new Vector3(tangnet.x, tangnet.y, tangnet.z);
            return Vector3.Cross(norm.normalized, lt.normalized) * tangnet.w;
        }

        void OnBake() 
        {
            var finished = new Action<Texture, Texture2D>((x, y) =>
           {
               TextureGeneratedEventArgs ea = new TextureGeneratedEventArgs()
               {
                   generated_texture = y,
                   original_texture = x,
               };
               TextureGenerated?.Invoke(this, ea);
           });
            Bake(_skin.sharedMesh, _shapes, SavePath, finished);
        }

        public static void Bake(Mesh mesh, List<int> shapes, string outputFileName, Action<Texture2D, Texture2D> OnBakeFinished = null)
        {
            int verts = mesh.vertexCount;

            int shapeCount = shapes.Count;
            int dataPoints = verts * Types * shapeCount + 1;
            int sizeRoot = Mathf.CeilToInt(Mathf.Sqrt(dataPoints));
            int textureSideLength = (int)Mathf.Pow(2, Mathf.CeilToInt(Mathf.Log(sizeRoot) / Mathf.Log(2)));
            //int textureSideLength = sizeRoot;

            Texture2D tex = new Texture2D(textureSideLength, textureSideLength, TextureFormat.RGBAFloat, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
                anisoLevel = 1,
            };

            for(int i=0; i<tex.width * tex.width; i++)
            {
                writePixel(tex, i, Color.magenta);
            }

            int index = 0;
            writePixel(tex, index++, new Color(mesh.vertexCount, shapeCount, 0, 0));

            void ConvertAndSave(Texture2D _tex, int _index, Vector3 normal, Vector4 tangent, Vector3 delta)
            {
                Vector3 coTan = BiTangnent(normal, tangent);
                tangent.w = 0;

                float tn = Vector3.Project(delta, normal).magnitude * Mathf.Sign(Vector3.Dot(delta, normal));
                float tt = Vector3.Project(delta, tangent).magnitude * Mathf.Sign(Vector3.Dot(delta, tangent));
                float tb = Vector3.Project(delta, coTan).magnitude * Mathf.Sign(Vector3.Dot(delta, coTan));

                Vector3 toWrite = new Vector3(tn, tt, tb);
                writePixel(_tex, _index, toWrite);
            }

            Vector3[] dV = new Vector3[mesh.vertexCount];
            Vector3[] dN = new Vector3[mesh.vertexCount];
            Vector3[] dT = new Vector3[mesh.vertexCount];
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;


            for (int i = 0; i < shapeCount; i++) {
                int shape = shapes[i];
                mesh.GetBlendShapeFrameVertices(shape, 0, dV, dN, dT);
                for (int currentVert = 0; currentVert < mesh.vertexCount; currentVert++) {
                    int progress = (i * mesh.vertexCount + currentVert);
                    if(progress % 100 == 0)
                    {
                        EditorUtility.DisplayProgressBar("Baking blenshapes", $"Shapkey {i+1} of {shapeCount}", progress / (float)(shapeCount * mesh.vertexCount));
                    }

                    Vector3 normal = normals[currentVert];
                    Vector4 tangent = tangents[currentVert];
                    Vector3 position = dV[currentVert];
                    Vector3 dNormal = dN[currentVert];

                    ConvertAndSave(tex, index++, normal, tangent, position);
                    ConvertAndSave(tex, index++, normal, tangent, dNormal);
                }
            }
            EditorUtility.ClearProgressBar();

            tex.Apply();
            var old = AssetDatabase.LoadAssetAtPath<Texture2D> (outputFileName);
            if( old == null)
            {
                AssetDatabase.CreateAsset(tex, outputFileName);
            }
            else
            {
                if( old.format != TextureFormat.RGBAFloat)
                {
                    bool del = EditorUtility.DisplayDialog("Critical error", "The file specified is of the wrong format. Would you like to delete it?", "yes", "no");
                    if (del)
                    {
                        AssetDatabase.DeleteAsset(outputFileName);
                        AssetDatabase.CreateAsset(tex, outputFileName);
                    }
                    else
                    {
                        return;
                    }
                }
                old.Resize(tex.width, tex.width);

                for(int i=0; i<tex.width; i++)
                {
                    for(int j=0; j<tex.width; j++)
                    {
                        old.SetPixel(i, j, tex.GetPixel(i, j));
                    }
                }
                old.Apply();
            }

            AssetDatabase.SaveAssets();
             OnBakeFinished?.Invoke(old, tex);
        }

        static Tuple<int,int> IndexToLocation(Texture2D tex, int index)
        {
            int x = index % tex.width;
            int y = index / tex.width;
            return new Tuple<int, int>(x, y);
        }

        Vector4 ReadPixel(Texture2D tex, int index)
        {
            var location = IndexToLocation(tex, index);
            int x = location.Item1;
            int y = location.Item2;
            Color c = tex.GetPixel(x, y);

            return new Vector4(c.r, c.g, c.b, c.a);
        }

        public static string ToCSV(object obj)
        {
            Vector3? v3 = obj as Vector3?;
            Vector3? v2 = obj as Vector2?;
            float? f = obj as float?;

            if(v3 != null)
            {
                return $"{v3.Value.x},{v3.Value.y},{v3.Value.z}";
            }
            if(v2 != null)
            {
                return $"{v2.Value.x},{v2.Value.y}";
            }


            Vector4? v4 = obj as Vector4?;
            if(v4 != null)
            {
                return $"{v4.Value.x},{v4.Value.y},{v4.Value.z},{v4.Value.w}";
            }
     
            if(f != null)
            {
                return $"{f.Value}";
            }

            double? d = obj as double?;
            if(d != null)
            {
                return $"{d.Value}";
            }

            int? i = obj as int?;
            if(i != null)
            {
                return $"{i.Value}";
            }

            bool? b = obj as bool?;
            if(b != null)
            {
                return b.Value ? "true" : "false";
            }

            throw new Exception($"Don't know how to convert type [{obj.GetType()}] into CSV");
        }

        private void DumpCSV(Texture2D tex)
        {
            string fname = $"Assets/AngryLabs/AudioBump/output/{_skin.name}.csv";
            if (File.Exists(fname))
            {
                File.Delete(fname);
            }
            using(var file = File.OpenWrite(fname))
            {
                var bb = Encoding.ASCII.GetBytes($"vertex,shape,nx,ny,nz,tx,ty,tz,px,py,pz,nnx,nny,nnz,bpX,bpY,bpZ,bnX,bnY,bnZ,btX,btY,btZ,tanX,tanY,tanZ,tanW,coX,coY,coZ\n");
                file.Write(bb, 0, bb.Length);

                int dataPoint = 1;


                var mesh = _skin.sharedMesh;
                for(int shape=0; shape < ShapeCount; shape ++)
                {
                    Vector3[] blendP = new Vector3[mesh.vertexCount];
                    Vector3[] blendT = new Vector3[mesh.vertexCount];
                    Vector3[] blendN = new Vector3[mesh.vertexCount];

                    mesh.GetBlendShapeFrameVertices(_shapes[shape], 0, blendP, blendN, blendT);

                    for(int index=0; index < _skin.sharedMesh.vertexCount; index++)
                    {
                        Vector4 nn = ReadPixel(tex, dataPoint++);
                        Vector4 tt = ReadPixel(tex, dataPoint++);


                        Vector3 vert = _skin.sharedMesh.vertices[index];
                        Vector3 norm = _skin.sharedMesh.normals[index];
                        Vector4 tangent = _skin.sharedMesh.tangents[index];
                        Vector3 coTan = BiTangnent(norm, tangent);

                        var ll = new List<object>
                        {
                            index, shape,
                            nn.x, nn.y, nn.z,
                            tt.x, tt.y, tt.z,
                            vert,
                            norm,
                            blendP[index],
                            blendN[index],
                            blendT[index],
                            tangent,
                            coTan,
                        };

                        string line = ll.Select(x => ToCSV(x)).Aggregate((a, b) => $"{a},{b}");

                        bb = Encoding.ASCII.GetBytes($"{line}\n");
                        file.Write(bb, 0, bb.Length);
                    }
                }
            }
        }

        bool _dumpCSV;

        void OnGUI()
        {
            GUILayout.Label("Bake Blendshapes", EditorStyles.boldLabel);
            GUILayout.ExpandWidth(true);

            EditorGUILayout.BeginVertical();
            SkinnedMeshRenderer old = _skin;

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
                _blendshapeNames = new List<string>(GetBlendShapeNames(_skin.GetComponent<SkinnedMeshRenderer>().sharedMesh));

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
                    OnBake();
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif
