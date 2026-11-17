using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GerenciadorVideoExplicativo : MonoBehaviour
{
    public static GerenciadorVideoExplicativo Instance { get; private set; }

    [Header("Links Diretos do YouTube por Dificuldade (Português)")]
    [Tooltip("Vídeo direto do YouTube para o Nível Fácil (Uso de S e SS)")]
    public string urlYoutubeFacil = "https://www.youtube.com/watch?v=TMvXPgoQsJA";

    [Tooltip("Vídeo direto do YouTube para o Nível Médio (Regras e sons da letra X e dígrafos)")]
    public string urlYoutubeMedio = "https://www.youtube.com/watch?v=HOMTry9ncmE";

    [Tooltip("Vídeo direto do YouTube para o Nível Difícil (Substantivo, Adjetivo e Verbo)")]
    public string urlYoutubeDificil = "https://www.youtube.com/watch?v=-TwnJNqePnw";

    [Tooltip("Vídeo geral padrão")]
    public string urlYoutubeGeral = "https://www.youtube.com/watch?v=prsJNR0Zbqg";

    [Header("Links Diretos do YouTube por Dificuldade (Matemática)")]
    [Tooltip("Vídeo direto do YouTube para Matemática Fácil (Adição, Subtração e Multiplicação Infantil)")]
    public string urlYoutubeMathFacil = "https://www.youtube.com/watch?v=prsJNR0Zbqg";

    [Tooltip("Vídeo direto do YouTube para Matemática Médio (Divisão e Expressões com Parênteses)")]
    public string urlYoutubeMathMedio = "https://www.youtube.com/watch?v=T7Co1o7fh6k";

    [Tooltip("Vídeo direto do YouTube para Matemática Difícil (Expressões Numéricas e Potenciação)")]
    public string urlYoutubeMathDificil = "https://www.youtube.com/watch?v=Xx8e6QGf7IQ";

    [Header("Sprites de UI (Carregados automaticamente se nulos)")]
    public Sprite spriteFundoPainel;
    public Sprite spriteXFechar;
    public Sprite spriteBtnAzul;
    public Sprite spriteBtnVerde;
    public Sprite spriteBtnLaranja;
    public Sprite spriteBtnVermelho;
    public Sprite spriteBadgeTopo;
    public TMP_FontAsset fonteTexto;
    public TMP_FontAsset fontLimpa;

    // Elementos do Modal
    private Canvas canvasVideo;
    private CanvasGroup canvasGroupVideo;
    private GameObject painelJanela;
    private TextMeshProUGUI textoTituloNivel;
    private TextMeshProUGUI textoCorpoExplicacao;
    private TextMeshProUGUI textoTituloVideo;

    private Action actionScript;
    private GameManager gameManager;
    private GerenciadorFases gerenciadorFases;

    private string urlAtual = "";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        actionScript = FindFirstObjectByType<Action>();
        gameManager = FindFirstObjectByType<GameManager>();
        gerenciadorFases = FindFirstObjectByType<GerenciadorFases>();

        CarregarRecursosVisuais();
    }

    void Start()
    {
        CriarPainelModalVideo();
        RemoverBotaoExtraGameOver();
    }

    private void CarregarRecursosVisuais()
    {
        if (fonteTexto == null)
        {
            TMP_FontAsset[] fontes = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            if (fontes != null && fontes.Length > 0) fonteTexto = fontes[0];
        }
        if (fontLimpa == null)
        {
            fontLimpa = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset ?? fonteTexto;
        }

        spriteFundoPainel = CarregarSprite(spriteFundoPainel, "fundo_pergaminho_jogo", "Botoes/fundo_pergaminho_jogo.png", new Vector4(40, 40, 40, 40));
        spriteXFechar = CarregarSprite(spriteXFechar, "x_fechar_jogo", "Botoes/x_fechar_jogo.png", Vector4.zero);
        spriteBtnAzul = CarregarSprite(spriteBtnAzul, "btn_azul_jogo", "Botoes/btn_azul_jogo.png", new Vector4(26, 10, 26, 10));
        spriteBtnVerde = CarregarSprite(spriteBtnVerde, "btn_verde_jogo", "Botoes/btn_verde_jogo.png", new Vector4(26, 10, 26, 10));
        spriteBtnLaranja = CarregarSprite(spriteBtnLaranja, "btn_laranja_jogo", "Botoes/btn_laranja_jogo.png", new Vector4(26, 10, 26, 10));
        spriteBtnVermelho = CarregarSprite(spriteBtnVermelho, "btn_vermelho_jogo", "Botoes/btn_vermelho_jogo.png", new Vector4(26, 10, 26, 10));
        spriteBadgeTopo = CarregarSprite(spriteBadgeTopo, "badge_lobby_jogo", "Botoes/badge_lobby_jogo.png", new Vector4(30, 30, 30, 30));
    }

    private Sprite CarregarSprite(Sprite existente, string resourceName, string relativePath, Vector4 border)
    {
        if (existente != null) return existente;

        // 1. Tentar carregar Sprite diretamente de Resources
        try
        {
            Sprite r = Resources.Load<Sprite>(resourceName);
            if (r != null) return r;
        }
        catch { }

        // 2. Se falhar, tentar carregar Texture2D de Resources e converter em Sprite
        try
        {
            Texture2D tex = Resources.Load<Texture2D>(resourceName);
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            }
        }
        catch { }

        // 3. Tentar direto do disco (Assets/...) apenas no Editor
#if UNITY_EDITOR
        try
        {
            string fullPath = Path.Combine(Application.dataPath, relativePath);
            if (File.Exists(fullPath))
            {
                byte[] data = File.ReadAllBytes(fullPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(data))
                {
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                }
            }
        }
        catch { }
#endif

        return null;
    }

    /// <summary>
    /// Garante que NENHUM botão extra seja colocado na tela de Game Over e restaura as posições originais dos botões Continuar e Menu
    /// </summary>
    private void RemoverBotaoExtraGameOver()
    {
        CanvasGroup groupGameOver = null;
        if (actionScript != null && actionScript.groupGameOver != null)
        {
            groupGameOver = actionScript.groupGameOver;
        }
        else
        {
            GameObject go = GameObject.Find("Painel Game Over");
            if (go != null) groupGameOver = go.GetComponent<CanvasGroup>();
        }

        if (groupGameOver == null) return;

        Transform tGameOver = groupGameOver.transform;

        // Se houver algum botão btnVideoExplicativo injetado anteriormente, destrói
        Transform tBtnExtra = tGameOver.Find("btnVideoExplicativo");
        if (tBtnExtra != null)
        {
            Destroy(tBtnExtra.gameObject);
        }

        // Restaura posições originais exatas dos botões
        Transform tContinuar = tGameOver.Find("btnContinuar");
        if (tContinuar == null) tContinuar = tGameOver.Find("button continue");

        Transform tMenu = tGameOver.Find("btnMenu");
        if (tMenu == null) tMenu = tGameOver.Find("button menu");

        if (tContinuar != null)
        {
            RectTransform rtCont = tContinuar.GetComponent<RectTransform>();
            if (rtCont != null) rtCont.anchoredPosition = new Vector2(-0.7032f, -2.27f);
        }

        if (tMenu != null)
        {
            RectTransform rtM = tMenu.GetComponent<RectTransform>();
            if (rtM != null) rtM.anchoredPosition = new Vector2(-0.6848f, -3.3422f);
        }
    }

    /// <summary>
    /// Cria uma sprite procedural de triângulo Play branco para o YouTube (garante que nunca vire quadrado com interrogação)
    /// </summary>
    private Sprite CriarSpriteTrianguloPlay()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (x >= 18 && x <= 48)
                {
                    float prog = (x - 18f) / 30f;
                    float metadeAltura = (1f - prog) * 18f;
                    if (Mathf.Abs(y - (size / 2f)) <= metadeAltura)
                    {
                        tex.SetPixel(x, y, Color.white);
                        continue;
                    }
                }
                tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    /// <summary>
    /// Constrói o modal com design exato: moldura amarela com duas caixas laranjas (Dica à esquerda e Vídeo YouTube à direita)
    /// </summary>
    private void CriarPainelModalVideo()
    {
        // Canvas do Modal
        GameObject goCanvas = new GameObject("Canvas_VideoExplicativo", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasVideo = goCanvas.GetComponent<Canvas>();
        canvasVideo.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasVideo.sortingOrder = 1000; // Na camada superior absoluta (acima do Game Over e TopBar)

        CanvasScaler scaler = goCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGroupVideo = goCanvas.GetComponent<CanvasGroup>();
        canvasGroupVideo.alpha = 0f;
        canvasGroupVideo.interactable = false;
        canvasGroupVideo.blocksRaycasts = false;
        goCanvas.SetActive(false);

        // Fundo Escuro com Transparência (permite ver a Tela GameOver ao fundo)
        GameObject goFundo = new GameObject("FundoEscuro", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        goFundo.transform.SetParent(goCanvas.transform, false);
        RectTransform rtFundo = goFundo.GetComponent<RectTransform>();
        rtFundo.anchorMin = Vector2.zero;
        rtFundo.anchorMax = Vector2.one;
        rtFundo.sizeDelta = Vector2.zero;
        Image imgFundo = goFundo.GetComponent<Image>();
        imgFundo.color = new Color(0.12f, 0.12f, 0.15f, 0.78f);

        // Moldura Principal Acolhedora (Estilo Pergaminho / Quadro Educativo Lúdico)
        painelJanela = new GameObject("JanelaModalAcolhedora", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        painelJanela.transform.SetParent(goCanvas.transform, false);
        RectTransform rtJanela = painelJanela.GetComponent<RectTransform>();
        rtJanela.anchorMin = new Vector2(0.5f, 0.5f);
        rtJanela.anchorMax = new Vector2(0.5f, 0.5f);
        rtJanela.pivot = new Vector2(0.5f, 0.5f);
        rtJanela.sizeDelta = new Vector2(1440f, 780f);

        Image imgJanela = painelJanela.GetComponent<Image>();
        if (spriteFundoPainel != null)
        {
            imgJanela.sprite = spriteFundoPainel;
            imgJanela.type = Image.Type.Sliced;
            imgJanela.color = Color.white;
        }
        else
        {
            imgJanela.color = new Color(0.99f, 0.98f, 0.95f, 1f); // Creme suave e elegante
        }

        // Faixa de Topo com Ícone da Lâmpada Cartoon e Título Alegre (Baixado para não colar na borda)
        GameObject goHeader = new GameObject("HeaderModal", typeof(RectTransform), typeof(CanvasRenderer));
        goHeader.transform.SetParent(painelJanela.transform, false);
        RectTransform rtH = goHeader.GetComponent<RectTransform>();
        rtH.anchorMin = new Vector2(0.5f, 1f);
        rtH.anchorMax = new Vector2(0.5f, 1f);
        rtH.pivot = new Vector2(0.5f, 1f);
        rtH.anchoredPosition = new Vector2(0f, -65f);
        rtH.sizeDelta = new Vector2(1100f, 65f);

        // Ícone da lâmpada cartoon no topo
        GameObject goLamp = new GameObject("LampadaIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goLamp.transform.SetParent(goHeader.transform, false);
        RectTransform rtLamp = goLamp.GetComponent<RectTransform>();
        rtLamp.anchorMin = new Vector2(0f, 0.5f);
        rtLamp.anchorMax = new Vector2(0f, 0.5f);
        rtLamp.pivot = new Vector2(0f, 0.5f);
        rtLamp.anchoredPosition = new Vector2(90f, 0f);
        rtLamp.sizeDelta = new Vector2(56f, 56f);
        Image imgLamp = goLamp.GetComponent<Image>();
        Sprite sprL = Action.ObterSpriteLampadaCartoon();
        if (sprL != null)
        {
            imgLamp.sprite = sprL;
            imgLamp.preserveAspect = true;
            imgLamp.color = Color.white;
        }

        // Título do Topo
        GameObject goTitTopo = new GameObject("TituloTopo", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTitTopo.transform.SetParent(goHeader.transform, false);
        RectTransform rtTT = goTitTopo.GetComponent<RectTransform>();
        rtTT.anchorMin = Vector2.zero;
        rtTT.anchorMax = Vector2.one;
        rtTT.offsetMin = new Vector2(160f, 0f);
        rtTT.offsetMax = new Vector2(-100f, 0f);
        TextMeshProUGUI tmpTT = goTitTopo.GetComponent<TextMeshProUGUI>();
        tmpTT.font = fontLimpa ?? fonteTexto;
        tmpTT.text = "<b><size=28><color=#2C1808>DICA DO AMIGUINHO - COMO RESOLVER</color></size></b>\n<size=16><color=#6D4C41>Aprenda o passo a passo e assista ao vídeo explicativo em desenho animado!</color></size>";
        tmpTT.alignment = TextAlignmentOptions.MidlineLeft;

        // Botão Fechar 'X' no topo superior direito (posicionado mais para dentro da moldura)
        GameObject goX = new GameObject("BtnFecharX", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        goX.transform.SetParent(painelJanela.transform, false);
        RectTransform rtX = goX.GetComponent<RectTransform>();
        rtX.anchorMin = new Vector2(1f, 1f);
        rtX.anchorMax = new Vector2(1f, 1f);
        rtX.pivot = new Vector2(1f, 1f);
        rtX.anchoredPosition = new Vector2(-55f, -55f);
        rtX.sizeDelta = new Vector2(50f, 50f);

        Image imgX = goX.GetComponent<Image>();
        if (spriteXFechar != null)
        {
            imgX.sprite = spriteXFechar;
            imgX.color = Color.white;
        }
        else
        {
            imgX.color = new Color(0.85f, 0.2f, 0.2f, 1f);
            GameObject goTxtX = new GameObject("TxtX", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            goTxtX.transform.SetParent(goX.transform, false);
            RectTransform rtTxtX = goTxtX.GetComponent<RectTransform>();
            rtTxtX.anchorMin = Vector2.zero;
            rtTxtX.anchorMax = Vector2.one;
            rtTxtX.offsetMin = Vector2.zero;
            rtTxtX.offsetMax = Vector2.zero;
            TextMeshProUGUI tmpX = goTxtX.GetComponent<TextMeshProUGUI>();
            tmpX.font = fontLimpa ?? fonteTexto;
            tmpX.text = "<b>X</b>";
            tmpX.fontSize = 28f;
            tmpX.alignment = TextAlignmentOptions.Center;
            tmpX.color = Color.white;
        }

        Button btnX = goX.GetComponent<Button>();
        btnX.onClick.AddListener(FecharTelaVideo);

        // =========================================================================
        // CAIXA 1 (ESQUERDA): Card Educativo Acolhedor (Passo a Passo)
        // =========================================================================
        GameObject goCaixaEsquerda = new GameObject("CaixaExplicacaoEsquerda", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goCaixaEsquerda.transform.SetParent(painelJanela.transform, false);
        RectTransform rtEsq = goCaixaEsquerda.GetComponent<RectTransform>();
        rtEsq.anchorMin = new Vector2(0.5f, 0.5f);
        rtEsq.anchorMax = new Vector2(0.5f, 0.5f);
        rtEsq.pivot = new Vector2(0.5f, 0.5f);
        rtEsq.anchoredPosition = new Vector2(-345f, -50f);
        rtEsq.sizeDelta = new Vector2(650f, 580f);

        Image imgEsq = goCaixaEsquerda.GetComponent<Image>();
        imgEsq.color = new Color(0.95f, 0.93f, 0.88f, 1f); // Card pergaminho suave
        if (spriteBadgeTopo != null)
        {
            imgEsq.sprite = spriteBadgeTopo;
            imgEsq.type = Image.Type.Sliced;
            imgEsq.color = Color.white;
        }

        // Badge no topo do card esquerdo: Azul escuro educacional
        GameObject goBadgeDica = new GameObject("BadgeDica", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goBadgeDica.transform.SetParent(goCaixaEsquerda.transform, false);
        RectTransform rtBadge = goBadgeDica.GetComponent<RectTransform>();
        rtBadge.anchorMin = new Vector2(0.5f, 1f);
        rtBadge.anchorMax = new Vector2(0.5f, 1f);
        rtBadge.pivot = new Vector2(0.5f, 1f);
        rtBadge.anchoredPosition = new Vector2(0f, -18f);
        rtBadge.sizeDelta = new Vector2(560f, 52f);

        Image imgBadge = goBadgeDica.GetComponent<Image>();
        imgBadge.color = new Color(0.12f, 0.28f, 0.58f, 1f); // Azul nobre amigável
        if (spriteBtnAzul != null)
        {
            imgBadge.sprite = spriteBtnAzul;
            imgBadge.type = Image.Type.Sliced;
            imgBadge.color = Color.white;
        }

        GameObject goTxtBadge = new GameObject("TextoBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxtBadge.transform.SetParent(goBadgeDica.transform, false);
        RectTransform rtTB = goTxtBadge.GetComponent<RectTransform>();
        rtTB.anchorMin = Vector2.zero;
        rtTB.anchorMax = Vector2.one;
        rtTB.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpBadge = goTxtBadge.GetComponent<TextMeshProUGUI>();
        tmpBadge.font = fontLimpa ?? fonteTexto;
        tmpBadge.text = "<b>DICA PASSO A PASSO</b>";
        tmpBadge.fontSize = 22f;
        tmpBadge.alignment = TextAlignmentOptions.Center;
        tmpBadge.color = Color.white;

        // Subtítulo do Nível
        GameObject goTituloNivel = new GameObject("TituloNivel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTituloNivel.transform.SetParent(goCaixaEsquerda.transform, false);
        RectTransform rtTN = goTituloNivel.GetComponent<RectTransform>();
        rtTN.anchorMin = new Vector2(0.5f, 1f);
        rtTN.anchorMax = new Vector2(0.5f, 1f);
        rtTN.pivot = new Vector2(0.5f, 1f);
        rtTN.anchoredPosition = new Vector2(0f, -70f);
        rtTN.sizeDelta = new Vector2(580f, 36f);

        textoTituloNivel = goTituloNivel.GetComponent<TextMeshProUGUI>();
        textoTituloNivel.font = fontLimpa ?? fonteTexto;
        textoTituloNivel.fontSize = 20f;
        textoTituloNivel.alignment = TextAlignmentOptions.Center;
        textoTituloNivel.color = new Color(0.15f, 0.22f, 0.35f, 1f);
        textoTituloNivel.fontStyle = FontStyles.Bold;
        textoTituloNivel.text = "Como funcionam as palavras desse nível";

        // Texto com a Explicação do Nível e Passo a Passo (100% legível)
        GameObject goCorpo = new GameObject("CorpoExplicacao", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goCorpo.transform.SetParent(goCaixaEsquerda.transform, false);
        RectTransform rtCorpo = goCorpo.GetComponent<RectTransform>();
        rtCorpo.anchorMin = new Vector2(0.5f, 0.5f);
        rtCorpo.anchorMax = new Vector2(0.5f, 0.5f);
        rtCorpo.pivot = new Vector2(0.5f, 0.5f);
        rtCorpo.anchoredPosition = new Vector2(0f, -6f);
        rtCorpo.sizeDelta = new Vector2(580f, 350f);

        textoCorpoExplicacao = goCorpo.GetComponent<TextMeshProUGUI>();
        textoCorpoExplicacao.font = fontLimpa ?? fonteTexto;
        textoCorpoExplicacao.fontSize = 18f;
        textoCorpoExplicacao.alignment = TextAlignmentOptions.TopLeft;
        textoCorpoExplicacao.color = new Color(0.18f, 0.14f, 0.10f, 1f);
        textoCorpoExplicacao.lineSpacing = 8f;
        textoCorpoExplicacao.text = "Explicando sobre o nível e falando sobre abrir o vídeo explicativo no Youtube.";

        // Botão CONTINUAR JOGANDO na Caixa Esquerda
        GameObject goBtnTentar = new GameObject("BtnTentarNovamente", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        goBtnTentar.transform.SetParent(goCaixaEsquerda.transform, false);
        RectTransform rtBtnTentar = goBtnTentar.GetComponent<RectTransform>();
        rtBtnTentar.anchorMin = new Vector2(0.5f, 0f);
        rtBtnTentar.anchorMax = new Vector2(0.5f, 0f);
        rtBtnTentar.pivot = new Vector2(0.5f, 0f);
        rtBtnTentar.anchoredPosition = new Vector2(0f, 20f);
        rtBtnTentar.sizeDelta = new Vector2(320f, 52f);

        Image imgBtnTentar = goBtnTentar.GetComponent<Image>();
        if (spriteBtnVerde != null)
        {
            imgBtnTentar.sprite = spriteBtnVerde;
            imgBtnTentar.type = Image.Type.Sliced;
            imgBtnTentar.color = Color.white;
        }
        else
        {
            imgBtnTentar.color = new Color(0.2f, 0.68f, 0.25f, 1f);
        }

        Button btnTentar = goBtnTentar.GetComponent<Button>();
        btnTentar.onClick.AddListener(ClicarTentarNovamente);

        GameObject goTxtTentar = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxtTentar.transform.SetParent(goBtnTentar.transform, false);
        RectTransform rtTT2 = goTxtTentar.GetComponent<RectTransform>();
        rtTT2.anchorMin = Vector2.zero;
        rtTT2.anchorMax = Vector2.one;
        rtTT2.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpTentar = goTxtTentar.GetComponent<TextMeshProUGUI>();
        tmpTentar.font = fontLimpa ?? fonteTexto;
        tmpTentar.text = "<b>CONTINUAR JOGANDO</b>";
        tmpTentar.fontSize = 19f;
        tmpTentar.alignment = TextAlignmentOptions.Center;
        tmpTentar.color = Color.white;

        // =========================================================================
        // CAIXA 2 (DIREITA): Card com Player e Vídeo do YouTube
        // =========================================================================
        GameObject goCaixaDireita = new GameObject("CaixaVideoDireita", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goCaixaDireita.transform.SetParent(painelJanela.transform, false);
        RectTransform rtDir = goCaixaDireita.GetComponent<RectTransform>();
        rtDir.anchorMin = new Vector2(0.5f, 0.5f);
        rtDir.anchorMax = new Vector2(0.5f, 0.5f);
        rtDir.pivot = new Vector2(0.5f, 0.5f);
        rtDir.anchoredPosition = new Vector2(345f, -50f);
        rtDir.sizeDelta = new Vector2(650f, 580f);

        Image imgDir = goCaixaDireita.GetComponent<Image>();
        imgDir.color = new Color(0.95f, 0.93f, 0.88f, 1f);
        if (spriteBadgeTopo != null)
        {
            imgDir.sprite = spriteBadgeTopo;
            imgDir.type = Image.Type.Sliced;
            imgDir.color = Color.white;
        }

        // Tela / Thumbnail de Preview do YouTube (Clicável)
        GameObject goTelaPreview = new GameObject("TelaPreviewYoutube", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        goTelaPreview.transform.SetParent(goCaixaDireita.transform, false);
        RectTransform rtPreview = goTelaPreview.GetComponent<RectTransform>();
        rtPreview.anchorMin = new Vector2(0.5f, 1f);
        rtPreview.anchorMax = new Vector2(0.5f, 1f);
        rtPreview.pivot = new Vector2(0.5f, 1f);
        rtPreview.anchoredPosition = new Vector2(0f, -20f);
        rtPreview.sizeDelta = new Vector2(580f, 320f);

        Image imgPreview = goTelaPreview.GetComponent<Image>();
        imgPreview.color = new Color(0.12f, 0.12f, 0.14f, 1f); // Fundo escuro estilo player

        Button btnPreview = goTelaPreview.GetComponent<Button>();
        btnPreview.onClick.AddListener(AbrirLinkYoutube);

        // Selo do YouTube no topo da preview
        GameObject goBadgeYT = new GameObject("BadgeYT", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goBadgeYT.transform.SetParent(goTelaPreview.transform, false);
        RectTransform rtBYT = goBadgeYT.GetComponent<RectTransform>();
        rtBYT.anchorMin = new Vector2(0f, 1f);
        rtBYT.anchorMax = new Vector2(0f, 1f);
        rtBYT.pivot = new Vector2(0f, 1f);
        rtBYT.anchoredPosition = new Vector2(18f, -18f);
        rtBYT.sizeDelta = new Vector2(130f, 36f);

        Image imgBYT = goBadgeYT.GetComponent<Image>();
        imgBYT.color = new Color(0.9f, 0.13f, 0.09f, 1f); // Vermelho YouTube

        GameObject goTxtYT = new GameObject("TextoYT", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxtYT.transform.SetParent(goBadgeYT.transform, false);
        RectTransform rtTYT = goTxtYT.GetComponent<RectTransform>();
        rtTYT.anchorMin = Vector2.zero;
        rtTYT.anchorMax = Vector2.one;
        rtTYT.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpYT = goTxtYT.GetComponent<TextMeshProUGUI>();
        tmpYT.font = fontLimpa ?? fonteTexto;
        tmpYT.text = "YouTube";
        tmpYT.fontSize = 20f;
        tmpYT.alignment = TextAlignmentOptions.Center;
        tmpYT.color = Color.white;
        tmpYT.fontStyle = FontStyles.Bold;

        // Ícone Central de Play do YouTube (Procedural, nunca vira quadrado □)
        GameObject goIconePlay = new GameObject("IconePlayCentral", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goIconePlay.transform.SetParent(goTelaPreview.transform, false);
        RectTransform rtPlay = goIconePlay.GetComponent<RectTransform>();
        rtPlay.anchorMin = new Vector2(0.5f, 0.5f);
        rtPlay.anchorMax = new Vector2(0.5f, 0.5f);
        rtPlay.pivot = new Vector2(0.5f, 0.5f);
        rtPlay.anchoredPosition = new Vector2(0f, 20f);
        rtPlay.sizeDelta = new Vector2(110f, 75f);

        Image imgPlay = goIconePlay.GetComponent<Image>();
        imgPlay.color = new Color(1f, 0f, 0f, 1f); // Vermelho clássico

        // Triângulo de Play Branco nítido
        GameObject goTriangulo = new GameObject("TrianguloPlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goTriangulo.transform.SetParent(goIconePlay.transform, false);
        RectTransform rtTri = goTriangulo.GetComponent<RectTransform>();
        rtTri.anchorMin = new Vector2(0.5f, 0.5f);
        rtTri.anchorMax = new Vector2(0.5f, 0.5f);
        rtTri.pivot = new Vector2(0.5f, 0.5f);
        rtTri.sizeDelta = new Vector2(36f, 36f);

        Image imgTri = goTriangulo.GetComponent<Image>();
        imgTri.sprite = CriarSpriteTrianguloPlay();
        imgTri.color = Color.white;

        // Texto explicativo na preview
        GameObject goTextoPreview = new GameObject("TextoPreview", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTextoPreview.transform.SetParent(goTelaPreview.transform, false);
        RectTransform rtTP = goTextoPreview.GetComponent<RectTransform>();
        rtTP.anchorMin = new Vector2(0.5f, 0f);
        rtTP.anchorMax = new Vector2(0.5f, 0f);
        rtTP.pivot = new Vector2(0.5f, 0f);
        rtTP.anchoredPosition = new Vector2(0f, 25f);
        rtTP.sizeDelta = new Vector2(500f, 60f);

        TextMeshProUGUI tmpP = goTextoPreview.GetComponent<TextMeshProUGUI>();
        tmpP.font = fontLimpa ?? fonteTexto;
        tmpP.text = "<b>Vídeo Youtube</b>\n<size=80%>Clique aqui para assistir a explicação</size>";
        tmpP.fontSize = 22f;
        tmpP.alignment = TextAlignmentOptions.Center;
        tmpP.color = Color.white;

        // Título do Vídeo abaixo da Preview
        GameObject goTituloVideo = new GameObject("TituloVideo", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTituloVideo.transform.SetParent(goCaixaDireita.transform, false);
        RectTransform rtTV = goTituloVideo.GetComponent<RectTransform>();
        rtTV.anchorMin = new Vector2(0.5f, 0f);
        rtTV.anchorMax = new Vector2(0.5f, 0f);
        rtTV.pivot = new Vector2(0.5f, 0f);
        rtTV.anchoredPosition = new Vector2(0f, 115f);
        rtTV.sizeDelta = new Vector2(580f, 60f);

        textoTituloVideo = goTituloVideo.GetComponent<TextMeshProUGUI>();
        textoTituloVideo.font = fontLimpa ?? fonteTexto;
        textoTituloVideo.fontSize = 20f;
        textoTituloVideo.alignment = TextAlignmentOptions.Center;
        textoTituloVideo.color = new Color(0.18f, 0.14f, 0.10f, 1f);
        textoTituloVideo.fontStyle = FontStyles.Bold;
        textoTituloVideo.text = "Vídeo: Aprenda as Regrinhas Ortográficas de Forma Fácil!";

        // Botão Grande Vermelho de Acesso Direto ao YouTube
        GameObject goBtnYT = new GameObject("BtnAbrirYoutube", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        goBtnYT.transform.SetParent(goCaixaDireita.transform, false);
        RectTransform rtBtnYT = goBtnYT.GetComponent<RectTransform>();
        rtBtnYT.anchorMin = new Vector2(0.5f, 0f);
        rtBtnYT.anchorMax = new Vector2(0.5f, 0f);
        rtBtnYT.pivot = new Vector2(0.5f, 0f);
        rtBtnYT.anchoredPosition = new Vector2(0f, 20f);
        rtBtnYT.sizeDelta = new Vector2(420f, 60f);

        Image imgBtnYT = goBtnYT.GetComponent<Image>();
        imgBtnYT.color = new Color(0.9f, 0.13f, 0.09f, 1f); // Vermelho YouTube oficial

        Button btnYT = goBtnYT.GetComponent<Button>();
        btnYT.onClick.AddListener(AbrirLinkYoutube);

        GameObject goTxtYTBtn = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxtYTBtn.transform.SetParent(goBtnYT.transform, false);
        RectTransform rtTYTB = goTxtYTBtn.GetComponent<RectTransform>();
        rtTYTB.anchorMin = Vector2.zero;
        rtTYTB.anchorMax = Vector2.one;
        rtTYTB.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpYTBtn = goTxtYTBtn.GetComponent<TextMeshProUGUI>();
        tmpYTBtn.font = fontLimpa ?? fonteTexto;
        tmpYTBtn.text = "ASSISTIR NO YOUTUBE";
        tmpYTBtn.fontSize = 22f;
        tmpYTBtn.alignment = TextAlignmentOptions.Center;
        tmpYTBtn.color = Color.white;
        tmpYTBtn.fontStyle = FontStyles.Bold;
    }

    /// <summary>
    /// Abre a tela do vídeo explicativo na camada de cima da do Game Over
    /// </summary>
    public void AbrirTelaVideo()
    {
        if (actionScript != null)
        {
            actionScript.PararMusica();
        }

        ConfigurarConteudoPorNivel();

        if (canvasVideo != null)
        {
            canvasVideo.gameObject.SetActive(true);
            canvasGroupVideo.alpha = 1f;
            canvasGroupVideo.interactable = true;
            canvasGroupVideo.blocksRaycasts = true;
        }
    }

    private void ConfigurarConteudoPorNivel()
    {
        int nivel = 0;
        if (gerenciadorFases != null) nivel = gerenciadorFases.nivelAtual;
        else if (gameManager != null) nivel = gameManager.NivelAtualID;

        bool isMath = (gameManager != null && gameManager.JogandoMatematica);
        if (!isMath && actionScript != null && actionScript.telaDificuldadeAtual == actionScript.groupDificuldadeMatematica)
        {
            isMath = true;
        }

        if (isMath)
        {
            ConfigurarConteudoMatematica(nivel);
        }
        else
        {
            ConfigurarConteudoPortugues(nivel);
        }
    }

    private void ConfigurarConteudoMatematica(int nivel)
    {
        string resolucaoQuestao = "";
        if (gameManager != null && !string.IsNullOrEmpty(gameManager.UltimaPerguntaMatematica))
        {
            resolucaoQuestao = 
                $"<b>RESOLUÇÃO PASSO A PASSO DA SUA QUESTÃO:</b>\n" +
                $"<size=22><color=#FFE082><b>{gameManager.UltimaPerguntaMatematica}</b></color></size>\n" +
                $"{gameManager.UltimoPassoAPassoMatematica}\n\n";
        }

        switch (nivel)
        {
            case 0: // Matemática Fácil
                urlAtual = !string.IsNullOrEmpty(urlYoutubeMathFacil) ? urlYoutubeMathFacil : urlYoutubeGeral;
                if (textoTituloNivel != null) textoTituloNivel.text = "Matemática Fácil: Adição, Subtração e Tabuada";
                if (textoCorpoExplicacao != null)
                {
                    textoCorpoExplicacao.text =
                        resolucaoQuestao +
                        "<b>COMO RESOLVER PASSO A PASSO:</b>\n" +
                        "- <b>Adição (+):</b> Comece somando as unidades (direita) e depois as dezenas!\n" +
                        "- <b>Subtração (-):</b> Subtraia as unidades e depois as dezenas. Se faltar, empreste do vizinho!\n" +
                        "- <b>Multiplicação (x):</b> Multiplicar é somar o mesmo número várias vezes (ex: 3 x 4 = 3 + 3 + 3 + 3 = 12)!\n\n" +
                        "<b>Assista ao vídeo no YouTube ao lado</b> para ver desenhos animados ensinando as continhas!";
                }
                if (textoTituloVideo != null) textoTituloVideo.text = "Vídeo: Aprendendo a Somar com Dinossauros (Smile and Learn)";
                break;

            case 1: // Matemática Médio
                urlAtual = !string.IsNullOrEmpty(urlYoutubeMathMedio) ? urlYoutubeMathMedio : urlYoutubeGeral;
                if (textoTituloNivel != null) textoTituloNivel.text = "Matemática Médio: Divisão e Parênteses";
                if (textoCorpoExplicacao != null)
                {
                    textoCorpoExplicacao.text =
                        resolucaoQuestao +
                        "<b>COMO RESOLVER PASSO A PASSO:</b>\n" +
                        "- <b>Divisão (/):</b> Pense na multiplicação inversa! Se 4 x 5 = 20, então 20 ÷ 4 = 5!\n" +
                        "- <b>Expressões com Parênteses:</b>\n" +
                        "  Em contas como <b>15 + (4 x 3)</b>, resolva <b>SEMPRE os parênteses primeiro</b> (4 x 3 = 12) e depois some (15 + 12 = 27)!\n\n" +
                        "<b>Abra o vídeo no YouTube ao lado</b> para conferir o método de divisão e expressões!";
                }
                if (textoTituloVideo != null) textoTituloVideo.text = "Vídeo: Divisão Divertida com os Macacos (Smile and Learn)";
                break;

            case 2: // Matemática Difícil
            default:
                urlAtual = !string.IsNullOrEmpty(urlYoutubeMathDificil) ? urlYoutubeMathDificil : urlYoutubeGeral;
                if (textoTituloNivel != null) textoTituloNivel.text = "Matemática Difícil: Expressões e Potenciação";
                if (textoCorpoExplicacao != null)
                {
                    textoCorpoExplicacao.text =
                        resolucaoQuestao +
                        "<b>ORDEM DAS OPERAÇÕES (PASSO A PASSO):</b>\n" +
                        "1. <b>Parênteses:</b> Resolva tudo o que estiver dentro de ( ) primeiro.\n" +
                        "2. <b>Potências:</b> 5² significa 5 multiplicado por ele mesmo (5 x 5 = 25)!\n" +
                        "3. <b>Multiplicação e Divisão</b>\n" +
                        "4. <b>Adição e Subtração</b>\n\n" +
                        "<b>Clique no vídeo no YouTube ao lado</b> para aprender a resolver qualquer expressão!";
                }
                if (textoTituloVideo != null) textoTituloVideo.text = "Vídeo: Operações Combinadas e Parênteses (Smile and Learn)";
                break;
        }
    }

    private void ConfigurarConteudoPortugues(int nivel)
    {
        switch (nivel)
        {
            case 0: // Fácil
                urlAtual = !string.IsNullOrEmpty(urlYoutubeFacil) ? urlYoutubeFacil : urlYoutubeGeral;
                if (textoTituloNivel != null) textoTituloNivel.text = "Nível Fácil: Letras e Sons Especiais";
                if (textoCorpoExplicacao != null)
                {
                    textoCorpoExplicacao.text =
                        "Não se preocupe em errar, faz parte do aprendizado!\n\n" +
                        "Neste nível, treinamos letrinhas que têm sons parecidos:\n" +
                        "- <b>SS e S:</b> O SS tem som forte e fica entre vogais (ex: PÁSSARO).\n" +
                        "- <b>CH e X:</b> CH e X têm som de 'ch' (ex: CHAVE e CAIXA).\n" +
                        "- <b>Ç e C:</b> O Ç só é usado antes de A, O e U!\n\n" +
                        "<b>Assista ao vídeo no YouTube ao lado</b> para ver uma aula divertida com desenhos explicando cada regrinha!";
                }
                if (textoTituloVideo != null) textoTituloVideo.text = "Vídeo: Regras do S e SS (Vila Educativa)";
                break;

            case 1: // Médio
                urlAtual = !string.IsNullOrEmpty(urlYoutubeMedio) ? urlYoutubeMedio : urlYoutubeGeral;
                if (textoTituloNivel != null) textoTituloNivel.text = "Nível Médio: Palavras e Sílabas Complexas";
                if (textoCorpoExplicacao != null)
                {
                    textoCorpoExplicacao.text =
                        "Você está evoluindo muito bem!\n\n" +
                        "Nas fases médias, as palavras têm sílabas mais longas e desafiadoras:\n" +
                        "- Atenção aos encontros de consoantes e sons de <b>Z, X e Ç</b>.\n" +
                        "- Palavras como <i>PROFESSOR</i>, <i>CHOCOLATE</i> e <i>AMIZADE</i> exigem prestar atenção em cada pedacinho!\n\n" +
                        "<b>Abra o vídeo no YouTube ao lado</b> para ver dicas práticas e não errar mais nenhuma palavra!";
                }
                if (textoTituloVideo != null) textoTituloVideo.text = "Vídeo: Sons e Regras da Letra X (Vila Educativa)";
                break;

            case 2: // Difícil
            default:
                urlAtual = !string.IsNullOrEmpty(urlYoutubeDificil) ? urlYoutubeDificil : urlYoutubeGeral;
                if (textoTituloNivel != null) textoTituloNivel.text = "Nível Difícil: Sentidos e Classes de Palavras";
                if (textoCorpoExplicacao != null)
                {
                    textoCorpoExplicacao.text =
                        "O nível difícil traz os maiores segredos do Português!\n\n" +
                        "Aqui você aprende:\n" +
                        "- <b>Os 4 Porquês:</b> <i>Por que</i> (pergunta), <i>Porque</i> (resposta), <i>Por quê</i> (fim de frase) e <i>Porquê</i> (substantivo).\n" +
                        "- <b>Classes Gramaticais:</b> Substantivo (nome), Verbo (ação) e Adjetivo (qualidade)!\n" +
                        "- <b>Mau com U</b> (oposto de bom) e <b>Mal com L</b> (oposto de bem).\n\n" +
                        "<b>Clique no vídeo no YouTube ao lado</b> para assistir a uma aula completa e se tornar um mestre!";
                }
                if (textoTituloVideo != null) textoTituloVideo.text = "Vídeo: Substantivo, Adjetivo e Verbo (Vila Educativa)";
                break;
        }
    }

    /// <summary>
    /// Abre o link do YouTube diretamente no navegador ou app do YouTube
    /// </summary>
    public void AbrirLinkYoutube()
    {
        if (actionScript != null) actionScript.TocarSomClique();

        if (string.IsNullOrEmpty(urlAtual)) urlAtual = urlYoutubeGeral;

        Debug.Log($"[YOUTUBE] Abrindo vídeo no YouTube: {urlAtual}");
        Application.OpenURL(urlAtual);
    }

    /// <summary>
    /// Fecha a tela do modal e reinicia imediatamente a fase para a criança tentar de novo
    /// </summary>
    public void ClicarTentarNovamente()
    {
        if (actionScript != null) actionScript.TocarSomClique();

        FecharTelaVideo();

        if (gameManager != null)
        {
            gameManager.BotaoReiniciarFase();
        }
    }

    /// <summary>
    /// Fecha o modal de vídeo e retorna à tela de Game Over (com os botões de continuar e menu)
    /// </summary>
    public void FecharTelaVideo()
    {
        if (actionScript != null) actionScript.TocarSomClique();

        if (canvasVideo != null)
        {
            canvasGroupVideo.alpha = 0f;
            canvasGroupVideo.interactable = false;
            canvasGroupVideo.blocksRaycasts = false;
            canvasVideo.gameObject.SetActive(false);
        }
    }
}
