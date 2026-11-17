using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AreaDoProfessor : MonoBehaviour
{
    public static AreaDoProfessor Instance { get; private set; }

    // Estado da Sessão do Professor
    public static bool SessaoProfessorAtiva = false;

    // Chaves de Credenciais
    private const string KEY_USER = "Config_Prof_User";
    private const string KEY_PASS = "Config_Prof_Pass";
    private const string DEFAULT_USER = "professor";
    private const string DEFAULT_PASS = "1234";

    [Header("Recursos Visuais")]
    public TMP_FontAsset fontProf;
    public TMP_FontAsset fontJogo;
    public Sprite spritePainelPergaminho;
    public Sprite spriteXFechar;
    public Sprite spriteBtnAzul;
    public Sprite spriteBtnVerde;
    public Sprite spriteBtnLaranja;
    public Sprite spriteBtnVermelho;
    public Sprite spriteCardFundo;
    public Sprite spriteCardAtivo;
    public Sprite spriteBarraPesquisa;
    public Sprite spriteAvatarAluno;

    // Raiz do Canvas Dedicado
    private Canvas canvasRoot;
    private CanvasScaler canvasScaler;

    // Modais
    private GameObject modalLogin;
    private GameObject modalDashboard;
    private GameObject modalResolucaoGameplay;
    private GameObject modalAlterarSenha;
    private GameObject btnHUDProfessor;
    private GameObject btnLobbyProfessor;
    private bool abrirResolucaoAposLogin = false;

    // Componentes de Login
    private TMP_InputField inputLoginUser;
    private TMP_InputField inputLoginPass;
    private TextMeshProUGUI textoFeedbackLogin;

    // Componentes do Dashboard
    private TMP_InputField inputPesquisaAlunos;
    private Transform containerListaAlunos;
    private TextMeshProUGUI textoHeaderAluno;
    private TextMeshProUGUI textoProgressoGeral;
    private Image imgBarraProgressoFill;
    private TextMeshProUGUI textoTrofeusResumo;
    private TextMeshProUGUI textoVidasResumo;
    private TextMeshProUGUI textoAproveitamentoResumo;
    private TextMeshProUGUI textoTabelaPort;
    private TextMeshProUGUI textoTabelaMath;
    private TextMeshProUGUI textoDiagnosticoPedagogico;
    private TextMeshProUGUI textoFeedbackAcoes;
    private TextMeshProUGUI textoBtnReset;
    private string alunoSelecionado = "";
    private string alunoEmConfirmacaoReset = "";
    private float tempoConfirmacaoReset = 0f;

    // Componentes de Resolução Gameplay
    private TextMeshProUGUI textoTituloResolucao;
    private TextMeshProUGUI textoCorpoResolucao;

    // Componentes Alterar Senha
    private TMP_InputField inputNovaSenha;
    private TMP_InputField inputConfirmaSenha;
    private TextMeshProUGUI textoFeedbackSenha;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        CarregarRecursos();
        ConstruirCanvasDedicado();
    }

    void Start()
    {
        ConstruirModalLogin();
        ConstruirModalDashboard();
        ConstruirModalResolucaoGameplay();
        ConstruirModalAlterarSenha();
        CriarBotaoHUDGameplay();
        CriarBotaoLobby();
    }

    void Update()
    {
        if (!string.IsNullOrEmpty(alunoEmConfirmacaoReset))
        {
            tempoConfirmacaoReset -= Time.deltaTime;
            if (tempoConfirmacaoReset <= 0f)
            {
                alunoEmConfirmacaoReset = "";
                if (textoBtnReset != null) textoBtnReset.text = "RESETAR PROGRESSO";
            }
        }

        // Atualiza visibilidade dos botões do professor (Lobby e Gameplay)
        AtualizarBotoesAcesso();
    }

    private void CarregarRecursos()
    {
        if (fontProf == null)
            fontProf = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset;

        if (fontJogo == null)
            fontJogo = Resources.Load<TMP_FontAsset>("CrayonHandRegular2016Demo SDF") ?? fontProf;

        if (fontProf == null) fontProf = fontJogo;

        if (spritePainelPergaminho == null) spritePainelPergaminho = Resources.Load<Sprite>("painel_pergaminho") ?? Resources.Load<Sprite>("fundo_pergaminho_jogo");
        if (spriteXFechar == null) spriteXFechar = Resources.Load<Sprite>("x_fechar_jogo");
        if (spriteBtnAzul == null) spriteBtnAzul = Resources.Load<Sprite>("btn_azul_jogo");
        if (spriteBtnVerde == null) spriteBtnVerde = Resources.Load<Sprite>("btn_verde_jogo") ?? Resources.Load<Sprite>("btn_jogar_verde");
        if (spriteBtnLaranja == null) spriteBtnLaranja = Resources.Load<Sprite>("btn_laranja_jogo") ?? Resources.Load<Sprite>("btn_laranja");
        if (spriteBtnVermelho == null) spriteBtnVermelho = Resources.Load<Sprite>("btn_vermelho_jogo");
        if (spriteCardFundo == null) spriteCardFundo = Resources.Load<Sprite>("card_aluno_fundo_jogo") ?? Resources.Load<Sprite>("card_aluno_fundo");
        if (spriteCardAtivo == null) spriteCardAtivo = Resources.Load<Sprite>("card_aluno_ativo_jogo") ?? Resources.Load<Sprite>("card_aluno_ativo");
        if (spriteBarraPesquisa == null) spriteBarraPesquisa = Resources.Load<Sprite>("barra_pesquisa_jogo");
        if (spriteAvatarAluno == null) spriteAvatarAluno = Resources.Load<Sprite>("avatar_aluno_jogo") ?? Resources.Load<Sprite>("icone_aluno");
    }

    private void ConstruirCanvasDedicado()
    {
        canvasRoot = gameObject.GetComponent<Canvas>();
        if (canvasRoot == null) canvasRoot = gameObject.AddComponent<Canvas>();
        canvasRoot.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasRoot.sortingOrder = 998;

        canvasScaler = gameObject.GetComponent<CanvasScaler>();
        if (canvasScaler == null) canvasScaler = gameObject.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    // =========================================================
    // MODAL 1: LOGIN DO PROFESSOR (USUÁRIO E SENHA)
    // =========================================================
    private void ConstruirModalLogin()
    {
        modalLogin = new GameObject("ModalLoginProfessor", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalLogin.transform.SetParent(transform, false);
        RectTransform rtModal = modalLogin.GetComponent<RectTransform>();
        rtModal.anchorMin = Vector2.zero;
        rtModal.anchorMax = Vector2.one;
        rtModal.offsetMin = Vector2.zero;
        rtModal.offsetMax = Vector2.zero;

        Image imgOverlay = modalLogin.GetComponent<Image>();
        imgOverlay.color = new Color(0.06f, 0.04f, 0.03f, 0.88f);
        imgOverlay.raycastTarget = true;

        // Janela Central de Login (680 x 540)
        GameObject janela = new GameObject("JanelaLogin", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        janela.transform.SetParent(modalLogin.transform, false);
        RectTransform rtJanela = janela.GetComponent<RectTransform>();
        rtJanela.anchorMin = new Vector2(0.5f, 0.5f);
        rtJanela.anchorMax = new Vector2(0.5f, 0.5f);
        rtJanela.pivot = new Vector2(0.5f, 0.5f);
        rtJanela.anchoredPosition = Vector2.zero;
        rtJanela.sizeDelta = new Vector2(680f, 540f);

        Image imgJanela = janela.GetComponent<Image>();
        if (spritePainelPergaminho != null)
        {
            imgJanela.sprite = spritePainelPergaminho;
            imgJanela.type = Image.Type.Sliced;
            imgJanela.color = Color.white;
        }
        else
        {
            imgJanela.color = new Color(1f, 0.95f, 0.82f, 1f);
        }

        // Botão Fechar X
        CriarBotaoFechar(janela.transform, FecharAreaDoProfessor, new Vector2(-28f, -28f));

        // Título e Subtítulo
        GameObject goHeader = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goHeader.transform.SetParent(janela.transform, false);
        RectTransform rtHeader = goHeader.GetComponent<RectTransform>();
        rtHeader.anchorMin = new Vector2(0.5f, 1f);
        rtHeader.anchorMax = new Vector2(0.5f, 1f);
        rtHeader.pivot = new Vector2(0.5f, 1f);
        rtHeader.anchoredPosition = new Vector2(0f, -35f);
        rtHeader.sizeDelta = new Vector2(600f, 95f);

        TextMeshProUGUI tmpHeader = goHeader.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) tmpHeader.font = fontJogo;
        tmpHeader.text = "<size=30><b>[ACESSO RESTRITO] ÁREA DO PROFESSOR</b></size>\n<size=20><color=#5D4037>Área pedagógica restrita aos educadores</color></size>";
        tmpHeader.alignment = TextAlignmentOptions.Center;
        tmpHeader.color = new Color(0.2f, 0.12f, 0.05f, 1f);

        // Campo Usuário
        GameObject goUser = CriarCampoInput(janela.transform, "InputUsuario", new Vector2(0f, -170f), new Vector2(460f, 56f), "Usuário: professor", false, out inputLoginUser);

        // Campo Senha
        GameObject goPass = CriarCampoInput(janela.transform, "InputSenha", new Vector2(0f, -245f), new Vector2(460f, 56f), "Senha (ex: 1234)", true, out inputLoginPass);

        // Feedback de Erro
        GameObject goFeed = new GameObject("FeedbackLogin", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goFeed.transform.SetParent(janela.transform, false);
        RectTransform rtFeed = goFeed.GetComponent<RectTransform>();
        rtFeed.anchorMin = new Vector2(0.5f, 1f);
        rtFeed.anchorMax = new Vector2(0.5f, 1f);
        rtFeed.pivot = new Vector2(0.5f, 1f);
        rtFeed.anchoredPosition = new Vector2(0f, -315f);
        rtFeed.sizeDelta = new Vector2(560f, 35f);
        textoFeedbackLogin = goFeed.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) textoFeedbackLogin.font = fontJogo;
        textoFeedbackLogin.fontSize = 18;
        textoFeedbackLogin.alignment = TextAlignmentOptions.Center;
        textoFeedbackLogin.color = new Color(0.85f, 0.15f, 0.15f, 1f);
        textoFeedbackLogin.text = "";

        // Botões de Ação: Entrar e Cancelar
        CriarBotaoAcao(janela.transform, "BtnEntrar", new Vector2(-120f, -385f), new Vector2(210f, 60f), "ENTRAR", spriteBtnVerde, new Color(0.18f, 0.55f, 0.22f, 1f), OnClickSubmeterLogin);
        CriarBotaoAcao(janela.transform, "BtnCancelar", new Vector2(120f, -385f), new Vector2(210f, 60f), "CANCELAR", spriteBtnVermelho, new Color(0.75f, 0.2f, 0.2f, 1f), FecharAreaDoProfessor);

        // Dica de Credenciais
        GameObject goDica = new GameObject("DicaLogin", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goDica.transform.SetParent(janela.transform, false);
        RectTransform rtDica = goDica.GetComponent<RectTransform>();
        rtDica.anchorMin = new Vector2(0.5f, 0f);
        rtDica.anchorMax = new Vector2(0.5f, 0f);
        rtDica.pivot = new Vector2(0.5f, 0f);
        rtDica.anchoredPosition = new Vector2(0f, 25f);
        rtDica.sizeDelta = new Vector2(600f, 40f);
        TextMeshProUGUI tmpDica = goDica.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) tmpDica.font = fontJogo;
        tmpDica.text = "<size=17><color=#795548>Padrão inicial: usuário <b>professor</b> | senha <b>1234</b></color></size>";
        tmpDica.alignment = TextAlignmentOptions.Center;

        modalLogin.SetActive(false);
    }

    // =========================================================
    // MODAL 2: DASHBOARD COMPLETO DO PROFESSOR
    // =========================================================
    private void ConstruirModalDashboard()
    {
        modalDashboard = new GameObject("ModalDashboardProfessor", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalDashboard.transform.SetParent(transform, false);
        RectTransform rtModal = modalDashboard.GetComponent<RectTransform>();
        rtModal.anchorMin = Vector2.zero;
        rtModal.anchorMax = Vector2.one;
        rtModal.offsetMin = Vector2.zero;
        rtModal.offsetMax = Vector2.zero;

        Image imgOverlay = modalDashboard.GetComponent<Image>();
        imgOverlay.color = new Color(0.06f, 0.04f, 0.03f, 0.90f);
        imgOverlay.raycastTarget = true;

        // Janela Central (1560 x 890)
        GameObject janela = new GameObject("JanelaDashboard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        janela.transform.SetParent(modalDashboard.transform, false);
        RectTransform rtJanela = janela.GetComponent<RectTransform>();
        rtJanela.anchorMin = new Vector2(0.5f, 0.5f);
        rtJanela.anchorMax = new Vector2(0.5f, 0.5f);
        rtJanela.pivot = new Vector2(0.5f, 0.5f);
        rtJanela.anchoredPosition = Vector2.zero;
        rtJanela.sizeDelta = new Vector2(1560f, 890f);

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

        // Botão Fechar X
        CriarBotaoFechar(janela.transform, FecharAreaDoProfessor, new Vector2(-35f, -30f));

        // Cabeçalho Principal (Alinhado à esquerda com margem para evitar qualquer sobreposição)
        GameObject goHeader = new GameObject("HeaderDashboard", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goHeader.transform.SetParent(janela.transform, false);
        RectTransform rtH = goHeader.GetComponent<RectTransform>();
        rtH.anchorMin = new Vector2(0f, 1f);
        rtH.anchorMax = new Vector2(0f, 1f);
        rtH.pivot = new Vector2(0f, 1f);
        rtH.anchoredPosition = new Vector2(50f, -24f);
        rtH.sizeDelta = new Vector2(900f, 75f);

        TextMeshProUGUI tmpH = goHeader.GetComponent<TextMeshProUGUI>();
        tmpH.font = fontProf ?? fontJogo;
        tmpH.text = "<size=30><b>PAINEL PEDAGÓGICO DO PROFESSOR</b></size>\n<size=17><color=#5D4037>Acompanhamento de Alunos | Diagnóstico de Aprendizagem | Modo Educador</color></size>";
        tmpH.alignment = TextAlignmentOptions.TopLeft;
        tmpH.color = new Color(0.2f, 0.12f, 0.05f, 1f);

        // Barra de Status do Professor (Sessão Ativa + Botão Sair)
        CriarBarraStatusProfessor(janela.transform);

        // ==========================================
        // COLUNA ESQUERDA: LISTA DE ALUNOS (440 px)
        // ==========================================
        ConstruirColunaAlunos(janela.transform);

        // ==========================================
        // COLUNA DIREITA: RELATÓRIO DO ALUNO (1010 px)
        // ==========================================
        ConstruirColunaRelatorio(janela.transform);

        modalDashboard.SetActive(false);
    }

    private void CriarBarraStatusProfessor(Transform pai)
    {
        GameObject barra = new GameObject("BarraStatusProfessor", typeof(RectTransform));
        barra.transform.SetParent(pai, false);
        RectTransform rt = barra.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-110f, -22f);
        rt.sizeDelta = new Vector2(360f, 50f);

        // Texto Sessão Ativa
        GameObject goTxt = new GameObject("TxtSessao", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxt.transform.SetParent(barra.transform, false);
        RectTransform rtT = goTxt.GetComponent<RectTransform>();
        rtT.anchorMin = new Vector2(0f, 0.5f);
        rtT.anchorMax = new Vector2(0f, 0.5f);
        rtT.pivot = new Vector2(0f, 0.5f);
        rtT.anchoredPosition = new Vector2(0f, 0f);
        rtT.sizeDelta = new Vector2(200f, 40f);
        TextMeshProUGUI tmpS = goTxt.GetComponent<TextMeshProUGUI>();
        tmpS.font = fontProf ?? fontJogo;
        tmpS.text = "<b><color=#1B5E20>[Sessão: Professor]</color></b>";
        tmpS.fontSize = 16;
        tmpS.alignment = TextAlignmentOptions.MidlineLeft;

        // Botão Encerrar Sessão perfeitamente posicionado e sem cortes
        CriarBotaoAcao(barra.transform, "BtnSairSessao", new Vector2(0f, 0f), new Vector2(130f, 42f), "ENCERRAR", spriteBtnVermelho, new Color(0.75f, 0.2f, 0.2f, 1f), EncerrarSessaoProfessor, new Vector2(1f, 0.5f));
    }

    private void ConstruirColunaAlunos(Transform pai)
    {
        GameObject cardCol = new GameObject("ColunaAlunos", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardCol.transform.SetParent(pai, false);
        RectTransform rt = cardCol.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(50f, -30f);
        rt.sizeDelta = new Vector2(430f, -145f);

        Image img = cardCol.GetComponent<Image>();
        img.color = new Color(0.94f, 0.88f, 0.77f, 0.95f);
        if (spriteCardFundo != null)
        {
            img.sprite = spriteCardFundo;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }

        // Título da Coluna
        GameObject goTit = new GameObject("TituloLista", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTit.transform.SetParent(cardCol.transform, false);
        RectTransform rtTit = goTit.GetComponent<RectTransform>();
        rtTit.anchorMin = new Vector2(0f, 1f);
        rtTit.anchorMax = new Vector2(1f, 1f);
        rtTit.pivot = new Vector2(0.5f, 1f);
        rtTit.anchoredPosition = new Vector2(0f, -16f);
        rtTit.sizeDelta = new Vector2(390f, 36f);
        TextMeshProUGUI tmpTit = goTit.GetComponent<TextMeshProUGUI>();
        tmpTit.font = fontProf ?? fontJogo;
        tmpTit.text = "<b>SELECIONE O ALUNO</b>";
        tmpTit.fontSize = 22;
        tmpTit.alignment = TextAlignmentOptions.Center;
        tmpTit.color = new Color(0.25f, 0.15f, 0.05f, 1f);

        // Barra de Pesquisa
        GameObject goPesq = CriarCampoInput(cardCol.transform, "InputPesquisa", new Vector2(0f, -72f), new Vector2(380f, 46f), "Buscar aluno...", false, out inputPesquisaAlunos);
        if (inputPesquisaAlunos != null)
        {
            inputPesquisaAlunos.onValueChanged.AddListener(FiltrarListaAlunos);
        }

        // ScrollView de Alunos
        GameObject scrollObj = new GameObject("ScrollAlunos", typeof(RectTransform), typeof(CanvasRenderer), typeof(ScrollRect));
        scrollObj.transform.SetParent(cardCol.transform, false);
        RectTransform rtScroll = scrollObj.GetComponent<RectTransform>();
        rtScroll.anchorMin = new Vector2(0f, 0f);
        rtScroll.anchorMax = new Vector2(1f, 1f);
        rtScroll.offsetMin = new Vector2(15f, 15f);
        rtScroll.offsetMax = new Vector2(-15f, -105f);

        ScrollRect scroll = scrollObj.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform rtVp = viewport.GetComponent<RectTransform>();
        rtVp.anchorMin = Vector2.zero;
        rtVp.anchorMax = Vector2.one;
        rtVp.offsetMin = Vector2.zero;
        rtVp.offsetMax = Vector2.zero;
        scroll.viewport = rtVp;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        containerListaAlunos = content.transform;
        RectTransform rtContent = content.GetComponent<RectTransform>();
        rtContent.anchorMin = new Vector2(0f, 1f);
        rtContent.anchorMax = new Vector2(1f, 1f);
        rtContent.pivot = new Vector2(0.5f, 1f);
        rtContent.anchoredPosition = Vector2.zero;
        rtContent.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(4, 4, 6, 6);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = rtContent;
    }

    private void ConstruirColunaRelatorio(Transform pai)
    {
        GameObject cardRel = new GameObject("ColunaRelatorio", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cardRel.transform.SetParent(pai, false);
        RectTransform rt = cardRel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(505f, 35f);
        rt.offsetMax = new Vector2(-50f, -80f);

        Image img = cardRel.GetComponent<Image>();
        img.color = new Color(0.96f, 0.92f, 0.82f, 0.95f);
        if (spriteCardFundo != null)
        {
            img.sprite = spriteCardFundo;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }

        // 1. Header do Aluno Selecionado + Barra de Progresso Geral
        GameObject goAlunoHeader = new GameObject("AlunoHeader", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goAlunoHeader.transform.SetParent(cardRel.transform, false);
        RectTransform rtAH = goAlunoHeader.GetComponent<RectTransform>();
        rtAH.anchorMin = new Vector2(0f, 1f);
        rtAH.anchorMax = new Vector2(1f, 1f);
        rtAH.pivot = new Vector2(0f, 1f);
        rtAH.anchoredPosition = new Vector2(25f, -20f);
        rtAH.sizeDelta = new Vector2(550f, 40f);
        textoHeaderAluno = goAlunoHeader.GetComponent<TextMeshProUGUI>();
        textoHeaderAluno.font = fontProf ?? fontJogo;
        textoHeaderAluno.fontSize = 28;
        textoHeaderAluno.alignment = TextAlignmentOptions.MidlineLeft;
        textoHeaderAluno.color = new Color(0.2f, 0.12f, 0.05f, 1f);
        textoHeaderAluno.text = "<b>Aluno: Selecione na lista</b>";

        // Texto Progresso Geral
        GameObject goProgTxt = new GameObject("ProgressoGeralTxt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goProgTxt.transform.SetParent(cardRel.transform, false);
        RectTransform rtPT = goProgTxt.GetComponent<RectTransform>();
        rtPT.anchorMin = new Vector2(1f, 1f);
        rtPT.anchorMax = new Vector2(1f, 1f);
        rtPT.pivot = new Vector2(1f, 1f);
        rtPT.anchoredPosition = new Vector2(-25f, -20f);
        rtPT.sizeDelta = new Vector2(380f, 40f);
        textoProgressoGeral = goProgTxt.GetComponent<TextMeshProUGUI>();
        textoProgressoGeral.font = fontProf ?? fontJogo;
        textoProgressoGeral.fontSize = 20;
        textoProgressoGeral.alignment = TextAlignmentOptions.MidlineRight;
        textoProgressoGeral.color = new Color(0.1f, 0.35f, 0.15f, 1f);
        textoProgressoGeral.text = "<b>0 / 180 Fases Concluídas (0.0%)</b>";

        // Barra de Progresso Visual
        GameObject goBarBg = new GameObject("BarraProgressoBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goBarBg.transform.SetParent(cardRel.transform, false);
        RectTransform rtBarBg = goBarBg.GetComponent<RectTransform>();
        rtBarBg.anchorMin = new Vector2(0f, 1f);
        rtBarBg.anchorMax = new Vector2(1f, 1f);
        rtBarBg.pivot = new Vector2(0.5f, 1f);
        rtBarBg.anchoredPosition = new Vector2(0f, -65f);
        rtBarBg.sizeDelta = new Vector2(-50f, 18f);
        Image imgBarBg = goBarBg.GetComponent<Image>();
        imgBarBg.color = new Color(0.78f, 0.72f, 0.62f, 1f);

        GameObject goBarFill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goBarFill.transform.SetParent(goBarBg.transform, false);
        RectTransform rtBarFill = goBarFill.GetComponent<RectTransform>();
        rtBarFill.anchorMin = Vector2.zero;
        rtBarFill.anchorMax = Vector2.one;
        rtBarFill.offsetMin = Vector2.zero;
        rtBarFill.offsetMax = Vector2.zero;
        imgBarraProgressoFill = goBarFill.GetComponent<Image>();
        imgBarraProgressoFill.color = new Color(0.25f, 0.72f, 0.32f, 1f);
        imgBarraProgressoFill.type = Image.Type.Filled;
        imgBarraProgressoFill.fillMethod = Image.FillMethod.Horizontal;
        imgBarraProgressoFill.fillAmount = 0f;

        // 2. Linha de Resumo (3 Cards Elegantes: Troféus, Vidas/Precisão, Aproveitamento)
        ConstruirCardsEstatisticas(cardRel.transform);

        // 3. Duas Tabelas Detalhadas Lado a Lado (Português & Matemática)
        ConstruirTabelasDisciplinas(cardRel.transform);

        // 4. Caixa de Parecer Pedagógico Inteligente
        ConstruirParecerPedagogico(cardRel.transform);

        // 5. Barra de Ações (Copiar Relatório, Resetar, Alterar Senha)
        ConstruirBarraAcoesRelatorio(cardRel.transform);
    }

    private void ConstruirCardsEstatisticas(Transform pai)
    {
        GameObject linhaStats = new GameObject("LinhaStats", typeof(RectTransform));
        linhaStats.transform.SetParent(pai, false);
        RectTransform rt = linhaStats.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -95f);
        rt.sizeDelta = new Vector2(-50f, 75f);

        // Card 1: Troféus
        textoTrofeusResumo = CriarMiniCardStat(linhaStats.transform, "CardTrofeus", new Vector2(-315f, 0f), new Vector2(290f, 70f), "TROFÉUS CONQUISTADOS", "0 Ouro | 0 Prata | 0 Bronze");

        // Card 2: Vidas Preservadas
        textoVidasResumo = CriarMiniCardStat(linhaStats.transform, "CardVidas", new Vector2(0f, 0f), new Vector2(290f, 70f), "VIDAS PRESERVADAS", "0 / 360 Corações");

        // Card 3: Aproveitamento Geral
        textoAproveitamentoResumo = CriarMiniCardStat(linhaStats.transform, "CardAproveitamento", new Vector2(315f, 0f), new Vector2(290f, 70f), "DESEMPENHO GERAL", "Iniciante (0%)");
    }

    private TextMeshProUGUI CriarMiniCardStat(Transform pai, string nome, Vector2 pos, Vector2 tam, string titulo, string valorInicial)
    {
        GameObject card = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(pai, false);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;

        Image img = card.GetComponent<Image>();
        img.color = new Color(0.90f, 0.84f, 0.73f, 0.95f);
        if (spriteCardFundo != null)
        {
            img.sprite = spriteCardFundo;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }

        GameObject goT = new GameObject("Titulo", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goT.transform.SetParent(card.transform, false);
        RectTransform rtT = goT.GetComponent<RectTransform>();
        rtT.anchorMin = new Vector2(0f, 1f);
        rtT.anchorMax = new Vector2(1f, 1f);
        rtT.pivot = new Vector2(0.5f, 1f);
        rtT.anchoredPosition = new Vector2(0f, -6f);
        rtT.sizeDelta = new Vector2(tam.x - 16f, 22f);
        TextMeshProUGUI tmpT = goT.GetComponent<TextMeshProUGUI>();
        tmpT.font = fontProf ?? fontJogo;
        tmpT.fontSize = 15;
        tmpT.alignment = TextAlignmentOptions.Center;
        tmpT.color = new Color(0.45f, 0.3f, 0.12f, 1f);
        tmpT.text = $"<b>{titulo}</b>";

        GameObject goV = new GameObject("Valor", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goV.transform.SetParent(card.transform, false);
        RectTransform rtV = goV.GetComponent<RectTransform>();
        rtV.anchorMin = new Vector2(0f, 0f);
        rtV.anchorMax = new Vector2(1f, 1f);
        rtV.pivot = new Vector2(0.5f, 0.5f);
        rtV.offsetMin = new Vector2(8f, 6f);
        rtV.offsetMax = new Vector2(-8f, -24f);
        TextMeshProUGUI tmpV = goV.GetComponent<TextMeshProUGUI>();
        tmpV.font = fontProf ?? fontJogo;
        tmpV.fontSize = 17;
        tmpV.alignment = TextAlignmentOptions.Center;
        tmpV.color = new Color(0.18f, 0.11f, 0.04f, 1f);
        tmpV.text = valorInicial;

        return tmpV;
    }

    private void ConstruirTabelasDisciplinas(Transform pai)
    {
        GameObject containerTab = new GameObject("ContainerTabelas", typeof(RectTransform));
        containerTab.transform.SetParent(pai, false);
        RectTransform rt = containerTab.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -180f);
        rt.sizeDelta = new Vector2(-50f, 240f);

        // Tabela Português
        GameObject colPort = new GameObject("ColPort", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        colPort.transform.SetParent(containerTab.transform, false);
        RectTransform rtPort = colPort.GetComponent<RectTransform>();
        rtPort.anchorMin = new Vector2(0f, 0f);
        rtPort.anchorMax = new Vector2(0.5f, 1f);
        rtPort.offsetMin = Vector2.zero;
        rtPort.offsetMax = new Vector2(-10f, 0f);
        Image imgPort = colPort.GetComponent<Image>();
        imgPort.color = new Color(0.92f, 0.86f, 0.75f, 0.9f);
        if (spriteCardFundo != null) { imgPort.sprite = spriteCardFundo; imgPort.type = Image.Type.Sliced; imgPort.color = Color.white; }

        GameObject goTxtPort = new GameObject("TextoPort", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxtPort.transform.SetParent(colPort.transform, false);
        RectTransform rtTP = goTxtPort.GetComponent<RectTransform>();
        rtTP.anchorMin = Vector2.zero;
        rtTP.anchorMax = Vector2.one;
        rtTP.offsetMin = new Vector2(16f, 12f);
        rtTP.offsetMax = new Vector2(-16f, -12f);
        textoTabelaPort = goTxtPort.GetComponent<TextMeshProUGUI>();
        textoTabelaPort.font = fontProf ?? fontJogo;
        textoTabelaPort.fontSize = 17;
        textoTabelaPort.lineSpacing = 12f;
        textoTabelaPort.color = new Color(0.18f, 0.11f, 0.05f, 1f);
        textoTabelaPort.text = "<b>LÍNGUA PORTUGUESA</b>\n- Fácil: 0/30\n- Médio: 0/30\n- Difícil: 0/30";

        // Tabela Matemática
        GameObject colMath = new GameObject("ColMath", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        colMath.transform.SetParent(containerTab.transform, false);
        RectTransform rtMath = colMath.GetComponent<RectTransform>();
        rtMath.anchorMin = new Vector2(0.5f, 0f);
        rtMath.anchorMax = new Vector2(1f, 1f);
        rtMath.offsetMin = new Vector2(10f, 0f);
        rtMath.offsetMax = Vector2.zero;
        Image imgMath = colMath.GetComponent<Image>();
        imgMath.color = new Color(0.92f, 0.86f, 0.75f, 0.9f);
        if (spriteCardFundo != null) { imgMath.sprite = spriteCardFundo; imgMath.type = Image.Type.Sliced; imgMath.color = Color.white; }

        GameObject goTxtMath = new GameObject("TextoMath", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxtMath.transform.SetParent(colMath.transform, false);
        RectTransform rtTM = goTxtMath.GetComponent<RectTransform>();
        rtTM.anchorMin = Vector2.zero;
        rtTM.anchorMax = Vector2.one;
        rtTM.offsetMin = new Vector2(16f, 12f);
        rtTM.offsetMax = new Vector2(-16f, -12f);
        textoTabelaMath = goTxtMath.GetComponent<TextMeshProUGUI>();
        textoTabelaMath.font = fontProf ?? fontJogo;
        textoTabelaMath.fontSize = 17;
        textoTabelaMath.lineSpacing = 12f;
        textoTabelaMath.color = new Color(0.18f, 0.11f, 0.05f, 1f);
        textoTabelaMath.text = "<b>MATEMÁTICA</b>\n- Fácil: 0/30\n- Médio: 0/30\n- Difícil: 0/30";
    }

    private void ConstruirParecerPedagogico(Transform pai)
    {
        GameObject box = new GameObject("BoxParecer", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        box.transform.SetParent(pai, false);
        RectTransform rt = box.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(25f, 90f);
        rt.offsetMax = new Vector2(-25f, -430f);

        Image img = box.GetComponent<Image>();
        img.color = new Color(0.93f, 0.88f, 0.77f, 0.95f);
        if (spriteCardFundo != null)
        {
            img.sprite = spriteCardFundo;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }

        GameObject goTxt = new GameObject("TextoParecer", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxt.transform.SetParent(box.transform, false);
        RectTransform rtT = goTxt.GetComponent<RectTransform>();
        rtT.anchorMin = Vector2.zero;
        rtT.anchorMax = Vector2.one;
        rtT.offsetMin = new Vector2(18f, 12f);
        rtT.offsetMax = new Vector2(-18f, -12f);

        textoDiagnosticoPedagogico = goTxt.GetComponent<TextMeshProUGUI>();
        textoDiagnosticoPedagogico.font = fontProf ?? fontJogo;
        textoDiagnosticoPedagogico.fontSize = 17;
        textoDiagnosticoPedagogico.lineSpacing = 8f;
        textoDiagnosticoPedagogico.color = new Color(0.18f, 0.11f, 0.05f, 1f);
        textoDiagnosticoPedagogico.text = "<b>[PARECER PEDAGÓGICO]</b>\nSelecione um aluno para visualizar o diagnóstico detalhado de aprendizagem.";
    }

    private void ConstruirBarraAcoesRelatorio(Transform pai)
    {
        GameObject barra = new GameObject("BarraAcoes", typeof(RectTransform));
        barra.transform.SetParent(pai, false);
        RectTransform rt = barra.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 15f);
        rt.sizeDelta = new Vector2(-50f, 65f);

        // Feedback de Ações
        GameObject goFeed = new GameObject("FeedbackAcoes", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goFeed.transform.SetParent(barra.transform, false);
        RectTransform rtF = goFeed.GetComponent<RectTransform>();
        rtF.anchorMin = new Vector2(0f, 1f);
        rtF.anchorMax = new Vector2(1f, 1f);
        rtF.pivot = new Vector2(0.5f, 1f);
        rtF.anchoredPosition = new Vector2(0f, 22f);
        rtF.sizeDelta = new Vector2(600f, 25f);
        textoFeedbackAcoes = goFeed.GetComponent<TextMeshProUGUI>();
        textoFeedbackAcoes.font = fontProf ?? fontJogo;
        textoFeedbackAcoes.fontSize = 16;
        textoFeedbackAcoes.alignment = TextAlignmentOptions.Center;
        textoFeedbackAcoes.color = new Color(0.12f, 0.45f, 0.18f, 1f);
        textoFeedbackAcoes.text = "";

        // Três botões centralizados e distribuídos harmonicamente na barra inferior
        CriarBotaoAcao(barra.transform, "BtnCopiar", new Vector2(-270f, 0f), new Vector2(250f, 52f), "COPIAR RELATÓRIO", spriteBtnAzul, new Color(0.15f, 0.35f, 0.7f, 1f), OnClickCopiarRelatorio, new Vector2(0.5f, 0.5f));

        GameObject btnR = CriarBotaoAcao(barra.transform, "BtnReset", new Vector2(0f, 0f), new Vector2(250f, 52f), "RESETAR PROGRESSO", spriteBtnVermelho, new Color(0.75f, 0.2f, 0.2f, 1f), OnClickResetarAluno, new Vector2(0.5f, 0.5f));
        textoBtnReset = btnR.GetComponentInChildren<TextMeshProUGUI>();

        CriarBotaoAcao(barra.transform, "BtnAltSenha", new Vector2(270f, 0f), new Vector2(250f, 52f), "ALTERAR SENHA", spriteBtnLaranja, new Color(0.85f, 0.45f, 0.1f, 1f), AbrirModalAlterarSenha, new Vector2(0.5f, 0.5f));
    }

    // =========================================================
    // MODAL 3: RESOLUÇÃO DA FASE (DURANTE A GAMEPLAY)
    // =========================================================
    private void ConstruirModalResolucaoGameplay()
    {
        modalResolucaoGameplay = new GameObject("ModalResolucaoGameplay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalResolucaoGameplay.transform.SetParent(transform, false);
        RectTransform rtModal = modalResolucaoGameplay.GetComponent<RectTransform>();
        rtModal.anchorMin = Vector2.zero;
        rtModal.anchorMax = Vector2.one;
        rtModal.offsetMin = Vector2.zero;
        rtModal.offsetMax = Vector2.zero;

        Image imgOverlay = modalResolucaoGameplay.GetComponent<Image>();
        imgOverlay.color = new Color(0.06f, 0.04f, 0.03f, 0.88f);
        imgOverlay.raycastTarget = true;

        // Janela Central Ampliada (960 x 660)
        GameObject janela = new GameObject("JanelaResolucao", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        janela.transform.SetParent(modalResolucaoGameplay.transform, false);
        RectTransform rtJanela = janela.GetComponent<RectTransform>();
        rtJanela.anchorMin = new Vector2(0.5f, 0.5f);
        rtJanela.anchorMax = new Vector2(0.5f, 0.5f);
        rtJanela.pivot = new Vector2(0.5f, 0.5f);
        rtJanela.anchoredPosition = Vector2.zero;
        rtJanela.sizeDelta = new Vector2(1050f, 720f);

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

        CriarBotaoFechar(janela.transform, FecharResolucaoGameplay, new Vector2(-36f, -36f));

        // Título Limpo sem Emojis
        GameObject goTit = new GameObject("Titulo", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTit.transform.SetParent(janela.transform, false);
        RectTransform rtT = goTit.GetComponent<RectTransform>();
        rtT.anchorMin = new Vector2(0.5f, 1f);
        rtT.anchorMax = new Vector2(0.5f, 1f);
        rtT.pivot = new Vector2(0.5f, 1f);
        rtT.anchoredPosition = new Vector2(0f, -32f);
        rtT.sizeDelta = new Vector2(900f, 55f);
        textoTituloResolucao = goTit.GetComponent<TextMeshProUGUI>();
        textoTituloResolucao.font = fontProf ?? fontJogo;
        textoTituloResolucao.fontSize = 24;
        textoTituloResolucao.alignment = TextAlignmentOptions.Center;
        textoTituloResolucao.color = new Color(0.2f, 0.12f, 0.05f, 1f);
        textoTituloResolucao.text = "<b>GABARITO E RESOLUÇÃO DA FASE</b>";

        // Caixa de Conteúdo da Resolução (com margem para não sobrepor botões)
        GameObject goCorpo = new GameObject("CorpoResolucao", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goCorpo.transform.SetParent(janela.transform, false);
        RectTransform rtC = goCorpo.GetComponent<RectTransform>();
        rtC.anchorMin = new Vector2(0.5f, 1f);
        rtC.anchorMax = new Vector2(0.5f, 1f);
        rtC.pivot = new Vector2(0.5f, 1f);
        rtC.anchoredPosition = new Vector2(0f, -95f);
        rtC.sizeDelta = new Vector2(950f, 490f);
        textoCorpoResolucao = goCorpo.GetComponent<TextMeshProUGUI>();
        textoCorpoResolucao.font = fontProf ?? fontJogo;
        textoCorpoResolucao.fontSize = 19;
        textoCorpoResolucao.lineSpacing = 7f;
        textoCorpoResolucao.color = new Color(0.18f, 0.11f, 0.05f, 1f);
        textoCorpoResolucao.alignment = TextAlignmentOptions.TopLeft;

        // Botões de Ação na Gameplay ancorados na parte INFERIOR (nunca sobrepõem o texto)
        CriarBotaoAcao(janela.transform, "BtnVideo", new Vector2(-200f, 35f), new Vector2(360f, 58f), "VÍDEO EXPLICATIVO (YOUTUBE)", spriteBtnLaranja, new Color(0.85f, 0.45f, 0.1f, 1f), OnClickAssistirVideoExplicativo, new Vector2(0.5f, 0f));
        CriarBotaoAcao(janela.transform, "BtnAutoCompletar", new Vector2(200f, 35f), new Vector2(360f, 58f), "AUTO-COMPLETAR FASE", spriteBtnVerde, new Color(0.18f, 0.55f, 0.22f, 1f), OnClickAutoCompletarFase, new Vector2(0.5f, 0f));

        modalResolucaoGameplay.SetActive(false);
    }

    // =========================================================
    // MODAL 4: ALTERAÇÃO DE SENHA DO PROFESSOR
    // =========================================================
    private void ConstruirModalAlterarSenha()
    {
        modalAlterarSenha = new GameObject("ModalAlterarSenha", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalAlterarSenha.transform.SetParent(transform, false);
        RectTransform rtModal = modalAlterarSenha.GetComponent<RectTransform>();
        rtModal.anchorMin = Vector2.zero;
        rtModal.anchorMax = Vector2.one;
        rtModal.offsetMin = Vector2.zero;
        rtModal.offsetMax = Vector2.zero;

        Image imgOverlay = modalAlterarSenha.GetComponent<Image>();
        imgOverlay.color = new Color(0.06f, 0.04f, 0.03f, 0.88f);
        imgOverlay.raycastTarget = true;

        GameObject janela = new GameObject("JanelaAltSenha", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        janela.transform.SetParent(modalAlterarSenha.transform, false);
        RectTransform rtJanela = janela.GetComponent<RectTransform>();
        rtJanela.anchorMin = new Vector2(0.5f, 0.5f);
        rtJanela.anchorMax = new Vector2(0.5f, 0.5f);
        rtJanela.pivot = new Vector2(0.5f, 0.5f);
        rtJanela.anchoredPosition = Vector2.zero;
        rtJanela.sizeDelta = new Vector2(620f, 460f);

        Image imgJanela = janela.GetComponent<Image>();
        if (spritePainelPergaminho != null) { imgJanela.sprite = spritePainelPergaminho; imgJanela.type = Image.Type.Sliced; imgJanela.color = Color.white; }
        else imgJanela.color = new Color(1f, 0.96f, 0.85f, 1f);

        CriarBotaoFechar(janela.transform, FecharModalAlterarSenha, new Vector2(-28f, -28f));

        GameObject goTit = new GameObject("Titulo", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTit.transform.SetParent(janela.transform, false);
        RectTransform rtT = goTit.GetComponent<RectTransform>();
        rtT.anchorMin = new Vector2(0.5f, 1f);
        rtT.anchorMax = new Vector2(0.5f, 1f);
        rtT.pivot = new Vector2(0.5f, 1f);
        rtT.anchoredPosition = new Vector2(0f, -35f);
        rtT.sizeDelta = new Vector2(520f, 65f);
        TextMeshProUGUI tmpT = goTit.GetComponent<TextMeshProUGUI>();
        tmpT.font = fontProf ?? fontJogo;
        tmpT.fontSize = 28;
        tmpT.alignment = TextAlignmentOptions.Center;
        tmpT.color = new Color(0.2f, 0.12f, 0.05f, 1f);
        tmpT.text = "<b>ALTERAR SENHA DO PROFESSOR</b>";

        CriarCampoInput(janela.transform, "InputNovaSenha", new Vector2(0f, -140f), new Vector2(420f, 54f), "Nova senha...", true, out inputNovaSenha);
        CriarCampoInput(janela.transform, "InputConfirmaSenha", new Vector2(0f, -215f), new Vector2(420f, 54f), "Confirme a nova senha...", true, out inputConfirmaSenha);

        GameObject goFeed = new GameObject("FeedbackSenha", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goFeed.transform.SetParent(janela.transform, false);
        RectTransform rtF = goFeed.GetComponent<RectTransform>();
        rtF.anchorMin = new Vector2(0.5f, 1f);
        rtF.anchorMax = new Vector2(0.5f, 1f);
        rtF.pivot = new Vector2(0.5f, 1f);
        rtF.anchoredPosition = new Vector2(0f, -280f);
        rtF.sizeDelta = new Vector2(500f, 30f);
        textoFeedbackSenha = goFeed.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) textoFeedbackSenha.font = fontJogo;
        textoFeedbackSenha.fontSize = 17;
        textoFeedbackSenha.alignment = TextAlignmentOptions.Center;
        textoFeedbackSenha.color = new Color(0.85f, 0.15f, 0.15f, 1f);
        textoFeedbackSenha.text = "";

        CriarBotaoAcao(janela.transform, "BtnSalvarSenha", new Vector2(-110f, -345f), new Vector2(190f, 54f), "SALVAR", spriteBtnVerde, new Color(0.18f, 0.55f, 0.22f, 1f), OnClickSalvarNovaSenha);
        CriarBotaoAcao(janela.transform, "BtnCancelarSenha", new Vector2(110f, -345f), new Vector2(190f, 54f), "CANCELAR", spriteBtnVermelho, new Color(0.75f, 0.2f, 0.2f, 1f), FecharModalAlterarSenha);

        modalAlterarSenha.SetActive(false);
    }

    // =========================================================
    // BOTÃO HUD DO PROFESSOR (DURANTE A GAMEPLAY)
    // =========================================================
    private void CriarBotaoHUDGameplay()
    {
        btnHUDProfessor = new GameObject("BtnHUDProfessor", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnHUDProfessor.transform.SetParent(transform, false);
        RectTransform rt = btnHUDProfessor.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-25f, -25f);
        rt.sizeDelta = new Vector2(240f, 58f);

        Image img = btnHUDProfessor.GetComponent<Image>();
        img.color = new Color(0.12f, 0.28f, 0.58f, 0.95f);
        if (spriteBtnAzul != null)
        {
            img.sprite = spriteBtnAzul;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }

        Button btn = btnHUDProfessor.GetComponent<Button>();
        btn.onClick.AddListener(OnClickBotaoHUDProfessor);

        GameObject goTxt = new GameObject("TextoHUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxt.transform.SetParent(btnHUDProfessor.transform, false);
        RectTransform rtT = goTxt.GetComponent<RectTransform>();
        rtT.anchorMin = Vector2.zero;
        rtT.anchorMax = Vector2.one;
        rtT.offsetMin = Vector2.zero;
        rtT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = goTxt.GetComponent<TextMeshProUGUI>();
        tmp.font = fontProf ?? fontJogo;
        tmp.text = "<b>[MODO PROFESSOR]</b>\n<size=14>Gabarito & Vídeo</size>";
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineColor = new Color32(10, 20, 50, 220);
        tmp.outlineWidth = 0.18f;

        btnHUDProfessor.SetActive(false);
    }

    private void CriarBotaoLobby()
    {
        btnLobbyProfessor = new GameObject("BtnLobbyProfessor", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnLobbyProfessor.transform.SetParent(transform, false);
        RectTransform rt = btnLobbyProfessor.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-30f, -32f);
        rt.sizeDelta = new Vector2(250f, 60f);

        Image img = btnLobbyProfessor.GetComponent<Image>();
        img.color = new Color(0.12f, 0.28f, 0.58f, 0.95f);
        if (spriteBtnAzul != null)
        {
            img.sprite = spriteBtnAzul;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }

        Button btn = btnLobbyProfessor.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            abrirResolucaoAposLogin = false;
            AbrirAreaDoProfessor();
        });

        GameObject goTxt = new GameObject("TextoLobby", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxt.transform.SetParent(btnLobbyProfessor.transform, false);
        RectTransform rtT = goTxt.GetComponent<RectTransform>();
        rtT.anchorMin = Vector2.zero;
        rtT.anchorMax = Vector2.one;
        rtT.offsetMin = Vector2.zero;
        rtT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = goTxt.GetComponent<TextMeshProUGUI>();
        tmp.font = fontProf ?? fontJogo;
        tmp.text = "<b>[ÁREA DO PROFESSOR]</b>\n<size=14>Desempenho & Acesso</size>";
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineColor = new Color32(10, 20, 50, 220);
        tmp.outlineWidth = 0.18f;

        btnLobbyProfessor.SetActive(false);
    }

    private void AtualizarBotoesAcesso()
    {
        Action action = FindFirstObjectByType<Action>();
        if (action == null)
        {
            if (btnHUDProfessor != null) btnHUDProfessor.SetActive(false);
            if (btnLobbyProfessor != null) btnLobbyProfessor.SetActive(false);
            return;
        }

        bool modalAberto = (modalLogin != null && modalLogin.activeSelf) ||
                           (modalDashboard != null && modalDashboard.activeSelf) ||
                           (modalResolucaoGameplay != null && modalResolucaoGameplay.activeSelf) ||
                           (modalAlterarSenha != null && modalAlterarSenha.activeSelf);

        // 1. Botão no Lobby (Aparece apenas na tela inicial do Lobby se nenhum modal estiver aberto)
        bool noLobby = (action.groupLobby != null && action.groupLobby.gameObject.activeSelf && action.groupLobby.alpha > 0.5f);
        if (btnLobbyProfessor != null)
        {
            btnLobbyProfessor.SetActive(noLobby && !modalAberto);
        }

        // 2. Botão no HUD da Gameplay (Aparece apenas durante o jogo ativo se nenhum modal estiver aberto)
        bool emGameplay = (action.groupJogoFacil != null && action.groupJogoFacil.gameObject.activeSelf && action.groupJogoFacil.alpha > 0.5f) ||
                          (action.groupJogoMedio != null && action.groupJogoMedio.gameObject.activeSelf && action.groupJogoMedio.alpha > 0.5f) ||
                          (action.groupJogoDificil != null && action.groupJogoDificil.gameObject.activeSelf && action.groupJogoDificil.alpha > 0.5f) ||
                          (action.groupJogoMathFacil != null && action.groupJogoMathFacil.gameObject.activeSelf && action.groupJogoMathFacil.alpha > 0.5f) ||
                          (action.groupJogoMathMedio != null && action.groupJogoMathMedio.gameObject.activeSelf && action.groupJogoMathMedio.alpha > 0.5f) ||
                          (action.groupJogoMathDificil != null && action.groupJogoMathDificil.gameObject.activeSelf && action.groupJogoMathDificil.alpha > 0.5f);

        if (btnHUDProfessor != null)
        {
            btnHUDProfessor.SetActive(emGameplay && !modalAberto);
        }
    }

    private void OnClickBotaoHUDProfessor()
    {
        TocarSomClique();
        if (!SessaoProfessorAtiva)
        {
            // Solicita autenticação para que alunos não acessem a resolução
            abrirResolucaoAposLogin = true;
            AbrirLoginProfessor();
        }
        else
        {
            AbrirResolucaoGameplay();
        }
    }

    // =========================================================
    // CONTROLE DE FLUXO E ABERTURA DE JANELAS
    // =========================================================
    public void AbrirAreaDoProfessor()
    {
        TocarSomClique();
        if (!SessaoProfessorAtiva)
        {
            AbrirLoginProfessor();
        }
        else
        {
            AbrirDashboardProfessor();
        }
    }

    public void AbrirLoginProfessor()
    {
        if (modalDashboard != null) modalDashboard.SetActive(false);
        if (modalResolucaoGameplay != null) modalResolucaoGameplay.SetActive(false);
        if (modalAlterarSenha != null) modalAlterarSenha.SetActive(false);

        if (inputLoginUser != null) inputLoginUser.text = "";
        if (inputLoginPass != null) inputLoginPass.text = "";
        if (textoFeedbackLogin != null) textoFeedbackLogin.text = "";

        if (modalLogin != null) modalLogin.SetActive(true);
    }

    public void AbrirDashboardProfessor()
    {
        if (modalLogin != null) modalLogin.SetActive(false);
        if (modalResolucaoGameplay != null) modalResolucaoGameplay.SetActive(false);
        if (modalAlterarSenha != null) modalAlterarSenha.SetActive(false);

        if (modalDashboard != null) modalDashboard.SetActive(true);

        CarregarListaAlunos("");
    }

    public void FecharAreaDoProfessor()
    {
        TocarSomClique();
        if (modalLogin != null) modalLogin.SetActive(false);
        if (modalDashboard != null) modalDashboard.SetActive(false);
        if (modalResolucaoGameplay != null) modalResolucaoGameplay.SetActive(false);
        if (modalAlterarSenha != null) modalAlterarSenha.SetActive(false);
    }

    public void EncerrarSessaoProfessor()
    {
        TocarSomClique();
        SessaoProfessorAtiva = false;

        GerenciadorFases gf = FindFirstObjectByType<GerenciadorFases>();
        if (gf != null) gf.ConfigurarTodosBotoes();
        Action action = FindFirstObjectByType<Action>();
        if (action != null) action.VerificarDesbloqueios();

        FecharAreaDoProfessor();
    }

    // =========================================================
    // LÓGICA DE AUTENTICAÇÃO E SENHA
    // =========================================================
    private void OnClickSubmeterLogin()
    {
        string usuarioDigitado = inputLoginUser != null ? inputLoginUser.text.Trim() : "";
        string senhaDigitada = inputLoginPass != null ? inputLoginPass.text.Trim() : "";

        string usuarioCorreto = PlayerPrefs.GetString(KEY_USER, DEFAULT_USER).Trim();
        string senhaCorreta = PlayerPrefs.GetString(KEY_PASS, DEFAULT_PASS).Trim();

        bool usuarioValido = string.Equals(usuarioDigitado, usuarioCorreto, StringComparison.OrdinalIgnoreCase);
        bool senhaValida = string.Equals(senhaDigitada, senhaCorreta);

        if (usuarioValido && senhaValida)
        {
            TocarSomClique();
            SessaoProfessorAtiva = true;

            GerenciadorFases gf = FindFirstObjectByType<GerenciadorFases>();
            if (gf != null) gf.ConfigurarTodosBotoes();
            Action action = FindFirstObjectByType<Action>();
            if (action != null) action.VerificarDesbloqueios();

            if (textoFeedbackLogin != null)
            {
                textoFeedbackLogin.color = new Color(0.15f, 0.55f, 0.2f, 1f);
                textoFeedbackLogin.text = "Login efetuado com sucesso!";
            }

            if (abrirResolucaoAposLogin)
            {
                Invoke(nameof(AbrirResolucaoGameplay), 0.35f);
            }
            else
            {
                Invoke(nameof(AbrirDashboardProfessor), 0.35f);
            }
        }
        else
        {
            TocarSomErro();
            if (textoFeedbackLogin != null)
            {
                textoFeedbackLogin.color = new Color(0.85f, 0.15f, 0.15f, 1f);
                textoFeedbackLogin.text = "Atenção: Usuário ou senha incorretos! Tente novamente.";
            }
        }
    }

    private void AbrirModalAlterarSenha()
    {
        TocarSomClique();
        if (inputNovaSenha != null) inputNovaSenha.text = "";
        if (inputConfirmaSenha != null) inputConfirmaSenha.text = "";
        if (textoFeedbackSenha != null) textoFeedbackSenha.text = "";
        if (modalAlterarSenha != null) modalAlterarSenha.SetActive(true);
    }

    private void FecharModalAlterarSenha()
    {
        TocarSomClique();
        if (modalAlterarSenha != null) modalAlterarSenha.SetActive(false);
    }

    private void OnClickSalvarNovaSenha()
    {
        string nova = inputNovaSenha != null ? inputNovaSenha.text.Trim() : "";
        string confirma = inputConfirmaSenha != null ? inputConfirmaSenha.text.Trim() : "";

        if (string.IsNullOrEmpty(nova))
        {
            if (textoFeedbackSenha != null) textoFeedbackSenha.text = "Atenção: A senha não pode ficar em branco.";
            TocarSomErro();
            return;
        }

        if (nova.Length < 3)
        {
            if (textoFeedbackSenha != null) textoFeedbackSenha.text = "Atenção: A senha deve ter ao menos 3 caracteres.";
            TocarSomErro();
            return;
        }

        if (nova != confirma)
        {
            if (textoFeedbackSenha != null) textoFeedbackSenha.text = "Atenção: A confirmação não coincide com a nova senha.";
            TocarSomErro();
            return;
        }

        PlayerPrefs.SetString(KEY_PASS, nova);
        PlayerPrefs.Save();

        TocarSomClique();
        if (textoFeedbackSenha != null)
        {
            textoFeedbackSenha.color = new Color(0.15f, 0.55f, 0.2f, 1f);
            textoFeedbackSenha.text = "Senha do professor alterada com sucesso!";
        }
        if (textoFeedbackAcoes != null)
        {
            textoFeedbackAcoes.text = "Senha de acesso atualizada!";
        }

        Invoke(nameof(FecharModalAlterarSenha), 1f);
    }

    // =========================================================
    // RESOLUÇÃO DE FASE NA GAMEPLAY
    // =========================================================
    public void AbrirResolucaoGameplay()
    {
        GameManager gm = FindFirstObjectByType<GameManager>();
        GerenciadorFases gf = FindFirstObjectByType<GerenciadorFases>();

        if (gm == null) return;

        int faseID = gm.FaseAtualID;
        int nivelID = gm.NivelAtualID;
        bool isMath = gm.JogandoMatematica;

        string nomeMateria = isMath ? "MATEMÁTICA" : "LÍNGUA PORTUGUESA";
        string nomeNivel = (nivelID == 0) ? "FÁCIL" : (nivelID == 1) ? "MÉDIO" : "DIFÍCIL";

        if (textoTituloResolucao != null)
        {
            textoTituloResolucao.text = $"<b>GABARITO: {nomeMateria} - {nomeNivel} - FASE {faseID + 1}/30</b>";
        }

        StringBuilder sb = new StringBuilder();

        if (isMath)
        {
            sb.AppendLine("<b>[PROBLEMA DE MATEMÁTICA]</b>");
            sb.AppendLine($"- <b>Enunciado:</b> <size=22><color=#007700>{gm.UltimaPerguntaMatematica}</color></size>");
            sb.AppendLine($"- <b>Resposta Correta:</b> <size=24><color=#B71C1C>{gm.UltimaRespostaMatematica}</color></size>");
            sb.AppendLine();
            sb.AppendLine("<b>[RESOLUÇÃO PASSO A PASSO]</b>");
            sb.AppendLine(gm.UltimoPassoAPassoMatematica);
        }
        else
        {
            if (nivelID == 2)
            {
                var frase = gm.FraseDificilAtual;
                sb.AppendLine("<b>[DESAFIO GRAMATICAL E ORTOGRÁFICO]</b>");
                sb.AppendLine($"- <b>Frase:</b> <size=21>\"{frase.fraseComLacuna}\"</size>");
                sb.AppendLine($"- <b>Palavra Correta:</b> <size=23><color=#007700>{frase.palavraCorreta}</color></size> (Classe: <b>{frase.classeGramatical}</b>)");
                sb.AppendLine($"- <b>Distratores:</b> {frase.opcaoErrada1}, {frase.opcaoErrada2}");
                sb.AppendLine();
                sb.AppendLine("<b>[JUSTIFICATIVA PEDAGÓGICA]</b>");
                sb.AppendLine($"A palavra correta é <color=#007700><b>{frase.palavraCorreta}</b></color>, classificada morfologicamente como <b>{frase.classeGramatical}</b> dentro do contexto semântico da oração.");
            }
            else
            {
                sb.AppendLine("<b>[DESAFIO DE ORTOGRAFIA]</b>");
                sb.AppendLine($"- <b>Palavra Completa:</b> <size=24><color=#007700>{gm.PalavraCompleta}</color></size>");
                sb.AppendLine($"- <b>Letra(s) Oculta(s):</b> <size=24><color=#B71C1C>{gm.RespostaCorreta}</color></size>");
                sb.AppendLine();
                sb.AppendLine("<b>[REGRA ORTOGRÁFICA]</b>");
                if (gm.PalavraCompleta.Contains("SS"))
                    sb.AppendLine("- Usa-se <b>SS</b> entre vogais para reproduzir o som de /s/. Nunca se inicia palavra com SS.");
                else if (gm.PalavraCompleta.Contains("CH"))
                    sb.AppendLine("- O dígrafo <b>CH</b> possui o mesmo fonema que a letra X em diversas posições.");
                else if (gm.PalavraCompleta.Contains("Ç"))
                    sb.AppendLine("- Usa-se <b>Ç</b> apenas antes de A, O, U para som de /s/. Nunca antes de E ou I.");
                else if (gm.PalavraCompleta.Contains("X"))
                    sb.AppendLine("- A letra <b>X</b> pode apresentar sons de /ch/, /s/, /z/ ou /ks/ dependendo da etimologia.");
                else if (gm.PalavraCompleta.Contains("Z"))
                    sb.AppendLine("- A letra <b>Z</b> representa o fonema /z/ intervocálico ou inicial.");
                else
                    sb.AppendLine("- Atenção ao preenchimento da lacuna fonética correspondente.");
            }
        }

        if (textoCorpoResolucao != null)
        {
            textoCorpoResolucao.text = sb.ToString();
        }

        if (modalResolucaoGameplay != null)
            modalResolucaoGameplay.SetActive(true);
    }

    public void FecharResolucaoGameplay()
    {
        TocarSomClique();
        if (modalResolucaoGameplay != null)
            modalResolucaoGameplay.SetActive(false);
    }

    private void OnClickAssistirVideoExplicativo()
    {
        TocarSomClique();
        GerenciadorVideoExplicativo gve = FindFirstObjectByType<GerenciadorVideoExplicativo>();
        if (gve != null)
        {
            gve.AbrirLinkYoutube();
        }
        else
        {
            Application.OpenURL("https://www.youtube.com");
        }
    }

    private void OnClickAutoCompletarFase()
    {
        TocarSomClique();
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.ResolverFaseProfessor();
        }
        FecharResolucaoGameplay();
    }

    // =========================================================
    // DADOS E LISTAGEM DE ALUNOS
    // =========================================================
    private void FiltrarListaAlunos(string termo)
    {
        CarregarListaAlunos(termo);
    }

    private void CarregarListaAlunos(string filtro)
    {
        if (containerListaAlunos == null) return;

        foreach (Transform child in containerListaAlunos)
        {
            Destroy(child.gameObject);
        }

        List<string> perfis = GerenciadorPerfis.ObterListaPerfis();
        if (perfis == null || perfis.Count == 0)
        {
            perfis = new List<string> { "Aluno 1" };
        }

        string perfilAtivo = GerenciadorPerfis.PerfilAtivo;
        if (string.IsNullOrEmpty(alunoSelecionado)) alunoSelecionado = perfilAtivo;

        bool encontrouSelecionado = false;

        foreach (string nome in perfis)
        {
            if (!string.IsNullOrEmpty(filtro) && !nome.ToLower().Contains(filtro.ToLower()))
                continue;

            if (nome == alunoSelecionado) encontrouSelecionado = true;

            CriarCardAlunoLista(nome, nome == alunoSelecionado, nome == perfilAtivo);
        }

        if (!encontrouSelecionado && perfis.Count > 0)
        {
            alunoSelecionado = perfis[0];
        }

        ExibirDesempenhoAluno(alunoSelecionado);
    }

    private void CriarCardAlunoLista(string nomeAluno, bool isSelecionado, bool isAtivo)
    {
        GameObject card = new GameObject($"Card_{nomeAluno}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        card.transform.SetParent(containerListaAlunos, false);

        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(380f, 68f);

        Image img = card.GetComponent<Image>();
        if (isSelecionado && spriteCardAtivo != null)
        {
            img.sprite = spriteCardAtivo;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else if (spriteCardFundo != null)
        {
            img.sprite = spriteCardFundo;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = isSelecionado ? new Color(0.85f, 0.95f, 0.85f, 1f) : new Color(0.96f, 0.94f, 0.90f, 1f);
        }

        Button btn = card.GetComponent<Button>();
        string capturado = nomeAluno;
        btn.onClick.AddListener(() =>
        {
            TocarSomClique();
            alunoSelecionado = capturado;
            CarregarListaAlunos(inputPesquisaAlunos != null ? inputPesquisaAlunos.text : "");
        });

        // Ícone Avatar
        GameObject goIcon = new GameObject("Avatar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goIcon.transform.SetParent(card.transform, false);
        RectTransform rtI = goIcon.GetComponent<RectTransform>();
        rtI.anchorMin = new Vector2(0f, 0.5f);
        rtI.anchorMax = new Vector2(0f, 0.5f);
        rtI.pivot = new Vector2(0f, 0.5f);
        rtI.anchoredPosition = new Vector2(12f, 0f);
        rtI.sizeDelta = new Vector2(46f, 46f);
        Image imgI = goIcon.GetComponent<Image>();
        if (spriteAvatarAluno != null) imgI.sprite = spriteAvatarAluno;
        else imgI.color = new Color(0.2f, 0.45f, 0.8f, 1f);

        // Nome e Total de Fases
        int totalFases = GerenciadorPerfis.ObterTotalFasesConcluidas(nomeAluno);
        string badgeAtivo = isAtivo ? " <color=#1B5E20>[Em jogo]</color>" : "";

        GameObject goTxt = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxt.transform.SetParent(card.transform, false);
        RectTransform rtT = goTxt.GetComponent<RectTransform>();
        rtT.anchorMin = Vector2.zero;
        rtT.anchorMax = Vector2.one;
        rtT.offsetMin = new Vector2(68f, 6f);
        rtT.offsetMax = new Vector2(-12f, -6f);

        TextMeshProUGUI tmp = goTxt.GetComponent<TextMeshProUGUI>();
        tmp.font = fontProf ?? fontJogo;
        tmp.text = $"<b>{nomeAluno}</b>{badgeAtivo}\n<size=15><color=#5D4037>{totalFases}/180 fases concluídas</color></size>";
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = new Color(0.2f, 0.12f, 0.05f, 1f);
    }

    private void ExibirDesempenhoAluno(string nomeAluno)
    {
        if (string.IsNullOrEmpty(nomeAluno)) return;

        string pfx = $"P_{GerenciadorPerfis.SanitizarNome(nomeAluno)}_";

        int pf = ContarFases(pfx, "Port", 0);
        int pm = ContarFases(pfx, "Port", 1);
        int pd = ContarFases(pfx, "Port", 2);

        int mf = ContarFases(pfx, "Math", 0);
        int mm = ContarFases(pfx, "Math", 1);
        int md = ContarFases(pfx, "Math", 2);

        int totalConcluidas = pf + pm + pd + mf + mm + md;
        float percentual = (totalConcluidas / 180f) * 100f;

        if (textoHeaderAluno != null)
        {
            textoHeaderAluno.text = $"<b>Aluno: {nomeAluno}</b>";
        }

        if (textoProgressoGeral != null)
        {
            textoProgressoGeral.text = $"<b>{totalConcluidas} / 180 Fases Concluídas ({percentual:F1}%)</b>";
        }

        if (imgBarraProgressoFill != null)
        {
            imgBarraProgressoFill.fillAmount = Mathf.Clamp01(totalConcluidas / 180f);
        }

        // Estatísticas de Troféus e Vidas
        int ouro = 0, prata = 0, bronze = 0;
        ContarRank(PlayerPrefs.GetInt($"{pfx}TrofeuRank_Port_0", 0), ref ouro, ref prata, ref bronze);
        ContarRank(PlayerPrefs.GetInt($"{pfx}TrofeuRank_Port_1", 0), ref ouro, ref prata, ref bronze);
        ContarRank(PlayerPrefs.GetInt($"{pfx}TrofeuRank_Port_2", 0), ref ouro, ref prata, ref bronze);
        ContarRank(PlayerPrefs.GetInt($"{pfx}TrofeuRank_Math_0", 0), ref ouro, ref prata, ref bronze);
        ContarRank(PlayerPrefs.GetInt($"{pfx}TrofeuRank_Math_1", 0), ref ouro, ref prata, ref bronze);
        ContarRank(PlayerPrefs.GetInt($"{pfx}TrofeuRank_Math_2", 0), ref ouro, ref prata, ref bronze);

        if (textoTrofeusResumo != null)
        {
            textoTrofeusResumo.text = $"<color=#FF8F00>Ouro: {ouro}</color> | <color=#546E7A>Prata: {prata}</color> | <color=#6D4C41>Bronze: {bronze}</color>";
        }

        int vidasPort = ContarVidas(pfx, "Port", 0) + ContarVidas(pfx, "Port", 1) + ContarVidas(pfx, "Port", 2);
        int vidasMath = ContarVidas(pfx, "Math", 0) + ContarVidas(pfx, "Math", 1) + ContarVidas(pfx, "Math", 2);
        int totalVidas = vidasPort + vidasMath;

        if (textoVidasResumo != null)
        {
            textoVidasResumo.text = $"<b>{totalVidas} / 360 Corações</b>";
        }

        if (textoAproveitamentoResumo != null)
        {
            string nivelAproveitamento = (percentual >= 80f) ? "Avançado (Excelente)" : (percentual >= 40f) ? "Intermediário (Bom)" : "Em Desenvolvimento";
            textoAproveitamentoResumo.text = $"<b>{nivelAproveitamento}</b>";
        }

        // Tabela Português
        if (textoTabelaPort != null)
        {
            StringBuilder sbp = new StringBuilder();
            sbp.AppendLine("<b>LÍNGUA PORTUGUESA (ORTOGRAFIA & GRAMÁTICA)</b>");
            sbp.AppendLine($"- <b>Fácil:</b> {pf}/30 fases (Dígrafos SS, CH, X, Z, Ç, S/C)");
            sbp.AppendLine($"- <b>Médio:</b> {pm}/30 fases (Vocabulário contextualizado)");
            sbp.AppendLine($"- <b>Difícil:</b> {pd}/30 fases (Morfologia e Classes Gramaticais)");
            textoTabelaPort.text = sbp.ToString();
        }

        // Tabela Matemática
        if (textoTabelaMath != null)
        {
            StringBuilder sbm = new StringBuilder();
            sbm.AppendLine("<b>MATEMÁTICA (CÁLCULO & RESOLUÇÃO DE PROBLEMAS)</b>");
            sbm.AppendLine($"- <b>Fácil:</b> {mf}/30 fases (Adição e Subtração Básica)");
            sbm.AppendLine($"- <b>Médio:</b> {mm}/30 fases (Multiplicação e Divisão)");
            sbm.AppendLine($"- <b>Difícil:</b> {md}/30 fases (Expressões e Equações)");
            textoTabelaMath.text = sbm.ToString();
        }

        // Parecer Pedagógico
        if (textoDiagnosticoPedagogico != null)
        {
            textoDiagnosticoPedagogico.text = GerarDiagnosticoInteligente(nomeAluno, totalConcluidas, pf, pm, pd, mf, mm, md, ouro, totalVidas);
        }

        if (textoFeedbackAcoes != null) textoFeedbackAcoes.text = "";
    }

    private string GerarDiagnosticoInteligente(string aluno, int total, int pf, int pm, int pd, int mf, int mm, int md, int trofeusOuro, int vidas)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>[PARECER PEDAGÓGICO INDIVIDUAL]</b>");

        if (total == 0)
        {
            sb.AppendLine($"O aluno <b>{aluno}</b> ainda não concluiu nenhuma fase no jogo.");
            sb.AppendLine("- <i>Recomendação:</i> Iniciar pelas fases fáceis de Português e Matemática para ambientação com a interface e fonemas básicos.");
            return sb.ToString();
        }

        float taxaVidas = total > 0 ? (vidas / (float)(total * 2)) * 100f : 0f;

        if (total >= 90)
        {
            sb.AppendLine($"O estudante <b>{aluno}</b> demonstra excelente constância de estudo com <b>{total} fases</b> concluídas e precisão de <b>{taxaVidas:F1}%</b>.");
        }
        else
        {
            sb.AppendLine($"O estudante <b>{aluno}</b> está avançando no plano pedagógico com <b>{total} fases</b> concluídas e precisão de <b>{taxaVidas:F1}%</b>.");
        }

        // Análise de Português
        if (pd >= 15)
            sb.AppendLine("- <b>Português:</b> Demonstra maturidade na identificação morfossintática (substantivos, adjetivos e verbos) e boa discriminação de homófonos.");
        else if (pm >= 15)
            sb.AppendLine("- <b>Português:</b> Bom domínio das regras de dígrafos (SS/CH) e sons da letra X. Recomenda-se avançar para o nível Difícil de gramática.");
        else
            sb.AppendLine("- <b>Português:</b> Consolidando percepção fonética em dígrafos e sibilantes (SS, Ç, S). Manter atividades lúdicas de leitura.");

        // Análise de Matemática
        if (md >= 15)
            sb.AppendLine("- <b>Matemática:</b> Raciocínio lógico aguçado na resolução de equações algébricas e expressões prioritárias.");
        else if (mm >= 15)
            sb.AppendLine("- <b>Matemática:</b> Boa compreensão das quatro operações fundamentais. Estimular o cálculo mental de múltiplos.");
        else
            sb.AppendLine("- <b>Matemática:</b> Fixando tabuada e operações elementares de adição e subtração.");

        return sb.ToString();
    }

    private void OnClickCopiarRelatorio()
    {
        if (string.IsNullOrEmpty(alunoSelecionado)) return;

        string pfx = $"P_{GerenciadorPerfis.SanitizarNome(alunoSelecionado)}_";
        int pf = ContarFases(pfx, "Port", 0);
        int pm = ContarFases(pfx, "Port", 1);
        int pd = ContarFases(pfx, "Port", 2);
        int mf = ContarFases(pfx, "Math", 0);
        int mm = ContarFases(pfx, "Math", 1);
        int md = ContarFases(pfx, "Math", 2);
        int total = pf + pm + pd + mf + mm + md;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("===================================================================");
        sb.AppendLine("RELATÓRIO DE DESEMPENHO PEDAGÓGICO - JOGO EDUCATIVO");
        sb.AppendLine($"Aluno: {alunoSelecionado}");
        sb.AppendLine($"Data de Emissão: {DateTime.Now:dd/MM/yyyy HH:mm}");
        sb.AppendLine($"Total de Fases Concluídas: {total} de 180 ({(total / 180f) * 100f:F1}%)");
        sb.AppendLine("-------------------------------------------------------------------");
        sb.AppendLine("PORTUGUÊS:");
        sb.AppendLine($"  - Nível Fácil: {pf}/30 fases concluídas");
        sb.AppendLine($"  - Nível Médio: {pm}/30 fases concluídas");
        sb.AppendLine($"  - Nível Difícil: {pd}/30 fases concluídas");
        sb.AppendLine("MATEMÁTICA:");
        sb.AppendLine($"  - Nível Fácil: {mf}/30 fases concluídas");
        sb.AppendLine($"  - Nível Médio: {mm}/30 fases concluídas");
        sb.AppendLine($"  - Nível Difícil: {md}/30 fases concluídas");
        sb.AppendLine("-------------------------------------------------------------------");
        sb.AppendLine("PARECER PEDAGÓGICO:");
        sb.AppendLine(GerarDiagnosticoInteligente(alunoSelecionado, total, pf, pm, pd, mf, mm, md, 0, 0).Replace("<b>", "").Replace("</b>", "").Replace("•", "-"));
        sb.AppendLine("===================================================================");

        GUIUtility.systemCopyBuffer = sb.ToString();

        if (textoFeedbackAcoes != null)
        {
            textoFeedbackAcoes.text = "Relatório copiado para a área de transferência com sucesso!";
        }
        TocarSomClique();
    }

    private void OnClickResetarAluno()
    {
        if (string.IsNullOrEmpty(alunoSelecionado)) return;

        TocarSomClique();

        if (alunoEmConfirmacaoReset == alunoSelecionado)
        {
            alunoEmConfirmacaoReset = "";
            ResetarProgressoAluno(alunoSelecionado);
            if (textoBtnReset != null) textoBtnReset.text = "RESETAR PROGRESSO";
            if (textoFeedbackAcoes != null) textoFeedbackAcoes.text = $"Atenção: O progresso de {alunoSelecionado} foi reiniciado com sucesso!";
            ExibirDesempenhoAluno(alunoSelecionado);
            CarregarListaAlunos(inputPesquisaAlunos != null ? inputPesquisaAlunos.text : "");
        }
        else
        {
            alunoEmConfirmacaoReset = alunoSelecionado;
            tempoConfirmacaoReset = 4f;
            if (textoBtnReset != null) textoBtnReset.text = "CONFIRMA RESET?";
        }
    }

    private void ResetarProgressoAluno(string nome)
    {
        string pfx = $"P_{GerenciadorPerfis.SanitizarNome(nome)}_";
        string[] mats = new string[] { "Port", "Math" };
        for (int m = 0; m < mats.Length; m++)
        {
            for (int niv = 0; niv < 3; niv++)
            {
                PlayerPrefs.DeleteKey($"{pfx}TrofeuRank_{mats[m]}_{niv}");
                for (int f = 0; f < 30; f++)
                {
                    PlayerPrefs.DeleteKey($"{pfx}FaseCompleta_{mats[m]}_{niv}_{f}");
                    PlayerPrefs.DeleteKey($"{pfx}VidasRestantes_{mats[m]}_{niv}_{f}");
                }
            }
        }
        PlayerPrefs.DeleteKey($"{pfx}Desbloqueio_Medio_Port");
        PlayerPrefs.DeleteKey($"{pfx}Desbloqueio_Dificil_Port");
        PlayerPrefs.DeleteKey($"{pfx}Desbloqueio_Medio_Math");
        PlayerPrefs.DeleteKey($"{pfx}Desbloqueio_Dificil_Math");
        PlayerPrefs.Save();

        Action action = FindFirstObjectByType<Action>();
        if (action != null) action.VerificarDesbloqueios();
    }

    // =========================================================
    // MÉTODOS AUXILIARES DE CONSTRUÇÃO DE UI
    // =========================================================
    private GameObject CriarCampoInput(Transform pai, string nome, Vector2 pos, Vector2 tam, string placeholder, bool isPassword, out TMP_InputField campo)
    {
        GameObject goInput = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        goInput.transform.SetParent(pai, false);
        RectTransform rt = goInput.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;

        Image img = goInput.GetComponent<Image>();
        img.color = new Color(0.95f, 0.92f, 0.84f, 1f);
        if (spriteBarraPesquisa != null)
        {
            img.sprite = spriteBarraPesquisa;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }

        campo = goInput.GetComponent<TMP_InputField>();

        GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(goInput.transform, false);
        RectTransform rtArea = textArea.GetComponent<RectTransform>();
        rtArea.anchorMin = Vector2.zero;
        rtArea.anchorMax = Vector2.one;
        rtArea.offsetMin = new Vector2(16f, 4f);
        rtArea.offsetMax = new Vector2(-16f, -4f);

        GameObject goPh = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goPh.transform.SetParent(textArea.transform, false);
        RectTransform rtPh = goPh.GetComponent<RectTransform>();
        rtPh.anchorMin = Vector2.zero;
        rtPh.anchorMax = Vector2.one;
        rtPh.offsetMin = Vector2.zero;
        rtPh.offsetMax = Vector2.zero;
        TextMeshProUGUI tmpPh = goPh.GetComponent<TextMeshProUGUI>();
        tmpPh.font = fontProf ?? fontJogo;
        tmpPh.text = placeholder;
        tmpPh.fontSize = 19;
        tmpPh.fontStyle = FontStyles.Italic;
        tmpPh.alignment = TextAlignmentOptions.MidlineLeft;
        tmpPh.color = new Color(0.55f, 0.42f, 0.32f, 0.7f);

        GameObject goTxt = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxt.transform.SetParent(textArea.transform, false);
        RectTransform rtT = goTxt.GetComponent<RectTransform>();
        rtT.anchorMin = Vector2.zero;
        rtT.anchorMax = Vector2.one;
        rtT.offsetMin = Vector2.zero;
        rtT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmpT = goTxt.GetComponent<TextMeshProUGUI>();
        tmpT.font = fontProf ?? fontJogo;
        tmpT.fontSize = 20;
        tmpT.alignment = TextAlignmentOptions.MidlineLeft;
        tmpT.color = new Color(0.18f, 0.11f, 0.04f, 1f);

        campo.textViewport = rtArea;
        campo.textComponent = tmpT;
        campo.placeholder = tmpPh;
        if (isPassword)
        {
            campo.contentType = TMP_InputField.ContentType.Password;
            campo.inputType = TMP_InputField.InputType.Password;
        }

        return goInput;
    }

    private GameObject CriarBotaoAcao(Transform pai, string nome, Vector2 pos, Vector2 tam, string texto, Sprite spr, Color corFallback, UnityEngine.Events.UnityAction callback, Vector2? anchor = null)
    {
        GameObject goBtn = new GameObject(nome, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        goBtn.transform.SetParent(pai, false);
        RectTransform rt = goBtn.GetComponent<RectTransform>();
        Vector2 anc = anchor ?? new Vector2(0.5f, 1f);
        rt.anchorMin = anc;
        rt.anchorMax = anc;
        rt.pivot = anc;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;

        Image img = goBtn.GetComponent<Image>();
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

        Button btn = goBtn.GetComponent<Button>();
        btn.onClick.AddListener(callback);

        GameObject goTxt = new GameObject("TextoBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTxt.transform.SetParent(goBtn.transform, false);
        RectTransform rtT = goTxt.GetComponent<RectTransform>();
        rtT.anchorMin = Vector2.zero;
        rtT.anchorMax = Vector2.one;
        rtT.offsetMin = Vector2.zero;
        rtT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = goTxt.GetComponent<TextMeshProUGUI>();
        tmp.font = fontProf ?? fontJogo;
        tmp.text = $"<b>{texto}</b>";
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.outlineColor = new Color32(20, 10, 5, 220);
        tmp.outlineWidth = 0.16f;

        return goBtn;
    }

    private void CriarBotaoFechar(Transform pai, UnityEngine.Events.UnityAction callback, Vector2 pos)
    {
        GameObject goFechar = new GameObject("BtnFechar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        goFechar.transform.SetParent(pai, false);
        RectTransform rt = goFechar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(50f, 50f);

        Image img = goFechar.GetComponent<Image>();
        if (spriteXFechar != null)
        {
            img.sprite = spriteXFechar;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(0.85f, 0.2f, 0.2f, 1f);
            GameObject goTxtX = new GameObject("TxtX", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            goTxtX.transform.SetParent(goFechar.transform, false);
            RectTransform rtTxtX = goTxtX.GetComponent<RectTransform>();
            rtTxtX.anchorMin = Vector2.zero;
            rtTxtX.anchorMax = Vector2.one;
            rtTxtX.offsetMin = Vector2.zero;
            rtTxtX.offsetMax = Vector2.zero;
            TextMeshProUGUI tmpX = goTxtX.GetComponent<TextMeshProUGUI>();
            tmpX.font = fontProf ?? fontJogo;
            tmpX.text = "<b>X</b>";
            tmpX.fontSize = 28f;
            tmpX.alignment = TextAlignmentOptions.Center;
            tmpX.color = Color.white;
        }

        Button btn = goFechar.GetComponent<Button>();
        btn.onClick.AddListener(callback);
    }

    private int ContarFases(string pfx, string materia, int nivel)
    {
        int count = 0;
        for (int i = 0; i < 30; i++)
        {
            if (PlayerPrefs.GetInt($"{pfx}FaseCompleta_{materia}_{nivel}_{i}", 0) == 1)
                count++;
        }
        return count;
    }

    private int ContarVidas(string pfx, string materia, int nivel)
    {
        int total = 0;
        for (int i = 0; i < 30; i++)
        {
            total += PlayerPrefs.GetInt($"{pfx}VidasRestantes_{materia}_{nivel}_{i}", 0);
        }
        return total;
    }

    private void ContarRank(int rank, ref int ouro, ref int prata, ref int bronze)
    {
        if (rank == 3) ouro++;
        else if (rank == 2) prata++;
        else if (rank == 1) bronze++;
    }

    private void TocarSomClique()
    {
        Action act = FindFirstObjectByType<Action>();
        if (act != null) act.TocarSomClique();
    }

    private void TocarSomErro()
    {
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null && gm.sfxSource != null && gm.somErro != null)
        {
            gm.sfxSource.PlayOneShot(gm.somErro);
        }
    }
}
