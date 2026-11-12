using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class ItchBuildHelper
{
    private const string SCENE_PATH = "Assets/Lobby.unity";
    private const string WIN_BUILD_FOLDER = "C:/Users/Anna Luiza/Downloads/Jogo_RevoltaDasLetrinhas_Windows";
    private const string WIN_EXE_NAME = "RevoltaDasLetrinhas.exe";
    private const string WIN_ZIP_PATH = "C:/Users/Anna Luiza/Downloads/Jogo_RevoltaDasLetrinhas_Windows.zip";

    private const string WEBGL_BUILD_FOLDER = "C:/Users/Anna Luiza/Downloads/Jogo_RevoltaDasLetrinhas_WebGL";
    private const string WEBGL_ZIP_PATH = "C:/Users/Anna Luiza/Downloads/Jogo_RevoltaDasLetrinhas_WebGL.zip";

    [MenuItem("Publicacao/1. Corrigir Proporcao da Tela (1920x1080)")]
    public static void FixCanvases()
    {
        Debug.Log("--- Iniciando ajuste e padronizacao da proporcao 1920x1080 ---");
        ProjectConfigurator.ApplyAllFixes();

        // 1. Configuracoes Globais do Player
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.resizableWindow = false;

        PlayerSettings.defaultWebScreenWidth = 1920;
        PlayerSettings.defaultWebScreenHeight = 1080;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.template = "PROJECT:FixedAspect1080p";

        // 2. Ajuste dos Canvases na Cena
        var scene = EditorSceneManager.OpenScene(SCENE_PATH);
        Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
        int count = 0;

        foreach (var canvas in canvases)
        {
            if (canvas == null || canvas.gameObject.scene != scene) continue;

            // Ignorar personagens 2D (ex: Ze) para preservar escala com sprites do mundo
            if (canvas.transform.parent != null && canvas.transform.parent.name.Contains("Ze"))
            {
                var localScaler = canvas.GetComponent<CanvasScaler>();
                if (localScaler != null)
                {
                    Undo.RecordObject(localScaler, "Preservar ConstantPixelSize Ze");
                    localScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    localScaler.scaleFactor = 1f;
                    EditorUtility.SetDirty(localScaler);
                }
                continue;
            }

            // Canvases principais de tela cheia
            if (canvas.isRootCanvas)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null)
                {
                    scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                }

                Undo.RecordObject(scaler, "Ajustar CanvasScaler 1920x1080");
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                EditorUtility.SetDirty(scaler);
                count++;
            }
            else
            {
                // Sub-canvases internos
                var subScaler = canvas.GetComponent<CanvasScaler>();
                if (subScaler != null)
                {
                    Undo.RecordObject(subScaler, "Ajustar SubScaler");
                    subScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    subScaler.scaleFactor = 1f;
                    EditorUtility.SetDirty(subScaler);
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"Concluido! {count} Canvases principais salvos para 1920x1080 e PlayerSettings atualizados.");

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog("Proporcao 1920x1080 Aplicada!",
                $"{count} telas principais foram configuradas para 1920x1080.\n" +
                "Configuracoes para Windows (.exe) e WebGL alinhadas com sucesso!",
                "OK");
        }
    }

    [MenuItem("Publicacao/2. Gerar Jogo para Windows (.exe)")]
    public static void BuildWindows()
    {
        Debug.Log("=== Iniciando Build para Windows Standalone 64-bit (1920x1080) ===");

        // Fechar instancias anteriores se estiverem rodando
        try
        {
            foreach (var proc in System.Diagnostics.Process.GetProcessesByName("RevoltaDasLetrinhas"))
            {
                try { proc.Kill(); proc.WaitForExit(3000); } catch { }
            }
        }
        catch { }

        try
        {
            FixCanvases();

            if (Directory.Exists(WIN_BUILD_FOLDER))
            {
                Directory.Delete(WIN_BUILD_FOLDER, true);
            }
            Directory.CreateDirectory(WIN_BUILD_FOLDER);

            string exePath = Path.Combine(WIN_BUILD_FOLDER, WIN_EXE_NAME);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = GetScenesToBuild();
            buildPlayerOptions.locationPathName = exePath;
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = BuildOptions.None;

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"Build Windows concluida com sucesso! ({summary.totalSize / (1024 * 1024)} MB)");

                // Remover pasta de depuracao Burst se gerada
                string burstFolder = Path.Combine(WIN_BUILD_FOLDER, "My project_BurstDebugInformation_DoNotShip");
                if (Directory.Exists(burstFolder)) Directory.Delete(burstFolder, true);

                if (File.Exists(WIN_ZIP_PATH)) File.Delete(WIN_ZIP_PATH);

                Debug.Log("Compactando pasta para arquivo ZIP...");
                ZipFile.CreateFromDirectory(WIN_BUILD_FOLDER, WIN_ZIP_PATH, System.IO.Compression.CompressionLevel.Optimal, false);
                FileInfo zipInfo = new FileInfo(WIN_ZIP_PATH);
                Debug.Log($"ZIP Windows gerado com sucesso: {WIN_ZIP_PATH} ({zipInfo.Length / (1024 * 1024)} MB)");

                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Sucesso - Windows!",
                        $"O jogo foi gerado com sucesso!\n\nPasta: {WIN_BUILD_FOLDER}\nArquivo ZIP: {WIN_ZIP_PATH}",
                        "OK");
                }
            }
            else
            {
                Debug.LogError($"Falha na Build Windows: {summary.result}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Erro", $"A compilacao Windows falhou: {summary.result}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Erro durante build Windows: " + ex);
        }
    }

    [MenuItem("Publicacao/3. Gerar Jogo para WebGL")]
    public static void BuildWebGL()
    {
        Debug.Log("=== Iniciando Build para WebGL (1920x1080) ===");

        try
        {
            FixCanvases();

            // Garantir configuracoes especificas do WebGL antes da compilacao
            PlayerSettings.defaultWebScreenWidth = 1920;
            PlayerSettings.defaultWebScreenHeight = 1080;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = "PROJECT:FixedAspect1080p";

            if (Directory.Exists(WEBGL_BUILD_FOLDER))
            {
                Directory.Delete(WEBGL_BUILD_FOLDER, true);
            }
            Directory.CreateDirectory(WEBGL_BUILD_FOLDER);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = GetScenesToBuild();
            buildPlayerOptions.locationPathName = WEBGL_BUILD_FOLDER;
            buildPlayerOptions.target = BuildTarget.WebGL;
            buildPlayerOptions.options = BuildOptions.None;

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"Build WebGL concluida com sucesso! ({summary.totalSize / (1024 * 1024)} MB)");

                if (File.Exists(WEBGL_ZIP_PATH)) File.Delete(WEBGL_ZIP_PATH);

                Debug.Log("Compactando pasta para arquivo ZIP (pronto para itch.io / web)...");
                ZipFile.CreateFromDirectory(WEBGL_BUILD_FOLDER, WEBGL_ZIP_PATH, System.IO.Compression.CompressionLevel.Optimal, false);
                FileInfo zipInfo = new FileInfo(WEBGL_ZIP_PATH);
                Debug.Log($"ZIP WebGL gerado com sucesso: {WEBGL_ZIP_PATH} ({zipInfo.Length / (1024 * 1024)} MB)");

                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Sucesso - WebGL!",
                        $"O jogo WebGL foi gerado com sucesso!\n\nPasta: {WEBGL_BUILD_FOLDER}\nArquivo ZIP pronto: {WEBGL_ZIP_PATH}",
                        "OK");
                }
            }
            else
            {
                Debug.LogError($"Falha na Build WebGL: {summary.result}");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog("Erro", $"A compilacao WebGL falhou: {summary.result}", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Erro durante build WebGL: " + ex);
        }
    }

    [MenuItem("Publicacao/4. Gerar Ambos (.exe e WebGL)")]
    public static void BuildAll()
    {
        BuildWindows();
        BuildWebGL();
    }

    private static string[] GetScenesToBuild()
    {
        List<string> scenes = new List<string>();
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.enabled) scenes.Add(s.path);
        }

        if (scenes.Count == 0)
        {
            scenes.Add(SCENE_PATH);
        }

        return scenes.ToArray();
    }
}
