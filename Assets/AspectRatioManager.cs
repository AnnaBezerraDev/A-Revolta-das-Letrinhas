using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Garante que o jogo mantenha estritamente a proporção 16:9 (1920x1080)
/// tanto no executável Windows (.exe) quanto na versão WebGL,
/// independentemente da resolução da tela, janela ou monitor do jogador.
/// </summary>
public class AspectRatioManager : MonoBehaviour
{
    private static AspectRatioManager instance;

    public const float TARGET_WIDTH = 1920f;
    public const float TARGET_HEIGHT = 1080f;
    public const float TARGET_ASPECT = TARGET_WIDTH / TARGET_HEIGHT; // 1.777778 (16:9)

    private int lastWidth = 0;
    private int lastHeight = 0;

    private Canvas letterboxCanvas;
    private RectTransform topBar;
    private RectTransform bottomBar;
    private RectTransform leftBar;
    private RectTransform rightBar;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (instance == null)
        {
            GameObject obj = new GameObject("[AspectRatioManager]");
            instance = obj.AddComponent<AspectRatioManager>();
            DontDestroyOnLoad(obj);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_STANDALONE && !UNITY_EDITOR
        // No executável Windows, garantir resolução inicial ideal em 1920x1080
        if (!Screen.fullScreen)
        {
            int screenW = Display.main.systemWidth;
            int screenH = Display.main.systemHeight;
            if (screenW >= 1920 && screenH >= 1080)
            {
                Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            }
            else
            {
                // Se o monitor for menor que 1080p, usa a maior resolução 16:9 que caiba
                int fitH = Mathf.Min(720, screenH - 80);
                int fitW = Mathf.RoundToInt(fitH * TARGET_ASPECT);
                Screen.SetResolution(fitW, fitH, FullScreenMode.Windowed);
            }
        }
#endif

        CreateLetterboxOverlay();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ApplyAspectRatio(true);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyAspectRatio(true);
    }

    private void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            ApplyAspectRatio(false);
        }
    }

    private void LateUpdate()
    {
        // Re-verificar câmeras caso tenham sido ativadas ou alteradas dinamicamente
        UpdateCameras();
    }

    private void CreateLetterboxOverlay()
    {
        if (letterboxCanvas != null) return;

        GameObject canvasObj = new GameObject("LetterboxCanvas_Overlay");
        canvasObj.transform.SetParent(transform, false);

        letterboxCanvas = canvasObj.AddComponent<Canvas>();
        letterboxCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        letterboxCanvas.sortingOrder = 32767; // Sobrepõe qualquer interface

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        topBar = CreateBlackBar("TopBar", canvasObj.transform);
        bottomBar = CreateBlackBar("BottomBar", canvasObj.transform);
        leftBar = CreateBlackBar("LeftBar", canvasObj.transform);
        rightBar = CreateBlackBar("RightBar", canvasObj.transform);
    }

    private RectTransform CreateBlackBar(string barName, Transform parent)
    {
        GameObject barObj = new GameObject(barName);
        barObj.transform.SetParent(parent, false);

        Image img = barObj.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = true; // Bloqueia cliques fora da área útil do jogo

        RectTransform rt = barObj.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }

    public void ApplyAspectRatio(bool forceAll)
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        if (lastWidth <= 0 || lastHeight <= 0) return;

        float currentAspect = (float)lastWidth / (float)lastHeight;
        float variance = currentAspect / TARGET_ASPECT;

        // 1. Ajustar Barras Pretas (Letterbox / Pillarbox)
        if (letterboxCanvas != null)
        {
            if (variance < 0.999f)
            {
                // Tela mais alta que 16:9 (Letterbox - barras pretas no topo e embaixo)
                float barHeightNormalized = (1.0f - variance) / 2.0f;

                SetRectAnchors(topBar, 0f, 1f - barHeightNormalized, 1f, 1f);
                SetRectAnchors(bottomBar, 0f, 0f, 1f, barHeightNormalized);

                topBar.gameObject.SetActive(true);
                bottomBar.gameObject.SetActive(true);
                leftBar.gameObject.SetActive(false);
                rightBar.gameObject.SetActive(false);
            }
            else if (variance > 1.001f)
            {
                // Tela mais larga que 16:9 (Pillarbox - barras pretas na esquerda e direita)
                float scaleWidth = 1.0f / variance;
                float barWidthNormalized = (1.0f - scaleWidth) / 2.0f;

                SetRectAnchors(leftBar, 0f, 0f, barWidthNormalized, 1f);
                SetRectAnchors(rightBar, 1f - barWidthNormalized, 0f, 1f, 1f);

                leftBar.gameObject.SetActive(true);
                rightBar.gameObject.SetActive(true);
                topBar.gameObject.SetActive(false);
                bottomBar.gameObject.SetActive(false);
            }
            else
            {
                // Proporção exatamente 16:9
                topBar.gameObject.SetActive(false);
                bottomBar.gameObject.SetActive(false);
                leftBar.gameObject.SetActive(false);
                rightBar.gameObject.SetActive(false);
            }
        }

        // 2. Ajustar Câmeras
        UpdateCameras();

        // 3. Ajustar CanvasScalers dos Canvases de Tela Inteira
        UpdateCanvasScalers(variance);
    }

    private void SetRectAnchors(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void UpdateCameras()
    {
        if (lastWidth <= 0 || lastHeight <= 0) return;

        float currentAspect = (float)lastWidth / (float)lastHeight;
        float variance = currentAspect / TARGET_ASPECT;

        Rect targetRect;
        if (variance < 0.999f)
        {
            // Letterbox
            targetRect = new Rect(0f, (1.0f - variance) / 2.0f, 1.0f, variance);
        }
        else if (variance > 1.001f)
        {
            // Pillarbox
            float scaleWidth = 1.0f / variance;
            targetRect = new Rect((1.0f - scaleWidth) / 2.0f, 0f, scaleWidth, 1.0f);
        }
        else
        {
            targetRect = new Rect(0f, 0f, 1f, 1f);
        }

        Camera[] cameras = Camera.allCameras;
        foreach (var cam in cameras)
        {
            if (cam == null || !cam.enabled) continue;
            // Não alterar câmera do letterbox se houvesse uma
            if (cam.rect != targetRect)
            {
                cam.rect = targetRect;
            }
        }
    }

    private void UpdateCanvasScalers(float variance)
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            if (canvas == null || !canvas.isRootCanvas) continue;
            if (letterboxCanvas != null && canvas == letterboxCanvas) continue;

            // Personagens 2D com ConstantPixelSize não devem ser alterados
            if (canvas.transform.parent != null && canvas.transform.parent.name.Contains("Ze"))
            {
                continue;
            }

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.referenceResolution = new Vector2(TARGET_WIDTH, TARGET_HEIGHT);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

                if (variance < 0.999f)
                {
                    // Tela mais alta: fixa a largura para 1920 (as barras pretas cobrem a sobra vertical)
                    scaler.matchWidthOrHeight = 0f;
                }
                else if (variance > 1.001f)
                {
                    // Tela mais larga: fixa a altura para 1080 (as barras pretas cobrem a sobra horizontal)
                    scaler.matchWidthOrHeight = 1f;
                }
                else
                {
                    scaler.matchWidthOrHeight = 0.5f;
                }
            }
        }
    }
}

