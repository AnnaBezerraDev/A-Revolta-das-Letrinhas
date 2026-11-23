using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Exibe botões fixos e discretos de Minimizar e Sair no canto superior direito
/// para a versão Desktop (.exe), com modal de confirmação ao sair.
/// </summary>
public class ControlesJanelaDesktop : MonoBehaviour
{
    private static ControlesJanelaDesktop instance;

    private Canvas canvasRoot;
    private GameObject modalConfirmarSaida;
    private TMP_FontAsset fontJogo;
    private TMP_FontAsset fontPadrao;
    private Sprite spriteBtnAzul;
    private Sprite spriteBtnVermelho;
    private Sprite spriteBtnVerde;
    private Sprite spritePainelPergaminho;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    private const int SW_MINIMIZE = 6;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInitialize()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        if (instance == null)
        {
            GameObject go = new GameObject("[ControlesJanelaDesktop]");
            instance = go.AddComponent<ControlesJanelaDesktop>();
            DontDestroyOnLoad(go);
        }
#endif
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

        CarregarRecursos();
        ConstruirInterface();
    }

    private void CarregarRecursos()
    {
        fontPadrao = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset;
        fontJogo = Resources.Load<TMP_FontAsset>("CrayonHandRegular2016Demo SDF") ?? fontPadrao;
        if (fontPadrao == null) fontPadrao = fontJogo;

        spriteBtnAzul = Resources.Load<Sprite>("btn_azul_jogo");
        spriteBtnVermelho = Resources.Load<Sprite>("btn_vermelho_jogo");
        spriteBtnVerde = Resources.Load<Sprite>("btn_verde_jogo") ?? Resources.Load<Sprite>("btn_jogar_verde");
        spritePainelPergaminho = Resources.Load<Sprite>("painel_pergaminho") ?? Resources.Load<Sprite>("fundo_pergaminho_jogo");
    }

    private void ConstruirInterface()
    {
        canvasRoot = gameObject.AddComponent<Canvas>();
        canvasRoot.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasRoot.sortingOrder = 1005;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // 1. Botão Minimizar ("—") no canto superior direito, ao lado do botão Sair
        CriarBotaoMinimizar();

        // 2. Botão Sair ("X") na extremidade superior direita
        CriarBotaoSair();

        // 3. Modal de Confirmação ao Sair
        ConstruirModalConfirmarSaida();
    }

    private void Update()
    {
        if (EscPressionadoNesteFrame())
        {
            if (modalConfirmarSaida != null && modalConfirmarSaida.activeSelf)
            {
                FecharConfirmacaoSaida();
            }
            else
            {
                AbrirConfirmacaoSaida();
            }
        }
    }

    /// <summary>
    /// Lê a tecla Esc pelo pacote Input System (o projeto usa só ele em Active Input Handling).
    /// A classe antiga UnityEngine.Input lança InvalidOperationException nessa configuração.
    /// </summary>
    private static bool EscPressionadoNesteFrame()
    {
#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.Keyboard teclado = UnityEngine.InputSystem.Keyboard.current;
        return teclado != null && teclado.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    private void CriarBotaoMinimizar()
    {
        GameObject go = new GameObject("BtnMinimizarJogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-72f, -24f);
        rt.sizeDelta = new Vector2(50f, 50f);

        Image img = go.GetComponent<Image>();
        if (spriteBtnAzul != null)
        {
            img.sprite = spriteBtnAzul;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.16f, 0.44f, 0.78f, 0.96f);
        }

        Button btn = go.GetComponent<Button>();
        btn.onClick.AddListener(MinimizarJogo);

        // Ícone de barra horizontal ("—") desenhado em UI para nitidez em qualquer fonte
        GameObject barra = new GameObject("IconeMinimizar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barra.transform.SetParent(go.transform, false);
        RectTransform rtBarra = barra.GetComponent<RectTransform>();
        rtBarra.anchorMin = new Vector2(0.5f, 0.5f);
        rtBarra.anchorMax = new Vector2(0.5f, 0.5f);
        rtBarra.pivot = new Vector2(0.5f, 0.5f);
        rtBarra.anchoredPosition = new Vector2(0f, -4f);
        rtBarra.sizeDelta = new Vector2(22f, 5f);

        Image imgBarra = barra.GetComponent<Image>();
        imgBarra.color = Color.white;
        imgBarra.raycastTarget = false;
    }

    private void CriarBotaoSair()
    {
        GameObject go = new GameObject("BtnSairJogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-14f, -24f);
        rt.sizeDelta = new Vector2(50f, 50f);

        Image img = go.GetComponent<Image>();
        if (spriteBtnVermelho != null)
        {
            img.sprite = spriteBtnVermelho;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.82f, 0.18f, 0.18f, 0.96f);
        }

        Button btn = go.GetComponent<Button>();
        btn.onClick.AddListener(AbrirConfirmacaoSaida);

        // Ícone "X" desenhado com 2 barras cruzadas para nitidez perfeita independente de fonte
        CriarBarraCruzada(go.transform, "BarraX1", 45f);
        CriarBarraCruzada(go.transform, "BarraX2", -45f);
    }

    private void CriarBarraCruzada(Transform parent, string nome, float anguloZ)
    {
        GameObject barra = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barra.transform.SetParent(parent, false);
        RectTransform rt = barra.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(24f, 5f);
        rt.localRotation = Quaternion.Euler(0f, 0f, anguloZ);

        Image img = barra.GetComponent<Image>();
        img.color = Color.white;
        img.raycastTarget = false;
    }

    private void ConstruirModalConfirmarSaida()
    {
        modalConfirmarSaida = new GameObject("ModalConfirmarSaida", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalConfirmarSaida.transform.SetParent(transform, false);

        RectTransform rtModal = modalConfirmarSaida.GetComponent<RectTransform>();
        rtModal.anchorMin = Vector2.zero;
        rtModal.anchorMax = Vector2.one;
        rtModal.offsetMin = Vector2.zero;
        rtModal.offsetMax = Vector2.zero;

        Image overlay = modalConfirmarSaida.GetComponent<Image>();
        overlay.color = new Color(0.05f, 0.04f, 0.03f, 0.78f);
        overlay.raycastTarget = true;

        GameObject janela = new GameObject("JanelaConfirmarSaida", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        janela.transform.SetParent(modalConfirmarSaida.transform, false);

        RectTransform rtJanela = janela.GetComponent<RectTransform>();
        rtJanela.anchorMin = new Vector2(0.5f, 0.5f);
        rtJanela.anchorMax = new Vector2(0.5f, 0.5f);
        rtJanela.pivot = new Vector2(0.5f, 0.5f);
        rtJanela.anchoredPosition = Vector2.zero;
        rtJanela.sizeDelta = new Vector2(640f, 340f);

        Image imgJanela = janela.GetComponent<Image>();
        if (spritePainelPergaminho != null)
        {
            imgJanela.sprite = spritePainelPergaminho;
            imgJanela.type = Image.Type.Sliced;
            imgJanela.color = Color.white;
        }
        else
        {
            imgJanela.color = new Color(1f, 0.96f, 0.85f, 1f);
        }

        // Título
        GameObject goTitulo = new GameObject("TituloSair", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTitulo.transform.SetParent(janela.transform, false);
        RectTransform rtTit = goTitulo.GetComponent<RectTransform>();
        rtTit.anchorMin = new Vector2(0.5f, 1f);
        rtTit.anchorMax = new Vector2(0.5f, 1f);
        rtTit.pivot = new Vector2(0.5f, 1f);
        rtTit.anchoredPosition = new Vector2(0f, -48f);
        rtTit.sizeDelta = new Vector2(560f, 110f);

        TextMeshProUGUI tmpTit = goTitulo.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) tmpTit.font = fontJogo;
        tmpTit.text = "<size=34><b>DESEJA SAIR DO JOGO?</b></size>\n<size=21><color=#5D4037>Seu progresso já está salvo!</color></size>";
        tmpTit.alignment = TextAlignmentOptions.Center;
        tmpTit.color = new Color(0.2f, 0.12f, 0.05f, 1f);

        // Botão Cancelar (Verde)
        CriarBotaoModal(janela.transform, "BtnCancelarSaida", new Vector2(-135f, -215f), new Vector2(240f, 68f),
            "CONTINUAR", spriteBtnVerde, new Color(0.18f, 0.62f, 0.25f, 1f), FecharConfirmacaoSaida);

        // Botão Confirmar Saída (Vermelho)
        CriarBotaoModal(janela.transform, "BtnConfirmarSaida", new Vector2(135f, -215f), new Vector2(240f, 68f),
            "SAIR", spriteBtnVermelho, new Color(0.8f, 0.2f, 0.2f, 1f), ConfirmarSaidaJogo);

        modalConfirmarSaida.SetActive(false);
    }

    private void CriarBotaoModal(Transform parent, string nome, Vector2 pos, Vector2 size, string texto, Sprite spr, Color corFallback, UnityEngine.Events.UnityAction acao)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        if (spr != null)
        {
            img.sprite = spr;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = corFallback;
        }

        Button btn = go.GetComponent<Button>();
        btn.onClick.AddListener(acao);

        GameObject goTxt = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxt.transform.SetParent(go.transform, false);
        RectTransform rtTxt = goTxt.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.offsetMin = new Vector2(8f, 4f);
        rtTxt.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI tmp = goTxt.GetComponent<TextMeshProUGUI>();
        tmp.font = fontJogo ?? fontPadrao;
        tmp.text = $"<b>{texto}</b>";
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
    }

    private void TocarSomClique()
    {
        Action act = FindFirstObjectByType<Action>();
        if (act != null) act.TocarSomClique();
    }

    public void MinimizarJogo()
    {
        TocarSomClique();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        try
        {
            IntPtr hWnd = GetActiveWindow();
            if (hWnd == IntPtr.Zero) hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero) hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, SW_MINIMIZE);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ControlesJanelaDesktop] Erro ao minimizar janela: {ex.Message}");
        }
#else
        Debug.Log("[ControlesJanelaDesktop] Minimizar acionado (disponível no executável Windows).");
#endif
    }

    public void AbrirConfirmacaoSaida()
    {
        TocarSomClique();
        if (modalConfirmarSaida != null)
        {
            modalConfirmarSaida.SetActive(true);
            modalConfirmarSaida.transform.SetAsLastSibling();
        }
    }

    public void FecharConfirmacaoSaida()
    {
        TocarSomClique();
        if (modalConfirmarSaida != null)
        {
            modalConfirmarSaida.SetActive(false);
        }
    }

    public void ConfirmarSaidaJogo()
    {
        TocarSomClique();
        try
        {
            PlayerPrefs.Save();
        }
        catch { }

        // Se houver uma atualização baixada em segundo plano, executa o script de troca na saída
        try
        {
            string managedDir = System.IO.Path.Combine(Application.dataPath, "Managed");
            string patchNovo = System.IO.Path.Combine(managedDir, "Assembly-CSharp.dll.novo");
            string batPath = System.IO.Path.Combine(Application.dataPath, "..", "atualizar_jogo.bat");

            if (System.IO.File.Exists(patchNovo) && System.IO.File.Exists(batPath))
            {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = batPath,
                    Arguments = "",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(psi);
            }
        }
        catch { }

        // Libera na hora a vaga deste computador no limite de computadores da escola
        try { if (ServicoNuvem.Instancia != null) ServicoNuvem.Instancia.EncerrarSessaoDaMaquina(); } catch { }

        Application.Quit();

        // Garante fechamento imediato do executavel no Windows
        try
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
        catch { }
    }
}
