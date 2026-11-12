using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GerenciadorVideoExplicativo : MonoBehaviour
{
    public static GerenciadorVideoExplicativo Instance { get; private set; }

    [Header("Links Diretos do YouTube por Dificuldade")]
    [Tooltip("Vídeo direto do YouTube para o Nível Fácil (Uso de S e SS)")]
    public string urlYoutubeFacil = "https://www.youtube.com/watch?v=TMvXPgoQsJA";

    [Tooltip("Vídeo direto do YouTube para o Nível Médio (Regras e sons da letra X e dígrafos)")]
    public string urlYoutubeMedio = "https://www.youtube.com/watch?v=HOMTry9ncmE";

    [Tooltip("Vídeo direto do YouTube para o Nível Difícil (Substantivo, Adjetivo e Verbo)")]
    public string urlYoutubeDificil = "https://www.youtube.com/watch?v=-TwnJNqePnw";

    [Tooltip("Vídeo geral padrão")]
    public string urlYoutubeGeral = "https://www.youtube.com/watch?v=TMvXPgoQsJA";

    [Header("Sprites de UI (Carregados automaticamente se nulos)")]
    public Sprite spriteFundoPainel;
    public Sprite spriteXFechar;
    public Sprite spriteBtnAzul;
    public Sprite spriteBtnVerde;
    public Sprite spriteBtnLaranja;
    public Sprite spriteBtnVermelho;
    public Sprite spriteBadgeTopo;
    public TMP_FontAsset fonteTexto;

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
        canvasVideo.sortingOrder = 200; // Na camada de cima da do Game Over

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

        // Moldura Amarela Principal (Container Amarelo da imagem de referência)
        painelJanela = new GameObject("JanelaModalAmarela", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        painelJanela.transform.SetParent(goCanvas.transform, false);
        RectTransform rtJanela = painelJanela.GetComponent<RectTransform>();
        rtJanela.anchorMin = new Vector2(0.5f, 0.5f);
        rtJanela.anchorMax = new Vector2(0.5f, 0.5f);
        rtJanela.pivot = new Vector2(0.5f, 0.5f);
        rtJanela.sizeDelta = new Vector2(1380f, 740f);

        Image imgJanela = painelJanela.GetComponent<Image>();
        imgJanela.color = new Color(1f, 0.88f, 0.15f, 1f); // Amarelo vivo e acolhedor

        // Botão Fechar 'X' no topo superior direito
        GameObject goX = new GameObject("BtnFecharX", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        goX.transform.SetParent(painelJanela.transform, false);
        RectTransform rtX = goX.GetComponent<RectTransform>();
        rtX.anchorMin = new Vector2(1f, 1f);
        rtX.anchorMax = new Vector2(1f, 1f);
        rtX.pivot = new Vector2(1f, 1f);
        rtX.anchoredPosition = new Vector2(-15f, -15f);
        rtX.sizeDelta = new Vector2(55f, 55f);

        Image imgX = goX.GetComponent<Image>();
        if (spriteXFechar != null)
        {
            imgX.sprite = spriteXFechar;
            imgX.color = Color.white;
        }
        else
        {
            imgX.color = new Color(0.85f, 0.2f, 0.2f, 1f);
        }

        Button btnX = goX.GetComponent<Button>();
        btnX.onClick.AddListener(FecharTelaVideo);

        // =========================================================================
        // CAIXA 1 (ESQUERDA - LARANJA): Explicação do nível e convite para o YouTube
        // =========================================================================
        GameObject goCaixaEsquerda = new GameObject("CaixaExplicacaoEsquerda", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goCaixaEsquerda.transform.SetParent(painelJanela.transform, false);
        RectTransform rtEsq = goCaixaEsquerda.GetComponent<RectTransform>();
        rtEsq.anchorMin = new Vector2(0.5f, 0.5f);
        rtEsq.anchorMax = new Vector2(0.5f, 0.5f);
        rtEsq.pivot = new Vector2(0.5f, 0.5f);
        rtEsq.anchoredPosition = new Vector2(-335f, 0f);
        rtEsq.sizeDelta = new Vector2(590f, 630f);

        Image imgEsq = goCaixaEsquerda.GetComponent<Image>();
        imgEsq.color = new Color(0.98f, 0.48f, 0.12f, 1f); // Laranja vivo da ilustração

        // Quadradinho de cima: Fundo escuro com texto de ALTÍSSIMO contraste (100% legível)
        GameObject goBadgeDica = new GameObject("BadgeDica", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goBadgeDica.transform.SetParent(goCaixaEsquerda.transform, false);
        RectTransform rtBadge = goBadgeDica.GetComponent<RectTransform>();
        rtBadge.anchorMin = new Vector2(0.5f, 1f);
        rtBadge.anchorMax = new Vector2(0.5f, 1f);
        rtBadge.pivot = new Vector2(0.5f, 1f);
        rtBadge.anchoredPosition = new Vector2(0f, -25f);
        rtBadge.sizeDelta = new Vector2(520f, 58f);

        Image imgBadge = goBadgeDica.GetComponent<Image>();
        imgBadge.color = new Color(0.24f, 0.08f, 0.02f, 1f); // Marrom escuro café intenso para contraste perfeito

        GameObject goTxtBadge = new GameObject("TextoBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxtBadge.transform.SetParent(goBadgeDica.transform, false);
        RectTransform rtTB = goTxtBadge.GetComponent<RectTransform>();
        rtTB.anchorMin = Vector2.zero;
        rtTB.anchorMax = Vector2.one;
        rtTB.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpBadge = goTxtBadge.GetComponent<TextMeshProUGUI>();
        if (fonteTexto != null) tmpBadge.font = fonteTexto;
        tmpBadge.text = "DICA PEDAGÓGICA"; // Texto sem emoji para evitar quadrados não mapeados
        tmpBadge.fontSize = 28f;
        tmpBadge.alignment = TextAlignmentOptions.Center;
        tmpBadge.color = new Color(1f, 0.94f, 0.72f, 1f); // Dourado creme brilhante: máxima nitidez!
        tmpBadge.fontStyle = FontStyles.Bold;

        // Subtítulo do Nível
        GameObject goTituloNivel = new GameObject("TituloNivel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTituloNivel.transform.SetParent(goCaixaEsquerda.transform, false);
        RectTransform rtTN = goTituloNivel.GetComponent<RectTransform>();
        rtTN.anchorMin = new Vector2(0.5f, 1f);
        rtTN.anchorMax = new Vector2(0.5f, 1f);
        rtTN.pivot = new Vector2(0.5f, 1f);
        rtTN.anchoredPosition = new Vector2(0f, -95f);
        rtTN.sizeDelta = new Vector2(520f, 50f);

        textoTituloNivel = goTituloNivel.GetComponent<TextMeshProUGUI>();
        if (fonteTexto != null) textoTituloNivel.font = fonteTexto;
        textoTituloNivel.fontSize = 24f;
        textoTituloNivel.alignment = TextAlignmentOptions.Center;
        textoTituloNivel.color = new Color(1f, 0.95f, 0.8f, 1f);
        textoTituloNivel.fontStyle = FontStyles.Bold;
        textoTituloNivel.text = "Como funcionam as palavras desse nível";

        // Texto com a Explicação do Nível e Convite ao YouTube
        GameObject goCorpo = new GameObject("CorpoExplicacao", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goCorpo.transform.SetParent(goCaixaEsquerda.transform, false);
        RectTransform rtCorpo = goCorpo.GetComponent<RectTransform>();
        rtCorpo.anchorMin = new Vector2(0.5f, 0.5f);
        rtCorpo.anchorMax = new Vector2(0.5f, 0.5f);
        rtCorpo.pivot = new Vector2(0.5f, 0.5f);
        rtCorpo.anchoredPosition = new Vector2(0f, -15f);
        rtCorpo.sizeDelta = new Vector2(510f, 340f);

        textoCorpoExplicacao = goCorpo.GetComponent<TextMeshProUGUI>();
        if (fonteTexto != null) textoCorpoExplicacao.font = fonteTexto;
        textoCorpoExplicacao.fontSize = 20f;
        textoCorpoExplicacao.alignment = TextAlignmentOptions.TopLeft;
        textoCorpoExplicacao.color = Color.white;
        textoCorpoExplicacao.lineSpacing = 15f;
        textoCorpoExplicacao.text = "Explicando sobre o nível e falando sobre abrir o vídeo explicativo no Youtube.";

        // Botão TENTAR NOVAMENTE na Caixa Esquerda
        GameObject goBtnTentar = new GameObject("BtnTentarNovamente", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        goBtnTentar.transform.SetParent(goCaixaEsquerda.transform, false);
        RectTransform rtBtnTentar = goBtnTentar.GetComponent<RectTransform>();
        rtBtnTentar.anchorMin = new Vector2(0.5f, 0f);
        rtBtnTentar.anchorMax = new Vector2(0.5f, 0f);
        rtBtnTentar.pivot = new Vector2(0.5f, 0f);
        rtBtnTentar.anchoredPosition = new Vector2(0f, 25f);
        rtBtnTentar.sizeDelta = new Vector2(290f, 54f);

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
        RectTransform rtTT = goTxtTentar.GetComponent<RectTransform>();
        rtTT.anchorMin = Vector2.zero;
        rtTT.anchorMax = Vector2.one;
        rtTT.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpTentar = goTxtTentar.GetComponent<TextMeshProUGUI>();
        if (fonteTexto != null) tmpTentar.font = fonteTexto;
        tmpTentar.text = "TENTAR NOVAMENTE";
        tmpTentar.fontSize = 19f;
        tmpTentar.alignment = TextAlignmentOptions.Center;
        tmpTentar.color = Color.white;
        tmpTentar.fontStyle = FontStyles.Bold;

        // =========================================================================
        // CAIXA 2 (DIREITA - LARANJA): Card com Acesso ao Vídeo no YouTube
        // =========================================================================
        GameObject goCaixaDireita = new GameObject("CaixaVideoDireita", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goCaixaDireita.transform.SetParent(painelJanela.transform, false);
        RectTransform rtDir = goCaixaDireita.GetComponent<RectTransform>();
        rtDir.anchorMin = new Vector2(0.5f, 0.5f);
        rtDir.anchorMax = new Vector2(0.5f, 0.5f);
        rtDir.pivot = new Vector2(0.5f, 0.5f);
        rtDir.anchoredPosition = new Vector2(335f, 0f);
        rtDir.sizeDelta = new Vector2(650f, 630f);

        Image imgDir = goCaixaDireita.GetComponent<Image>();
        imgDir.color = new Color(0.98f, 0.48f, 0.12f, 1f); // Laranja vivo da ilustração

        // Tela / Thumbnail de Preview do YouTube (Clicável)
        GameObject goTelaPreview = new GameObject("TelaPreviewYoutube", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        goTelaPreview.transform.SetParent(goCaixaDireita.transform, false);
        RectTransform rtPreview = goTelaPreview.GetComponent<RectTransform>();
        rtPreview.anchorMin = new Vector2(0.5f, 1f);
        rtPreview.anchorMax = new Vector2(0.5f, 1f);
        rtPreview.pivot = new Vector2(0.5f, 1f);
        rtPreview.anchoredPosition = new Vector2(0f, -25f);
        rtPreview.sizeDelta = new Vector2(580f, 360f);

        Image imgPreview = goTelaPreview.GetComponent<Image>();
        imgPreview.color = new Color(0.12f, 0.12f, 0.14f, 1f); // Fundo escuro estilo player

        Button btnPreview = goTelaPreview.GetComponent<Button>();
        btnPreview.onClick.AddListener(AbrirLinkYoutube);

        // Selo do YouTube no topo da preview (Sem caracteres ausentes)
        GameObject goBadgeYT = new GameObject("BadgeYT", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goBadgeYT.transform.SetParent(goTelaPreview.transform, false);
        RectTransform rtBYT = goBadgeYT.GetComponent<RectTransform>();
        rtBYT.anchorMin = new Vector2(0f, 1f);
        rtBYT.anchorMax = new Vector2(0f, 1f);
        rtBYT.pivot = new Vector2(0f, 1f);
        rtBYT.anchoredPosition = new Vector2(18f, -18f);
        rtBYT.sizeDelta = new Vector2(130f, 38f);

        Image imgBYT = goBadgeYT.GetComponent<Image>();
        imgBYT.color = new Color(0.9f, 0.13f, 0.09f, 1f); // Vermelho YouTube

        GameObject goTxtYT = new GameObject("TextoYT", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxtYT.transform.SetParent(goBadgeYT.transform, false);
        RectTransform rtTYT = goTxtYT.GetComponent<RectTransform>();
        rtTYT.anchorMin = Vector2.zero;
        rtTYT.anchorMax = Vector2.one;
        rtTYT.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpYT = goTxtYT.GetComponent<TextMeshProUGUI>();
        if (fonteTexto != null) tmpYT.font = fonteTexto;
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
        if (fonteTexto != null) tmpP.font = fonteTexto;
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
        rtTV.anchoredPosition = new Vector2(0f, 130f);
        rtTV.sizeDelta = new Vector2(580f, 70f);

        textoTituloVideo = goTituloVideo.GetComponent<TextMeshProUGUI>();
        if (fonteTexto != null) textoTituloVideo.font = fonteTexto;
        textoTituloVideo.fontSize = 20f;
        textoTituloVideo.alignment = TextAlignmentOptions.Center;
        textoTituloVideo.color = new Color(1f, 0.95f, 0.8f, 1f);
        textoTituloVideo.fontStyle = FontStyles.Bold;
        textoTituloVideo.text = "Vídeo: Aprenda as Regrinhas Ortográficas de Forma Fácil!";

        // Botão Grande Vermelho de Acesso Direto ao YouTube
        GameObject goBtnYT = new GameObject("BtnAbrirYoutube", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        goBtnYT.transform.SetParent(goCaixaDireita.transform, false);
        RectTransform rtBtnYT = goBtnYT.GetComponent<RectTransform>();
        rtBtnYT.anchorMin = new Vector2(0.5f, 0f);
        rtBtnYT.anchorMax = new Vector2(0.5f, 0f);
        rtBtnYT.pivot = new Vector2(0.5f, 0f);
        rtBtnYT.anchoredPosition = new Vector2(0f, 25f);
        rtBtnYT.sizeDelta = new Vector2(420f, 65f);

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
        if (fonteTexto != null) tmpYTBtn.font = fonteTexto;
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
                        "• <b>SS e S:</b> O SS tem som forte e fica entre vogais (ex: PÁSSARO).\n" +
                        "• <b>CH e X:</b> CH e X têm som de 'ch' (ex: CHAVE e CAIXA).\n" +
                        "• <b>Ç e C:</b> O Ç só é usado antes de A, O e U!\n\n" +
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
                        "• Atenção aos encontros de consoantes e sons de <b>Z, X e Ç</b>.\n" +
                        "• Palavras como <i>PROFESSOR</i>, <i>CHOCOLATE</i> e <i>AMIZADE</i> exigem prestar atenção em cada pedacinho!\n\n" +
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
                        "• <b>Os 4 Porquês:</b> <i>Por que</i> (pergunta), <i>Porque</i> (resposta), <i>Por quê</i> (fim de frase) e <i>Porquê</i> (substantivo).\n" +
                        "• <b>Classes Gramaticais:</b> Substantivo (nome), Verbo (ação) e Adjetivo (qualidade)!\n" +
                        "• <b>Mau com U</b> (oposto de bom) e <b>Mal com L</b> (oposto de bem).\n\n" +
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
