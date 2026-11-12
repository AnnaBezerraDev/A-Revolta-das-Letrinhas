using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using TMPro;

public static class ProjectConfigurator
{
    private const string SCENE_PATH = "Assets/Lobby.unity";
    private const string UNIVERSAL_RP_PATH = "Assets/Settings/UniversalRP.asset";
    private const string ICON_PATH = "Assets/AppIcon.png";
    private const string SPLASH_PATH = "Assets/TelaCarregamento.png";

    [MenuItem("Configuracao/Aplicar Todas as Correcoes e Otimizacoes")]
    public static void ApplyAllFixes()
    {
        Debug.Log("=== INICIANDO CONFIGURACAO COMPLETA DO PROJETO ===");

        // 1. Configurar Render Pipeline Global
        ConfigurarRenderPipeline();

        // 2. Configurar Importador de Texturas de Resources e Icones
        ConfigurarTexturasEIcones();

        // 3. Configurar PlayerSettings (Icone, Splash, Resolucoes 1920x1080)
        ConfigurarPlayerSettings();

        // 4. Configurar e Salvar a Cena Lobby.unity
        ConfigurarCenaLobby();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("=== CONFIGURACAO COMPLETA CONCLUIDA COM SUCESSO! ===");
    }

    private static void ConfigurarRenderPipeline()
    {
        var urpAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(UNIVERSAL_RP_PATH);
        if (urpAsset != null)
        {
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            QualitySettings.renderPipeline = urpAsset;
            Debug.Log($"Render Pipeline configurado para: {urpAsset.name}");
        }
        else
        {
            Debug.LogWarning($"Nao foi possivel carregar o asset URP em: {UNIVERSAL_RP_PATH}");
        }
    }

    private static void ConfigurarTexturasEIcones()
    {
        // 1. Configurar Icone do App
        if (File.Exists(ICON_PATH))
        {
            TextureImporter iconImporter = AssetImporter.GetAtPath(ICON_PATH) as TextureImporter;
            if (iconImporter != null)
            {
                iconImporter.textureType = TextureImporterType.Default;
                iconImporter.isReadable = true;
                iconImporter.npotScale = TextureImporterNPOTScale.None;
                iconImporter.alphaIsTransparency = true;
                iconImporter.SaveAndReimport();
            }
        }

        // 2. Configurar Tela de Carregamento
        if (File.Exists(SPLASH_PATH))
        {
            TextureImporter splashImporter = AssetImporter.GetAtPath(SPLASH_PATH) as TextureImporter;
            if (splashImporter != null)
            {
                splashImporter.textureType = TextureImporterType.Sprite;
                splashImporter.spriteImportMode = SpriteImportMode.Single;
                splashImporter.maxTextureSize = 4096;
                splashImporter.isReadable = true;
                splashImporter.alphaIsTransparency = true;
                splashImporter.SaveAndReimport();
            }
        }

        // 3. Garantir que todas as texturas em Assets/Resources sejam importadas como Sprite (2D and UI)
        string resourcesDir = "Assets/Resources";
        if (Directory.Exists(resourcesDir))
        {
            string[] files = Directory.GetFiles(resourcesDir, "*.png");
            foreach (var file in files)
            {
                string assetPath = file.Replace("\\", "/");
                TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (ti != null)
                {
                    bool changed = false;
                    if (ti.textureType != TextureImporterType.Sprite)
                    {
                        ti.textureType = TextureImporterType.Sprite;
                        ti.spriteImportMode = SpriteImportMode.Single;
                        changed = true;
                    }

                    // Remover overrides invalidos de WebGL (como DXT5)
                    var webglSettings = ti.GetPlatformTextureSettings("WebGL");
                    if (webglSettings.overridden && webglSettings.format == TextureImporterFormat.DXT5)
                    {
                        webglSettings.overridden = false;
                        ti.SetPlatformTextureSettings(webglSettings);
                        changed = true;
                    }

                    if (changed)
                    {
                        ti.SaveAndReimport();
                    }
                }
            }
        }
    }

    private static void ConfigurarPlayerSettings()
    {
        PlayerSettings.productName = "Revolta das Letrinhas";
        PlayerSettings.companyName = "Revolta das Letrinhas";

        // Resolucao e Proporcao 1920x1080 (16:9)
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.runInBackground = true;
        PlayerSettings.resizableWindow = false;

        PlayerSettings.defaultWebScreenWidth = 1920;
        PlayerSettings.defaultWebScreenHeight = 1080;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.template = "PROJECT:FixedAspect1080p";

        // Definir Icone do App
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(ICON_PATH);
        if (icon != null)
        {
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { icon });
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, new Texture2D[] { icon });
            Debug.Log("Icone do aplicativo configurado com sucesso a partir de: " + ICON_PATH);
        }

        // Definir Splash Screen / Loading Screen
        PlayerSettings.SplashScreen.show = true;
        PlayerSettings.SplashScreen.showUnityLogo = false; // Desabilitar logo da Unity quando possivel
        Sprite splashSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPLASH_PATH);
        if (splashSprite != null)
        {
            PlayerSettings.SplashScreenLogo splashLogo = PlayerSettings.SplashScreenLogo.Create(3f, splashSprite);
            PlayerSettings.SplashScreen.logos = new PlayerSettings.SplashScreenLogo[] { splashLogo };
            Debug.Log("Tela de carregamento (Splash Screen) configurada com sucesso a partir de: " + SPLASH_PATH);
        }
    }

    private static void ConfigurarCenaLobby()
    {
        var scene = EditorSceneManager.OpenScene(SCENE_PATH);
        if (!scene.IsValid())
        {
            Debug.LogError("Cena Lobby.unity nao pode ser aberta!");
            return;
        }

        // 1. Limpar SpriteRenderer fantasma em Ze/Canvas_Texto
        GameObject ze = GameObject.Find("Ze");
        if (ze != null)
        {
            Transform tCanvasTexto = ze.transform.Find("Canvas_Texto");
            if (tCanvasTexto != null)
            {
                SpriteRenderer sr = tCanvasTexto.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite == null)
                {
                    UnityEngine.Object.DestroyImmediate(sr, true);
                    Debug.Log("SpriteRenderer fantasma removido de Ze/Canvas_Texto.");
                }
            }
        }

        // 2. Vincular referencias de Sprites no GerenciadorPerfis
        GerenciadorPerfis gp = UnityEngine.Object.FindFirstObjectByType<GerenciadorPerfis>();
        if (gp != null)
        {
            gp.spriteCardFundo = CarregarSpriteDoAsset("card_aluno_fundo_jogo");
            gp.spriteCardAtivo = CarregarSpriteDoAsset("card_aluno_ativo_jogo");
            gp.spriteBarraPesquisa = CarregarSpriteDoAsset("barra_pesquisa_jogo");
            gp.spriteBadgeTopo = CarregarSpriteDoAsset("badge_lobby_jogo");
            gp.spriteBtnJogar = CarregarSpriteDoAsset("btn_verde_jogo");
            gp.spriteBtnContinuar = CarregarSpriteDoAsset("btn_azul_jogo");
            gp.spriteBtnLaranja = CarregarSpriteDoAsset("btn_laranja_jogo");
            gp.spriteBtnVermelho = CarregarSpriteDoAsset("btn_vermelho_jogo");
            gp.spriteAvatarKid = CarregarSpriteDoAsset("avatar_aluno_jogo");
            gp.spriteLixeira = CarregarSpriteDoAsset("icone_lixeira_jogo");
            gp.spriteLupa = CarregarSpriteDoAsset("icone_lupa_jogo");
            gp.spritePainelPergaminho = CarregarSpriteDoAsset("painel_pergaminho");
            gp.spriteXFechar = CarregarSpriteDoAsset("x_fechar_jogo");
            EditorUtility.SetDirty(gp);
            Debug.Log("Referencias de Sprites vinculadas no GerenciadorPerfis.");
        }

        // 3. Vincular referencias no GerenciadorVideoExplicativo
        GerenciadorVideoExplicativo gve = UnityEngine.Object.FindFirstObjectByType<GerenciadorVideoExplicativo>();
        if (gve != null)
        {
            gve.spriteFundoPainel = CarregarSpriteDoAsset("fundo_pergaminho_jogo");
            gve.spriteXFechar = CarregarSpriteDoAsset("x_fechar_jogo");
            gve.spriteBtnAzul = CarregarSpriteDoAsset("btn_azul_jogo");
            gve.spriteBtnVerde = CarregarSpriteDoAsset("btn_verde_jogo");
            gve.spriteBtnLaranja = CarregarSpriteDoAsset("btn_laranja_jogo");
            gve.spriteBtnVermelho = CarregarSpriteDoAsset("btn_vermelho_jogo");
            gve.spriteBadgeTopo = CarregarSpriteDoAsset("badge_lobby_jogo");
            EditorUtility.SetDirty(gve);
            Debug.Log("Referencias de Sprites vinculadas no GerenciadorVideoExplicativo.");
        }

        // 4. Ajustar Canvases para 1920x1080 (ScaleWithScreenSize)
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        int count = 0;
        foreach (var canvas in canvases)
        {
            if (canvas.gameObject.scene != scene) continue;

            if (canvas.transform.parent != null && canvas.transform.parent.name.Contains("Ze"))
            {
                var localScaler = canvas.GetComponent<CanvasScaler>();
                if (localScaler != null)
                {
                    localScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    localScaler.scaleFactor = 1f;
                    EditorUtility.SetDirty(localScaler);
                }
                continue;
            }

            if (canvas.isRootCanvas)
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();

                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                EditorUtility.SetDirty(scaler);
                count++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"Cena Lobby salva com {count} telas adaptadas para 1920x1080.");
    }

    private static Sprite CarregarSpriteDoAsset(string nameWithoutExt)
    {
        string path = $"Assets/Resources/{nameWithoutExt}.png";
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (s == null)
        {
            // Tentar carregar como sub-asset de Texture2D
            var all = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in all)
            {
                if (a is Sprite sp) return sp;
            }
        }
        return s;
    }
}

