using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GerenciadorPerfis : MonoBehaviour
{
    private const string KEY_LISTA_PERFIS = "Lista_Alunos_Perfis";
    private const string KEY_PERFIL_ATIVO = "Aluno_Perfil_Ativo";

    public delegate void PerfilAlteradoHandler();
    public static event PerfilAlteradoHandler OnPerfilAlterado;

    public enum ResultadoCriacaoPerfil
    {
        Sucesso,
        NomeVazioOuCurto,
        NomeJaExiste
    }

    private static string _perfilAtivo;
    public static string PerfilAtivo
    {
        get
        {
            if (string.IsNullOrEmpty(_perfilAtivo))
            {
                Inicializar();
            }
            return _perfilAtivo;
        }
        private set
        {
            _perfilAtivo = value;
        }
    }

    [Header("Assets e Sprites do Jogo")]
    public Sprite spritePainelPergaminho;
    public Sprite spriteXFechar;
    public Sprite spriteCardFundo;
    public Sprite spriteCardAtivo;
    public Sprite spriteBarraPesquisa;
    public Sprite spriteBadgeTopo;
    public Sprite spriteBtnJogar;
    public Sprite spriteBtnContinuar;
    public Sprite spriteBtnLaranja;
    public Sprite spriteBtnVermelho;
    public Sprite spriteAvatarKid;
    public Sprite spriteLixeira;
    public Sprite spriteLupa;
    public TMP_FontAsset fontJogo;

    // Componentes de UI do Lobby
    private GameObject badgeLobby;
    private TextMeshProUGUI textoBadgeAluno;
    private GameObject modalPerfis;
    private Transform containerCards;
    private TMP_InputField inputPesquisa;
    private TextMeshProUGUI textoContadorAlunos;
    private TMP_InputField inputNomeNovoAluno;
    private TextMeshProUGUI textoAvisoNovoAluno;

    private string filtroPesquisaAtual = "";
    private string alunoEmConfirmacaoExclusao = "";
    private float tempoConfirmacaoExclusao = 0f;

    void Awake()
    {
        Inicializar();
    }

    void Start()
    {
        CarregarRecursosTema();
        CriarUILobby();
    }

    void Update()
    {
        if (!string.IsNullOrEmpty(alunoEmConfirmacaoExclusao))
        {
            tempoConfirmacaoExclusao -= Time.deltaTime;
            if (tempoConfirmacaoExclusao <= 0f)
            {
                alunoEmConfirmacaoExclusao = "";
                AtualizarListaCards();
            }
        }
    }

    public static void Inicializar()
    {
        MigrarDadosLegadosSeNecessario();

        List<string> perfis = ObterListaPerfis();
        if (perfis.Count == 0)
        {
            perfis.Add("Aluno 1");
            SalvarListaPerfis(perfis);
        }

        string salvo = PlayerPrefs.GetString(KEY_PERFIL_ATIVO, "");
        if (string.IsNullOrEmpty(salvo) || !perfis.Contains(salvo))
        {
            _perfilAtivo = perfis[0];
            PlayerPrefs.SetString(KEY_PERFIL_ATIVO, _perfilAtivo);
            PlayerPrefs.Save();
        }
        else
        {
            _perfilAtivo = salvo;
        }
    }

    public static string ObterChave(string chaveBase)
    {
        if (string.IsNullOrEmpty(chaveBase)) return chaveBase;

        if (chaveBase == "SomMutado" || chaveBase == "VolumeMusica" || chaveBase == "VolumeSFX")
            return chaveBase;

        string sanitizado = SanitizarNome(PerfilAtivo);
        return $"P_{sanitizado}_{chaveBase}";
    }

    public static int GetInt(string chave, int padrao = 0)
    {
        return PlayerPrefs.GetInt(ObterChave(chave), padrao);
    }

    public static void SetInt(string chave, int valor)
    {
        PlayerPrefs.SetInt(ObterChave(chave), valor);
    }

    public static float GetFloat(string chave, float padrao = 0f)
    {
        return PlayerPrefs.GetFloat(ObterChave(chave), padrao);
    }

    public static void SetFloat(string chave, float valor)
    {
        PlayerPrefs.SetFloat(ObterChave(chave), valor);
    }

    public static string GetString(string chave, string padrao = "")
    {
        return PlayerPrefs.GetString(ObterChave(chave), padrao);
    }

    public static void SetString(string chave, string valor)
    {
        PlayerPrefs.SetString(ObterChave(chave), valor);
    }

    public static bool HasKey(string chave)
    {
        return PlayerPrefs.HasKey(ObterChave(chave));
    }

    public static void DeleteKey(string chave)
    {
        PlayerPrefs.DeleteKey(ObterChave(chave));
    }

    public static void Save()
    {
        PlayerPrefs.Save();
    }

    public static List<string> ObterListaPerfis()
    {
        string raw = PlayerPrefs.GetString(KEY_LISTA_PERFIS, "");
        List<string> lista = new List<string>();
        if (!string.IsNullOrEmpty(raw))
        {
            string[] split = raw.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in split)
            {
                string trimmed = s.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !lista.Contains(trimmed))
                    lista.Add(trimmed);
            }
        }
        return lista;
    }

    private static void SalvarListaPerfis(List<string> lista)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var nome in lista)
        {
            if (sb.Length > 0) sb.Append(";");
            sb.Append(nome.Trim());
        }
        PlayerPrefs.SetString(KEY_LISTA_PERFIS, sb.ToString());
        PlayerPrefs.Save();
    }

    public static void SelecionarPerfil(string nome)
    {
        if (string.IsNullOrEmpty(nome)) return;
        string trimmed = nome.Trim();
        List<string> perfis = ObterListaPerfis();
        if (!perfis.Contains(trimmed)) return;

        _perfilAtivo = trimmed;
        PlayerPrefs.SetString(KEY_PERFIL_ATIVO, _perfilAtivo);
        PlayerPrefs.Save();

        OnPerfilAlterado?.Invoke();
    }

    public static ResultadoCriacaoPerfil TentarCriarPerfil(string nome, out string perfilCriado)
    {
        perfilCriado = "";
        if (string.IsNullOrEmpty(nome)) return ResultadoCriacaoPerfil.NomeVazioOuCurto;
        string trimmed = nome.Trim();
        if (trimmed.Length < 2) return ResultadoCriacaoPerfil.NomeVazioOuCurto;

        List<string> perfis = ObterListaPerfis();
        string trimmedNorm = RemoverAcentos(trimmed).ToLowerInvariant();

        foreach (var p in perfis)
        {
            string pNorm = RemoverAcentos(p.Trim()).ToLowerInvariant();
            if (pNorm == trimmedNorm)
            {
                return ResultadoCriacaoPerfil.NomeJaExiste;
            }
        }

        perfis.Add(trimmed);
        SalvarListaPerfis(perfis);

        SelecionarPerfil(trimmed);
        perfilCriado = trimmed;
        return ResultadoCriacaoPerfil.Sucesso;
    }

    public static bool CriarPerfil(string nome)
    {
        return TentarCriarPerfil(nome, out _) == ResultadoCriacaoPerfil.Sucesso;
    }

    public static void ExcluirPerfil(string nome)
    {
        if (string.IsNullOrEmpty(nome)) return;
        string trimmed = nome.Trim();
        List<string> perfis = ObterListaPerfis();
        if (!perfis.Contains(trimmed)) return;

        perfis.Remove(trimmed);
        if (perfis.Count == 0)
        {
            perfis.Add("Aluno 1");
        }
        SalvarListaPerfis(perfis);

        LimparChavesDoPerfil(trimmed);

        if (_perfilAtivo == trimmed)
        {
            _perfilAtivo = perfis[0];
            PlayerPrefs.SetString(KEY_PERFIL_ATIVO, _perfilAtivo);
            PlayerPrefs.Save();
        }

        OnPerfilAlterado?.Invoke();
    }

    private static void LimparChavesDoPerfil(string nome)
    {
        string prefixo = $"P_{SanitizarNome(nome)}_";
        string[] materias = new string[] { "Matematica", "Portugues" };
        string[] niveis = new string[] { "Facil", "Medio", "Dificil" };

        foreach (var mat in materias)
        {
            foreach (var niv in niveis)
            {
                for (int f = 1; f <= 30; f++)
                {
                    PlayerPrefs.DeleteKey($"{prefixo}FaseCompleta_{mat}_{niv}_{f}");
                }
            }
        }

        PlayerPrefs.DeleteKey($"{prefixo}Desbloqueio_Medio_Math");
        PlayerPrefs.DeleteKey($"{prefixo}Desbloqueio_Dificil_Math");
        PlayerPrefs.DeleteKey($"{prefixo}Desbloqueio_Medio_Port");
        PlayerPrefs.DeleteKey($"{prefixo}Desbloqueio_Dificil_Port");
        PlayerPrefs.DeleteKey($"{prefixo}VidasSalvas");
        PlayerPrefs.DeleteKey($"{prefixo}TrofeusDesbloqueados");
        PlayerPrefs.Save();
    }

    public static string SanitizarNome(string nome)
    {
        if (string.IsNullOrEmpty(nome)) return "Padrao";
        StringBuilder sb = new StringBuilder();
        foreach (char c in nome)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (c == ' ' || c == '_' || c == '-')
                sb.Append('_');
        }
        string res = sb.ToString().Trim('_');
        return string.IsNullOrEmpty(res) ? "Aluno" : res;
    }

    public static int ObterTotalFasesConcluidas(string nomeAluno)
    {
        string prefixo = $"P_{SanitizarNome(nomeAluno)}_";
        int total = 0;
        string[] materias = new string[] { "Matematica", "Portugues" };
        string[] niveis = new string[] { "Facil", "Medio", "Dificil" };

        foreach (var mat in materias)
        {
            foreach (var niv in niveis)
            {
                for (int f = 1; f <= 30; f++)
                {
                    if (PlayerPrefs.GetInt($"{prefixo}FaseCompleta_{mat}_{niv}_{f}", 0) == 1)
                        total++;
                }
            }
        }
        return total;
    }

    private static void MigrarDadosLegadosSeNecessario()
    {
        const string MIGRADOS_KEY = "SistemaPerfis_DadosLegadosMigrados";
        if (PlayerPrefs.GetInt(MIGRADOS_KEY, 0) == 1) return;

        string alunoDestino = "Aluno 1";
        string prefixo = $"P_{SanitizarNome(alunoDestino)}_";
        string[] materias = new string[] { "Matematica", "Portugues" };
        string[] niveis = new string[] { "Facil", "Medio", "Dificil" };

        foreach (var mat in materias)
        {
            foreach (var niv in niveis)
            {
                for (int f = 1; f <= 30; f++)
                {
                    string oldKey = $"FaseCompleta_{mat}_{niv}_{f}";
                    if (PlayerPrefs.HasKey(oldKey))
                    {
                        PlayerPrefs.SetInt($"{prefixo}{oldKey}", PlayerPrefs.GetInt(oldKey));
                    }
                }
            }
        }

        string[] chavesGerais = new string[]
        {
            "Desbloqueio_Medio_Math", "Desbloqueio_Dificil_Math",
            "Desbloqueio_Medio_Port", "Desbloqueio_Dificil_Port",
            "VidasSalvas", "TrofeusDesbloqueados"
        };

        foreach (var k in chavesGerais)
        {
            if (PlayerPrefs.HasKey(k))
            {
                PlayerPrefs.SetInt($"{prefixo}{k}", PlayerPrefs.GetInt(k));
            }
        }

        PlayerPrefs.SetInt(MIGRADOS_KEY, 1);
        PlayerPrefs.Save();
    }

    // ==========================================
    // CARREGAMENTO SEGURO DE SPRITES E FONTES
    // ==========================================
    private void CarregarRecursosTema()
    {
        if (fontJogo == null)
            fontJogo = Resources.Load<TMP_FontAsset>("CrayonHandRegular2016Demo SDF");

        // Painel e Fechar
        spritePainelPergaminho = CarregarSprite(spritePainelPergaminho, "fundo_pergaminho_jogo", "Botoes/fundo_pergaminho_jogo.png", new Vector4(40, 40, 40, 40));
        spriteXFechar = CarregarSprite(spriteXFechar, "x_fechar_jogo", "Botoes/x_fechar_jogo.png", Vector4.zero);

        // Botoes com bordas SLIM (26, 10, 26, 10) para maxima visibilidade do texto
        spriteBtnJogar = CarregarSprite(spriteBtnJogar, "btn_verde_jogo", "Botoes/btn_verde_jogo.png", new Vector4(26, 10, 26, 10));
        spriteBtnContinuar = CarregarSprite(spriteBtnContinuar, "btn_azul_jogo", "Botoes/btn_azul_jogo.png", new Vector4(26, 10, 26, 10));
        spriteBtnLaranja = CarregarSprite(spriteBtnLaranja, "btn_laranja_jogo", "Botoes/btn_laranja_jogo.png", new Vector4(26, 10, 26, 10));
        spriteBtnVermelho = CarregarSprite(spriteBtnVermelho, "btn_vermelho_jogo", "Botoes/btn_vermelho_jogo.png", new Vector4(26, 10, 26, 10));

        // Cards, Pesquisa e Elementos de UI
        spriteCardFundo = CarregarSprite(spriteCardFundo, "card_aluno_fundo_jogo", "Botoes/card_aluno_fundo_jogo.png", new Vector4(30, 30, 30, 30));
        spriteCardAtivo = CarregarSprite(spriteCardAtivo, "card_aluno_ativo_jogo", "Botoes/card_aluno_ativo_jogo.png", new Vector4(30, 30, 30, 30));
        spriteBarraPesquisa = CarregarSprite(spriteBarraPesquisa, "barra_pesquisa_jogo", "Botoes/barra_pesquisa_jogo.png", new Vector4(25, 25, 25, 25));
        spriteBadgeTopo = CarregarSprite(spriteBadgeTopo, "badge_lobby_jogo", "Botoes/badge_lobby_jogo.png", new Vector4(30, 30, 30, 30));
        spriteAvatarKid = CarregarSprite(spriteAvatarKid, "avatar_aluno_jogo", "Botoes/avatar_aluno_jogo.png", Vector4.zero);
        spriteLixeira = CarregarSprite(spriteLixeira, "icone_lixeira_jogo", "Botoes/icone_lixeira_jogo.png", Vector4.zero);
        spriteLupa = CarregarSprite(spriteLupa, "icone_lupa_jogo", "Botoes/icone_lupa_jogo.png", Vector4.zero);
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
                byte[] bytes = File.ReadAllBytes(fullPath);
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(bytes))
                {
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                }
            }
        }
        catch { }
#endif

        return null;
    }

    // ==========================================
    // CRIACAO DA INTERFACE NO LOBBY
    // ==========================================
    private void CriarUILobby()
    {
        Action action = FindFirstObjectByType<Action>();
        Transform rootLobby = (action != null && action.groupLobby != null) ? action.groupLobby.transform : null;
        if (rootLobby == null)
        {
            GameObject lobbyGO = GameObject.Find("Principal");
            if (lobbyGO != null) rootLobby = lobbyGO.transform;
        }

        if (rootLobby == null) return;

        CriarBadgeLobby(rootLobby);
        CriarModalPerfis(rootLobby);

        OnPerfilAlterado -= AtualizarUICompleta;
        OnPerfilAlterado += AtualizarUICompleta;

        AtualizarUICompleta();
    }

    private void CriarBadgeLobby(Transform root)
    {
        Transform tBadge = root.Find("CardAlunoLobby");
        if (tBadge != null)
        {
            badgeLobby = tBadge.gameObject;
            textoBadgeAluno = badgeLobby.GetComponentInChildren<TextMeshProUGUI>();
            return;
        }

        badgeLobby = new GameObject("CardAlunoLobby", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        badgeLobby.transform.SetParent(root, false);

        RectTransform rt = badgeLobby.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -32f);
        rt.sizeDelta = new Vector2(560f, 72f);

        Image img = badgeLobby.GetComponent<Image>();
        if (spriteBadgeTopo != null)
        {
            img.sprite = spriteBadgeTopo;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = new Color(1f, 0.96f, 0.85f, 0.98f);
        }

        // Icone Avatar
        GameObject goIcon = new GameObject("IconeAluno", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        goIcon.transform.SetParent(badgeLobby.transform, false);
        RectTransform rtIcon = goIcon.GetComponent<RectTransform>();
        rtIcon.anchorMin = new Vector2(0f, 0.5f);
        rtIcon.anchorMax = new Vector2(0f, 0.5f);
        rtIcon.pivot = new Vector2(0f, 0.5f);
        rtIcon.anchoredPosition = new Vector2(14f, 0f);
        rtIcon.sizeDelta = new Vector2(52f, 52f);
        Image imgIcon = goIcon.GetComponent<Image>();
        if (spriteAvatarKid != null) imgIcon.sprite = spriteAvatarKid;
        imgIcon.color = (spriteAvatarKid != null) ? Color.white : new Color(0.9f, 0.7f, 0.3f, 1f);

        // Texto Aluno
        GameObject goText = new GameObject("TextoAluno", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goText.transform.SetParent(badgeLobby.transform, false);
        RectTransform rtText = goText.GetComponent<RectTransform>();
        rtText.anchorMin = new Vector2(0f, 0f);
        rtText.anchorMax = new Vector2(1f, 1f);
        rtText.offsetMin = new Vector2(76f, 0f);
        rtText.offsetMax = new Vector2(-160f, 0f);

        textoBadgeAluno = goText.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) textoBadgeAluno.font = fontJogo;
        textoBadgeAluno.text = $"Aluno: <b>{PerfilAtivo}</b>";
        textoBadgeAluno.fontSize = 24;
        textoBadgeAluno.alignment = TextAlignmentOptions.MidlineLeft;
        textoBadgeAluno.color = new Color(0.18f, 0.11f, 0.05f, 1f);

        // Botao Trocar Aluno
        GameObject goBtn = new GameObject("BtnTrocarAluno", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        goBtn.transform.SetParent(badgeLobby.transform, false);
        RectTransform rtBtn = goBtn.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(1f, 0.5f);
        rtBtn.anchorMax = new Vector2(1f, 0.5f);
        rtBtn.pivot = new Vector2(1f, 0.5f);
        rtBtn.anchoredPosition = new Vector2(-12f, 0f);
        rtBtn.sizeDelta = new Vector2(142f, 48f);

        Image imgBtn = goBtn.GetComponent<Image>();
        if (spriteBtnLaranja != null)
        {
            imgBtn.sprite = spriteBtnLaranja;
            imgBtn.type = Image.Type.Sliced;
            imgBtn.color = Color.white;
        }
        else
        {
            imgBtn.color = new Color(0.95f, 0.55f, 0.12f, 1f);
        }

        Button btn = goBtn.GetComponent<Button>();
        btn.onClick.AddListener(AbrirModalPerfis);

        GameObject goBtnText = new GameObject("TextoBtn", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goBtnText.transform.SetParent(goBtn.transform, false);
        RectTransform rtBtnText = goBtnText.GetComponent<RectTransform>();
        rtBtnText.anchorMin = Vector2.zero;
        rtBtnText.anchorMax = Vector2.one;
        rtBtnText.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpBtn = goBtnText.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) tmpBtn.font = fontJogo;
        tmpBtn.text = "TROCAR";
        tmpBtn.fontSize = 21;
        tmpBtn.fontStyle = FontStyles.Bold;
        tmpBtn.alignment = TextAlignmentOptions.Center;
        tmpBtn.color = Color.white;
        tmpBtn.outlineColor = new Color32(40, 20, 10, 220);
        tmpBtn.outlineWidth = 0.18f;
    }

    private void CriarModalPerfis(Transform root)
    {
        Transform tModal = root.Find("ModalPerfisAlunos");
        if (tModal != null)
        {
            modalPerfis = tModal.gameObject;
            return;
        }

        // Overlay escuro que bloqueia cliques
        modalPerfis = new GameObject("ModalPerfisAlunos", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        modalPerfis.transform.SetParent(root, false);

        RectTransform rtModal = modalPerfis.GetComponent<RectTransform>();
        rtModal.anchorMin = Vector2.zero;
        rtModal.anchorMax = Vector2.one;
        rtModal.offsetMin = Vector2.zero;
        rtModal.offsetMax = Vector2.zero;

        Image imgModal = modalPerfis.GetComponent<Image>();
        imgModal.color = new Color(0.08f, 0.05f, 0.03f, 0.82f);
        imgModal.raycastTarget = true;

        // Janela Central (Grande e espacosa: 1520 x 860)
        GameObject janela = new GameObject("JanelaDialogo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        janela.transform.SetParent(modalPerfis.transform, false);
        RectTransform rtJanela = janela.GetComponent<RectTransform>();
        rtJanela.anchorMin = new Vector2(0.5f, 0.5f);
        rtJanela.anchorMax = new Vector2(0.5f, 0.5f);
        rtJanela.pivot = new Vector2(0.5f, 0.5f);
        rtJanela.anchoredPosition = Vector2.zero;
        rtJanela.sizeDelta = new Vector2(1520f, 860f);

        Image imgJanela = janela.GetComponent<Image>();
        if (spritePainelPergaminho != null)
        {
            imgJanela.sprite = spritePainelPergaminho;
            imgJanela.type = Image.Type.Simple;
            imgJanela.color = Color.white;
        }
        else
        {
            imgJanela.color = new Color(1f, 0.94f, 0.75f, 1f);
        }

        // Titulo do Cabecalho
        GameObject goTitulo = new GameObject("Titulo", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTitulo.transform.SetParent(janela.transform, false);
        RectTransform rtTitulo = goTitulo.GetComponent<RectTransform>();
        rtTitulo.anchorMin = new Vector2(0.5f, 1f);
        rtTitulo.anchorMax = new Vector2(0.5f, 1f);
        rtTitulo.pivot = new Vector2(0.5f, 1f);
        rtTitulo.anchoredPosition = new Vector2(0f, -32f);
        rtTitulo.sizeDelta = new Vector2(1100f, 82f);

        TextMeshProUGUI tmpTitulo = goTitulo.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) tmpTitulo.font = fontJogo;
        tmpTitulo.text = "<size=40><b>QUEM VAI JOGAR HOJE?</b></size>\n<size=22><color=#5D4037>Selecione seu nome para continuar sua aventura</color></size>";
        tmpTitulo.alignment = TextAlignmentOptions.Center;
        tmpTitulo.color = new Color(0.18f, 0.11f, 0.05f, 1f);

        // Botao Fechar (X autentico do jogo)
        GameObject goFechar = new GameObject("BtnFechar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        goFechar.transform.SetParent(janela.transform, false);
        RectTransform rtFechar = goFechar.GetComponent<RectTransform>();
        rtFechar.anchorMin = new Vector2(1f, 1f);
        rtFechar.anchorMax = new Vector2(1f, 1f);
        rtFechar.pivot = new Vector2(1f, 1f);
        rtFechar.anchoredPosition = new Vector2(-55f, -48f);
        rtFechar.sizeDelta = new Vector2(52f, 52f);

        Image imgFechar = goFechar.GetComponent<Image>();
        if (spriteXFechar != null)
        {
            imgFechar.sprite = spriteXFechar;
            imgFechar.color = Color.white;
        }
        else
        {
            imgFechar.color = new Color(0.85f, 0.25f, 0.2f, 1f);
        }

        Button btnFechar = goFechar.GetComponent<Button>();
        btnFechar.onClick.AddListener(FecharModalPerfis);

        if (spriteXFechar == null)
        {
            GameObject goX = new GameObject("X", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            goX.transform.SetParent(goFechar.transform, false);
            RectTransform rtX = goX.GetComponent<RectTransform>();
            rtX.anchorMin = Vector2.zero;
            rtX.anchorMax = Vector2.one;
            rtX.sizeDelta = Vector2.zero;
            TextMeshProUGUI tmpX = goX.GetComponent<TextMeshProUGUI>();
            if (fontJogo != null) tmpX.font = fontJogo;
            tmpX.text = "X";
            tmpX.fontSize = 28;
            tmpX.fontStyle = FontStyles.Bold;
            tmpX.alignment = TextAlignmentOptions.Center;
            tmpX.color = Color.white;
        }

        // ==========================================
        // BARRA DE PESQUISA DE ALUNOS
        // ==========================================
        GameObject containerPesquisa = new GameObject("ContainerPesquisa", typeof(RectTransform));
        containerPesquisa.transform.SetParent(janela.transform, false);
        RectTransform rtPesqCont = containerPesquisa.GetComponent<RectTransform>();
        rtPesqCont.anchorMin = new Vector2(0.5f, 1f);
        rtPesqCont.anchorMax = new Vector2(0.5f, 1f);
        rtPesqCont.pivot = new Vector2(0.5f, 1f);
        rtPesqCont.anchoredPosition = new Vector2(0f, -125f);
        rtPesqCont.sizeDelta = new Vector2(1360f, 62f);

        // Input de Pesquisa
        GameObject goPesq = new GameObject("InputPesquisa", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        goPesq.transform.SetParent(containerPesquisa.transform, false);
        RectTransform rtPesq = goPesq.GetComponent<RectTransform>();
        rtPesq.anchorMin = new Vector2(0f, 0.5f);
        rtPesq.anchorMax = new Vector2(0f, 0.5f);
        rtPesq.pivot = new Vector2(0f, 0.5f);
        rtPesq.anchoredPosition = new Vector2(0f, 0f);
        rtPesq.sizeDelta = new Vector2(920f, 52f);

        Image imgPesq = goPesq.GetComponent<Image>();
        if (spriteBarraPesquisa != null)
        {
            imgPesq.sprite = spriteBarraPesquisa;
            imgPesq.type = Image.Type.Sliced;
            imgPesq.color = Color.white;
        }
        else
        {
            imgPesq.color = Color.white;
        }

        inputPesquisa = goPesq.GetComponent<TMP_InputField>();

        // Text Area do Input Pesquisa
        GameObject textAreaP = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textAreaP.transform.SetParent(goPesq.transform, false);
        RectTransform rtAreaP = textAreaP.GetComponent<RectTransform>();
        rtAreaP.anchorMin = Vector2.zero;
        rtAreaP.anchorMax = Vector2.one;
        rtAreaP.offsetMin = new Vector2(20f, 4f);
        rtAreaP.offsetMax = new Vector2(-60f, -4f);

        // Placeholder Pesquisa
        GameObject placeholderP = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        placeholderP.transform.SetParent(textAreaP.transform, false);
        RectTransform rtPlaceP = placeholderP.GetComponent<RectTransform>();
        rtPlaceP.anchorMin = Vector2.zero;
        rtPlaceP.anchorMax = Vector2.one;
        rtPlaceP.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpPlaceP = placeholderP.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) tmpPlaceP.font = fontJogo;
        tmpPlaceP.text = "🔍  Pesquisar aluno por nome...";
        tmpPlaceP.fontSize = 22;
        tmpPlaceP.fontStyle = FontStyles.Italic;
        tmpPlaceP.alignment = TextAlignmentOptions.MidlineLeft;
        tmpPlaceP.color = new Color(0.48f, 0.42f, 0.35f, 0.85f);

        // Texto Pesquisa
        GameObject textPesq = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textPesq.transform.SetParent(textAreaP.transform, false);
        RectTransform rtTextP = textPesq.GetComponent<RectTransform>();
        rtTextP.anchorMin = Vector2.zero;
        rtTextP.anchorMax = Vector2.one;
        rtTextP.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpTextP = textPesq.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) tmpTextP.font = fontJogo;
        tmpTextP.fontSize = 22;
        tmpTextP.alignment = TextAlignmentOptions.MidlineLeft;
        tmpTextP.color = new Color(0.18f, 0.11f, 0.05f, 1f);

        inputPesquisa.textViewport = rtAreaP;
        inputPesquisa.textComponent = tmpTextP;
        inputPesquisa.placeholder = tmpPlaceP;
        inputPesquisa.characterLimit = 30;
        inputPesquisa.onValueChanged.AddListener((val) =>
        {
            filtroPesquisaAtual = val;
            AtualizarListaCards();
        });

        // Botao Limpar Pesquisa (X)
        GameObject btnLimpar = new GameObject("BtnLimparPesquisa", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnLimpar.transform.SetParent(goPesq.transform, false);
        RectTransform rtLimpar = btnLimpar.GetComponent<RectTransform>();
        rtLimpar.anchorMin = new Vector2(1f, 0.5f);
        rtLimpar.anchorMax = new Vector2(1f, 0.5f);
        rtLimpar.pivot = new Vector2(1f, 0.5f);
        rtLimpar.anchoredPosition = new Vector2(-12f, 0f);
        rtLimpar.sizeDelta = new Vector2(32f, 32f);

        Image imgLimpar = btnLimpar.GetComponent<Image>();
        imgLimpar.color = new Color(0.8f, 0.3f, 0.2f, 0.7f);

        Button bLimpar = btnLimpar.GetComponent<Button>();
        bLimpar.onClick.AddListener(() =>
        {
            if (inputPesquisa != null)
            {
                inputPesquisa.text = "";
                inputPesquisa.ActivateInputField();
            }
        });

        GameObject txtLimpar = new GameObject("X", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtLimpar.transform.SetParent(btnLimpar.transform, false);
        RectTransform rtTL = txtLimpar.GetComponent<RectTransform>();
        rtTL.anchorMin = Vector2.zero;
        rtTL.anchorMax = Vector2.one;
        rtTL.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmpTL = txtLimpar.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) tmpTL.font = fontJogo;
        tmpTL.text = "✖";
        tmpTL.fontSize = 18;
        tmpTL.alignment = TextAlignmentOptions.Center;
        tmpTL.color = Color.white;

        // Texto Contador de Alunos
        GameObject goCont = new GameObject("ContadorAlunos", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goCont.transform.SetParent(containerPesquisa.transform, false);
        RectTransform rtCont = goCont.GetComponent<RectTransform>();
        rtCont.anchorMin = new Vector2(1f, 0.5f);
        rtCont.anchorMax = new Vector2(1f, 0.5f);
        rtCont.pivot = new Vector2(1f, 0.5f);
        rtCont.anchoredPosition = new Vector2(0f, 0f);
        rtCont.sizeDelta = new Vector2(400f, 52f);

        textoContadorAlunos = goCont.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) textoContadorAlunos.font = fontJogo;
        textoContadorAlunos.fontSize = 20;
        textoContadorAlunos.alignment = TextAlignmentOptions.MidlineRight;
        textoContadorAlunos.color = new Color(0.35f, 0.25f, 0.15f, 1f);

        // ==========================================
        // SCROLL VIEW ESPACOSO PARA LISTA DE ALUNOS
        // ==========================================
        GameObject scrollObj = new GameObject("ScrollViewAlunos", typeof(RectTransform), typeof(CanvasRenderer), typeof(ScrollRect));
        scrollObj.transform.SetParent(janela.transform, false);
        RectTransform rtScroll = scrollObj.GetComponent<RectTransform>();
        rtScroll.anchorMin = new Vector2(0f, 0f);
        rtScroll.anchorMax = new Vector2(1f, 1f);
        rtScroll.offsetMin = new Vector2(75f, 145f);
        rtScroll.offsetMax = new Vector2(-75f, -195f);

        ScrollRect sr = scrollObj.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.scrollSensitivity = 35f;

        // Viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObj.transform, false);
        RectTransform rtViewport = viewport.GetComponent<RectTransform>();
        rtViewport.anchorMin = Vector2.zero;
        rtViewport.anchorMax = Vector2.one;
        rtViewport.sizeDelta = Vector2.zero;
        sr.viewport = rtViewport;

        // Content
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform rtContent = content.GetComponent<RectTransform>();
        rtContent.anchorMin = new Vector2(0f, 1f);
        rtContent.anchorMax = new Vector2(1f, 1f);
        rtContent.pivot = new Vector2(0.5f, 1f);
        rtContent.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.spacing = 10f;
        vlg.padding = new RectOffset(6, 6, 6, 6);

        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = rtContent;

        containerCards = content.transform;

        // ==========================================
        // RODAPE DE CADASTRO DE NOVO ALUNO COM ALERTA
        // ==========================================
        GameObject rodape = new GameObject("RodapeNovoAluno", typeof(RectTransform));
        rodape.transform.SetParent(janela.transform, false);
        RectTransform rtRodape = rodape.GetComponent<RectTransform>();
        rtRodape.anchorMin = new Vector2(0.5f, 0f);
        rtRodape.anchorMax = new Vector2(0.5f, 0f);
        rtRodape.pivot = new Vector2(0.5f, 0f);
        rtRodape.anchoredPosition = new Vector2(0f, 35f);
        rtRodape.sizeDelta = new Vector2(1360f, 95f);

        // Mensagem de Aviso / Erro (ex: "Esse nome ja existe!")
        GameObject goAviso = new GameObject("AvisoNovoAluno", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goAviso.transform.SetParent(rodape.transform, false);
        RectTransform rtAviso = goAviso.GetComponent<RectTransform>();
        rtAviso.anchorMin = new Vector2(0f, 1f);
        rtAviso.anchorMax = new Vector2(0f, 1f);
        rtAviso.pivot = new Vector2(0f, 1f);
        rtAviso.anchoredPosition = new Vector2(10f, 0f);
        rtAviso.sizeDelta = new Vector2(1340f, 32f);

        textoAvisoNovoAluno = goAviso.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) textoAvisoNovoAluno.font = fontJogo;
        textoAvisoNovoAluno.fontSize = 20;
        textoAvisoNovoAluno.fontStyle = FontStyles.Bold;
        textoAvisoNovoAluno.alignment = TextAlignmentOptions.MidlineLeft;
        textoAvisoNovoAluno.color = new Color(0.88f, 0.15f, 0.12f, 1f); // Vermelho vibrante de alerta
        textoAvisoNovoAluno.text = "";

        // Input Field para Nome do Aluno
        GameObject goInput = new GameObject("InputNomeAluno", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        goInput.transform.SetParent(rodape.transform, false);
        RectTransform rtInput = goInput.GetComponent<RectTransform>();
        rtInput.anchorMin = new Vector2(0f, 0f);
        rtInput.anchorMax = new Vector2(0f, 0f);
        rtInput.pivot = new Vector2(0f, 0f);
        rtInput.anchoredPosition = new Vector2(0f, 4f);
        rtInput.sizeDelta = new Vector2(980f, 54f);

        Image imgInput = goInput.GetComponent<Image>();
        if (spriteBarraPesquisa != null)
        {
            imgInput.sprite = spriteBarraPesquisa;
            imgInput.type = Image.Type.Sliced;
            imgInput.color = Color.white;
        }
        else
        {
            imgInput.color = Color.white;
        }

        inputNomeNovoAluno = goInput.GetComponent<TMP_InputField>();

        // Text Area
        GameObject textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(goInput.transform, false);
        RectTransform rtTextArea = textArea.GetComponent<RectTransform>();
        rtTextArea.anchorMin = Vector2.zero;
        rtTextArea.anchorMax = Vector2.one;
        rtTextArea.offsetMin = new Vector2(20f, 4f);
        rtTextArea.offsetMax = new Vector2(-20f, -4f);

        // Placeholder
        GameObject placeholder = new GameObject("Placeholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        placeholder.transform.SetParent(textArea.transform, false);
        RectTransform rtPlaceholder = placeholder.GetComponent<RectTransform>();
        rtPlaceholder.anchorMin = Vector2.zero;
        rtPlaceholder.anchorMax = Vector2.one;
        rtPlaceholder.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpPlaceholder = placeholder.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) tmpPlaceholder.font = fontJogo;
        tmpPlaceholder.text = "Cadastrar novo aluno (ex: Mariana Souza 2º Ano)...";
        tmpPlaceholder.fontSize = 22;
        tmpPlaceholder.fontStyle = FontStyles.Italic;
        tmpPlaceholder.alignment = TextAlignmentOptions.MidlineLeft;
        tmpPlaceholder.color = new Color(0.48f, 0.42f, 0.35f, 0.85f);

        // Texto digitado
        GameObject textInput = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textInput.transform.SetParent(textArea.transform, false);
        RectTransform rtTextInput = textInput.GetComponent<RectTransform>();
        rtTextInput.anchorMin = Vector2.zero;
        rtTextInput.anchorMax = Vector2.one;
        rtTextInput.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpTextInput = textInput.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) tmpTextInput.font = fontJogo;
        tmpTextInput.fontSize = 22;
        tmpTextInput.alignment = TextAlignmentOptions.MidlineLeft;
        tmpTextInput.color = new Color(0.18f, 0.11f, 0.05f, 1f);

        inputNomeNovoAluno.textViewport = rtTextArea;
        inputNomeNovoAluno.textComponent = tmpTextInput;
        inputNomeNovoAluno.placeholder = tmpPlaceholder;
        inputNomeNovoAluno.characterLimit = 30;
        inputNomeNovoAluno.onSubmit.AddListener((val) => OnClickCriarNovoAluno());
        inputNomeNovoAluno.onValueChanged.AddListener((val) =>
        {
            if (textoAvisoNovoAluno != null && !string.IsNullOrEmpty(textoAvisoNovoAluno.text))
            {
                textoAvisoNovoAluno.text = "";
            }
        });

        // Botao Adicionar Aluno
        GameObject goBtnAdd = new GameObject("BtnAdicionarAluno", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        goBtnAdd.transform.SetParent(rodape.transform, false);
        RectTransform rtBtnAdd = goBtnAdd.GetComponent<RectTransform>();
        rtBtnAdd.anchorMin = new Vector2(1f, 0f);
        rtBtnAdd.anchorMax = new Vector2(1f, 0f);
        rtBtnAdd.pivot = new Vector2(1f, 0f);
        rtBtnAdd.anchoredPosition = new Vector2(0f, 4f);
        rtBtnAdd.sizeDelta = new Vector2(350f, 54f);

        Image imgBtnAdd = goBtnAdd.GetComponent<Image>();
        if (spriteBtnLaranja != null)
        {
            imgBtnAdd.sprite = spriteBtnLaranja;
            imgBtnAdd.type = Image.Type.Sliced;
            imgBtnAdd.color = Color.white;
        }
        else
        {
            imgBtnAdd.color = new Color(0.95f, 0.55f, 0.12f, 1f);
        }

        Button btnAdd = goBtnAdd.GetComponent<Button>();
        btnAdd.onClick.AddListener(OnClickCriarNovoAluno);

        GameObject goTextAdd = new GameObject("TextoAdd", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goTextAdd.transform.SetParent(goBtnAdd.transform, false);
        RectTransform rtTextAdd = goTextAdd.GetComponent<RectTransform>();
        rtTextAdd.anchorMin = Vector2.zero;
        rtTextAdd.anchorMax = Vector2.one;
        rtTextAdd.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpAdd = goTextAdd.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) tmpAdd.font = fontJogo;
        tmpAdd.text = "+ CADASTRAR ALUNO";
        tmpAdd.fontSize = 22;
        tmpAdd.fontStyle = FontStyles.Bold;
        tmpAdd.alignment = TextAlignmentOptions.Center;
        tmpAdd.color = Color.white;
        tmpAdd.outlineColor = new Color32(40, 20, 10, 220);
        tmpAdd.outlineWidth = 0.18f;

        modalPerfis.SetActive(false);
    }

    public void AbrirModalPerfis()
    {
        if (modalPerfis != null)
        {
            modalPerfis.SetActive(true);
            filtroPesquisaAtual = "";
            alunoEmConfirmacaoExclusao = "";
            if (textoAvisoNovoAluno != null) textoAvisoNovoAluno.text = "";
            if (inputPesquisa != null) inputPesquisa.text = "";
            if (inputNomeNovoAluno != null) inputNomeNovoAluno.text = "";
            AtualizarListaCards();
        }
    }

    public void FecharModalPerfis()
    {
        if (modalPerfis != null)
        {
            modalPerfis.SetActive(false);
            alunoEmConfirmacaoExclusao = "";
            if (textoAvisoNovoAluno != null) textoAvisoNovoAluno.text = "";
        }
    }

    private void OnClickCriarNovoAluno()
    {
        if (inputNomeNovoAluno == null) return;
        string nome = inputNomeNovoAluno.text.Trim();

        ResultadoCriacaoPerfil res = TentarCriarPerfil(nome, out string criado);

        if (res == ResultadoCriacaoPerfil.NomeJaExiste)
        {
            if (textoAvisoNovoAluno != null)
            {
                textoAvisoNovoAluno.text = $"⚠️ Esse nome já existe! Adicione a turma ou sobrenome (ex: {nome} 2º Ano)";
            }
            inputNomeNovoAluno.ActivateInputField();
            return;
        }
        else if (res == ResultadoCriacaoPerfil.NomeVazioOuCurto)
        {
            if (textoAvisoNovoAluno != null)
            {
                textoAvisoNovoAluno.text = "⚠️ Digite pelo menos 2 letras para o nome do aluno!";
            }
            inputNomeNovoAluno.ActivateInputField();
            return;
        }

        // Sucesso!
        if (textoAvisoNovoAluno != null) textoAvisoNovoAluno.text = "";
        inputNomeNovoAluno.text = "";
        filtroPesquisaAtual = "";
        if (inputPesquisa != null) inputPesquisa.text = "";
        FecharModalPerfis();
    }

    public void AtualizarUICompleta()
    {
        if (textoBadgeAluno != null)
        {
            textoBadgeAluno.text = $"Aluno: <b>{PerfilAtivo}</b>";
        }

        if (modalPerfis != null && modalPerfis.activeSelf)
        {
            AtualizarListaCards();
        }

        Action action = FindFirstObjectByType<Action>();
        if (action != null)
        {
            action.VerificarDesbloqueios();
        }

        GerenciadorFases gf = FindFirstObjectByType<GerenciadorFases>();
        if (gf != null)
        {
            gf.DefinirNivel(gf.nivelAtual);
        }
    }

    private static string RemoverAcentos(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";
        string normalized = texto.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();
        foreach (char c in normalized)
        {
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private void AtualizarListaCards()
    {
        if (containerCards == null) return;

        for (int i = containerCards.childCount - 1; i >= 0; i--)
        {
            Destroy(containerCards.GetChild(i).gameObject);
        }

        List<string> perfis = ObterListaPerfis();
        string filtroNormalizado = RemoverAcentos(filtroPesquisaAtual.Trim()).ToLowerInvariant();

        List<string> filtrados = new List<string>();
        foreach (var p in perfis)
        {
            if (string.IsNullOrEmpty(filtroNormalizado))
            {
                filtrados.Add(p);
            }
            else
            {
                string nomeNorm = RemoverAcentos(p).ToLowerInvariant();
                if (nomeNorm.Contains(filtroNormalizado))
                {
                    filtrados.Add(p);
                }
            }
        }

        // Atualizar texto do contador
        if (textoContadorAlunos != null)
        {
            if (string.IsNullOrEmpty(filtroNormalizado))
            {
                textoContadorAlunos.text = $"<b>{perfis.Count}</b> aluno(s) cadastrado(s)";
            }
            else
            {
                textoContadorAlunos.text = $"Exibindo <b>{filtrados.Count}</b> de {perfis.Count} aluno(s)";
            }
        }

        // Se nenhum aluno encontrado pelo filtro
        if (filtrados.Count == 0)
        {
            GameObject aviso = new GameObject("AvisoVazio", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            aviso.transform.SetParent(containerCards, false);
            RectTransform rtAviso = aviso.GetComponent<RectTransform>();
            rtAviso.sizeDelta = new Vector2(1300f, 100f);

            TextMeshProUGUI tmpAviso = aviso.GetComponent<TextMeshProUGUI>();
            if (fontJogo != null) tmpAviso.font = fontJogo;
            tmpAviso.text = $"<size=26><color=#795548>Nenhum aluno encontrado para \"{filtroPesquisaAtual}\".</color></size>\n<size=20><color=#D84315>Você pode cadastrar este aluno no campo abaixo!</color></size>";
            tmpAviso.alignment = TextAlignmentOptions.Center;
            return;
        }

        foreach (var p in filtrados)
        {
            CriarCardAluno(p);
        }
    }

    private void CriarCardAluno(string nome)
    {
        GameObject card = new GameObject($"Card_{nome}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(containerCards, false);

        RectTransform rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(1340f, 88f);

        Image img = card.GetComponent<Image>();
        bool isAtivo = (nome == PerfilAtivo);
        Sprite cardSprite = isAtivo ? spriteCardAtivo : spriteCardFundo;

        if (cardSprite != null)
        {
            img.sprite = cardSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
        }
        else
        {
            img.color = isAtivo ? new Color(1f, 0.94f, 0.78f, 1f) : new Color(1f, 0.98f, 0.92f, 1f);
        }

        // Icone Avatar
        GameObject icon = new GameObject("Icone", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        icon.transform.SetParent(card.transform, false);
        RectTransform rtIcon = icon.GetComponent<RectTransform>();
        rtIcon.anchorMin = new Vector2(0f, 0.5f);
        rtIcon.anchorMax = new Vector2(0f, 0.5f);
        rtIcon.pivot = new Vector2(0f, 0.5f);
        rtIcon.anchoredPosition = new Vector2(18f, 0f);
        rtIcon.sizeDelta = new Vector2(56f, 56f);
        Image imgIcon = icon.GetComponent<Image>();
        if (spriteAvatarKid != null) imgIcon.sprite = spriteAvatarKid;
        imgIcon.color = (spriteAvatarKid != null) ? Color.white : new Color(0.95f, 0.75f, 0.35f, 1f);

        // Textos Nome e Progresso
        GameObject goInfo = new GameObject("Info", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        goInfo.transform.SetParent(card.transform, false);
        RectTransform rtInfo = goInfo.GetComponent<RectTransform>();
        rtInfo.anchorMin = new Vector2(0f, 0f);
        rtInfo.anchorMax = new Vector2(1f, 1f);
        rtInfo.offsetMin = new Vector2(90f, 0f);
        rtInfo.offsetMax = new Vector2(-360f, 0f);

        int concluidas = ObterTotalFasesConcluidas(nome);
        string statusTag = isAtivo ? "   <color=#E65100><b>[JOGANDO AGORA]</b></color>" : "";
        string textoProgresso = concluidas == 0 ? "<color=#8D6E63>Novo aluno   •   Nenhuma fase jogada ainda</color>" : $"<color=#2E7D32><b>⭐ {concluidas} fases concluídas</b></color>";

        TextMeshProUGUI tmpInfo = goInfo.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) tmpInfo.font = fontJogo;
        tmpInfo.text = $"<size=26><b>{nome}</b></size>{statusTag}\n<size=18>{textoProgresso}</size>";
        tmpInfo.alignment = TextAlignmentOptions.MidlineLeft;
        tmpInfo.color = new Color(0.18f, 0.11f, 0.05f, 1f);

        // Botao Selecionar / Jogar (Com borda slim e texto nitido)
        GameObject btnJogar = new GameObject("BtnJogar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnJogar.transform.SetParent(card.transform, false);
        RectTransform rtJogar = btnJogar.GetComponent<RectTransform>();
        rtJogar.anchorMin = new Vector2(1f, 0.5f);
        rtJogar.anchorMax = new Vector2(1f, 0.5f);
        rtJogar.pivot = new Vector2(1f, 0.5f);
        rtJogar.anchoredPosition = new Vector2(-160f, 0f);
        rtJogar.sizeDelta = new Vector2(180f, 52f);

        Image imgJogar = btnJogar.GetComponent<Image>();
        Sprite sprBotaoAcao = isAtivo ? (spriteBtnContinuar != null ? spriteBtnContinuar : spriteBtnJogar) : spriteBtnJogar;
        if (sprBotaoAcao != null)
        {
            imgJogar.sprite = sprBotaoAcao;
            imgJogar.type = Image.Type.Sliced;
            imgJogar.color = Color.white;
        }
        else
        {
            imgJogar.color = isAtivo ? new Color(0.15f, 0.55f, 0.9f, 1f) : new Color(0.18f, 0.72f, 0.35f, 1f);
        }

        Button bJogar = btnJogar.GetComponent<Button>();
        bJogar.onClick.AddListener(() =>
        {
            SelecionarPerfil(nome);
            FecharModalPerfis();
        });

        GameObject txtJogar = new GameObject("Texto", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtJogar.transform.SetParent(btnJogar.transform, false);
        RectTransform rtTxtJogar = txtJogar.GetComponent<RectTransform>();
        rtTxtJogar.anchorMin = Vector2.zero;
        rtTxtJogar.anchorMax = Vector2.one;
        rtTxtJogar.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmpJogar = txtJogar.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) tmpJogar.font = fontJogo;
        tmpJogar.text = isAtivo ? "CONTINUAR" : "JOGAR";
        tmpJogar.fontSize = 21;
        tmpJogar.fontStyle = FontStyles.Bold;
        tmpJogar.alignment = TextAlignmentOptions.Center;
        tmpJogar.color = Color.white;
        tmpJogar.outlineColor = new Color32(20, 30, 20, 220);
        tmpJogar.outlineWidth = 0.18f;

        // Botao Excluir com Confirmacao Segura
        GameObject btnDel = new GameObject("BtnExcluir", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnDel.transform.SetParent(card.transform, false);
        RectTransform rtDel = btnDel.GetComponent<RectTransform>();
        rtDel.anchorMin = new Vector2(1f, 0.5f);
        rtDel.anchorMax = new Vector2(1f, 0.5f);
        rtDel.pivot = new Vector2(1f, 0.5f);
        rtDel.anchoredPosition = new Vector2(-15f, 0f);
        rtDel.sizeDelta = new Vector2(135f, 50f);

        Image imgDel = btnDel.GetComponent<Image>();
        bool emConfirmacao = (alunoEmConfirmacaoExclusao == nome);

        if (spriteBtnVermelho != null)
        {
            imgDel.sprite = spriteBtnVermelho;
            imgDel.type = Image.Type.Sliced;
            imgDel.color = Color.white;
        }
        else
        {
            imgDel.color = emConfirmacao ? new Color(0.95f, 0.2f, 0.15f, 1f) : new Color(0.85f, 0.35f, 0.3f, 0.9f);
        }

        Button bDel = btnDel.GetComponent<Button>();
        bDel.onClick.AddListener(() =>
        {
            if (alunoEmConfirmacaoExclusao == nome)
            {
                alunoEmConfirmacaoExclusao = "";
                ExcluirPerfil(nome);
            }
            else
            {
                alunoEmConfirmacaoExclusao = nome;
                tempoConfirmacaoExclusao = 4f; // 4 segundos para confirmar ou cancela
                AtualizarListaCards();
            }
        });

        GameObject txtDel = new GameObject("TextoDel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtDel.transform.SetParent(btnDel.transform, false);
        RectTransform rtTxtDel = txtDel.GetComponent<RectTransform>();
        rtTxtDel.anchorMin = Vector2.zero;
        rtTxtDel.anchorMax = Vector2.one;
        rtTxtDel.sizeDelta = Vector2.zero;
        TextMeshProUGUI tmpDel = txtDel.GetComponent<TextMeshProUGUI>();
        if (fontJogo != null) tmpDel.font = fontJogo;
        tmpDel.text = emConfirmacao ? "CERTEZA?" : "EXCLUIR";
        tmpDel.fontSize = 18;
        tmpDel.fontStyle = FontStyles.Bold;
        tmpDel.alignment = TextAlignmentOptions.Center;
        tmpDel.color = Color.white;
        tmpDel.outlineColor = new Color32(40, 15, 15, 220);
        tmpDel.outlineWidth = 0.18f;
    }
}
