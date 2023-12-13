//In Editor assemblie

//This is a proposal to work around several limitation when authoring a project with both URP and HDRP.
//It may not adapt completely with your internal workflow when working on authoring a scene.
//Feel free to adapt  it or inspire from it for your own project recquirement.

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Coexistance
{
    [FilePath("ProjectSettings/RenderPipelineSavedOrderForCoexistance.asset", FilePathAttribute.Location.ProjectFolder)]
    class RenderPipelineSavedOrder : ScriptableSingleton<RenderPipelineSavedOrder>
    {
        [SerializeField] List<string> m_FullNames = new();
        static GUIContent[] m_Labels = null;

        public static int length => instance.m_FullNames.Count;

        public static Type GetTypeAt(int i)
            => Type.GetType(instance.m_FullNames[i]); 

        public static int GetIndex<T>() where T : RenderPipeline 
            => GetIndex(typeof(T).AssemblyQualifiedName);

        static int GetIndex(string assemblyQualifiedName)
            => instance.m_FullNames.IndexOf(assemblyQualifiedName);

        public static GUIContent[] GetLabels()
        {
            if (m_Labels != null)
                return m_Labels;
            m_Labels = new GUIContent[length];
            for (int i = 0; i < length; ++i)
            {
                Type type = GetTypeAt(i);
                m_Labels[i] = EditorGUIUtility.TrTextContent(type.Name, type.AssemblyQualifiedName);
            }
            return m_Labels;
        }

        public static void AddPipelinesInProject()
        {
            var oldLength = instance.m_FullNames.Count;
            foreach (var type in TypeCache.GetTypesDerivedFrom<RenderPipeline>())
            {
                string name = type.AssemblyQualifiedName;
                if (GetIndex(name) == -1)
                    instance.m_FullNames.Add(name);
            }
            if (oldLength != instance.m_FullNames.Count)
                instance.Save(saveAsText: true);
            m_Labels = null;
        }
    }

    [InitializeOnLoad]
    static class Coexistance
    {
        static Coexistance()
        {
            RenderPipelineSavedOrder.AddPipelinesInProject();
            RenderPipelineManager.activeRenderPipelineAssetChanged += OnChange;
            EditorSceneManager.sceneOpened += OnSceneLoaded;
        }

        #region SceneKind API
        enum SceneKind
        {
            NonCompatible,
            Common,
            URP,
            HDRP
        }

        static (string baseName, SceneKind kind) DecomposeSceneName(string name)
        {
            int index = name.LastIndexOf("_");
            if (index == -1)
                return (name, SceneKind.NonCompatible);

            return (name.Substring(0, index), name.Substring(index + 1) switch
            {
                "Common" => SceneKind.Common,
                "URP" => SceneKind.URP,
                "HDRP" => SceneKind.HDRP,
                _ => SceneKind.NonCompatible
            });
        }

        static string GetSceneName(string baseName, SceneKind kind)
            => kind == SceneKind.NonCompatible ? baseName : $"{baseName}_{kind}";
        #endregion

        #region On Pipeline Change
        static void OnChange(RenderPipelineAsset from, RenderPipelineAsset to)
	    {
            //force reloading scenes. This will trigger all the set up on load
            if (from != to)
            {
                HashSet<string> pathes = new();
                for (int i = 0; i < EditorSceneManager.sceneCount; ++i)
                {
                    var scene = EditorSceneManager.GetSceneAt(i);
                    (var baseName, var kind) = DecomposeSceneName(scene.name);
                    if (kind == SceneKind.NonCompatible)
                    {
                        pathes.Add(scene.path);
                        continue;
                    }

                    var basePath = scene.path.Substring(0, scene.path.Length - scene.name.Length - ".unity".Length);
                    pathes.Add($"{basePath}{GetSceneName(baseName, SceneKind.Common)}.unity");
                }
                bool first = true;
                foreach (var path in pathes)
                {
                    EditorSceneManager.OpenScene(path, first ? OpenSceneMode.Single : OpenSceneMode.Additive);
                    first = false;
                }
            }
	    }
        #endregion

        #region On Scene Change
        static void OnSceneLoaded(Scene sceneLoaded, OpenSceneMode _)
        {
            (var baseName, var kind) = DecomposeSceneName(sceneLoaded.name);
            if (kind == SceneKind.NonCompatible)
                return;

            string boundName;
            switch (kind)
            {
                case SceneKind.Common:
                    //Use RPAsset type as the RP may not be recreated yet from RPAsset in case we switched pipeline
                    var boundKind = GraphicsSettings.currentRenderPipeline switch
                    {
                        UniversalRenderPipelineAsset => SceneKind.URP,
                        HDRenderPipelineAsset => SceneKind.HDRP,
                        _ => SceneKind.NonCompatible
                    };
                    if (boundKind == SceneKind.NonCompatible)
                        return;

                    boundName = GetSceneName(baseName, boundKind);

                    kind = boundKind; //finish update for URP/HDRP
                    break;
                default: //as NonCompatible is already discarded, this is a RP kind (URP or HDRP)
                    if (kind == SceneKind.URP && GraphicsSettings.currentRenderPipeline?.GetType() != typeof(UniversalRenderPipelineAsset)
                        || kind == SceneKind.HDRP && GraphicsSettings.currentRenderPipeline?.GetType() != typeof(HDRenderPipelineAsset))
                    {
                        Debug.LogWarning($"You cannot load a scene for {kind} while you currently use another pipeline. Switch quality to a compatible one first.");
                        //or add code doing it here
                        //Continuing as is would lead to have both pipeline scene opened.
                        return;
                    }

                    boundName = GetSceneName(baseName, SceneKind.Common);
                    break;
            }
            
            for (int i = 0; i < EditorSceneManager.sceneCount; ++i)
                if (EditorSceneManager.GetSceneAt(i).name == boundName)
                    return; //Already loaded

            var basePath = sceneLoaded.path.Substring(0, sceneLoaded.path.Length - sceneLoaded.name.Length - ".unity".Length);
            EditorSceneManager.OpenScene($"{basePath}{boundName}.unity", OpenSceneMode.Additive);

            switch (kind)
            {
                case SceneKind.HDRP:
                    UpdateLighting<HDRenderPipeline>();
                    break;
                case SceneKind.URP:
                    UpdateLighting<UniversalRenderPipeline>();
                    UpdateReflectionProbe_URP();
                    UpdateSky_URP();
                    break;
            }
        }
        #endregion

        #region Update Lighting
        static IEnumerable<(LightmapData, Scene)> GetAllLightmapDatas()
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; ++i)
                foreach (var rootObject in EditorSceneManager.GetSceneAt(i).GetRootGameObjects())
                {
                    var cmp = rootObject.GetComponentInChildren<LightmapData>();
                    if (cmp != null)
                    {
                        yield return (cmp, EditorSceneManager.GetSceneAt(i));
                        break; //only return first found
                    }
                }
        }

        static void UpdateLighting<T>() where T : RenderPipeline
        {
            int index = RenderPipelineSavedOrder.GetIndex<T>();
            Scene activeScene = EditorSceneManager.GetActiveScene();
            foreach ((var lightmaps, var scene) in GetAllLightmapDatas())
            {
                EditorSceneManager.SetActiveScene(scene);
                Lightmapping.lightingDataAsset = lightmaps[index];
            }
            EditorSceneManager.SetActiveScene(activeScene);
        }
        #endregion
        
        #region Update Reflection Probes
        static IEnumerable<(ReflectionProbeData, ReflectionProbe)> GetAllReflectionProbeDatas()
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; ++i)
                foreach (var rootObject in EditorSceneManager.GetSceneAt(i).GetRootGameObjects())
                    foreach (var reflectionProbeData in rootObject.GetComponentsInChildren<ReflectionProbeData>())
                        if (reflectionProbeData.TryGetComponent<ReflectionProbe>(out var reflectionProbe))
                            yield return (reflectionProbeData, reflectionProbe);
        }

        static void UpdateReflectionProbe_URP()
        {
            //When switching pipeline from HDRP back to URP reflection probe modes are set to Custom and references are lost.
            //We use this workaround to grab baked ref probe textures and assign them again.

            foreach ((var reflectionProbeData, var reflectionProbe) in GetAllReflectionProbeDatas())
            {
                reflectionProbe.mode = ReflectionProbeMode.Custom;
                reflectionProbe.customBakedTexture = reflectionProbeData.customCubemap;
            }
        }
        #endregion

        #region Update Sky
        static void UpdateSky_URP()
        {
            //Everything is set up correctly but sky is set in the URP specific scene data so it need to be the active one
            for (int i = 0; i < EditorSceneManager.sceneCount; ++i)
                if (EditorSceneManager.GetSceneAt(i).name.EndsWith("_URP"))
                {
                    EditorSceneManager.SetActiveScene(EditorSceneManager.GetSceneAt(i));
                    break;
                }
        }
        #endregion
    }
}
