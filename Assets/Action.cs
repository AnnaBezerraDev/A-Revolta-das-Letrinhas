using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using DG.Tweening;
using TMPro;

public class Action : MonoBehaviour
{
    [Header("Botão de Mute (Lobby)")]
    public Button soundButton;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;
    private bool isSoundOn = true;

    [Header("Controle de Som Geral")]
    public AudioSource sfxSource;
    public AudioClip somClique;
    public AudioClip somCaixa;

    [Header("Configurações de Áudio (Sliders)")]
    public AudioSource musicSource;
    public AudioClip musicaLobby;
    public AudioClip musicaCreditos;
    public AudioClip musicaPersonagens;

    public Slider sliderMusica;
    public Slider sliderSfx;

    [Header("Referências de Navegação")]
    public CanvasGroup groupLobby;
    public CanvasGroup groupMenu;
    public CanvasGroup groupDificuldadePortugues;
    public CanvasGroup groupDificuldadeMatematica;
    [HideInInspector] public CanvasGroup telaDificuldadeAtual;
    public CanvasGroup groupConfig;

    [Header("Telas Extras (Créditos e Troféus)")]
    public CanvasGroup groupCreditos;
    public CanvasGroup groupTrofeus;
    public TrofeusManager trofeusManager;

    [Header("Icones Personalizados")]
    public Sprite spriteCasinha;
    public Sprite spriteEngrenagem;
    public Sprite spriteCadeado;

    private CanvasGroup telaOrigemConfig;

    [Header("Telas de Seleção de Fases (Português)")]
    public CanvasGroup groupFasesFacil;
    public CanvasGroup groupFasesMedio;
    public CanvasGroup groupFasesDificil;

    [Header("Telas de Seleção de Fases (Matemática)")]
    public CanvasGroup groupFasesMathFacil;
    public CanvasGroup groupFasesMathMedio;
    public CanvasGroup groupFasesMathDificil;

    [Header("Paineis do Jogo (GamePlay Português)")]
    public CanvasGroup groupJogoFacil;
    public CanvasGroup groupJogoMedio;
    public CanvasGroup groupJogoDificil;

    [Header("Paineis do Jogo (GamePlay Matemática)")]
    public CanvasGroup groupJogoMathFacil;
    public CanvasGroup groupJogoMathMedio;
    public CanvasGroup groupJogoMathDificil;

    [Header("Novas Telas: Fim de Partida")]
    public CanvasGroup groupFaseConcluida;
    public CanvasGroup groupProxFaseOuro;
    public CanvasGroup groupProxFasePrata;
    public CanvasGroup groupProxFaseBronze;
    public CanvasGroup groupGameOver;

    [Header("Botões de Dificuldade (Português)")]
    public Button botaoMedio;
    public Button botaoDificil;

    [Header("Botões de Dificuldade (Matemática)")]
    public Button botaoMedioMatematica;
    public Button botaoDificilMatematica;

    [Header("Tutorial")]
    public TutorialManager tutorialManager;

    [Header("Tela de Final de Jogo & Vídeo")]
    public CanvasGroup groupFinalJogo;
    public VideoPlayer videoPlayerFinal;
    [Tooltip("Quantidade de fases por nível (padrão: 30)")]
    public int totalFasesPorNivel = 30;

    void OnEnable()
    {
        GerenciadorPerfis.OnPerfilAlterado -= VerificarDesbloqueios;
        GerenciadorPerfis.OnPerfilAlterado += VerificarDesbloqueios;
    }

    void OnDisable()
    {
        GerenciadorPerfis.OnPerfilAlterado -= VerificarDesbloqueios;
    }

    void Awake()
    {
        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
        Screen.fullScreen = true;
        CriarTelaDeCarregamentoInicial();
    }

    public static void AjustarProporcaoTelas()
    {
        // Configuração mantida estática na cena para estabilidade visual perfeita
    }


    void Start()

    {
        GerenciadorPerfis.Inicializar();
        if (FindFirstObjectByType<GerenciadorPerfis>() == null)
        {
            GameObject gpGO = new GameObject("GerenciadorPerfis", typeof(GerenciadorPerfis));
            DontDestroyOnLoad(gpGO);
        }
        if (FindFirstObjectByType<GerenciadorVideoExplicativo>() == null)
        {
            GameObject gvGO = new GameObject("GerenciadorVideoExplicativo", typeof(GerenciadorVideoExplicativo));
            DontDestroyOnLoad(gvGO);
        }
        if (FindFirstObjectByType<AreaDoProfessor>() == null)
        {
            GameObject apGO = new GameObject("AreaDoProfessor", typeof(AreaDoProfessor));
            DontDestroyOnLoad(apGO);
        }
        EsconderGrupo(groupMenu);
        EsconderGrupo(groupDificuldadePortugues);
        EsconderGrupo(groupDificuldadeMatematica);
        EsconderGrupo(groupConfig);

        EsconderGrupo(groupFasesFacil);
        EsconderGrupo(groupFasesMedio);
        EsconderGrupo(groupFasesDificil);

        EsconderGrupo(groupFasesMathFacil);
        EsconderGrupo(groupFasesMathMedio);
        EsconderGrupo(groupFasesMathDificil);

        EsconderGrupo(groupJogoFacil);
        EsconderGrupo(groupJogoMedio);
        EsconderGrupo(groupJogoDificil);

        EsconderGrupo(groupJogoMathFacil);
        EsconderGrupo(groupJogoMathMedio);
        EsconderGrupo(groupJogoMathDificil);

        EsconderGrupo(groupFaseConcluida);
        EsconderGrupo(groupProxFaseOuro);
        EsconderGrupo(groupProxFasePrata);
        EsconderGrupo(groupProxFaseBronze);
        EsconderGrupo(groupGameOver);

        EsconderGrupo(groupCreditos);
        EsconderGrupo(groupTrofeus);
        EsconderGrupo(groupFinalJogo);

        MostrarGrupo(groupLobby);
        VerificarDesbloqueios();
        ConfigurarBotoesTelaPersonagens();
        ConfigurarBotoesGameplayCanvases();
        ConfigurarBotoesPainelFaseConcluida();

        isSoundOn = PlayerPrefs.GetInt("SomMutado", 1) == 1;
        AtualizarIconeMute();

        if (sliderMusica != null)
        {
            sliderMusica.value = PlayerPrefs.GetFloat("VolumeMusica", 0.5f);
            sliderMusica.onValueChanged.AddListener(AlterarVolumeMusica);
            AlterarVolumeMusica(sliderMusica.value);
        }

        if (sliderSfx != null)
        {
            sliderSfx.value = PlayerPrefs.GetFloat("VolumeSFX", 0.5f);
            sliderSfx.onValueChanged.AddListener(AlterarVolumeSFX);
            AlterarVolumeSFX(sliderSfx.value);
        }

        if (videoPlayerFinal != null)
        {
            videoPlayerFinal.loopPointReached += AoTerminarVideoFinal;
        }
    }

    public void Som()
    {
        TocarSomClique();
        isSoundOn = !isSoundOn;
        PlayerPrefs.SetInt("SomMutado", isSoundOn ? 1 : 0);
        AtualizarIconeMute();
    }

    private void AtualizarIconeMute()
    {
        AudioListener.volume = isSoundOn ? 1f : 0f;
        if (soundButton != null) soundButton.image.sprite = isSoundOn ? soundOnSprite : soundOffSprite;
    }

    public void TocarSomClique()
    {
        if (sfxSource != null && somClique != null) sfxSource.PlayOneShot(somClique);
    }

    public void TocarSomCaixa()
    {
        if (sfxSource != null && somCaixa != null) sfxSource.PlayOneShot(somCaixa);
    }

    public void AlterarVolumeMusica(float volume)
    {
        if (musicSource != null) musicSource.volume = volume;
        PlayerPrefs.SetFloat("VolumeMusica", volume);
    }

    public void AlterarVolumeSFX(float volume)
    {
        if (sfxSource != null) sfxSource.volume = volume;
        PlayerPrefs.SetFloat("VolumeSFX", volume);

        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null && gm.sfxSource != null)
        {
            gm.sfxSource.volume = volume;
        }
    }

    public void Portugues()
    {
        TocarSomCaixa();
        TocarMusicaPersonagens();
        telaDificuldadeAtual = groupDificuldadePortugues;
        VerificarDesbloqueios();
        ConfigurarBotoesTelaPersonagens();
        if (tutorialManager != null) tutorialManager.IniciarTutorialPortugues();
        else TrocarTela(groupMenu, telaDificuldadeAtual);
    }

    public void Matematica()
    {
        TocarSomCaixa();
        TocarMusicaPersonagens();
        telaDificuldadeAtual = groupDificuldadeMatematica;
        VerificarDesbloqueios();
        ConfigurarBotoesTelaPersonagens();
        if (tutorialManager != null) tutorialManager.IniciarTutorialMatematica();
        else TrocarTela(groupMenu, telaDificuldadeAtual);
    }

    public void VerificarDesbloqueios()
    {
        bool isProfessor = AreaDoProfessor.SessaoProfessorAtiva;

        bool medioLiberadoPort = isProfessor || GerenciadorPerfis.GetInt("Desbloqueio_Medio_Port", 0) == 1 || GerenciadorPerfis.GetInt("Desbloqueio_Medio", 0) == 1;
        bool dificilLiberadoPort = isProfessor || GerenciadorPerfis.GetInt("Desbloqueio_Dificil_Port", 0) == 1 || GerenciadorPerfis.GetInt("Desbloqueio_Dificil", 0) == 1;

        bool medioLiberadoMath = isProfessor || GerenciadorPerfis.GetInt("Desbloqueio_Medio_Math", 0) == 1;
        bool dificilLiberadoMath = isProfessor || GerenciadorPerfis.GetInt("Desbloqueio_Dificil_Math", 0) == 1;

        AtualizarEstadoBotaoDificuldade(botaoMedio, medioLiberadoPort);
        AtualizarEstadoBotaoDificuldade(botaoDificil, dificilLiberadoPort);

        AtualizarEstadoBotaoDificuldade(botaoMedioMatematica, medioLiberadoMath);
        AtualizarEstadoBotaoDificuldade(botaoDificilMatematica, dificilLiberadoMath);
    }

    private void AtualizarEstadoBotaoDificuldade(Button btn, bool liberado)
    {
        if (btn == null) return;
        btn.interactable = liberado;
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.color = liberado ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.75f);
        }

        Transform tLock = btn.transform.Find("BadgeCadeado");
        if (!liberado)
        {
            if (tLock == null)
            {
                if (spriteCadeado == null) spriteCadeado = Resources.Load<Sprite>("cadeado");

                GameObject gLock = new GameObject("BadgeCadeado");
                gLock.transform.SetParent(btn.transform, false);
                RectTransform rt = gLock.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(90f, 90f);

                Image imgLock = gLock.AddComponent<Image>();
                imgLock.sprite = spriteCadeado;
                imgLock.raycastTarget = false;
            }
            else
            {
                tLock.gameObject.SetActive(true);
            }
        }
        else
        {
            if (tLock != null) tLock.gameObject.SetActive(false);
        }
    }

    public void ConfigurarBotoesTelaPersonagens()
    {
        if (spriteCasinha == null) spriteCasinha = Resources.Load<Sprite>("btnCasinha");
        if (spriteEngrenagem == null) spriteEngrenagem = Resources.Load<Sprite>("btnEngrenagem");
        if (spriteCadeado == null) spriteCadeado = Resources.Load<Sprite>("cadeado");

        ConfigurarIconesEmCanvasDificuldade(groupDificuldadePortugues);
        ConfigurarIconesEmCanvasDificuldade(groupDificuldadeMatematica);
    }

    private void ConfigurarIconesEmCanvasDificuldade(CanvasGroup cg)
    {
        if (cg == null) return;

        Transform tSair = cg.transform.Find("btnSair");
        if (tSair == null)
        {
            Button[] todos = cg.GetComponentsInChildren<Button>(true);
            foreach (var b in todos)
            {
                if (b.name.ToLower().Contains("sair") || b.name.ToLower().Contains("voltar"))
                {
                    tSair = b.transform;
                    break;
                }
            }
        }

        if (tSair != null)
        {
            tSair.name = "btnCasinha";
            Button btn = tSair.GetComponent<Button>();
            Image img = tSair.GetComponent<Image>();
            if (img != null && spriteCasinha != null)
            {
                img.sprite = spriteCasinha;
                img.color = Color.white;
            }

            RectTransform rt = tSair.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(-860f, 460f);
                rt.sizeDelta = new Vector2(100f, 100f);
            }

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(VoltarParaLobby);
            }
        }

       
        Transform tEng = cg.transform.Find("btnEngrenagem");
        if (tEng == null)
        {
            Transform container = (tSair != null && tSair.parent != null) ? tSair.parent : cg.transform;
            GameObject gEng = new GameObject("btnEngrenagem");
            gEng.transform.SetParent(container, false);

            RectTransform rtEng = gEng.AddComponent<RectTransform>();
            rtEng.anchorMin = new Vector2(0.5f, 0.5f);
            rtEng.anchorMax = new Vector2(0.5f, 0.5f);
            rtEng.pivot = new Vector2(0.5f, 0.5f);
            rtEng.anchoredPosition = new Vector2(-740f, 460f);
            rtEng.sizeDelta = new Vector2(100f, 100f);

            Image imgEng = gEng.AddComponent<Image>();
            imgEng.sprite = spriteEngrenagem;
            imgEng.color = Color.white;

            Button btnEng = gEng.AddComponent<Button>();
            btnEng.onClick.AddListener(Config);
        }
        else
        {
            Image imgEng = tEng.GetComponent<Image>();
            if (imgEng != null && spriteEngrenagem != null)
            {
                imgEng.sprite = spriteEngrenagem;
                imgEng.color = Color.white;
            }
            Button btnEng = tEng.GetComponent<Button>();
            if (btnEng != null)
            {
                btnEng.onClick.RemoveAllListeners();
                btnEng.onClick.AddListener(Config);
            }
        }
    }

    public void ConfigurarBotoesPainelFaseConcluida()
    {
        CanvasGroup[] paineis = { groupFaseConcluida, groupProxFaseOuro, groupProxFasePrata, groupProxFaseBronze };
        foreach (var p in paineis)
        {
            if (p == null) continue;
            try
            {
                Button[] botoes = p.GetComponentsInChildren<Button>(true);
                foreach (var b in botoes)
                {
                    string n = b.name.ToLower();
                    string txt = "";
                    var tmp = b.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
                    if (tmp != null) txt = tmp.text.ToLower();
                    var legacyTxt = b.GetComponentInChildren<Text>(true);
                    if (legacyTxt != null) txt += " " + legacyTxt.text.ToLower();

                    if (n.Contains("continuar") || n.Contains("proxim") || n.Contains("continue") || n.Contains("next") || txt.Contains("continuar") || txt.Contains("próxim") || txt.Contains("proxim"))
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(() =>
                        {
                            GerenciadorFases gf = FindFirstObjectByType<GerenciadorFases>();
                            if (gf != null) gf.BotaoProximaFase();
                        });
                    }
                    else if (n.Contains("menu") || n.Contains("voltar") || n.Contains("sair") || txt.Contains("menu") || txt.Contains("voltar"))
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(() =>
                        {
                            GerenciadorFases gf = FindFirstObjectByType<GerenciadorFases>();
                            if (gf != null) gf.BotaoSairParaFases();
                        });
                    }
                    else if (n.Contains("trofeu") || n.Contains("trofeus") || txt.Contains("troféu") || txt.Contains("trofeu"))
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(() =>
                        {
                            AbrirTrofeus();
                        });
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Action] Aviso ao configurar botões de {p.name}: {ex.Message}");
            }
        }
    }

    public void ConfigurarBotoesGameplayCanvases()
    {
        if (spriteCasinha == null) spriteCasinha = Resources.Load<Sprite>("btnCasinha");
        if (spriteEngrenagem == null) spriteEngrenagem = Resources.Load<Sprite>("btnEngrenagem");
        if (spriteCadeado == null) spriteCadeado = Resources.Load<Sprite>("cadeado");

        CanvasGroup[] telasJogo = {
            groupJogoFacil,
            groupJogoMedio,
            groupJogoDificil,
            groupJogoMathFacil,
            groupJogoMathMedio,
            groupJogoMathDificil
        };

        string[] nomesObjetos = {
            "Ze",
            "Fernanda",
            "Patty",
            "Canvas_Arthur",
            "Canvas_Gabriela",
            "Canvas_Mae"
        };

        for (int i = 0; i < telasJogo.Length; i++)
        {
            try
            {
                CanvasGroup cg = telasJogo[i];
                GameObject go = (cg != null) ? cg.gameObject : GameObject.Find(nomesObjetos[i]);
                if (go == null) continue;

                // Correção defensiva para a tela da Patty (Português Difícil):
                // Patty usa elementos 2D em World Space e Canvases filhos (Canvas_Texto e Canvas_btn) em ScreenSpaceCamera.
                // Não deve possuir Canvas/CanvasScaler em seu próprio GameObject raiz, pois isso corrompe a hierarquia.
                if (nomesObjetos[i] == "Patty" || cg == groupJogoDificil)
                {
                    Canvas cPatty = go.GetComponent<Canvas>();
                    if (cPatty != null) Destroy(cPatty);
                    CanvasScaler csPatty = go.GetComponent<CanvasScaler>();
                    if (csPatty != null) Destroy(csPatty);

                    Camera camMain = Camera.main;
                    Canvas[] childCanvases = go.GetComponentsInChildren<Canvas>(true);
                    foreach (var c in childCanvases)
                    {
                        if (c.gameObject.name == "TopBar_BotoesCanvas") continue;
                        if (c.renderMode == RenderMode.ScreenSpaceCamera)
                        {
                            if (c.worldCamera == null && camMain != null) c.worldCamera = camMain;
                            RectTransform rt = c.GetComponent<RectTransform>();
                            if (rt != null)
                            {
                                rt.anchoredPosition = Vector2.zero;
                                rt.localPosition = Vector3.zero;
                            }
                        }
                    }
                }

                ConfigurarBotoesEmCanvasJogo(go);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Action] Aviso ao configurar botões de gameplay ({nomesObjetos[i]}): {ex.Message}");
            }
        }
    }

    private static Sprite s_spriteLampadaCache = null;
    public static Sprite ObterSpriteLampadaCartoon()
    {
        if (s_spriteLampadaCache != null) return s_spriteLampadaCache;

        Sprite spr = Resources.Load<Sprite>("icone_lampada_cartoon");
        if (spr != null)
        {
            s_spriteLampadaCache = spr;
            return s_spriteLampadaCache;
        }

        string[] caminhos = {
            System.IO.Path.Combine(Application.dataPath, "Resources", "icone_lampada_cartoon.png"),
            System.IO.Path.Combine(Application.dataPath, "Botoes", "icone_lampada_cartoon.png")
        };

        foreach (var caminho in caminhos)
        {
            if (System.IO.File.Exists(caminho))
            {
                try
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(caminho);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        s_spriteLampadaCache = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                        return s_spriteLampadaCache;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[Action] Erro ao carregar icone_lampada_cartoon: {ex.Message}");
                }
            }
        }
        return null;
    }

    private void ConfigurarBotoesEmCanvasJogo(GameObject rootJogo)
    {
        if (rootJogo == null) return;

        // 0. Desativa botões legados ou corrompidos que possam ter ficado dentro de Canvas_btn (onde escala era ~0 em Matemática)
        Transform oldContainer = rootJogo.transform.Find("Canvas_btn");
        if (oldContainer != null)
        {
            Transform oldC = oldContainer.Find("btnCasinha");
            if (oldC != null) oldC.gameObject.SetActive(false);
            Transform oldE = oldContainer.Find("btnEngrenagem");
            if (oldE != null) oldE.gameObject.SetActive(false);
            Transform oldD = oldContainer.Find("btnDica");
            if (oldD != null) oldD.gameObject.SetActive(false);
        }

        // Limpa badge legado que possa ter sido instanciado diretamente no root de gameplay
        Transform oldDirectBadge = rootJogo.transform.Find("IndicadorFaseBadge");
        if (oldDirectBadge != null)
        {
            Destroy(oldDirectBadge.gameObject);
        }
        Transform oldFaseTxt = rootJogo.transform.Find("Fase");
        if (oldFaseTxt != null)
        {
            oldFaseTxt.gameObject.SetActive(false);
        }

        // 1. Procura ou cria o container de TopBar sob rootJogo com Canvas Overlay próprio
        // Isso garante 100% que os botões apareçam em todas as telas (Matemática Arthur/Gabriela/Mãe e Português Zé/Fernanda/Patty)
        Transform tBarra = rootJogo.transform.Find("TopBar_BotoesCanvas");
        GameObject gBarra;
        if (tBarra == null)
        {
            gBarra = new GameObject("TopBar_BotoesCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            gBarra.transform.SetParent(rootJogo.transform, false);
        }
        else
        {
            gBarra = tBarra.gameObject;
        }

        Canvas canvasBarra = gBarra.GetComponent<Canvas>();
        if (canvasBarra == null) canvasBarra = gBarra.AddComponent<Canvas>();
        canvasBarra.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasBarra.sortingOrder = 500; // Acima de qualquer elemento de gameplay, abaixo de modais popup (1000)

        CanvasScaler scaler = gBarra.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gBarra.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster gr = gBarra.GetComponent<GraphicRaycaster>();
        if (gr == null) gBarra.AddComponent<GraphicRaycaster>();

        RectTransform rtBarra = gBarra.GetComponent<RectTransform>();
        rtBarra.anchorMin = Vector2.zero;
        rtBarra.anchorMax = Vector2.one;
        rtBarra.offsetMin = Vector2.zero;
        rtBarra.offsetMax = Vector2.zero;
        rtBarra.localScale = Vector3.one;

        // 2. Botão Casinha (Voltar ao Lobby)
        Transform tCasinha = gBarra.transform.Find("btnCasinha");
        GameObject gCasinha = (tCasinha != null) ? tCasinha.gameObject : null;
        if (gCasinha == null)
        {
            gCasinha = new GameObject("btnCasinha", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            gCasinha.transform.SetParent(gBarra.transform, false);
        }

        RectTransform rtCasinha = gCasinha.GetComponent<RectTransform>();
        rtCasinha.anchorMin = new Vector2(0f, 1f);
        rtCasinha.anchorMax = new Vector2(0f, 1f);
        rtCasinha.pivot = new Vector2(0.5f, 0.5f);
        rtCasinha.anchoredPosition = new Vector2(90f, -75f);
        rtCasinha.sizeDelta = new Vector2(95f, 95f);
        rtCasinha.localScale = Vector3.one;

        Image imgCasinha = gCasinha.GetComponent<Image>();
        if (spriteCasinha == null) spriteCasinha = Resources.Load<Sprite>("btnCasinha");
        if (spriteCasinha != null) imgCasinha.sprite = spriteCasinha;
        imgCasinha.color = Color.white;
        imgCasinha.raycastTarget = true;
        imgCasinha.preserveAspect = true;

        Button btnCasinha = gCasinha.GetComponent<Button>();
        btnCasinha.onClick.RemoveAllListeners();
        btnCasinha.onClick.AddListener(VoltarParaLobby);

        // 3. Botão Engrenagem (Configurações)
        Transform tEng = gBarra.transform.Find("btnEngrenagem");
        GameObject gEng = (tEng != null) ? tEng.gameObject : null;
        if (gEng == null)
        {
            gEng = new GameObject("btnEngrenagem", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            gEng.transform.SetParent(gBarra.transform, false);
        }

        RectTransform rtEng = gEng.GetComponent<RectTransform>();
        rtEng.anchorMin = new Vector2(0f, 1f);
        rtEng.anchorMax = new Vector2(0f, 1f);
        rtEng.pivot = new Vector2(0.5f, 0.5f);
        rtEng.anchoredPosition = new Vector2(205f, -75f);
        rtEng.sizeDelta = new Vector2(95f, 95f);
        rtEng.localScale = Vector3.one;

        Image imgEng = gEng.GetComponent<Image>();
        if (spriteEngrenagem == null) spriteEngrenagem = Resources.Load<Sprite>("btnEngrenagem");
        if (spriteEngrenagem != null) imgEng.sprite = spriteEngrenagem;
        imgEng.color = Color.white;
        imgEng.raycastTarget = true;
        imgEng.preserveAspect = true;

        Button btnEng = gEng.GetComponent<Button>();
        btnEng.onClick.RemoveAllListeners();
        btnEng.onClick.AddListener(Config);

        // 4. Botão Dica / Lâmpada Cartoon (com sprite nítido)
        Transform tDica = gBarra.transform.Find("btnDica");
        GameObject gDica = (tDica != null) ? tDica.gameObject : null;
        if (gDica == null)
        {
            gDica = new GameObject("btnDica", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            gDica.transform.SetParent(gBarra.transform, false);
        }

        RectTransform rtDica = gDica.GetComponent<RectTransform>();
        rtDica.anchorMin = new Vector2(0f, 1f);
        rtDica.anchorMax = new Vector2(0f, 1f);
        rtDica.pivot = new Vector2(0.5f, 0.5f);
        rtDica.anchoredPosition = new Vector2(320f, -75f);
        rtDica.sizeDelta = new Vector2(100f, 100f);
        rtDica.localScale = Vector3.one;

        Image imgDica = gDica.GetComponent<Image>();
        Sprite sprLampada = ObterSpriteLampadaCartoon();
        if (sprLampada != null)
        {
            imgDica.sprite = sprLampada;
            imgDica.preserveAspect = true;
            imgDica.color = Color.white;
        }
        else
        {
            Sprite fallbackSpr = Resources.Load<Sprite>("badge_lobby_jogo") ?? Resources.Load<Sprite>("btn_laranja_jogo");
            if (fallbackSpr != null) imgDica.sprite = fallbackSpr;
            imgDica.color = new Color(1f, 0.85f, 0.15f, 1f);
        }
        imgDica.raycastTarget = true;

        Button btnDica = gDica.GetComponent<Button>();
        btnDica.onClick.RemoveAllListeners();
        btnDica.onClick.AddListener(AbrirVideoExplicativo);

        Transform tTxtDica = gDica.transform.Find("TextoDica");
        if (tTxtDica != null)
        {
            tTxtDica.gameObject.SetActive(false);
        }
    }

    public void BtnFacil()
    {
        TocarSomClique();
        GerenciadorFases gf = FindFirstObjectByType<GerenciadorFases>();
        if (gf != null) gf.DefinirNivel(0);

        if (telaDificuldadeAtual == groupDificuldadeMatematica) TrocarTela(telaDificuldadeAtual, groupFasesMathFacil);
        else TrocarTela(telaDificuldadeAtual, groupFasesFacil);
    }

    public void BtnMedio()
    {
        TocarSomClique();
        GerenciadorFases gf = FindFirstObjectByType<GerenciadorFases>();
        if (gf != null) gf.DefinirNivel(1);

        if (telaDificuldadeAtual == groupDificuldadeMatematica) TrocarTela(telaDificuldadeAtual, groupFasesMathMedio);
        else TrocarTela(telaDificuldadeAtual, groupFasesMedio);
    }

    public void BtnDificil()
    {
        TocarSomClique();
        GerenciadorFases gf = FindFirstObjectByType<GerenciadorFases>();
        if (gf != null) gf.DefinirNivel(2);

        if (telaDificuldadeAtual == groupDificuldadeMatematica) TrocarTela(telaDificuldadeAtual, groupFasesMathDificil);
        else TrocarTela(telaDificuldadeAtual, groupFasesDificil);
    }

    public void VoltarDasFasesFacil() { TocarSomClique(); TocarMusicaPersonagens(); VerificarDesbloqueios(); TrocarTela(groupFasesFacil, telaDificuldadeAtual); }
    public void VoltarDasFasesMedio() { TocarSomClique(); TocarMusicaPersonagens(); VerificarDesbloqueios(); TrocarTela(groupFasesMedio, telaDificuldadeAtual); }
    public void VoltarDasFasesDificil() { TocarSomClique(); TocarMusicaPersonagens(); VerificarDesbloqueios(); TrocarTela(groupFasesDificil, telaDificuldadeAtual); }

    public void VoltarDasFasesMathFacil() { TocarSomClique(); TocarMusicaPersonagens(); VerificarDesbloqueios(); TrocarTela(groupFasesMathFacil, telaDificuldadeAtual); }
    public void VoltarDasFasesMathMedio() { TocarSomClique(); TocarMusicaPersonagens(); VerificarDesbloqueios(); TrocarTela(groupFasesMathMedio, telaDificuldadeAtual); }
    public void VoltarDasFasesMathDificil() { TocarSomClique(); TocarMusicaPersonagens(); VerificarDesbloqueios(); TrocarTela(groupFasesMathDificil, telaDificuldadeAtual); }

    public void VoltarDoJogoParaFases(CanvasGroup grupoJogoAtual)
    {
        TocarSomClique();
        CanvasGroup telaFasesDestino = null;

        if (grupoJogoAtual == groupJogoFacil) telaFasesDestino = groupFasesFacil;
        else if (grupoJogoAtual == groupJogoMedio) telaFasesDestino = groupFasesMedio;
        else if (grupoJogoAtual == groupJogoDificil) telaFasesDestino = groupFasesDificil;
        else if (grupoJogoAtual == groupJogoMathFacil) telaFasesDestino = groupFasesMathFacil;
        else if (grupoJogoAtual == groupJogoMathMedio) telaFasesDestino = groupFasesMathMedio;
        else if (grupoJogoAtual == groupJogoMathDificil) telaFasesDestino = groupFasesMathDificil;

        if (telaFasesDestino != null)
        {
            TrocarTela(grupoJogoAtual, telaFasesDestino);
            GerenciadorFases gerenciador = FindFirstObjectByType<GerenciadorFases>();
            if (gerenciador != null) gerenciador.DefinirNivel(gerenciador.nivelAtual);
        }
        else
        {
            TrocarTela(grupoJogoAtual, telaDificuldadeAtual);
        }
    }

    public void Play()
    {
        TocarSomClique();
        TrocarTela(groupLobby, groupMenu);
    }

    public void VoltarParaLobby()
    {
        TocarSomClique();
        RestaurarMusicaLobby();

        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null && gm.sfxSource != null)
        {
            gm.sfxSource.Stop();
        }

        EsconderGrupo(groupMenu);
        EsconderGrupo(groupDificuldadePortugues);
        EsconderGrupo(groupDificuldadeMatematica);
        EsconderGrupo(groupConfig);

        EsconderGrupo(groupFasesFacil);
        EsconderGrupo(groupFasesMedio);
        EsconderGrupo(groupFasesDificil);
        EsconderGrupo(groupFasesMathFacil);
        EsconderGrupo(groupFasesMathMedio);
        EsconderGrupo(groupFasesMathDificil);

        EsconderGrupo(groupJogoFacil);
        EsconderGrupo(groupJogoMedio);
        EsconderGrupo(groupJogoDificil);
        EsconderGrupo(groupJogoMathFacil);
        EsconderGrupo(groupJogoMathMedio);
        EsconderGrupo(groupJogoMathDificil);

        EsconderGrupo(groupFaseConcluida);
        EsconderGrupo(groupProxFaseOuro);
        EsconderGrupo(groupProxFasePrata);
        EsconderGrupo(groupProxFaseBronze);
        EsconderGrupo(groupGameOver);

        EsconderGrupo(groupCreditos);
        EsconderGrupo(groupTrofeus);
        EsconderGrupo(groupFinalJogo);

        if (tutorialManager != null)
        {
            tutorialManager.EsconderTelas(tutorialManager.telasPortugues);
            tutorialManager.EsconderTelas(tutorialManager.telasMatematica);
        }

        MostrarGrupo(groupLobby);
        if (groupLobby != null)
        {
            groupLobby.interactable = true;
            groupLobby.blocksRaycasts = true;
            groupLobby.alpha = 1f;
        }
    }

    public void Config()
    {
        TocarSomClique();

        CanvasGroup[] telasOrigemPossiveis = {
            groupJogoFacil, groupJogoMedio, groupJogoDificil,
            groupJogoMathFacil, groupJogoMathMedio, groupJogoMathDificil,
            groupFasesFacil, groupFasesMedio, groupFasesDificil,
            groupFasesMathFacil, groupFasesMathMedio, groupFasesMathDificil,
            groupDificuldadePortugues, groupDificuldadeMatematica,
            groupMenu
        };

        telaOrigemConfig = groupLobby;
        foreach (var tela in telasOrigemPossiveis)
        {
            if (tela != null && tela.gameObject.activeSelf && tela.alpha > 0.5f)
            {
                telaOrigemConfig = tela;
                break;
            }
        }

        TrocarTela(telaOrigemConfig, groupConfig);
    }

    public void FecharConfig()
    {
        TocarSomClique();
        CanvasGroup destino = (telaOrigemConfig != null) ? telaOrigemConfig : groupLobby;
        TrocarTela(groupConfig, destino);
    }

    public void SairDoJogoCompleto()
    {
        TocarSomClique();
        try { PlayerPrefs.Save(); } catch {}
        try { System.Diagnostics.Process.GetCurrentProcess().Kill(); } catch {}
        Application.Quit();
    }

    public void TrocarTela(CanvasGroup sair, CanvasGroup entrar)
    {
        if (sair != null)
        {
            sair.interactable = false;
            sair.blocksRaycasts = false;
            sair.DOFade(0, 0.5f).OnComplete(() => { sair.gameObject.SetActive(false); });
        }
        if (entrar != null)
        {
            entrar.gameObject.SetActive(true);
            Canvas cEntrar = entrar.GetComponent<Canvas>();
            if ((cEntrar == null || cEntrar.renderMode == RenderMode.WorldSpace) && entrar.transform.localScale == Vector3.zero)
            {
                entrar.transform.localScale = Vector3.one;
            }
            entrar.alpha = 0;
            entrar.interactable = true;
            entrar.blocksRaycasts = true;
            entrar.DOFade(1, 0.5f);

            if (entrar == groupJogoFacil || entrar == groupJogoMedio || entrar == groupJogoDificil ||
                entrar == groupJogoMathFacil || entrar == groupJogoMathMedio || entrar == groupJogoMathDificil)
            {
                ConfigurarBotoesGameplayCanvases();
            }
        }
    }

    private void EsconderGrupo(CanvasGroup gp) { if (gp != null) { gp.alpha = 0; gp.gameObject.SetActive(false); } }
    private void MostrarGrupo(CanvasGroup gp)
    {
        if (gp != null)
        {
            gp.alpha = 1;
            gp.gameObject.SetActive(true);
            Canvas c = gp.GetComponent<Canvas>();
            if ((c == null || c.renderMode == RenderMode.WorldSpace) && gp.transform.localScale == Vector3.zero)
            {
                gp.transform.localScale = Vector3.one;
            }
        }
    }

    public void ResetarProgressoTeste()
    {
        TocarSomClique();
        string pfx = $"P_{GerenciadorPerfis.SanitizarNome(GerenciadorPerfis.PerfilAtivo)}_";
        PlayerPrefs.DeleteKey(pfx + "Desbloqueio_Medio_Port");
        PlayerPrefs.DeleteKey(pfx + "Desbloqueio_Dificil_Port");
        PlayerPrefs.DeleteKey(pfx + "Desbloqueio_Medio_Math");
        PlayerPrefs.DeleteKey(pfx + "Desbloqueio_Dificil_Math");
        for (int m = 0; m < 2; m++)
        {
            string mat = m == 0 ? "Port" : "Math";
            for (int n = 0; n < 3; n++)
            {
                for (int f = 0; f < 30; f++)
                {
                    PlayerPrefs.DeleteKey($"{pfx}FaseCompleta_{mat}_{n}_{f}");
                    PlayerPrefs.DeleteKey($"{pfx}VidasRestantes_{mat}_{n}_{f}");
                }
            }
        }
        PlayerPrefs.Save();
        VerificarDesbloqueios();
        Debug.Log($"Progresso do aluno {GerenciadorPerfis.PerfilAtivo} resetado!");
    }

    public CanvasGroup GetPainelConclusaoAtivo()
    {
        if (groupFaseConcluida != null && groupFaseConcluida.gameObject.activeSelf) return groupFaseConcluida;
        if (groupProxFaseOuro != null && groupProxFaseOuro.gameObject.activeSelf) return groupProxFaseOuro;
        if (groupProxFasePrata != null && groupProxFasePrata.gameObject.activeSelf) return groupProxFasePrata;
        if (groupProxFaseBronze != null && groupProxFaseBronze.gameObject.activeSelf) return groupProxFaseBronze;
        return null;
    }

    public void BotaoContinuarGameOver()
    {
        TocarSomClique();
        TocarMusicaPersonagens();

        CanvasGroup telaDestino = telaDificuldadeAtual;
        GerenciadorFases gf = FindFirstObjectByType<GerenciadorFases>();

        if (gf != null)
        {
            if (telaDificuldadeAtual == groupDificuldadeMatematica)
            {
                if (gf.nivelAtual == 0) telaDestino = groupFasesMathFacil;
                else if (gf.nivelAtual == 1) telaDestino = groupFasesMathMedio;
                else telaDestino = groupFasesMathDificil;
            }
            else
            {
                if (gf.nivelAtual == 0) telaDestino = groupFasesFacil;
                else if (gf.nivelAtual == 1) telaDestino = groupFasesMedio;
                else telaDestino = groupFasesDificil;
            }
        }

        TrocarTela(groupGameOver, telaDestino);
    }

    public void AbrirCreditos()
    {
        TocarSomClique();

        if (musicSource != null && musicaCreditos != null)
        {
            musicSource.clip = musicaCreditos;
            musicSource.Play();
        }

        TrocarTela(groupLobby, groupCreditos);
    }

    public void FecharCreditos()
    {
        TocarSomClique();
        RestaurarMusicaLobby();
        TrocarTela(groupCreditos, groupLobby);
    }

    public void AbrirTrofeus()
    {
        TocarSomClique();

        if (trofeusManager != null)
        {
            trofeusManager.AtualizarTelaDeTrofeus();
        }

        CanvasGroup telaAtual = GetPainelConclusaoAtivo();

        if (telaAtual != null)
        {
            TrocarTela(telaAtual, groupTrofeus);
        }
        else
        {
            TrocarTela(groupLobby, groupTrofeus);
        }
    }

    public void FecharTrofeus()
    {
        TocarSomClique();
        telaDificuldadeAtual = groupDificuldadePortugues;
        VerificarDesbloqueios();
        TocarMusicaPersonagens();
        TrocarTela(groupTrofeus, groupDificuldadePortugues);
    }

    public void TocarMusicaPersonagens()
    {
        if (musicSource != null && musicaPersonagens != null)
        {
            if (musicSource.clip != musicaPersonagens || !musicSource.isPlaying)
            {
                musicSource.clip = musicaPersonagens;
                musicSource.loop = true;
                musicSource.Play();
            }
        }
    }

    public void RestaurarMusicaLobby()
    {
        if (musicSource != null && musicaLobby != null)
        {
            if (musicSource.clip != musicaLobby || !musicSource.isPlaying)
            {
                musicSource.clip = musicaLobby;
                musicSource.loop = true;
                musicSource.Play();
            }
        }
    }

    public void PararMusica()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }


    public bool VerificarJogo100PorCento()
    {
        for (int nivel = 0; nivel < 3; nivel++)
        {
            for (int fase = 0; fase < totalFasesPorNivel; fase++)
            {
                bool portConcluido = GerenciadorPerfis.GetInt($"FaseCompleta_Port_{nivel}_{fase}", 0) == 1;
                bool mathConcluido = GerenciadorPerfis.GetInt($"FaseCompleta_Math_{nivel}_{fase}", 0) == 1;

                if (!portConcluido || !mathConcluido)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public bool TentarExecutarFinalDeJogo(CanvasGroup telaAtual)
    {
        if (VerificarJogo100PorCento())
        {
            ExecutarFinalDeJogo(telaAtual);
            return true;
        }
        return false;
    }

    public void ExecutarFinalDeJogo(CanvasGroup telaAtual)
    {
        if (musicSource != null) musicSource.Stop();

        TrocarTela(telaAtual, groupFinalJogo);

        if (videoPlayerFinal != null)
        {
            videoPlayerFinal.Stop();
            videoPlayerFinal.Play();
        }
    }

    private void AoTerminarVideoFinal(VideoPlayer vp)
    {
        FecharFinalDeJogo();
    }

    public void AbrirVideoExplicativo()
    {
        GerenciadorVideoExplicativo gve = FindFirstObjectByType<GerenciadorVideoExplicativo>();
        if (gve != null) gve.AbrirTelaVideo();
    }

    public void FecharVideoExplicativo()
    {
        GerenciadorVideoExplicativo gve = FindFirstObjectByType<GerenciadorVideoExplicativo>();
        if (gve != null) gve.FecharTelaVideo();
    }

    public void FecharFinalDeJogo()
    {
        if (videoPlayerFinal != null) videoPlayerFinal.Stop();
        RestaurarMusicaLobby();
        TrocarTela(groupFinalJogo, groupCreditos != null ? groupCreditos : groupLobby);
    }

    #region Tela de Carregamento em Tela Cheia (Startup Splash)
    private void CriarTelaDeCarregamentoInicial()
    {
        // Cria Canvas de Carregamento em Tela Cheia no topo de tudo
        GameObject loadingGO = new GameObject("TelaCarregamento_Canvas");
        Canvas canvas = loadingGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;

        CanvasScaler scaler = loadingGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        loadingGO.AddComponent<GraphicRaycaster>();
        CanvasGroup cg = loadingGO.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = true;

        // Fundo em Tela Cheia (1920x1080)
        GameObject bgGO = new GameObject("FundoCarregamento", typeof(RectTransform), typeof(Image));
        bgGO.transform.SetParent(loadingGO.transform, false);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        Image bgImg = bgGO.GetComponent<Image>();
        Sprite splashSprite = Resources.Load<Sprite>("TelaCarregamento");
        if (splashSprite == null)
        {
            Sprite[] sprites = Resources.FindObjectsOfTypeAll<Sprite>();
            foreach (var s in sprites)
            {
                if (s != null && s.name.Contains("TelaCarregamento"))
                {
                    splashSprite = s;
                    break;
                }
            }
        }

        if (splashSprite != null)
        {
            bgImg.sprite = splashSprite;
            bgImg.color = Color.white;
            bgImg.preserveAspect = false; // Cobre 100% da tela em tela cheia
        }
        else
        {
            bgImg.color = new Color(0.18f, 0.65f, 0.95f, 1f);
        }

        // Container inferior para escrita de carregamento e barra de progresso
        GameObject contGO = new GameObject("ContainerTextoCarregamento", typeof(RectTransform), typeof(Image));
        contGO.transform.SetParent(loadingGO.transform, false);
        RectTransform contRT = contGO.GetComponent<RectTransform>();
        contRT.anchorMin = new Vector2(0.5f, 0f);
        contRT.anchorMax = new Vector2(0.5f, 0f);
        contRT.pivot = new Vector2(0.5f, 0.5f);
        contRT.anchoredPosition = new Vector2(0, 125);
        contRT.sizeDelta = new Vector2(820, 140);

        Image contImg = contGO.GetComponent<Image>();
        Sprite cardFundo = Resources.Load<Sprite>("card_aluno_fundo_jogo");
        if (cardFundo != null)
        {
            contImg.sprite = cardFundo;
            contImg.type = Image.Type.Sliced;
            contImg.color = new Color(0.06f, 0.14f, 0.25f, 0.92f);
        }
        else
        {
            contImg.color = new Color(0.06f, 0.14f, 0.25f, 0.92f);
        }

        TMP_FontAsset fonteCrayon = Resources.Load<TMP_FontAsset>("CrayonHandRegular2016Demo SDF");

        // Escrita Principal de Carregamento
        GameObject txtTituloGO = new GameObject("TextoCarregando", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtTituloGO.transform.SetParent(contGO.transform, false);
        RectTransform txtTituloRT = txtTituloGO.GetComponent<RectTransform>();
        txtTituloRT.anchorMin = new Vector2(0f, 0.52f);
        txtTituloRT.anchorMax = new Vector2(1f, 1f);
        txtTituloRT.offsetMin = new Vector2(10, 0);
        txtTituloRT.offsetMax = new Vector2(-10, -5);

        TextMeshProUGUI txtTitulo = txtTituloGO.GetComponent<TextMeshProUGUI>();
        if (fonteCrayon != null) txtTitulo.font = fonteCrayon;
        txtTitulo.fontSize = 44;
        txtTitulo.alignment = TextAlignmentOptions.Center;
        txtTitulo.color = new Color(1f, 0.92f, 0.22f, 1f); // Amarelo vibrante cartoon
        txtTitulo.text = "CARREGANDO...";

        // Subtítulo de Carregamento
        GameObject txtSubGO = new GameObject("TextoSubtitulo", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtSubGO.transform.SetParent(contGO.transform, false);
        RectTransform txtSubRT = txtSubGO.GetComponent<RectTransform>();
        txtSubRT.anchorMin = new Vector2(0f, 0.22f);
        txtSubRT.anchorMax = new Vector2(1f, 0.55f);
        txtSubRT.offsetMin = new Vector2(10, 0);
        txtSubRT.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI txtSub = txtSubGO.GetComponent<TextMeshProUGUI>();
        if (fonteCrayon != null) txtSub.font = fonteCrayon;
        txtSub.fontSize = 22;
        txtSub.alignment = TextAlignmentOptions.Center;
        txtSub.color = new Color(0.92f, 0.96f, 1f, 0.95f);
        txtSub.text = "Por favor, aguarde... Preparando as letrinhas e lições!";

        // Fundo da Barra de Progresso
        GameObject barBgGO = new GameObject("BarraFundo", typeof(RectTransform), typeof(Image));
        barBgGO.transform.SetParent(contGO.transform, false);
        RectTransform barBgRT = barBgGO.GetComponent<RectTransform>();
        barBgRT.anchorMin = new Vector2(0.5f, 0f);
        barBgRT.anchorMax = new Vector2(0.5f, 0f);
        barBgRT.pivot = new Vector2(0.5f, 0.5f);
        barBgRT.anchoredPosition = new Vector2(0, 20);
        barBgRT.sizeDelta = new Vector2(680, 14);

        Image barBgImg = barBgGO.GetComponent<Image>();
        barBgImg.color = new Color(0f, 0f, 0f, 0.6f);

        // Preenchimento da Barra de Progresso
        GameObject barFillGO = new GameObject("BarraPreenchimento", typeof(RectTransform), typeof(Image));
        barFillGO.transform.SetParent(barBgGO.transform, false);
        RectTransform barFillRT = barFillGO.GetComponent<RectTransform>();
        barFillRT.anchorMin = Vector2.zero;
        barFillRT.anchorMax = Vector2.one;
        barFillRT.offsetMin = Vector2.zero;
        barFillRT.offsetMax = Vector2.zero;

        Image barFillImg = barFillGO.GetComponent<Image>();
        barFillImg.type = Image.Type.Filled;
        barFillImg.fillMethod = Image.FillMethod.Horizontal;
        barFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        barFillImg.fillAmount = 0.05f;
        barFillImg.color = new Color(0.18f, 0.88f, 0.42f, 1f); // Verde gamer animado

        StartCoroutine(RotinaAnimacaoCarregamento(cg, txtTitulo, txtSub, barFillImg, loadingGO));
    }

    private System.Collections.IEnumerator RotinaAnimacaoCarregamento(CanvasGroup cg, TextMeshProUGUI txtTitulo, TextMeshProUGUI txtSub, Image barFill, GameObject loadingGO)
    {
        float duracao = 2.4f;
        float decorrido = 0f;
        string[] animFrames = new string[] { "CARREGANDO.", "CARREGANDO..", "CARREGANDO..." };
        int frameIdx = 0;
        float timerFrame = 0f;

        while (decorrido < duracao)
        {
            decorrido += Time.deltaTime;
            timerFrame += Time.deltaTime;

            if (timerFrame >= 0.28f)
            {
                timerFrame = 0f;
                frameIdx = (frameIdx + 1) % animFrames.Length;
                if (txtTitulo != null) txtTitulo.text = animFrames[frameIdx];
            }

            if (barFill != null)
            {
                barFill.fillAmount = Mathf.Clamp01(decorrido / duracao);
            }

            yield return null;
        }

        if (barFill != null) barFill.fillAmount = 1f;
        if (txtTitulo != null) txtTitulo.text = "TUDO PRONTO!";
        if (txtSub != null) txtSub.text = "Bem-vindo à Revolta das Letrinhas!";

        yield return new WaitForSeconds(0.35f);

        if (cg != null)
        {
            cg.DOFade(0f, 0.6f).OnComplete(() =>
            {
                if (loadingGO != null) Destroy(loadingGO);
            });
        }
        else
        {
            if (loadingGO != null) Destroy(loadingGO);
        }
    }
    #endregion
}
