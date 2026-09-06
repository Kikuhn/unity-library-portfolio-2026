using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Pan.GridCompatibles2;
using Pan.StageGenerators;
using Pan.Util;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Portfolio2026.Editor
{
    public static class PortfolioBuilder
    {
        const string Root = "Assets/Portfolio";
        [MenuItem("Portfolio/Create Missing Demo Assets and Scenes")]
        public static void Prepare()
        {
            Directory.CreateDirectory(Root+"/Scenes");
            Directory.CreateDirectory(Root+"/Art");
            Directory.CreateDirectory(Root+"/Settings");
            AssetDatabase.Refresh();
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(Root+"/Settings/Renderer.asset");
            if(renderer==null){renderer=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(renderer,Root+"/Settings/Renderer.asset");}
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Root+"/Settings/Pipeline.asset");
            if(pipeline==null){pipeline=UniversalRenderPipelineAsset.Create(renderer);AssetDatabase.CreateAsset(pipeline,Root+"/Settings/Pipeline.asset");}
            GraphicsSettings.defaultRenderPipeline=pipeline;
            QualitySettings.renderPipeline=pipeline;
            var material=Material("Element", "Pan/HighDensityElement/Sprite-Unlit-Instanced",new Color(.2f,.8f,.9f));
            material.enableInstancing=true;
            string pngPath=Root+"/Art/OwnSquare.png";
            if(!File.Exists(pngPath)){var texture=new Texture2D(16,16);texture.SetPixels(Enumerable.Repeat(Color.white,256).ToArray());texture.Apply();File.WriteAllBytes(pngPath,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(pngPath);var importer=(TextureImporter)AssetImporter.GetAtPath(pngPath);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=16;importer.SaveAndReimport();}
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
            string rigPath=Root+"/Art/OwnRig.json";
            if(!File.Exists(rigPath))File.WriteAllText(rigPath,"{\"skeleton\":{\"hash\":\"portfolio-own-rig-v1\",\"spine\":\"4.3.39\"},\"bones\":[{\"name\":\"root\"},{\"name\":\"body\",\"parent\":\"root\",\"length\":3,\"rotation\":90},{\"name\":\"arm\",\"parent\":\"body\",\"x\":2.5,\"length\":2,\"rotation\":-60},{\"name\":\"forearm\",\"parent\":\"arm\",\"x\":2,\"length\":1.6,\"rotation\":-20}],\"skins\":[{\"name\":\"default\",\"attachments\":{}}],\"animations\":{\"idle\":{\"bones\":{\"body\":{\"rotate\":[{\"value\":-5},{\"time\":1,\"value\":5},{\"time\":2,\"value\":-5}]}}},\"swing\":{\"bones\":{\"arm\":{\"rotate\":[{\"value\":-50},{\"time\":0.5,\"value\":80},{\"time\":1,\"value\":-50}]},\"forearm\":{\"rotate\":[{\"value\":0},{\"time\":0.5,\"value\":50},{\"time\":1,\"value\":0}]}}}}}");
            AssetDatabase.Refresh();
            string prefabPath=Root+"/Art/OwnAddressableCube.prefab";
            if(!File.Exists(prefabPath)){var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.GetComponent<Renderer>().sharedMaterial=Material("Cube","Universal Render Pipeline/Unlit",new Color(.3f,.8f,.6f));PrefabUtility.SaveAsPrefabAsset(cube,prefabPath);UnityEngine.Object.DestroyImmediate(cube);}
            var settings=AddressableAssetSettingsDefaultObject.Settings;
            if(settings==null){settings=AddressableAssetSettings.Create("Assets/AddressableAssetsData","AddressableAssetSettings",true,true);AddressableAssetSettingsDefaultObject.Settings=settings;}
            settings.BuildAddressablesWithPlayerBuild=AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer; // Build() explicitly builds content once.
            var group=settings.FindGroup("Portfolio Local")??settings.CreateGroup("Portfolio Local",false,false,true,null,typeof(BundledAssetGroupSchema),typeof(ContentUpdateGroupSchema));
            var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(prefabPath),group);entry.address="portfolio-cube";
            var stageSettings=StageSettings();
            foreach(PortfolioDemo.Case item in Enum.GetValues(typeof(PortfolioDemo.Case)))
            {
                string scenePath=Root+"/Scenes/"+item+".unity";
                if(File.Exists(scenePath))continue; // Preserve manually edited scenes.
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var cameraObject=new GameObject("Main Camera");cameraObject.tag="MainCamera";
                var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=8;camera.clearFlags=CameraClearFlags.SolidColor;camera.transform.position=new Vector3(0,0,-10);camera.nearClipPlane=.1f;camera.farClipPlane=200;
                cameraObject.AddComponent<UniversalAdditionalCameraData>();
                var demo=new GameObject("Demo - edit Inspector settings").AddComponent<PortfolioDemo>();
                demo.SelectedCase=item;demo.ElementMaterial=material;demo.ElementSprite=sprite;demo.RigJson=AssetDatabase.LoadAssetAtPath<TextAsset>(rigPath);
                if(item==PortfolioDemo.Case.Stage){var stage=new GameObject("Stage Generator").AddComponent<StageGenerator>();Set(stage,"settingSbject",stageSettings);Set(stage,"calculateSettingSbject",GetAsset<StageGeneratorSettingCalculateSbject>("Calculation"));demo.Stage=stage;camera.orthographicSize=35;camera.transform.position=new Vector3(13,25,-10);}
                EditorSceneManager.SaveScene(scene,scenePath);
            }
            EditorBuildSettings.scenes=Enum.GetValues(typeof(PortfolioDemo.Case)).Cast<PortfolioDemo.Case>().Select(c=>new EditorBuildSettingsScene(Root+"/Scenes/"+c+".unity",true)).ToArray();
            PlayerSettings.companyName="Kikuhn";PlayerSettings.productName="Unity Library Portfolio 2026";
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            AssetDatabase.SaveAssets();Debug.Log("PORTFOLIO_PREPARE_COMPLETE");
        }
        static StageGeneratorSettingSbject StageSettings()
        {
            string path=Root+"/Settings/Stage.asset";
            var existing=AssetDatabase.LoadAssetAtPath<StageGeneratorSettingSbject>(path);if(existing!=null)return existing;
            var snap=GetAsset<GridCompatible2SnapSettingSbject>("Snap");
            var room=Geometry("Room",5,5,new Color(.24f,.7f,.5f),true,snap) as RoomObject;
            foreach(EDirection4 direction in new[]{EDirection4.Down,EDirection4.Up,EDirection4.Left,EDirection4.Right})
            {
                var door=new RoomObject.Door();door.InitializeRoomDoor(room,direction);door.DoorGridPositionAxis=2;room.RoomDoorM.GetDoorList(direction).Add(door);
            }
            PrefabUtility.SaveAsPrefabAsset(room.gameObject,Root+"/Art/Room.prefab");UnityEngine.Object.DestroyImmediate(room.gameObject);
            var hall=Geometry("Hallway",1,1,new Color(.9f,.7f,.25f),false,snap);
            PrefabUtility.SaveAsPrefabAsset(hall.gameObject,Root+"/Art/Hallway.prefab");UnityEngine.Object.DestroyImmediate(hall.gameObject);
            var edge=Geometry("Wall",1,1,new Color(.2f,.3f,.4f),false,snap);
            PrefabUtility.SaveAsPrefabAsset(edge.gameObject,Root+"/Art/Wall.prefab");UnityEngine.Object.DestroyImmediate(edge.gameObject);
            var prefabSettings=GetAsset<StageGeneratorSettingPrefabSbject>("Prefabs");
            if(prefabSettings.Setting==null)Set(prefabSettings,"setting",new StageGeneratorSettingPrefab());
            Set(prefabSettings.Setting,"gridCompatibleSnapSetting",snap);
            prefabSettings.Setting.RoomPrefabList.Add(AssetDatabase.LoadAssetAtPath<RoomObject>(Root+"/Art/Room.prefab"));
            prefabSettings.Setting.HallwayPrefabList.Add(AssetDatabase.LoadAssetAtPath<GridCompatible2Object>(Root+"/Art/Hallway.prefab"));
            prefabSettings.Setting.HallwayEdgePrefabList.Add(AssetDatabase.LoadAssetAtPath<GridCompatible2Object>(Root+"/Art/Wall.prefab"));
            var result=GetAsset<StageGeneratorSettingSbject>("Stage");
            if(result.Setting==null)Set(result,"setting",new StageGeneratorSetting());
            Set(result.Setting,"prefabSettingSbject",prefabSettings);result.Setting.OnValidate(true);
            result.Setting.StageVector.StageWidth=50;result.Setting.StageVector.StageHeight=50;
            ConfigureStage(result);EditorUtility.SetDirty(prefabSettings);EditorUtility.SetDirty(result);return result;
        }
        [MenuItem("Portfolio/Repair Demo Stage Defaults")]
        public static void RepairStageDefaults()
        {
            var stage=AssetDatabase.LoadAssetAtPath<StageGeneratorSettingSbject>(Root+"/Settings/Stage.asset");
            ConfigureStage(stage);EditorUtility.SetDirty(stage);AssetDatabase.SaveAssets();
            Debug.Log("PORTFOLIO_STAGE_DEFAULTS_REPAIRED");
        }
        [MenuItem("Portfolio/Repair Demo Visual Assets")]
        public static void RepairVisualAssets()
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(Root+"/Art/OwnSquare.png");
            importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/OwnSquare.png");
            if(sprite==null)throw new BuildFailedException("OwnSquare sprite missing");
            foreach(string name in new[]{"Room","Hallway","Wall"})
            {
                string path=Root+"/Art/"+name+".prefab";var root=PrefabUtility.LoadPrefabContents(path);
                root.transform.Find("Own geometric visual").localPosition=Vector3.zero;
                PrefabUtility.SaveAsPrefabAsset(root,path);PrefabUtility.UnloadPrefabContents(root);
            }
            var scene=EditorSceneManager.OpenScene(Root+"/Scenes/Density.unity");
            var demo=UnityEngine.Object.FindFirstObjectByType<PortfolioDemo>();demo.ElementSprite=sprite;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("PORTFOLIO_VISUAL_ASSETS_REPAIRED");
        }
        static void ConfigureStage(StageGeneratorSettingSbject stage)
        {
            stage.Setting.StageVector.StageWidth=60;stage.Setting.StageVector.StageHeight=60; stage.Setting.Space.SpaceWidthMax=20;stage.Setting.Space.SpaceHeightMax=20;stage.Setting.Space.SpaceWidthMin=20;stage.Setting.Space.SpaceHeightMin=20;
            stage.Setting.Room.RoomPlacementMinWidth=5;stage.Setting.Room.RoomPlacementMinHeight=5;
            var grid=stage.Setting.StageGrid;
            var fields=grid.GetType().GetFields(BindingFlags.Instance|BindingFlags.NonPublic).Where(f=>f.Name.StartsWith("gridTagSetting_")).ToArray();
            var listField=grid.GetType().GetField("gridTagSettingDefaultList",BindingFlags.Instance|BindingFlags.NonPublic);
            var list=(System.Collections.IList)Activator.CreateInstance(listField.FieldType);
            foreach(var field in fields)
            {
                var asset=AssetDatabase.LoadAssetAtPath("Packages/com.kikuhn.panstagegenerator2/GridTagSettingDefault/Default"+field.Name.Substring("gridTagSetting_".Length)+".asset",field.FieldType);
                if(asset==null)throw new BuildFailedException("Missing grid tag: "+field.Name);
                field.SetValue(grid,asset);list.Add(asset);
            }
            listField.SetValue(grid,list);
        }
        static GridCompatible2Object Geometry(string name,int width,int height,Color color,bool room,GridCompatible2SnapSettingSbject snap)
        {
            var root=new GameObject(name);var component=room?(GridCompatible2Object)root.AddComponent<RoomObject>():root.AddComponent<GridCompatible2Object>();
            Set(component.GridCompatible,"objectSizeX_Width",width);Set(component.GridCompatible,"objectSizeY_Height",height);Set(component.GridCompatible,"SnapSettingExternal",snap);
            var visual=GameObject.CreatePrimitive(PrimitiveType.Quad);visual.name="Own geometric visual";visual.transform.SetParent(root.transform);visual.transform.localPosition=Vector3.zero;visual.transform.localScale=new Vector3(width*.96f,height*.96f,1);visual.GetComponent<Renderer>().sharedMaterial=Material(name,"Universal Render Pipeline/Unlit",color);UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());return component;
        }
        static T GetAsset<T>(string name) where T:ScriptableObject
        {string path=Root+"/Settings/"+name+".asset";var result=AssetDatabase.LoadAssetAtPath<T>(path);if(result==null){result=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(result,path);}return result;}
        static Material Material(string name,string shader,Color color)
        {string path=Root+"/Art/"+name+".mat";var result=AssetDatabase.LoadAssetAtPath<Material>(path);if(result==null){var found=Shader.Find(shader);if(found==null)throw new BuildFailedException("Shader missing: "+shader);result=new Material(found);result.color=color;AssetDatabase.CreateAsset(result,path);}return result;}
        static void Set(object target,string field,object value)
        {var member=target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public);if(member==null)throw new MissingFieldException(target.GetType().Name,field);member.SetValue(target,value);}
        [MenuItem("Portfolio/Build Performance Validation Player")]
        public static void BuildPerformance()
        {
            Prepare();
            const string scenePath=Root+"/Scenes/Performance.unity";
            if(!File.Exists(scenePath))
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=50;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
                camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                var runner=new GameObject("Original project performance validation").AddComponent<HighDensityElementPerformanceValidation>();
                runner.Configure(AssetDatabase.LoadAssetAtPath<Material>(Root+"/Art/Element.mat"),camera);
                EditorSceneManager.SaveScene(scene,scenePath);
            }
            Directory.CreateDirectory("Builds/Performance");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath},target=BuildTarget.StandaloneWindows64,locationPathName="Builds/Performance/Performance.exe",options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException(report.summary.result.ToString());
            Debug.Log("PORTFOLIO_PERFORMANCE_BUILD_COMPLETE");
        }
        [MenuItem("Portfolio/Build Windows Development Player")]
        public static void Build()
        {
            Prepare();AddressableAssetSettings.BuildPlayerContent(out var contentResult);if(!string.IsNullOrEmpty(contentResult.Error))throw new BuildFailedException(contentResult.Error);
            Directory.CreateDirectory("Builds/Windows");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Select(s=>s.path).ToArray(),target=BuildTarget.StandaloneWindows64,locationPathName="Builds/Windows/Portfolio.exe",options=BuildOptions.Development});
            if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException(report.summary.result.ToString());Debug.Log("PORTFOLIO_BUILD_COMPLETE");
        }
    }
}




