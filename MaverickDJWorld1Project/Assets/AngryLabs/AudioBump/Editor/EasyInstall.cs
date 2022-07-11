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
using System.Text.RegularExpressions;
using Poiyomi.ModularShaderSystem;

//using Poiyomi.ModularShaderSystem;

// PoiyomiPatreon.Scripts.poi_tools.Editor

[ExecuteInEditMode]
public class EasyInstall : EditorWindow
{
    [MenuItem("Tools/AngryLabs/Easy Install Audio Bump")]
    static void Init()
    {
        //var window =  GetWindowWithRect<BlendshapeToTextures>(new Rect(0, 0, 200, 500));
        var window = GetWindow(typeof(EasyInstall));
        window.minSize = new Vector2(250, 300);
        window.Show();

    }


    private static void ShowWindow()
    {
        var window = GetWindow<EasyInstall>();
        window.titleContent = new GUIContent("Easy Install");
        window.Show();

        RefreshShaderList();
    }

    bool MakeCopy { get; set; } = true;
    string CopyName { get; set; }
    string NewID{ get; set; }

    bool _showGenerateNotification = false;

    static List<ModularShader> _shaders;

    static List<Shader> _compiledShaders;

    static IEnumerable<T> FindAssetsByType<T>(string searchFolder = null)
    {
        AssetDatabase.Refresh();
        string[] guids;
        if(searchFolder != null)
        {
            guids  = AssetDatabase.FindAssets($"t:{typeof(T).ToString().Replace("UnityEngine.", "")}", new string[] { searchFolder });
        }
        else
        {
            guids = AssetDatabase.FindAssets($"t:{typeof(T).ToString().Replace("UnityEngine.", "")}");
        }

        var paths = guids.Select(x => AssetDatabase.GUIDToAssetPath(x)).ToList();

        var oassets = paths
            .Select(path => AssetDatabase.LoadMainAssetAtPath(path))
            .Where(x => x != null)
            .ToList()
            ;

        List<T> assets = new List<T>(oassets.Cast<T>());
        return assets;
    }

    int ShaderIndex { get; set; }

    ModularShader ActiveShader
    {
        get
        {
            return _shaders[ShaderIndex];
        }
    }

    void RegenerateAllAudioBump()
    {
        var audioBump = FindModuleById("AudioBump", "Assets/AngryLabs/AudioBump");
        RefreshShaderList();
        var hasAudio = _shaders.Where(ms => ms.BaseModules.Contains(audioBump)).ToList();
        for(int i=0; i < hasAudio.Count; i++)
        {
            ModularShader ms = hasAudio[i];
            EditorUtility.DisplayProgressBar("Generating shaders", $"Generating shader '{ms.Name}' {i+1} of {hasAudio.Count}", i / (float)(hasAudio.Count));

            ShaderGenerator.GenerateShader("Assets/AngryLabs/AudioBump/", ms);
        }

        EditorUtility.ClearProgressBar();
    }

    bool AlreadyInstalled
    {
        get
        {
            var modules = ActiveShader.BaseModules;

            return modules.Any(x => x?.Name == "AudioBump");
        }
    }

    bool ValidCopyName
    {
        get
        {
            if (_shaders == null) RefreshShaderList();
            return !_shaders.Any(x => x.Name == CopyName) && ! string.IsNullOrEmpty(CopyName);
        }
    }

    bool ValidNewID
    {
        get
        {
            if (_compiledShaders == null) RefreshShaderList();
            Regex re = new Regex($@".*{NewID}$");
            return !_compiledShaders.Any(x => re.IsMatch(x.name)) && ! string.IsNullOrEmpty(NewID) ;
        }
    }

    Vector2 ScrollPosition { get; set; }

    void OnGUI()
    {
        GUILayout.Label("Easy Install Audio Bump", EditorStyles.boldLabel);
        GUILayout.ExpandWidth(true);

        using(var sv = new GUILayout.ScrollViewScope(ScrollPosition))
        using (new GUILayout.VerticalScope())
        {
            ScrollPosition = sv.scrollPosition;

            MakeCopy = EditorGUILayout.Toggle("Make a copy", MakeCopy);

            if (MakeCopy)
            {
                EditorGUILayout.LabelField("Shader asset name", EditorStyles.label);
                CopyName = EditorGUILayout.TextField(CopyName);
                EditorGUILayout.LabelField("New shader Name", EditorStyles.label);
                NewID = EditorGUILayout.TextField(NewID);
            }

            if (_shaders == null || _compiledShaders == null || GUILayout.Button("Refresh Shader List"))
            {
                RefreshShaderList();
            }

            EditorGUILayout.LabelField($"Shader to install to", EditorStyles.label);
            ShaderIndex = EditorGUILayout.Popup(ShaderIndex, _shaders.Select(x => x.Name).ToArray());


            GUILayout.Space(20);
            if (AlreadyInstalled)
            {
                GUILayout.Label("Can not install.");
                GUILayout.Label("The shader already contains audio bump");
            }

            if (MakeCopy && (!ValidCopyName || !ValidNewID))
            {
                if (CopyName == "")
                {
                    GUILayout.Label("An asset name is required");
                }
                else if (!ValidCopyName)
                {
                    GUILayout.Label("The asset name is already used.");
                    if (GUILayout.Button("Delete it"))
                    {
                        string savePath = $"Assets/AngryLabs/AudioBump/{CopyName}.asset";
                        Debug.Log($"Deleting asset at at [{savePath}]");
                        AssetDatabase.DeleteAsset(savePath);
                        RefreshShaderList();
                    }
                }

                if (NewID == "")
                {
                    GUILayout.Label("A new shader name is required");
                }
                else if (!ValidNewID)
                {
                    GUILayout.Label("The shader name is already used.");

                    if (GUILayout.Button("Delete it"))
                    {
                        DeleteShaderByName(NewID);
                        RefreshShaderList();
                    }
                }
                else
                {
                    GUILayout.Label("Valid NewID");
                }
            }
            else if (GUILayout.Button("Install Audio Bump"))
            {
                Install();
            }

            if (_showGenerateNotification)
            {
                GUILayout.Space(20);
                GUILayout.Label("Before you can use your new shader");
                GUILayout.Label("It needs to be genrated using the");
                GUILayout.Label("Poi shader generation window");
                GUILayout.Label("or use the \"Generate all AudioBump shaders\" button");
            }

            if (GUILayout.Button("Build all AudioBump shaders"))
            {
                RegenerateAllAudioBump();
                _showGenerateNotification = false;
            }
        }
    }

    private void DeleteShaderByName(string name)
    {
        AssetDatabase.Refresh();
        string[] guids;
        guids = AssetDatabase.FindAssets($"t:{typeof(Shader).ToString().Replace("UnityEngine.", "")}");
        
        IEnumerable<string> paths = guids.Select(x => AssetDatabase.GUIDToAssetPath(x)).ToList();
        var assets = paths
            .Select(path => new { Asset = AssetDatabase.LoadMainAssetAtPath(path) as Shader, Path = path })
            .Where(x => x.Asset != null)
            .ToList();

        Regex re = new Regex($@".*{name}$");

        string delAsset = assets
            .Where(x => re.IsMatch(x.Asset.name))
            .Select(x => x.Path)
            .FirstOrDefault()
            ;

        AssetDatabase.DeleteAsset(delAsset);
    }

    private void Install()
    {
        RefreshShaderList();
        if(MakeCopy && (!ValidCopyName || !ValidNewID))
        {
            return;
        }

        var VertexColor = FindModuleById("PoiVertexColor", "Assets/_PoiyomiShaders");
        var AudioBump = FindModuleById("AudioBump", "Assets/AngryLabs/AudioBump");

        if(VertexColor == null)
        {
            Debug.LogError("Can not find the VertexColor module. AudioBump will be installed at the end.");
        } 

        if(AudioBump == null)
        {
            Debug.LogError("Can not find the AudioBump ShaderModule. Install aborted");
            return;
        }

        ModularShader shader = ActiveShader;
        if (MakeCopy)
        {
            shader = new ModularShader()
            {
                AdditionalModules = shader.AdditionalModules.ToList(),
                AdditionalSerializedData = shader.AdditionalSerializedData,
                Author = shader.Author + " + AngriestSCV",
                BaseModules = shader.BaseModules.Where(x=>x!=null).ToList(),
                CustomEditor = shader.CustomEditor,
                Description = shader.Description + " with AudioBump",
                hideFlags = shader.hideFlags,
                Name = CopyName,
                Properties = shader.Properties.ToList(),
                ShaderTemplate = shader.ShaderTemplate,
                ShaderPropertiesTemplate = shader.ShaderPropertiesTemplate,
                UseTemplatesForProperties = shader.UseTemplatesForProperties,
                Version = shader.Version,
                Id = shader.Id + "AudioBump",
                ShaderPath = "AngryLabs/AudioBump/" + NewID,
                LockBaseModules = shader.LockBaseModules,
                LastGeneratedShaders = new List<Shader>(),
            };

            string savePath = $"Assets/AngryLabs/AudioBump/{CopyName}.asset";
            Debug.Log($"Creating new asset at [{savePath}]");
            AssetDatabase.CreateAsset(shader, savePath);
        }

        int vertexColorIndex = shader.BaseModules.FindIndex(x => x == VertexColor);
        if(vertexColorIndex != -1)
        {
            shader.BaseModules.Insert(vertexColorIndex + 1, AudioBump);
        }
        else
        {
            shader.BaseModules.Add(AudioBump);
        }

        AssetDatabase.SaveAssets();

        _showGenerateNotification = true;
        RefreshShaderList();
    }

    ShaderModule FindModuleById(string name, string searchPath)
    {
        var modules = FindAssetsByType<ShaderModule>(searchPath);

        return modules.FirstOrDefault(x => x?.Id == name);
    }
    

    private static void RefreshShaderList()
    {
        _shaders = FindAssetsByType<ModularShader>().ToList();
        _compiledShaders = FindAssetsByType<Shader>().ToList();
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


