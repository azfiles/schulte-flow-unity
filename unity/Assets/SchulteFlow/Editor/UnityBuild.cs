using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SchulteFlow.Editor
{
    [InitializeOnLoad]
    public static class UnityBuild
    {
        private const string Scene = "Assets/SchulteFlow/Scenes/Flow.unity";
        static UnityBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("SchulteFlow.Configured",false))
                {
                    Configure();SessionState.SetBool("SchulteFlow.Configured",true);
                }
            };
        }

        [MenuItem("Schulte Flow/Configure project")]
        public static void Configure()
        {
            PlayerSettings.companyName="SchulteFlow";
            PlayerSettings.productName="循光·舒尔特 预览";
            PlayerSettings.bundleVersion="2.0.0-preview.1";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,Environment.GetEnvironmentVariable("SCHULTE_APP_ID") ?? "com.alex.schulteflow.unitypreview");
            PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft=false;PlayerSettings.allowedAutorotateToLandscapeRight=false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
            PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=960;
            PlayerSettings.colorSpace=ColorSpace.Gamma;
            PlayerSettings.runInBackground=false;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel35;
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            int versionCode;
            PlayerSettings.Android.bundleVersionCode=int.TryParse(Argument("-androidVersionCode"),out versionCode) && versionCode>=20001 ? versionCode : 20001;
            PlayerSettings.Android.forceInternetPermission=false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Android,ApiCompatibilityLevel.NET_Standard);
            // UGUI and board picking use the built-in input manager.
            var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;settings.ApplyModifiedPropertiesWithoutUndo();}
            EditorSettings.serializationMode=SerializationMode.ForceText;
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scene,true)};
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Schulte Flow/Open first version")]
        public static void OpenScene()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Configure();EditorSceneManager.OpenScene(Scene);
        }

        [MenuItem("Schulte Flow/Build Android preview APK")]
        public static void Android()
        {
            Configure();
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)
                throw new BuildFailedException("Switch the active build platform to Android first, or launch Unity with -buildTarget Android.");
            EditorUserBuildSettings.buildAppBundle=false;
            string output=Environment.GetEnvironmentVariable("SCHULTE_BUILD_PATH") ?? Argument("-customBuildPath") ?? "Builds/Android/Schulte-Flow-Unity-preview.apk";
            Build(BuildTarget.Android,output);
        }

        [MenuItem("Schulte Flow/Build Linux preview")]
        public static void Linux()
        {
            Configure();Build(BuildTarget.StandaloneLinux64,"Builds/Linux/SchulteFlow.x86_64");
        }

        private static void Build(BuildTarget target,string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{Scene},locationPathName=path,target=target,options=BuildOptions.None });
            if(report.summary.result!=BuildResult.Succeeded)
                throw new BuildFailedException("Build failed: "+report.summary.totalErrors+" errors. See Unity Editor log.");
            Debug.Log("SCHULTE_BUILD_PASS: "+report.summary.outputPath);
        }

        private static string Argument(string key)
        {
            string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]==key && !string.IsNullOrWhiteSpace(args[i+1]))return args[i+1];
            return null;
        }
    }
}
