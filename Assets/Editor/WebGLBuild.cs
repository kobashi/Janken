using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Janken.Editor
{
    /// <summary>ローカルとGitHub Actionsの両方から同じ条件でWebGLを生成する。</summary>
    public static class WebGLBuild
    {
        private const string Output = "Builds/WebGL";

        [MenuItem("Janken/Build WebGL")]
        public static void Build()
        {
            Directory.CreateDirectory(Output);
            PlayerSettings.companyName = "Janken Classroom";
            PlayerSettings.productName = "Janken!";
            PlayerSettings.bundleVersion = "1.4.0";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.runInBackground = true;
            // コード生成UIで実行時に追加するコンポーネントをWebGLリンカーから保護する。
            PlayerSettings.stripEngineCode = false;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;

            BuildPlayerOptions options = new()
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = Output,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"WebGL build failed: {report.summary.result}");

            Debug.Log($"WebGL build complete: {Path.GetFullPath(Output)}");
        }
    }
}
