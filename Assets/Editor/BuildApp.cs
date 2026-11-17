using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildApp
{
    public static void BuildPC()
    {
        Debug.Log("==========================================");
        Debug.Log("=== INICIANDO BUILD PARA PC (WINDOWS) ===");
        Debug.Log("==========================================");

        try
        {
            // 1. Executa a rotina completa de padronização e configuração
            ProjectConfigurator.ApplyAllFixes();

            // 2. Configura PlayerSettings específicos para PC Standalone
            PlayerSettings.productName = "Revolta das Letrinhas";
            PlayerSettings.companyName = "Revolta das Letrinhas";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.allowFullscreenSwitch = true;
            PlayerSettings.runInBackground = true;

            // 3. Executa o build de Windows via ItchBuildHelper
            ItchBuildHelper.BuildWindows();

            Debug.Log("=== BUILD PC CONCLUIDO COM SUCESSO! ===");
        }
        catch (Exception ex)
        {
            Debug.LogError("ERRO FATAL NA BUILD PC: " + ex);
            EditorApplication.Exit(1);
        }
    }
}

