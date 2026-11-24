using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Recompensa do jogo: "Resgate das Letrinhas".
/// As letrinhas fugiram (é a revolta!) e o aluno as resgata com as estrelas que ganha nas fases
/// (1 a 3 estrelas por fase, pela quantidade de vidas que sobraram). Cada letrinha resgatada entra no álbum.
///
/// As estrelas são calculadas a partir das fases concluídas, que já vão para o servidor e voltam em
/// qualquer computador da escola: o álbum acompanha o aluno sem precisar de nada novo no servidor.
/// Fase resolvida pelo professor ("Auto-completar") não dá estrelas.
/// </summary>
public static class ResgateLetrinhas
{
    public struct Letrinha
    {
        public string letra;
        public string palavra;
        public Color cor;
    }

    public static readonly Letrinha[] Letras = Criar(new[]
    {
        "A:ABELHA:#F59E0B", "B:BOLA:#EF4444", "C:CASA:#10B981", "D:DADO:#3B82F6", "E:ELEFANTE:#8B5CF6", "F:FOCA:#EC4899",
        "G:GATO:#F97316", "H:HIPOPÓTAMO:#14B8A6", "I:ILHA:#6366F1", "J:JACARÉ:#22C55E", "K:KIWI:#84CC16", "L:LEÃO:#EAB308",
        "M:MACACO:#A855F7", "N:NAVIO:#0EA5E9", "O:OVELHA:#F43F5E", "P:PIPOCA:#D97706", "Q:QUEIJO:#CA8A04", "R:RATO:#64748B",
        "S:SAPO:#16A34A", "T:TARTARUGA:#0D9488", "U:URSO:#B45309", "V:VACA:#DB2777", "W:WAFFLE:#C2410C", "X:XÍCARA:#7C3AED",
        "Y:YAKISOBA:#E11D48", "Z:ZEBRA:#334155"
    });

    private static Letrinha[] Criar(string[] dados)
    {
        Letrinha[] r = new Letrinha[dados.Length];
        for (int i = 0; i < dados.Length; i++)
        {
            string[] p = dados[i].Split(':');
            r[i] = new Letrinha { letra = p[0], palavra = p[1], cor = InterfaceUtil.Hex(p[2]) };
        }
        return r;
    }

    /// <summary>
    /// Estrelas acumuladas necessárias para resgatar a letrinha i (0 = A ... 25 = Z).
    /// As primeiras saem rápido (a 1ª com uma fase) e as últimas pedem quase o jogo todo (máximo: 540 estrelas).
    /// </summary>
    public static int Limiar(int i) => Mathf.RoundToInt(3f + 497f * Mathf.Pow(i / 25f, 1.6f));

    public static string ChaveResolvidaProfessor(string materia, int nivel, int fase) => $"ResolvidaProfessor_{materia}_{nivel}_{fase}";

    /// <summary>Estrelas do aluno: soma das fases concluídas (3 sem erros, 2 com um erro).</summary>
    public static int EstrelasDoAluno(string nomeAluno)
    {
        if (string.IsNullOrEmpty(nomeAluno)) return 0;
        string pfx = $"P_{GerenciadorPerfis.SanitizarNome(nomeAluno)}_";
        int total = 0;
        foreach (string mat in new[] { "Port", "Math" })
        {
            for (int nivel = 0; nivel < 3; nivel++)
            {
                for (int fase = 0; fase < 30; fase++)
                {
                    if (PlayerPrefs.GetInt($"{pfx}FaseCompleta_{mat}_{nivel}_{fase}", 0) != 1) continue;
                    if (PlayerPrefs.GetInt(pfx + ChaveResolvidaProfessor(mat, nivel, fase), 0) == 1) continue;
                    int vidas = Mathf.Clamp(PlayerPrefs.GetInt($"{pfx}VidasRestantes_{mat}_{nivel}_{fase}", 2), 1, 2);
                    total += vidas + 1;
                }
            }
        }
        return total;
    }

    /// <summary>Troféus = níveis com as 30 fases concluídas (como o servidor confere ao resgatar um personagem).</summary>
    public static int TrofeusDoAluno(string nomeAluno)
    {
        if (string.IsNullOrEmpty(nomeAluno)) return 0;
        string pfx = $"P_{GerenciadorPerfis.SanitizarNome(nomeAluno)}_";
        int trofeus = 0;
        foreach (string mat in new[] { "Port", "Math" })
        {
            for (int nivel = 0; nivel < 3; nivel++)
            {
                bool completo = true;
                for (int fase = 0; fase < 30 && completo; fase++)
                {
                    if (PlayerPrefs.GetInt($"{pfx}FaseCompleta_{mat}_{nivel}_{fase}", 0) != 1 ||
                        PlayerPrefs.GetInt(pfx + ChaveResolvidaProfessor(mat, nivel, fase), 0) == 1) completo = false;
                }
                if (completo) trofeus++;
            }
        }
        return trofeus;
    }

    public static int Resgatadas(int estrelas)
    {
        int n = 0;
        while (n < Letras.Length && Limiar(n) <= estrelas) n++;
        return n;
    }

    /// <summary>Estrelas que faltam para a próxima letrinha (0 se já tem todas).</summary>
    public static int FaltamParaProxima(int estrelas)
    {
        int n = Resgatadas(estrelas);
        return n >= Letras.Length ? 0 : Limiar(n) - estrelas;
    }
}

/// <summary>Texto do botão do álbum no Lobby: estrelas e letrinhas do aluno que está jogando.</summary>
public class ResumoAlbumLobby : MonoBehaviour
{
    public TextMeshProUGUI texto;
    private float proxima;
    private string ultimoAluno;
    private int ultimasEstrelas = -1;

    private void OnEnable() => proxima = 0f;

    private void Update()
    {
        if (texto == null || Time.unscaledTime < proxima) return;
        proxima = Time.unscaledTime + 1.5f;
        string aluno = GerenciadorPerfis.PerfilAtivo;
        int estrelas = ResgateLetrinhas.EstrelasDoAluno(aluno);
        if (aluno == ultimoAluno && estrelas == ultimasEstrelas) return;
        ultimoAluno = aluno;
        ultimasEstrelas = estrelas;
        texto.text = $"{estrelas} estrelas  •  {ResgateLetrinhas.Resgatadas(estrelas)} de 26 letrinhas";
    }
}

/// <summary>Telas da recompensa: álbum, aviso "+estrelas" e letrinha resgatada.</summary>
public class TelaLetrinhas : MonoBehaviour
{
    private static TelaLetrinhas _instancia;

    // Acima das telas do jogo (até 500) e abaixo da Área do Professor (998) e do vídeo explicativo (1000)
    private const int ORDEM = 990;

    private GameObject album;
    private TextMeshProUGUI txtAlbumSubtitulo;
    private TextMeshProUGUI txtAlbumProxima;
    private RectTransform barraProxima;
    private readonly Image[] cartas = new Image[26];
    private readonly TextMeshProUGUI[] cartasLetra = new TextMeshProUGUI[26];
    private readonly TextMeshProUGUI[] cartasPalavra = new TextMeshProUGUI[26];
    private readonly Image[] cartasFigura = new Image[26];
    private Image resgateFigura;

    private GameObject aviso;
    private CanvasGroup avisoGrupo;
    private TextMeshProUGUI txtAviso;

    private GameObject resgate;
    private Image resgateCarta;
    private TextMeshProUGUI resgateLetra;
    private TextMeshProUGUI resgatePalavra;
    private TextMeshProUGUI resgateTitulo;
    private TextMeshProUGUI resgateRodape;

    public static bool AlbumAberto => _instancia != null && _instancia.album != null && _instancia.album.activeSelf;
    public static bool ResgateAberto => _instancia != null && _instancia.resgate != null && _instancia.resgate.activeSelf;

    private static TelaLetrinhas Obter()
    {
        if (_instancia == null)
        {
            GameObject go = InterfaceUtil.CriarCanvas("[TelaLetrinhas]", ORDEM);
            _instancia = go.AddComponent<TelaLetrinhas>();
            _instancia.Construir();
        }
        return _instancia;
    }

    // ================= API =================

    public static void AbrirAlbum()
    {
        TelaLetrinhas t = Obter();
        t.PreencherAlbum(GerenciadorPerfis.PerfilAtivo);
        t.album.SetActive(true);
    }

    public static void FecharAlbum()
    {
        if (_instancia != null && _instancia.album != null) _instancia.album.SetActive(false);
    }

    /// <summary>Chamado ao concluir uma fase: mostra as estrelas ganhas e, se for o caso, a letrinha resgatada.</summary>
    public static void MostrarRecompensa(string nomeAluno, int estrelasAntes, int estrelasDepois)
    {
        int ganhas = estrelasDepois - estrelasAntes;
        if (ganhas <= 0) return;
        Obter().StartCoroutine(_instancia.RecompensaCoroutine(nomeAluno, estrelasAntes, estrelasDepois));
    }

    // ================= Comportamento =================

    private IEnumerator RecompensaCoroutine(string nomeAluno, int antes, int depois)
    {
        // Espera a tela de "fase concluída" aparecer
        yield return new WaitForSecondsRealtime(1.4f);

        int resgatadasAntes = ResgateLetrinhas.Resgatadas(antes);
        int resgatadasDepois = ResgateLetrinhas.Resgatadas(depois);
        int ganhas = depois - antes;

        if (resgatadasDepois > resgatadasAntes)
        {
            MostrarResgate(resgatadasDepois - 1, resgatadasDepois);
            yield break;
        }

        int faltam = ResgateLetrinhas.FaltamParaProxima(depois);
        string proxima = resgatadasDepois < 26 ? ResgateLetrinhas.Letras[resgatadasDepois].letra : "";
        txtAviso.text = $"<size=120%><b>+{ganhas} {(ganhas == 1 ? "estrela" : "estrelas")}!</b></size>\n" +
                        (faltam > 0 ? $"Faltam {faltam} para resgatar a letrinha <b>{proxima}</b>." : "Você já resgatou todas as letrinhas!");
        aviso.SetActive(true);
        avisoGrupo.alpha = 1f;
        yield return new WaitForSecondsRealtime(3.5f);
        for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            avisoGrupo.alpha = 1f - t / 0.5f;
            yield return null;
        }
        aviso.SetActive(false);
    }

    private void MostrarResgate(int indice, int total)
    {
        ResgateLetrinhas.Letrinha l = ResgateLetrinhas.Letras[indice];
        resgateCarta.color = l.cor;
        resgateLetra.text = l.letra + l.letra.ToLowerInvariant();
        resgatePalavra.text = l.palavra;
        resgateTitulo.text = $"Você resgatou a letrinha {l.letra}!";
        resgateRodape.text = total >= 26 ? "Você resgatou TODAS as letrinhas! Parabéns!" : $"Agora você tem {total} de 26 letrinhas no álbum.";
        resgateFigura.gameObject.SetActive(false);
        ImagensRemotas.DesenhoDaPalavra(l.palavra, s => { if (s != null) { resgateFigura.sprite = s; resgateFigura.gameObject.SetActive(true); } });
        resgate.SetActive(true);
        LeitorDeVoz.Falar($"Você resgatou a letrinha {l.letra}! {l.letra} de {l.palavra.ToLowerInvariant()}!");
    }

    private void PreencherAlbum(string nomeAluno)
    {
        int estrelas = ResgateLetrinhas.EstrelasDoAluno(nomeAluno);
        int resgatadas = ResgateLetrinhas.Resgatadas(estrelas);
        txtAlbumSubtitulo.text = $"<b>{nomeAluno}</b>  •  {estrelas} estrelas  •  {resgatadas} de 26 letrinhas resgatadas";

        if (resgatadas >= 26)
        {
            txtAlbumProxima.text = "Você resgatou todas as letrinhas! Que orgulho!";
            barraProxima.anchorMax = new Vector2(1f, 1f);
        }
        else
        {
            int inicio = resgatadas == 0 ? 0 : ResgateLetrinhas.Limiar(resgatadas - 1);
            int fim = ResgateLetrinhas.Limiar(resgatadas);
            float p = Mathf.Clamp01((estrelas - inicio) / (float)Mathf.Max(1, fim - inicio));
            barraProxima.anchorMax = new Vector2(Mathf.Max(0.03f, p), 1f);
            int faltam = fim - estrelas;
            txtAlbumProxima.text = $"Próxima: letrinha <b>{ResgateLetrinhas.Letras[resgatadas].letra}</b>  —  faltam {faltam} {(faltam == 1 ? "estrela" : "estrelas")}";
        }

        for (int i = 0; i < 26; i++)
        {
            bool tem = i < resgatadas;
            ResgateLetrinhas.Letrinha l = ResgateLetrinhas.Letras[i];
            cartas[i].color = tem ? l.cor : new Color(0.82f, 0.84f, 0.88f);
            cartasLetra[i].text = tem ? l.letra + l.letra.ToLowerInvariant() : "?";
            cartasLetra[i].color = tem ? Color.white : new Color(0.55f, 0.58f, 0.64f);
            cartasPalavra[i].text = tem ? l.palavra : $"{ResgateLetrinhas.Limiar(i)} estrelas";
            cartasPalavra[i].color = tem ? Color.white : new Color(0.45f, 0.48f, 0.55f);
            Image fig = cartasFigura[i];
            fig.gameObject.SetActive(false);
            if (tem) ImagensRemotas.DesenhoDaPalavra(l.palavra, s => { if (s != null) { fig.sprite = s; fig.gameObject.SetActive(true); } });
        }
    }

    // ================= Construção =================

    private void Construir()
    {
        ConstruirAlbum();
        ConstruirAviso();
        ConstruirResgate();
    }

    private void ConstruirAlbum()
    {
        RectTransform raiz = InterfaceUtil.NovoRect("Album", transform);
        InterfaceUtil.Esticar(raiz, 0, 0, 0, 0);
        Image fundo = raiz.gameObject.AddComponent<Image>();
        fundo.color = new Color(0.05f, 0.07f, 0.14f, 0.88f);
        album = raiz.gameObject;

        Image card = InterfaceUtil.Caixa(raiz, "Cartao", new Color(1f, 0.98f, 0.93f));
        InterfaceUtil.Fixar(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1640f, 960f));

        TextMeshProUGUI titulo = InterfaceUtil.Texto(card.transform, "Álbum das Letrinhas", 64, InterfaceUtil.Hex("#7C2D12"), TextAlignmentOptions.Center, true);
        InterfaceUtil.Posicionar(titulo.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -110), new Vector2(-40, -20));

        txtAlbumSubtitulo = InterfaceUtil.Texto(card.transform, "", 28, InterfaceUtil.Hex("#44403C"), TextAlignmentOptions.Center);
        InterfaceUtil.Posicionar(txtAlbumSubtitulo.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -160), new Vector2(-40, -112));

        TextMeshProUGUI explicacao = InterfaceUtil.Texto(card.transform,
            "As letrinhas fugiram! Cada fase vale até 3 estrelas (menos erros, mais estrelas). Junte estrelas para resgatar as letrinhas.",
            22, InterfaceUtil.Hex("#57534E"), TextAlignmentOptions.Center);
        InterfaceUtil.Posicionar(explicacao.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(80, -200), new Vector2(-80, -164));

        // Barra até a próxima letrinha
        Image barra = InterfaceUtil.Caixa(card.transform, "BarraProxima", InterfaceUtil.Hex("#E7E5E4"));
        InterfaceUtil.Fixar(barra.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -232), new Vector2(760f, 26f));
        Image preench = InterfaceUtil.Caixa(barra.transform, "Preenchimento", InterfaceUtil.Hex("#F59E0B"));
        barraProxima = preench.rectTransform;
        barraProxima.anchorMin = Vector2.zero;
        barraProxima.anchorMax = new Vector2(0.03f, 1f);
        barraProxima.offsetMin = Vector2.zero;
        barraProxima.offsetMax = Vector2.zero;
        Image est = InterfaceUtil.Estrela(barra.transform, InterfaceUtil.Hex("#F59E0B"), 54f);
        InterfaceUtil.Fixar(est.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f), new Vector2(54f, 54f));

        txtAlbumProxima = InterfaceUtil.Texto(card.transform, "", 24, InterfaceUtil.Hex("#44403C"), TextAlignmentOptions.Center);
        InterfaceUtil.Posicionar(txtAlbumProxima.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -290), new Vector2(-40, -252));

        // Grade de 26 cartas (9 por linha)
        RectTransform grade = InterfaceUtil.NovoRect("Grade", card.transform);
        InterfaceUtil.Posicionar(grade, new Vector2(0, 0), new Vector2(1, 1), new Vector2(60, 120), new Vector2(-60, -305));
        GridLayoutGroup g = grade.gameObject.AddComponent<GridLayoutGroup>();
        g.cellSize = new Vector2(154f, 154f);
        g.spacing = new Vector2(12f, 14f);
        g.childAlignment = TextAnchor.MiddleCenter;
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = 9;

        for (int i = 0; i < 26; i++)
        {
            Image c = InterfaceUtil.Caixa(grade, "Carta_" + ResgateLetrinhas.Letras[i].letra, Color.gray);
            cartas[i] = c;
            cartasLetra[i] = InterfaceUtil.Texto(c.transform, "?", 70, Color.white, TextAlignmentOptions.Center, true);
            InterfaceUtil.Posicionar(cartasLetra[i].rectTransform, new Vector2(0, 0.28f), new Vector2(1, 1), new Vector2(4, 0), new Vector2(-4, -4));
            cartasPalavra[i] = InterfaceUtil.Texto(c.transform, "", 19, Color.white, TextAlignmentOptions.Center);
            cartasPalavra[i].fontStyle = FontStyles.Bold;
            cartasPalavra[i].enableAutoSizing = true;
            cartasPalavra[i].fontSizeMin = 12;
            cartasPalavra[i].fontSizeMax = 19;
            InterfaceUtil.Posicionar(cartasPalavra[i].rectTransform, new Vector2(0, 0), new Vector2(1, 0.3f), new Vector2(6, 6), new Vector2(-6, 0));
            RectTransform rtFig = InterfaceUtil.NovoRect("Desenho", c.transform);
            InterfaceUtil.Fixar(rtFig, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-4f, -4f), new Vector2(50f, 50f));
            cartasFigura[i] = rtFig.gameObject.AddComponent<Image>();
            cartasFigura[i].preserveAspect = true;
            cartasFigura[i].raycastTarget = false;
            rtFig.gameObject.SetActive(false);
        }

        Button fechar = InterfaceUtil.Botao(card.transform, "Fechar", "Fechar", InterfaceUtil.Hex("#EA580C"), 30, FecharAlbum, out _);
        InterfaceUtil.Fixar((RectTransform)fechar.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-170, 30), new Vector2(300f, 70f));
        Button personagens = InterfaceUtil.Botao(card.transform, "Personagens", "Meu personagem", InterfaceUtil.Hex("#7C3AED"), 30, () =>
        {
            FecharAlbum();
            TelaPersonagens.Abrir();
        }, out _);
        InterfaceUtil.Fixar((RectTransform)personagens.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(170, 30), new Vector2(300f, 70f));

        album.SetActive(false);
    }

    private void ConstruirAviso()
    {
        // À esquerda, na altura do meio da tela: não cobre o título nem os botões da tela de "fase concluída"
        RectTransform rt = InterfaceUtil.NovoRect("AvisoEstrelas", transform);
        InterfaceUtil.Fixar(rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 60f), new Vector2(560f, 170f));
        Image fundo = rt.gameObject.AddComponent<Image>();
        fundo.sprite = InterfaceUtil.SpriteArredondado;
        fundo.type = Image.Type.Sliced;
        fundo.color = new Color(0.1f, 0.12f, 0.2f, 0.92f);
        fundo.raycastTarget = false;
        avisoGrupo = rt.gameObject.AddComponent<CanvasGroup>();
        avisoGrupo.blocksRaycasts = false;
        Image est = InterfaceUtil.Estrela(rt, InterfaceUtil.Hex("#FBBF24"), 64f);
        InterfaceUtil.Fixar(est.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(64f, 64f));
        txtAviso = InterfaceUtil.Texto(rt, "", 30, Color.white, TextAlignmentOptions.MidlineLeft);
        txtAviso.enableAutoSizing = true;
        txtAviso.fontSizeMin = 20;
        txtAviso.fontSizeMax = 30;
        InterfaceUtil.Posicionar(txtAviso.rectTransform, Vector2.zero, Vector2.one, new Vector2(100, 10), new Vector2(-20, -10));
        aviso = rt.gameObject;
        aviso.SetActive(false);
    }

    private void ConstruirResgate()
    {
        RectTransform raiz = InterfaceUtil.NovoRect("LetrinhaResgatada", transform);
        InterfaceUtil.Esticar(raiz, 0, 0, 0, 0);
        Image fundo = raiz.gameObject.AddComponent<Image>();
        fundo.color = new Color(0.05f, 0.07f, 0.14f, 0.8f);
        resgate = raiz.gameObject;

        Image card = InterfaceUtil.Caixa(raiz, "Cartao", new Color(1f, 0.98f, 0.93f));
        InterfaceUtil.Fixar(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 700f));

        resgateTitulo = InterfaceUtil.Texto(card.transform, "", 50, InterfaceUtil.Hex("#7C2D12"), TextAlignmentOptions.Center, true);
        InterfaceUtil.Posicionar(resgateTitulo.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -110), new Vector2(-30, -24));

        resgateCarta = InterfaceUtil.Caixa(card.transform, "Letrinha", Color.white);
        InterfaceUtil.Fixar(resgateCarta.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(320f, 340f));
        resgateLetra = InterfaceUtil.Texto(resgateCarta.transform, "", 150, Color.white, TextAlignmentOptions.Center, true);
        InterfaceUtil.Posicionar(resgateLetra.rectTransform, new Vector2(0, 0.3f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
        resgatePalavra = InterfaceUtil.Texto(resgateCarta.transform, "", 36, Color.white, TextAlignmentOptions.Center);
        resgatePalavra.fontStyle = FontStyles.Bold;
        resgatePalavra.enableAutoSizing = true;
        resgatePalavra.fontSizeMin = 20;
        resgatePalavra.fontSizeMax = 36;
        InterfaceUtil.Posicionar(resgatePalavra.rectTransform, new Vector2(0, 0), new Vector2(1, 0.3f), new Vector2(10, 10), new Vector2(-10, 0));

        RectTransform rtFigR = InterfaceUtil.NovoRect("Desenho", resgateCarta.transform);
        InterfaceUtil.Fixar(rtFigR, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-10f, -10f), new Vector2(120f, 120f));
        resgateFigura = rtFigR.gameObject.AddComponent<Image>();
        resgateFigura.preserveAspect = true;
        resgateFigura.raycastTarget = false;
        rtFigR.gameObject.SetActive(false);

        resgateRodape = InterfaceUtil.Texto(card.transform, "", 26, InterfaceUtil.Hex("#44403C"), TextAlignmentOptions.Center);
        InterfaceUtil.Posicionar(resgateRodape.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(30, 110), new Vector2(-30, 160));

        Button ok = InterfaceUtil.Botao(card.transform, "Oba", "Oba!", InterfaceUtil.Hex("#16A34A"), 34, () => resgate.SetActive(false), out _);
        InterfaceUtil.Fixar((RectTransform)ok.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(260f, 74f));

        resgate.SetActive(false);
    }
}
