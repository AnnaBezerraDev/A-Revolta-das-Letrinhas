using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using DG.Tweening;

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
        Screen.fullScreen = true;
    }

    public static void AjustarProporcaoTelas()
    {
        // Configura��o mantida est�tica na cena para estabilidade visual perfeita
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
        bool medioLiberadoPort = GerenciadorPerfis.GetInt("Desbloqueio_Medio_Port", 0) == 1 || GerenciadorPerfis.GetInt("Desbloqueio_Medio", 0) == 1;
        bool dificilLiberadoPort = GerenciadorPerfis.GetInt("Desbloqueio_Dificil_Port", 0) == 1 || GerenciadorPerfis.GetInt("Desbloqueio_Dificil", 0) == 1;

        bool medioLiberadoMath = GerenciadorPerfis.GetInt("Desbloqueio_Medio_Math", 0) == 1;
        bool dificilLiberadoMath = GerenciadorPerfis.GetInt("Desbloqueio_Dificil_Math", 0) == 1;

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
            CanvasGroup cg = telasJogo[i];
            GameObject go = (cg != null) ? cg.gameObject : GameObject.Find(nomesObjetos[i]);
            if (go == null) continue;

            ConfigurarBotoesEmCanvasJogo(go);
        }
    }

    private void ConfigurarBotoesEmCanvasJogo(GameObject rootJogo)
    {
        if (rootJogo == null) return;


        Transform container = rootJogo.transform.Find("Canvas_btn");
        if (container == null)
        {
            Canvas[] canvases = rootJogo.GetComponentsInChildren<Canvas>(true);
            foreach (var c in canvases)
            {
                if (c.name.Contains("btn"))
                {
                    container = c.transform;
                    break;
                }
            }
            if (container == null && canvases.Length > 0)
            {
                container = canvases[0].transform;
            }
        }

        if (container == null) return;

      
        Transform tCasinha = container.Find("btnCasinha");
        if (tCasinha == null)
        {
            GameObject gCasinha = new GameObject("btnCasinha");
            gCasinha.transform.SetParent(container, false);
            tCasinha = gCasinha.transform;
        }

        RectTransform rtCasinha = tCasinha.GetComponent<RectTransform>();
        if (rtCasinha == null) rtCasinha = tCasinha.gameObject.AddComponent<RectTransform>();
        rtCasinha.anchorMin = new Vector2(0f, 1f);
        rtCasinha.anchorMax = new Vector2(0f, 1f);
        rtCasinha.pivot = new Vector2(0.5f, 0.5f);
        rtCasinha.anchoredPosition = new Vector2(100f, -80f);
        rtCasinha.sizeDelta = new Vector2(100f, 100f);
        rtCasinha.localScale = Vector3.one;

        Image imgCasinha = tCasinha.GetComponent<Image>();
        if (imgCasinha == null) imgCasinha = tCasinha.gameObject.AddComponent<Image>();
        if (spriteCasinha != null) imgCasinha.sprite = spriteCasinha;
        imgCasinha.color = Color.white;
        imgCasinha.raycastTarget = true;
        imgCasinha.preserveAspect = true;

        Button btnCasinha = tCasinha.GetComponent<Button>();
        if (btnCasinha == null) btnCasinha = tCasinha.gameObject.AddComponent<Button>();
        btnCasinha.onClick.RemoveAllListeners();
        btnCasinha.onClick.AddListener(VoltarParaLobby);

        
        Transform tEng = container.Find("btnEngrenagem");
        if (tEng == null)
        {
            GameObject gEng = new GameObject("btnEngrenagem");
            gEng.transform.SetParent(container, false);
            tEng = gEng.transform;
        }

        RectTransform rtEng = tEng.GetComponent<RectTransform>();
        if (rtEng == null) rtEng = tEng.gameObject.AddComponent<RectTransform>();
        rtEng.anchorMin = new Vector2(0f, 1f);
        rtEng.anchorMax = new Vector2(0f, 1f);
        rtEng.pivot = new Vector2(0.5f, 0.5f);
        rtEng.anchoredPosition = new Vector2(220f, -80f);
        rtEng.sizeDelta = new Vector2(100f, 100f);
        rtEng.localScale = Vector3.one;

        Image imgEng = tEng.GetComponent<Image>();
        if (imgEng == null) imgEng = tEng.gameObject.AddComponent<Image>();
        if (spriteEngrenagem != null) imgEng.sprite = spriteEngrenagem;
        imgEng.color = Color.white;
        imgEng.raycastTarget = true;
        imgEng.preserveAspect = true;

        Button btnEng = tEng.GetComponent<Button>();
        if (btnEng == null) btnEng = tEng.gameObject.AddComponent<Button>();
        btnEng.onClick.RemoveAllListeners();
        btnEng.onClick.AddListener(Config);
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
            if (entrar.transform.localScale == Vector3.zero) entrar.transform.localScale = Vector3.one;
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
            if (gp.transform.localScale == Vector3.zero) gp.transform.localScale = Vector3.one;
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
}
