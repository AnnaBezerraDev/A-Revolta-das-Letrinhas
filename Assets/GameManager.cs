using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [System.Serializable]
    public class ReferenciasDoPainel
    {
        public TextMeshProUGUI balaoTexto;
        public CartaBotao[] botoes;
        public GameObject painelObjeto;
        public Image imagemFundoCarta;

        [Header("Arraste os corações DESTA tela aqui")]
        public GameObject[] iconesVidasPainel;

        [Header("Indicador da Fase Atual")]
        public TextMeshProUGUI textoIndicadorFase;
    }

    [System.Serializable]
    public struct FraseDificil
    {
        [TextArea] public string fraseComLacuna;
        public string palavraCorreta;
        public string opcaoErrada1;
        public string opcaoErrada2;
        public string classeGramatical; // Substantivo, Adjetivo, Verbo, Advérbio, Conjunção, Pronome
        public string[] opcoesGramaticais; // 3 opções para os botões da etapa gramatical
    }

    [Header("Sons do Jogo")]
    public AudioSource sfxSource;
    public AudioClip somAcerto;
    public AudioClip somErro;
    public AudioClip somVitoria;
    public AudioClip somGameOver;

    [Header("UI por Dificuldade (Português)")]
    public ReferenciasDoPainel refsFacil;
    public ReferenciasDoPainel refsMedio;
    public ReferenciasDoPainel refsDificil;

    [Header("UI por Dificuldade (Matemática)")]
    public ReferenciasDoPainel refsMathFacil;
    public ReferenciasDoPainel refsMathMedio;
    public ReferenciasDoPainel refsMathDificil;

    [Header("Sistema de Vidas (Agora 2 Vidas)")]
    public int vidasMaximas = 2;
    private int vidasAtuais;
    private float tempoInicioFase; // para enviar ao servidor quanto tempo o aluno levou na fase

    [Header("Visual das Cartas")]
    public Sprite[] temasDasCartas;

    [Header("Banco de Palavras Expandido")]
    public List<string> palavrasFaceis = new List<string>();
    public List<string> palavrasMedias = new List<string>();
    public List<FraseDificil> frasesDificeis = new List<FraseDificil>();

    private ReferenciasDoPainel uiAtual;
    private int nivelAtualID;
    public int NivelAtualID => nivelAtualID;
    public bool JogandoMatematica => jogandoMatematica;
    private int faseAtualID = 0;
    public int FaseAtualID => faseAtualID;
    public string PalavraCompleta => palavraCompleta;
    public string RespostaCorreta => respostaCorreta;
    public FraseDificil FraseDificilAtual => fraseDificilAtual;
    public int EtapaDificilAtual => etapaDificilAtual;
    public bool PodeInteragir => podeInteragir;
    public float TempoUltimoClique => tempoUltimoClique;

    private void AtivarInteracaoGameplay()
    {
        podeInteragir = true;
        tempoUltimoClique = Time.time;
    }
    private bool jogandoMatematica = false;

    // Aluno do 1º ou 2º ano: palavras e continhas mais fáceis (ver ConteudoAlfabetizacao)
    private bool conteudoAlfabetizacao = false;
    public bool ConteudoAlfabetizacaoAtivo => conteudoAlfabetizacao;

    // true quando o professor usou "Auto-completar" nesta fase
    private bool faseResolvidaPeloProfessor = false;

    // Ano escolar do aluno (lido do nome da sala; 0 = desconhecido)
    private int anoEscolar = 0;
    public int AnoEscolar => anoEscolar;

    // Várias perguntas por fase (contra o "chute"): as 2 vidas valem para a fase inteira
    private int perguntaAtual = 1;
    private int totalPerguntas = 1;
    public int PerguntaAtual => perguntaAtual;
    public int TotalPerguntas => totalPerguntas;

    // Tipo da pergunta atual
    private bool perguntaComTexto = false;       // problema escrito: no acerto mostra "Resposta: X"
    private bool perguntaInterpretacao = false;  // 5º ano: texto + pergunta (sem etapa gramatical)
    private string tipoDesafioAlfabetizacao = ""; // "vogal", "silaba" ou "consoante"
    private ConteudoQuintoAno.Interpretacao interpretacaoAtual;
    public bool PerguntaInterpretacaoAtiva => perguntaInterpretacao;
    public ConteudoQuintoAno.Interpretacao InterpretacaoAtual => interpretacaoAtual;
    public string TipoDesafioAlfabetizacao => tipoDesafioAlfabetizacao;

    /// <summary>O que o botão "Ouvir" lê para a pergunta atual.</summary>
    public string TextoParaLeitura { get; private set; } = "";

    // Desenho da palavra mostrado ao lado do balão (só palavras que têm desenho no servidor)
    private string palavraParaFigura;

    // Habilidade da BNCC da pergunta atual e acertos/erros da fase (enviados ao servidor ao terminar a fase)
    private string habilidadeAtual = "";
    public string HabilidadeAtual => habilidadeAtual;
    private readonly Dictionary<string, int[]> habilidadesDaFase = new Dictionary<string, int[]>();
    private string alunoDasHabilidades = "";

    private void RegistrarResposta(bool acertou)
    {
        if (faseResolvidaPeloProfessor || string.IsNullOrEmpty(habilidadeAtual)) return; // resposta do professor não conta
        string aluno = GerenciadorPerfis.PerfilAtivo;
        if (alunoDasHabilidades != aluno) { EnviarHabilidadesDaFase(); alunoDasHabilidades = aluno; }
        if (!habilidadesDaFase.TryGetValue(habilidadeAtual, out int[] c)) habilidadesDaFase[habilidadeAtual] = c = new int[2];
        if (acertou) c[0]++; else c[1]++;
    }

    /// <summary>Manda pela fila (não se perde sem internet) os acertos/erros por habilidade da fase.</summary>
    private void EnviarHabilidadesDaFase()
    {
        if (habilidadesDaFase.Count == 0) return;
        var itens = new List<ServicoNuvem.DtoHabilidadeItem>();
        foreach (var kv in habilidadesDaFase) itens.Add(new ServicoNuvem.DtoHabilidadeItem { codigo = kv.Key, acertos = kv.Value[0], erros = kv.Value[1] });
        habilidadesDaFase.Clear();
        if (ServicoNuvem.Instancia != null && !string.IsNullOrEmpty(alunoDasHabilidades))
            ServicoNuvem.Instancia.EnviarHabilidades(alunoDasHabilidades, itens);
    }

    /// <summary>Ortografia do 3º ao 5º ano (palavra com letra/dígrafo faltando).</summary>
    private string CodigoOrtografia(string resposta)
    {
        int ano = anoEscolar >= 3 ? anoEscolar : 3;
        if (ano >= 5) return "EF05LP01";
        switch (resposta)
        {
            case "SS": case "S": case "C": return ano == 4 ? "EF04LP01" : "EF03LP01";
            case "CH": return ano == 3 ? "EF03LP03" : "EF35LP13";
            case "X": case "Z": case "Ç": return "EF35LP13"; // correspondências irregulares
            default: return ano == 4 ? "EF04LP01" : "EF03LP01";
        }
    }

    /// <summary>Classe gramatical da 2ª etapa do Português Difícil.</summary>
    private static string CodigoClasse(string classe)
    {
        string c = (classe ?? "").Trim().ToLowerInvariant();
        if (c.StartsWith("substant") || c.StartsWith("verbo")) return "EF03LP08";
        if (c.StartsWith("adjet")) return "EF03LP09";
        if (c.StartsWith("pronom")) return "EF35LP14";
        if (c.StartsWith("conjun")) return "EF05LP07";
        if (c.StartsWith("adv")) return "EF07LP09";
        return "";
    }

    private string respostaCorreta;
    private string palavraCompleta;
    private bool podeInteragir = true;
    private string alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    // Memória de últimas palavras e frases sorteadas para garantir que cada entrada tenha uma palavra diferente
    private static Dictionary<string, string> ultimasPalavrasUsadas = new Dictionary<string, string>();
    private static Dictionary<int, int> ultimasFrasesUsadas = new Dictionary<int, int>();

    // Resolução Passo a Passo e Dados da Questão de Matemática Atual
    public string UltimaPerguntaMatematica { get; private set; } = "";
    public string UltimoPassoAPassoMatematica { get; private set; } = "";
    public int UltimaRespostaMatematica { get; private set; } = 0;
    public string UltimaRespostaMatematicaTexto { get; private set; } = ""; // com centavos, ex.: "4,25"

    // Controle de 2 Etapas da Fase Difícil de Português (0 = Palavra, 1 = Classe Gramatical)
    private int etapaDificilAtual = 0;
    private string fraseOriginalDificil = "";
    private FraseDificil fraseDificilAtual;

    private float tempoUltimoClique = 0f;

    private Action actionScript;
    private GerenciadorFases gerenciadorFases;

    void Awake()
    {
        Instance = this;
        if (vidasMaximas > 2) vidasMaximas = 2;
        InterfaceUtil.GarantirFonteReserva(); // ponto, vírgula e "?" nos balões e nas cartas

        if (palavrasFaceis == null || palavrasFaceis.Count < 180) CarregarPalavrasFaceis();
        if (palavrasMedias == null || palavrasMedias.Count < 180) CarregarPalavrasMedias();
        if (frasesDificeis == null || frasesDificeis.Count < 72) CarregarFrasesDificeis();
    }

    void Start()
    {
        actionScript = FindFirstObjectByType<Action>();
        gerenciadorFases = FindFirstObjectByType<GerenciadorFases>();
    }

    public void IniciarFaseEspecifica(int numeroFase, int nivelDificuldade, bool isMath)
    {
        EnviarHabilidadesDaFase(); // respostas de uma fase abandonada no meio também contam
        nivelAtualID = nivelDificuldade;
        faseAtualID = numeroFase;
        jogandoMatematica = isMath;
        conteudoAlfabetizacao = GerenciadorPerfis.AlunoAtivoEmAlfabetizacao();
        anoEscolar = GerenciadorPerfis.AnoEscolarDoAlunoAtivo();
        faseResolvidaPeloProfessor = false;
        perguntaAtual = 1;
        // 1º/2º ano: 2 perguntas; Português Difícil: 2 (cada uma já tem 2 etapas); demais: 3
        totalPerguntas = conteudoAlfabetizacao ? 2 : (!isMath && nivelDificuldade == 2 ? 2 : 3);

        if (isMath)
        {
            switch (nivelDificuldade)
            {
                case 0: uiAtual = refsMathFacil; break;
                case 1: uiAtual = refsMathMedio; break;
                case 2: uiAtual = refsMathDificil; break;
                default: uiAtual = refsMathFacil; break;
            }
        }
        else
        {
            switch (nivelDificuldade)
            {
                case 0: uiAtual = refsFacil; break;
                case 1: uiAtual = refsMedio; break;
                case 2: uiAtual = refsDificil; break;
                default: uiAtual = refsFacil; break;
            }
        }

        vidasAtuais = vidasMaximas;
        tempoInicioFase = Time.realtimeSinceStartup;
        AtualizarUIVidas();

        if (actionScript == null) actionScript = FindFirstObjectByType<Action>();
        if (gerenciadorFases == null) gerenciadorFases = FindFirstObjectByType<GerenciadorFases>();

        if (actionScript != null)
        {
            actionScript.TocarMusicaPersonagens();
        }

        if (gerenciadorFases != null)
        {
            gerenciadorFases.faseAtualJogando = numeroFase;
            gerenciadorFases.nivelAtual = nivelDificuldade;
        }

        AtualizarIndicadorFase(numeroFase);

        if (uiAtual != null && uiAtual.painelObjeto != null)
        {
            uiAtual.painelObjeto.SetActive(true);
        }

        CancelInvoke();
        podeInteragir = false;
        tempoUltimoClique = Time.time;

        ConfigurarPergunta();

        // Aguarda 0.45s (tempo da transição da tela) para liberar cliques nos botões de cartas
        Invoke(nameof(AtivarInteracaoGameplay), 0.45f);
    }

    /// <summary>Monta a pergunta atual da fase (palavra, frase, texto ou conta) de acordo com a matéria, o nível e o ano.</summary>
    private void ConfigurarPergunta()
    {
        ApoioGameplay.LimparPergunta(); // tira desenho/dica da pergunta anterior antes de mexer no balão
        etapaDificilAtual = 0;
        perguntaComTexto = false;
        perguntaInterpretacao = false;
        tipoDesafioAlfabetizacao = "";
        palavraParaFigura = null;
        habilidadeAtual = "";
        int numeroFase = faseAtualID;
        int nivelDificuldade = nivelAtualID;

        if (jogandoMatematica)
        {
            ConfigurarFaseMatematica(numeroFase, nivelDificuldade);
        }
        else if (conteudoAlfabetizacao)
        {
            ConfigurarFaseVogalAlfabetizacao(numeroFase, nivelDificuldade);
        }
        else
        {
            if (nivelDificuldade == 2)
            {
                // 5º ano: metade das perguntas do Difícil é leitura de um texto curto
                if (anoEscolar == 5 && Random.value < 0.5f) ConfigurarFaseInterpretacao(numeroFase);
                else ConfigurarFaseFrase(numeroFase);
            }
            else
            {
                string palavraDaFase = ObterPalavraDinamicaFase(numeroFase, nivelDificuldade);

                if (!string.IsNullOrEmpty(palavraDaFase))
                {
                    palavraCompleta = palavraDaFase.Trim().ToUpper();
                    AplicarRegrasDeLacuna();
                    ConfigurarBotoesComRegras();
                    TextoParaLeitura = "Complete a palavra: " + palavraCompleta.ToLowerInvariant();
                    palavraParaFigura = palavraCompleta;
                    habilidadeAtual = CodigoOrtografia(respostaCorreta);
                }
                else
                {
                    Debug.LogError($"Fase {numeroFase} não possui palavra cadastrada no nível {nivelAtualID}!");
                }
            }
        }

        // No 1º e 2º ano a pergunta é lida em voz alta sozinha
        ApoioGameplay.NovaPergunta(conteudoAlfabetizacao);
        if (!string.IsNullOrEmpty(palavraParaFigura) && uiAtual != null && uiAtual.balaoTexto != null)
            ApoioGameplay.MostrarFigura(palavraParaFigura, uiAtual.balaoTexto);
    }

    /// <summary>Acertou uma pergunta e ainda há outras na fase.</summary>
    private void ProximaPergunta()
    {
        AtualizarIndicadorFase(faseAtualID);
        ConfigurarPergunta();
        podeInteragir = false;
        tempoUltimoClique = Time.time;
        Invoke(nameof(AtivarInteracaoGameplay), 0.35f);
    }

    void AtualizarUIVidas()
    {
        if (uiAtual == null || uiAtual.iconesVidasPainel == null) return;

        for (int i = 0; i < uiAtual.iconesVidasPainel.Length; i++)
        {
            if (uiAtual.iconesVidasPainel[i] != null)
            {
                // Se i < vidasAtuais ativa o coração; vidasMaximas = 2 esconde automaticamente o 3º coração
                uiAtual.iconesVidasPainel[i].SetActive(i < vidasAtuais);
            }
        }
    }

    void AtualizarIndicadorFase(int numeroFase)
    {
        if (uiAtual == null) return;

        string nomeNivel;
        switch (nivelAtualID)
        {
            case 0: nomeNivel = "FÁCIL"; break;
            case 1: nomeNivel = "MÉDIO"; break;
            case 2: nomeNivel = "DIFÍCIL"; break;
            default: nomeNivel = "FÁCIL"; break;
        }

        string materia = jogandoMatematica ? "MATEMÁTICA" : "PORTUGUÊS";
        string textoFase = $"<b><color=#FFE082>{materia}</color>  •  {nomeNivel}  •  <color=#FFFFFF>FASE {numeroFase + 1}/30</color>" +
                           (totalPerguntas > 1 ? $"  •  <color=#A7F3D0>PERGUNTA {perguntaAtual}/{totalPerguntas}</color>" : "") + "</b>";

        if (uiAtual.textoIndicadorFase != null)
        {
            uiAtual.textoIndicadorFase.text = textoFase;
            uiAtual.textoIndicadorFase.gameObject.SetActive(true);
        }

        if (uiAtual.painelObjeto != null)
        {
            // 1. Remove qualquer badge legado que tenha sido criado diretamente no painelObjeto
            Transform oldDirect = uiAtual.painelObjeto.transform.Find("IndicadorFaseBadge");
            if (oldDirect != null)
            {
                Destroy(oldDirect.gameObject);
            }

            // 2. Garante a localização da barra superior (TopBar_BotoesCanvas), que é um Canvas ScreenSpaceOverlay
            Transform tBarra = uiAtual.painelObjeto.transform.Find("TopBar_BotoesCanvas");
            if (tBarra == null)
            {
                if (actionScript != null) actionScript.ConfigurarBotoesGameplayCanvases();
                tBarra = uiAtual.painelObjeto.transform.Find("TopBar_BotoesCanvas");
            }

            Transform containerPai = (tBarra != null) ? tBarra : uiAtual.painelObjeto.transform;

            Transform tBadge = containerPai.Find("IndicadorFaseBadge");
            GameObject gBadge;
            if (tBadge == null)
            {
                gBadge = new GameObject("IndicadorFaseBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                gBadge.transform.SetParent(containerPai, false);
                tBadge = gBadge.transform;
            }
            else
            {
                gBadge = tBadge.gameObject;
            }

            // Garante que se o containerPai não for Canvas (fallback extremo), tenha Canvas próprio
            Canvas cExistente = gBadge.GetComponent<Canvas>();
            if (containerPai.GetComponentInParent<Canvas>() == null)
            {
                if (cExistente == null) cExistente = gBadge.AddComponent<Canvas>();
                cExistente.renderMode = RenderMode.ScreenSpaceOverlay;
                cExistente.sortingOrder = 500;

                CanvasScaler sc = gBadge.GetComponent<CanvasScaler>();
                if (sc == null) sc = gBadge.AddComponent<CanvasScaler>();
                sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                sc.referenceResolution = new Vector2(1920f, 1080f);
                sc.matchWidthOrHeight = 0.5f;
            }
            else if (cExistente != null)
            {
                // Se já está sob um Canvas (como TopBar_BotoesCanvas), não deve ter Canvas duplicado no badge
                Destroy(cExistente);
                CanvasScaler sc = gBadge.GetComponent<CanvasScaler>();
                if (sc != null) Destroy(sc);
            }

            RectTransform rt = gBadge.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -20f);
            rt.sizeDelta = new Vector2(500f, 54f);
            rt.localScale = Vector3.one;

            Image img = gBadge.GetComponent<Image>();
            if (img != null)
            {
                img.color = new Color(0.12f, 0.08f, 0.04f, 0.88f); // Fundo escuro elegante com alto contraste
                Sprite badgeSprite = Resources.Load<Sprite>("badge_aluno_topo") ?? Resources.Load<Sprite>("badge_lobby_jogo");
                if (badgeSprite != null)
                {
                    img.sprite = badgeSprite;
                    img.type = Image.Type.Sliced;
                    img.color = new Color(0.14f, 0.09f, 0.05f, 0.90f);
                }
                img.raycastTarget = false;
            }

            Transform tTxt = gBadge.transform.Find("TextoFase");
            GameObject gTxt;
            if (tTxt == null)
            {
                gTxt = new GameObject("TextoFase", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                gTxt.transform.SetParent(gBadge.transform, false);
                tTxt = gTxt.transform;
            }
            else
            {
                gTxt = tTxt.gameObject;
            }

            RectTransform rtTxt = gTxt.GetComponent<RectTransform>();
            rtTxt.anchorMin = Vector2.zero;
            rtTxt.anchorMax = Vector2.one;
            rtTxt.offsetMin = new Vector2(10f, 0f);
            rtTxt.offsetMax = new Vector2(-10f, 0f);
            rtTxt.localScale = Vector3.one;

            TextMeshProUGUI tmp = gTxt.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                TMP_FontAsset fonte = Resources.Load<TMP_FontAsset>("CrayonHandRegular2016Demo SDF");
                if (fonte != null) tmp.font = fonte;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 22f;
                tmp.color = Color.white;
                tmp.text = textoFase;
                tmp.raycastTarget = false;
            }

            gBadge.SetActive(true);
        }
    }

    public string ObterPalavraDinamicaFase(int numeroFase, int nivelDificuldade)
    {
        int categoria = numeroFase % 6;
        List<string> banco = (nivelDificuldade == 0) ? palavrasFaceis : palavrasMedias;
        if (banco == null || banco.Count < 180)
        {
            if (nivelDificuldade == 0) CarregarPalavrasFaceis();
            else CarregarPalavrasMedias();
            banco = (nivelDificuldade == 0) ? palavrasFaceis : palavrasMedias;
        }

        // Pega as palavras pertencentes a esta categoria ortográfica
        List<string> candidatos = new List<string>();
        int inicio = categoria * 30;
        int fim = Mathf.Min(inicio + 30, banco.Count);
        for (int i = inicio; i < fim; i++)
        {
            candidatos.Add(banco[i]);
        }

        if (candidatos.Count == 0) candidatos = banco;

        string chaveMemoria = $"{nivelDificuldade}_{numeroFase}";
        string ultimaUsada = ultimasPalavrasUsadas.ContainsKey(chaveMemoria) ? ultimasPalavrasUsadas[chaveMemoria] : "";

        // Remove a última palavra usada para garantir palavra nova a cada entrada do aluno
        List<string> opcoesDisponiveis = new List<string>(candidatos);
        if (opcoesDisponiveis.Count > 1 && !string.IsNullOrEmpty(ultimaUsada))
        {
            opcoesDisponiveis.Remove(ultimaUsada);
        }

        int sorteio = Random.Range(0, opcoesDisponiveis.Count);
        string escolhida = opcoesDisponiveis[sorteio];
        ultimasPalavrasUsadas[chaveMemoria] = escolhida;

        return escolhida;
    }

    public FraseDificil ObterFraseDinamicaFase(int numeroFase)
    {
        if (frasesDificeis == null || frasesDificeis.Count < 72) CarregarFrasesDificeis();

        int tema = numeroFase % 12;
        List<FraseDificil> candidatos = new List<FraseDificil>();
        int inicio = tema * 6;
        int fim = Mathf.Min(inicio + 6, frasesDificeis.Count);
        for (int i = inicio; i < fim; i++)
        {
            candidatos.Add(frasesDificeis[i]);
        }

        if (candidatos.Count == 0) candidatos = frasesDificeis;

        int ultimaIdx = ultimasFrasesUsadas.ContainsKey(numeroFase) ? ultimasFrasesUsadas[numeroFase] : -1;
        List<FraseDificil> disponiveis = new List<FraseDificil>(candidatos);
        if (disponiveis.Count > 1 && ultimaIdx >= 0 && ultimaIdx < candidatos.Count)
        {
            disponiveis.RemoveAt(ultimaIdx);
        }

        int sorteio = Random.Range(0, disponiveis.Count);
        FraseDificil escolhida = disponiveis[sorteio];
        ultimasFrasesUsadas[numeroFase] = candidatos.IndexOf(escolhida);

        return escolhida;
    }

    void ConfigurarFaseMatematica(int numeroFase, int nivelDificuldade)
    {
        string pergunta = "";
        int correta = 0;
        string passoAPasso = "";

        // Semente dinâmica para que a cada entrada a continha tenha valores novos
        Random.InitState((int)(System.DateTime.Now.Ticks ^ (numeroFase * 37 + nivelDificuldade * 1000)));

        // 5º ano: metade das perguntas do Difícil são frações, porcentagem, dinheiro e problemas
        if (!conteudoAlfabetizacao && anoEscolar == 5 && nivelDificuldade == 2 && Random.value < 0.5f)
        {
            ConfigurarProblemaQuintoAno();
            return;
        }

        if (conteudoAlfabetizacao)
        {
            // 1º ano: somas de 1 algarismo. 2º ano: somas/subtrações, dezenas, dobro, metade e probleminhas
            ConteudoAlfabetizacao.SortearConta(anoEscolar, nivelDificuldade, out pergunta, out correta, out passoAPasso, out bool comTexto);
            perguntaComTexto = comTexto;
        }
        else if (nivelDificuldade == 0)
        {
            if (numeroFase < 10)
            {
                int a = Random.Range(10, 50); int b = Random.Range(10, 50);
                pergunta = $"{a} + {b} = ?"; correta = a + b;
                passoAPasso = $"• <b>Some as Unidades:</b> {a % 10} + {b % 10} = {(a % 10) + (b % 10)}\n• <b>Some as Dezenas:</b> {a / 10} + {b / 10} = {(a / 10) + (b / 10)}\n• <b>Resultado Final:</b> <color=#00E676>{correta}</color>";
            }
            else if (numeroFase < 20)
            {
                int a = Random.Range(30, 99); int b = Random.Range(10, a);
                pergunta = $"{a} - {b} = ?"; correta = a - b;
                passoAPasso = $"• <b>Subtraia as Unidades:</b> {a % 10} - {b % 10}\n• <b>Subtraia as Dezenas:</b> {a / 10} - {b / 10}\n• <b>Resultado Final:</b> <color=#00E676>{correta}</color>";
            }
            else
            {
                int a = Random.Range(2, 11); int b = Random.Range(2, 11);
                pergunta = $"{a} x {b} = ?"; correta = a * b;
                passoAPasso = $"• <b>Multiplicação:</b> É somar o número {a}, {b} vezes ({a} + {a}...)\n• <b>Tabuada:</b> {a} x {b} = <color=#00E676>{correta}</color>";
            }
        }
        else if (nivelDificuldade == 1)
        {
            if (numeroFase < 10)
            {
                int divisor = Random.Range(2, 10); int quociente = Random.Range(5, 20); int dividendo = divisor * quociente;
                pergunta = $"{dividendo} ÷ {divisor} = ?"; correta = quociente;
                passoAPasso = $"• <b>Divisão:</b> Quantas vezes o {divisor} cabe em {dividendo}?\n• Pense na tabuada invertida: {divisor} x <b>{quociente}</b> = {dividendo}\n• <b>Resultado:</b> <color=#00E676>{correta}</color>";
            }
            else if (numeroFase < 20)
            {
                int a = Random.Range(10, 50); int b = Random.Range(2, 10); int c = Random.Range(2, 10);
                pergunta = $"{a} + ({b} x {c}) = ?"; correta = a + (b * c);
                passoAPasso = $"• <b>1º Resolva os Parênteses:</b> {b} x {c} = {b * c}\n• <b>2º Some com {a}:</b> {a} + {b * c} = <color=#00E676>{correta}</color>";
            }
            else
            {
                int a = Random.Range(15, 99); int b = Random.Range(5, 20);
                pergunta = $"({a} x 2) - {b} = ?"; correta = (a * 2) - b;
                passoAPasso = $"• <b>1º Resolva os Parênteses (dobro):</b> {a} x 2 = {a * 2}\n• <b>2º Subtraia {b}:</b> {a * 2} - {b} = <color=#00E676>{correta}</color>";
            }
        }
        else
        {
            // Nível Difícil: Alinhado à BNCC do Ensino Fundamental 1 (4º e 5º Anos - EF04MA03, EF04MA04, EF04MA05, EF05MA07, EF05MA08)
            // Expressões numéricas com parênteses e ordem de precedência das quatro operações fundamentais com números naturais, sem potenciação.
            if (numeroFase < 10)
            {
                // Fases 1 a 10: Expressões com multiplicação e adição/subtração combinadas: (A x B) - C ou (A x B) + C
                int a = Random.Range(3, 11);
                int b = Random.Range(3, 10);
                int c = Random.Range(2, 20);
                bool somar = Random.value > 0.5f;

                if (somar)
                {
                    pergunta = $"({a} x {b}) + {c} = ?";
                    correta = (a * b) + c;
                    passoAPasso = $"• <b>1º Resolva os Parênteses:</b> {a} x {b} = {a * b}\n• <b>2º Some {c}:</b> {a * b} + {c} = <color=#00E676>{correta}</color>";
                }
                else
                {
                    if (c >= (a * b)) c = (a * b) - 5;
                    if (c < 1) c = 1;
                    pergunta = $"({a} x {b}) - {c} = ?";
                    correta = (a * b) - c;
                    passoAPasso = $"• <b>1º Resolva os Parênteses:</b> {a} x {b} = {a * b}\n• <b>2º Subtraia {c}:</b> {a * b} - {c} = <color=#00E676>{correta}</color>";
                }
            }
            else if (numeroFase < 20)
            {
                // Fases 11 a 20: Precedência das 4 operações com divisão e multiplicação exatas: A + (B ÷ C) ou (A x B) ÷ C
                int c = Random.Range(2, 6);
                int quociente = Random.Range(3, 10);
                int b = c * quociente; // Garante divisão exata
                int a = Random.Range(10, 40);

                if (numeroFase % 2 == 0)
                {
                    pergunta = $"{a} + ({b} ÷ {c}) = ?";
                    correta = a + quociente;
                    passoAPasso = $"• <b>1º Resolva a Divisão:</b> {b} ÷ {c} = {quociente}\n• <b>2º Some {a}:</b> {a} + {quociente} = <color=#00E676>{correta}</color>";
                }
                else
                {
                    int fatorMult = Random.Range(2, 6);
                    pergunta = $"({b} ÷ {c}) x {fatorMult} = ?";
                    correta = quociente * fatorMult;
                    passoAPasso = $"• <b>1º Resolva a Divisão:</b> {b} ÷ {c} = {quociente}\n• <b>2º Multiplique por {fatorMult}:</b> {quociente} x {fatorMult} = <color=#00E676>{correta}</color>";
                }
            }
            else
            {
                // Fases 21 a 30: Expressões compostas com parênteses: (A + B) x C ou (A - B) x C
                int c = Random.Range(2, 6);
                int a = Random.Range(10, 35);
                int b = Random.Range(2, 15);
                bool somar = (numeroFase % 2 == 0);

                if (somar)
                {
                    pergunta = $"({a} + {b}) x {c} = ?";
                    correta = (a + b) * c;
                    passoAPasso = $"• <b>1º Resolva os Parênteses:</b> {a} + {b} = {a + b}\n• <b>2º Multiplique por {c}:</b> {a + b} x {c} = <color=#00E676>{correta}</color>";
                }
                else
                {
                    if (b >= a) b = a - 3;
                    pergunta = $"({a} - {b}) x {c} = ?";
                    correta = (a - b) * c;
                    passoAPasso = $"• <b>1º Resolva os Parênteses:</b> {a} - {b} = {a - b}\n• <b>2º Multiplique por {c}:</b> {a - b} x {c} = <color=#00E676>{correta}</color>";
                }
            }
        }

        // Habilidade da BNCC da conta
        if (conteudoAlfabetizacao)
        {
            if (anoEscolar != 2) habilidadeAtual = "EF01MA06";
            else if (pergunta.Contains("dobro") || pergunta.Contains("metade")) habilidadeAtual = "EF02MA08";
            else if (perguntaComTexto || nivelDificuldade >= 1) habilidadeAtual = "EF02MA06";
            else habilidadeAtual = "EF02MA05";
        }
        else if (nivelDificuldade == 0) habilidadeAtual = numeroFase < 20 ? "EF03MA05" : "EF03MA03"; // + e -, depois tabuada
        else if (nivelDificuldade == 1) habilidadeAtual = numeroFase < 10 ? "EF04MA07" : "EF04MA05"; // divisão, depois expressões
        else habilidadeAtual = "EF04MA05"; // expressões com parênteses (habilidade mais próxima na BNCC)

        UltimaPerguntaMatematica = pergunta;
        UltimoPassoAPassoMatematica = passoAPasso;
        UltimaRespostaMatematica = correta;
        UltimaRespostaMatematicaTexto = correta.ToString();
        TextoParaLeitura = LeitorDeVoz.ContaFalada(pergunta);

        respostaCorreta = correta.ToString();
        if (uiAtual.balaoTexto != null)
        {
            uiAtual.balaoTexto.enableAutoSizing = true;
            uiAtual.balaoTexto.fontSizeMin = perguntaComTexto ? 20f : 28f; // problema escrito precisa caber no balão
            uiAtual.balaoTexto.fontSizeMax = 56f;
            uiAtual.balaoTexto.text = pergunta;
        }

        List<string> opcoes;
        if (conteudoAlfabetizacao)
        {
            // Alternativas próximas da resposta e nunca negativas
            opcoes = ConteudoAlfabetizacao.OpcoesDaSoma(correta, uiAtual.botoes.Length);
        }
        else
        {
            opcoes = new List<string> { respostaCorreta };
            int errada1 = correta + Random.Range(1, 10);
            int errada2 = correta - Random.Range(1, 10);
            if (errada2 == correta || errada2 == errada1) errada2 = correta + 15;

            opcoes.Add(errada1.ToString());
            opcoes.Add(errada2.ToString());
        }

        opcoes = EmbaralharLista(opcoes);

        for (int i = 0; i < uiAtual.botoes.Length; i++)
        {
            if (i < opcoes.Count && uiAtual.botoes[i] != null)
                uiAtual.botoes[i].DefinirLetra(opcoes[i]);
        }
    }

    /// <summary>5º ano: frações, porcentagem, dinheiro com centavos e problemas de duas etapas.</summary>
    void ConfigurarProblemaQuintoAno()
    {
        ConteudoQuintoAno.SortearProblema(out string pergunta, out string correta, out List<string> opcoes, out string passo, out bool comTexto, out string habilidade);
        perguntaComTexto = comTexto;
        habilidadeAtual = habilidade;
        UltimaPerguntaMatematica = pergunta;
        UltimoPassoAPassoMatematica = passo;
        UltimaRespostaMatematica = int.TryParse(correta, out int inteiro) ? inteiro : 0;
        UltimaRespostaMatematicaTexto = correta;
        TextoParaLeitura = LeitorDeVoz.ContaFalada(pergunta);
        respostaCorreta = correta;

        if (uiAtual.balaoTexto != null)
        {
            uiAtual.balaoTexto.enableAutoSizing = true;
            uiAtual.balaoTexto.fontSizeMin = comTexto ? 20f : 28f;
            uiAtual.balaoTexto.fontSizeMax = 56f;
            uiAtual.balaoTexto.text = pergunta;
        }
        for (int i = 0; i < uiAtual.botoes.Length; i++)
        {
            if (i < opcoes.Count && uiAtual.botoes[i] != null) uiAtual.botoes[i].DefinirLetra(opcoes[i]);
        }
    }

    /// <summary>5º ano: texto curto + pergunta de interpretação (uma etapa só).</summary>
    void ConfigurarFaseInterpretacao(int numeroFase)
    {
        perguntaInterpretacao = true;
        habilidadeAtual = "EF15LP03"; // localizar informações explícitas em textos
        interpretacaoAtual = ConteudoQuintoAno.SortearInterpretacao(numeroFase);
        respostaCorreta = interpretacaoAtual.correta;
        palavraCompleta = interpretacaoAtual.texto + " " + interpretacaoAtual.pergunta + " " + interpretacaoAtual.correta;
        TextoParaLeitura = interpretacaoAtual.texto + " " + interpretacaoAtual.pergunta;

        if (uiAtual.balaoTexto != null)
        {
            uiAtual.balaoTexto.enableAutoSizing = true;
            uiAtual.balaoTexto.fontSizeMin = 22;
            uiAtual.balaoTexto.fontSizeMax = 52;
            uiAtual.balaoTexto.lineSpacing = 0;
            uiAtual.balaoTexto.margin = new Vector4(15, 10, 15, 10);
            uiAtual.balaoTexto.alignment = TextAlignmentOptions.Center;
            uiAtual.balaoTexto.text = $"{interpretacaoAtual.texto}\n<b><color=#1D4ED8>{interpretacaoAtual.pergunta}</color></b>";
        }

        List<string> opcoes = EmbaralharLista(new List<string> { interpretacaoAtual.correta, interpretacaoAtual.errada1, interpretacaoAtual.errada2 });
        for (int i = 0; i < uiAtual.botoes.Length; i++)
        {
            if (i < opcoes.Count && uiAtual.botoes[i] != null) uiAtual.botoes[i].DefinirLetra(opcoes[i]);
        }
    }

    // 1º e 2º ano (Fácil, Médio e Difícil): palavra pequena com vogal, sílaba ou consoante faltando.
    void ConfigurarFaseVogalAlfabetizacao(int numeroFase, int nivelDificuldade)
    {
        int qtdCartas = (uiAtual.botoes != null) ? uiAtual.botoes.Length : 3;
        ConteudoAlfabetizacao.DesafioLacuna desafio = ConteudoAlfabetizacao.SortearDesafio(anoEscolar, nivelDificuldade, numeroFase, qtdCartas);

        palavraCompleta = desafio.palavra;
        respostaCorreta = desafio.resposta;
        tipoDesafioAlfabetizacao = desafio.tipo;
        palavraParaFigura = desafio.palavra;
        habilidadeAtual = desafio.tipo == "silaba" ? "EF02LP02" : desafio.tipo == "consoante" ? "EF02LP03" : (anoEscolar == 2 ? "EF02LP04" : "EF01LP08");
        string oQueFalta = desafio.tipo == "silaba" ? "Qual sílaba" : desafio.tipo == "consoante" ? "Qual letra" : "Qual vogal";
        TextoParaLeitura = $"{oQueFalta} falta na palavra {desafio.palavra.ToLowerInvariant()}?";
        if (uiAtual.balaoTexto != null)
        {
            if (nivelDificuldade == 2)
            {
                // Mesmo formato que o balão do Difícil usa nas frases (centralizado e com letra grande)
                uiAtual.balaoTexto.enableAutoSizing = true;
                uiAtual.balaoTexto.fontSizeMin = 36;
                uiAtual.balaoTexto.fontSizeMax = 68;
                uiAtual.balaoTexto.margin = new Vector4(15, 10, 15, 10);
                uiAtual.balaoTexto.alignment = TextAlignmentOptions.Center;
            }
            uiAtual.balaoTexto.text = desafio.comLacuna;
        }

        for (int i = 0; i < uiAtual.botoes.Length; i++)
        {
            if (i < desafio.opcoes.Count && uiAtual.botoes[i] != null) uiAtual.botoes[i].DefinirLetra(desafio.opcoes[i]);
        }
    }

    void ConfigurarFaseFrase(int numeroFase)
    {
        etapaDificilAtual = 0;
        fraseDificilAtual = ObterFraseDinamicaFase(numeroFase);
        fraseOriginalDificil = fraseDificilAtual.fraseComLacuna;

        respostaCorreta = fraseDificilAtual.palavraCorreta.Trim().ToUpper();
        palavraCompleta = fraseOriginalDificil.Replace("_____", respostaCorreta);
        TextoParaLeitura = "Complete a frase: " + fraseOriginalDificil.Replace("_____", "...");
        // Homófonos/parônimos: sem habilidade própria até o 5º ano na BNCC → grafia irregular/ortografia (mais próximas)
        habilidadeAtual = anoEscolar >= 5 ? "EF05LP01" : "EF35LP13";

        if (uiAtual.balaoTexto != null)
        {
            uiAtual.balaoTexto.enableAutoSizing = true;
            uiAtual.balaoTexto.fontSizeMin = 36;
            uiAtual.balaoTexto.fontSizeMax = 68;
            uiAtual.balaoTexto.lineSpacing = -5;
            uiAtual.balaoTexto.margin = new Vector4(15, 10, 15, 10);
            uiAtual.balaoTexto.alignment = TextAlignmentOptions.Center;
            uiAtual.balaoTexto.text = fraseOriginalDificil.Replace("_____", " _ ");
        }

        List<string> opcoes = new List<string> {
            fraseDificilAtual.palavraCorreta.Trim().ToUpper(),
            fraseDificilAtual.opcaoErrada1.Trim().ToUpper(),
            fraseDificilAtual.opcaoErrada2.Trim().ToUpper()
        };

        opcoes = EmbaralharLista(opcoes);

        for (int i = 0; i < uiAtual.botoes.Length; i++)
        {
            if (i < opcoes.Count && uiAtual.botoes[i] != null) uiAtual.botoes[i].DefinirLetra(opcoes[i]);
        }
    }

    void AplicarRegrasDeLacuna()
    {
        if (palavraCompleta.Contains("SS")) EncontrarECompletarLacuna("SS");
        else if (palavraCompleta.Contains("CH")) EncontrarECompletarLacuna("CH");
        else if (palavraCompleta.Contains("X")) EncontrarECompletarLacuna("X");
        else if (palavraCompleta.Contains("Z")) EncontrarECompletarLacuna("Z");
        else if (palavraCompleta.Contains("Ç")) EncontrarECompletarLacuna("Ç");
        else if (palavraCompleta.Contains("S")) EncontrarSSozinho();
        else if (palavraCompleta.Contains("C")) EncontrarCSozinho();
        else GerarLacunaAleatoriaClassica();
    }

    void EncontrarECompletarLacuna(string alvo)
    {
        int index = palavraCompleta.IndexOf(alvo);
        if (index >= 0)
        {
            respostaCorreta = alvo;
            string inicio = palavraCompleta.Substring(0, index);
            string fim = palavraCompleta.Substring(index + alvo.Length);
            if (uiAtual.balaoTexto != null) uiAtual.balaoTexto.text = inicio + "_" + fim;
        }
    }

    void EncontrarSSozinho()
    {
        for (int i = 0; i < palavraCompleta.Length; i++)
        {
            if (palavraCompleta[i] == 'S')
            {
                bool parteDeSS = (i > 0 && palavraCompleta[i - 1] == 'S') || (i < palavraCompleta.Length - 1 && palavraCompleta[i + 1] == 'S');
                if (!parteDeSS) { respostaCorreta = "S"; CriarLacunaNaPosicao(i); return; }
            }
        }
        GerarLacunaAleatoriaClassica();
    }

    void EncontrarCSozinho()
    {
        for (int i = 0; i < palavraCompleta.Length; i++)
        {
            if (palavraCompleta[i] == 'C')
            {
                bool parteDeCH = (i < palavraCompleta.Length - 1 && palavraCompleta[i + 1] == 'H');
                if (!parteDeCH) { respostaCorreta = "C"; CriarLacunaNaPosicao(i); return; }
            }
        }
        GerarLacunaAleatoriaClassica();
    }

    void CriarLacunaNaPosicao(int posicao)
    {
        char[] caracteres = palavraCompleta.ToCharArray();
        caracteres[posicao] = '_';
        if (uiAtual.balaoTexto != null) uiAtual.balaoTexto.text = new string(caracteres);
    }

    void ConfigurarBotoesComRegras()
    {
        List<string> opcoes = new List<string>();
        opcoes.Add(respostaCorreta);

        List<string> candidatosConfusos = new List<string>();

        // Família 1: Sibilantes e dísticos ortográficos (S, SS, C, Ç, Z, X)
        if (respostaCorreta == "SS" || respostaCorreta == "S" || respostaCorreta == "C" || respostaCorreta == "Ç" || respostaCorreta == "Z")
        {
            candidatosConfusos.AddRange(new string[] { "S", "SS", "C", "Ç", "Z", "X" });
        }
        // Família 2: Sons de quiade / chiantes (CH, X, Z, S, SS, SH)
        else if (respostaCorreta == "CH" || respostaCorreta == "X")
        {
            candidatosConfusos.AddRange(new string[] { "CH", "X", "Z", "S", "SS", "C" });
        }
        // Família 3: Oclusivas e nasais comuns (M, N, NH)
        else if (respostaCorreta == "M" || respostaCorreta == "N")
        {
            candidatosConfusos.AddRange(new string[] { "M", "N", "NH", "L" });
        }
        // Família 4: Líquidas e vibrantes (R, RR, L)
        else if (respostaCorreta == "R" || respostaCorreta == "RR")
        {
            candidatosConfusos.AddRange(new string[] { "R", "RR", "L", "S" });
        }
        // Família 5: Vogais e semivogais com dúvidas fonéticas (E/I, O/U, A/E)
        else if (respostaCorreta == "E" || respostaCorreta == "I")
        {
            candidatosConfusos.AddRange(new string[] { "E", "I", "A", "O" });
        }
        else if (respostaCorreta == "O" || respostaCorreta == "U")
        {
            candidatosConfusos.AddRange(new string[] { "O", "U", "A", "E" });
        }
        else if (respostaCorreta == "G" || respostaCorreta == "J")
        {
            candidatosConfusos.AddRange(new string[] { "G", "J", "GU", "Q" });
        }
        else
        {
            candidatosConfusos.AddRange(new string[] { "B", "P", "D", "T", "V", "F", "M", "N" });
        }

        candidatosConfusos = EmbaralharLista(candidatosConfusos);

        foreach (var c in candidatosConfusos)
        {
            if (c != respostaCorreta && !opcoes.Contains(c) && opcoes.Count < uiAtual.botoes.Length)
            {
                opcoes.Add(c);
            }
        }

        PreencherComLetrasAleatorias(opcoes);
        opcoes = EmbaralharLista(opcoes);

        for (int i = 0; i < uiAtual.botoes.Length; i++)
        {
            if (i < opcoes.Count && uiAtual.botoes[i] != null) uiAtual.botoes[i].DefinirLetra(opcoes[i]);
        }
    }

    void PreencherComLetrasAleatorias(List<string> opcoes)
    {
        while (opcoes.Count < uiAtual.botoes.Length)
        {
            string letra = alfabeto[Random.Range(0, alfabeto.Length)].ToString();
            if (!opcoes.Contains(letra) && letra != "SS" && letra != "CH" && letra != "Ç" && letra != "S" && letra != "C" && letra != "X" && letra != "Z")
                opcoes.Add(letra);
        }
    }

    void GerarLacunaAleatoriaClassica()
    {
        char[] caracteres = palavraCompleta.ToCharArray();
        int indiceParaEsconder = Random.Range(0, caracteres.Length);
        while (!char.IsLetter(caracteres[indiceParaEsconder])) indiceParaEsconder = Random.Range(0, caracteres.Length);

        respostaCorreta = caracteres[indiceParaEsconder].ToString();
        caracteres[indiceParaEsconder] = '_';
        if (uiAtual.balaoTexto != null) uiAtual.balaoTexto.text = new string(caracteres);
    }

    List<string> EmbaralharLista(List<string> list)
    {
        System.Random rng = new System.Random(System.Guid.NewGuid().GetHashCode() ^ System.Environment.TickCount);
        for (int round = 0; round < 7; round++)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                string temp = list[k];
                list[k] = list[n];
                list[n] = temp;
            }
        }
        return list;
    }

    public void VerificarResposta(string inputJogador)
    {
        if (!podeInteragir) return;

        if (string.IsNullOrEmpty(respostaCorreta))
        {
            Debug.LogWarning("respostaCorreta está null! A fase não foi inicializada corretamente.");
            return;
        }

        if (Time.time - tempoUltimoClique < 0.5f) return;
        tempoUltimoClique = Time.time;

        string entradaNormalizada = inputJogador;
        string corretaNormalizada = respostaCorreta;

        if (jogandoMatematica)
        {
            // Mantém a vírgula: no 5º ano há respostas com centavos (ex.: 4,25 é diferente de 42,5)
            entradaNormalizada = Regex.Replace(entradaNormalizada, "[^0-9,-]", "");
            corretaNormalizada = Regex.Replace(corretaNormalizada, "[^0-9,-]", "");
        }
        else
        {
            entradaNormalizada = entradaNormalizada.Replace("\u200B", "").Replace("\u00A0", "").Trim().ToUpper();
            corretaNormalizada = corretaNormalizada.Replace("\u200B", "").Replace("\u00A0", "").Trim().ToUpper();
        }

        if (entradaNormalizada == corretaNormalizada && !string.IsNullOrEmpty(corretaNormalizada))
        {
            RegistrarResposta(true);
            if (!jogandoMatematica && nivelAtualID == 2 && !conteudoAlfabetizacao && !perguntaInterpretacao && etapaDificilAtual == 0)
            {
                ApoioGameplay.Esconder();
                habilidadeAtual = CodigoClasse(fraseDificilAtual.classeGramatical); // 2ª etapa: classe gramatical
                if (sfxSource != null && somAcerto != null) sfxSource.PlayOneShot(somAcerto);
                etapaDificilAtual = 1;

                if (uiAtual.balaoTexto != null)
                {
                    uiAtual.balaoTexto.enableAutoSizing = true;
                    uiAtual.balaoTexto.fontSizeMin = 40;
                    uiAtual.balaoTexto.fontSizeMax = 72;
                    uiAtual.balaoTexto.lineSpacing = 0;
                    uiAtual.balaoTexto.margin = new Vector4(15, 10, 15, 10);
                    uiAtual.balaoTexto.alignment = TextAlignmentOptions.Center;

                    uiAtual.balaoTexto.text = 
                        $"A palavra\n" +
                        $"<color=#007700><b>\"{fraseDificilAtual.palavraCorreta}\"</b></color>\n" +
                        $"é um(a):";
                }

                respostaCorreta = fraseDificilAtual.classeGramatical.Trim().ToUpper();
                List<string> opcoesGram = new List<string>(fraseDificilAtual.opcoesGramaticais);
                opcoesGram = EmbaralharLista(opcoesGram);
                TextoParaLeitura = $"A palavra {fraseDificilAtual.palavraCorreta.ToLowerInvariant()} é: {string.Join(", ", opcoesGram).ToLowerInvariant()}?";

                for (int i = 0; i < uiAtual.botoes.Length; i++)
                {
                    if (i < opcoesGram.Count && uiAtual.botoes[i] != null)
                    {
                        uiAtual.botoes[i].DefinirLetra(opcoesGram[i].ToUpper());
                    }
                }

                podeInteragir = true;
                return;
            }

            podeInteragir = false;
            ApoioGameplay.Esconder();

            if (sfxSource != null && somAcerto != null) sfxSource.PlayOneShot(somAcerto);

            if (uiAtual.balaoTexto != null)
            {
                if (jogandoMatematica)
                {
                    if (perguntaComTexto) uiAtual.balaoTexto.text += $"\n<color=#007700><b>Resposta: {respostaCorreta}</b></color>";
                    else uiAtual.balaoTexto.text = uiAtual.balaoTexto.text.Replace("?", respostaCorreta);
                }
                else if (perguntaInterpretacao)
                {
                    uiAtual.balaoTexto.text = $"Muito bem!\n<color=#007700><b>{interpretacaoAtual.correta}</b></color>";
                }
                else if (nivelAtualID == 2 && !conteudoAlfabetizacao)
                {
                    uiAtual.balaoTexto.enableAutoSizing = true;
                    uiAtual.balaoTexto.fontSizeMin = 36;
                    uiAtual.balaoTexto.fontSizeMax = 68;
                    uiAtual.balaoTexto.text = $"Muito bem!\n\"<color=#007700>{fraseDificilAtual.palavraCorreta}</color>\" é <color=#007700>{fraseDificilAtual.classeGramatical.ToUpper()}</color>!";
                }
                else uiAtual.balaoTexto.text = palavraCompleta;
            }

            // Ainda há perguntas nesta fase: a próxima aparece com as mesmas vidas
            if (perguntaAtual < totalPerguntas && !faseResolvidaPeloProfessor)
            {
                perguntaAtual++;
                Invoke(nameof(ProximaPergunta), 1.1f);
                return;
            }

            // Fase concluída
            string perfil = GerenciadorPerfis.PerfilAtivo;
            string materia = jogandoMatematica ? "Math" : "Port";
            int estrelasAntes = ResgateLetrinhas.EstrelasDoAluno(perfil);
            string chaveProfessor = ResgateLetrinhas.ChaveResolvidaProfessor(materia, nivelAtualID, faseAtualID);
            if (faseResolvidaPeloProfessor) GerenciadorPerfis.SetInt(chaveProfessor, 1);          // fase do professor não dá estrelas
            else if (GerenciadorPerfis.GetInt(chaveProfessor, 0) == 1) GerenciadorPerfis.SetInt(chaveProfessor, 0);

            if (gerenciadorFases != null)
            {
                int segundosNaFase = Mathf.RoundToInt(Time.realtimeSinceStartup - tempoInicioFase);
                gerenciadorFases.MarcarFaseComoCompleta(vidasAtuais, vidasMaximas, segundosNaFase, !faseResolvidaPeloProfessor);
            }
            EnviarHabilidadesDaFase();

            string chaveVidas = $"VidasRestantes_{materia}_{nivelAtualID}_{faseAtualID}";
            int vidasSalvas = GerenciadorPerfis.GetInt(chaveVidas, 0);
            if (vidasAtuais > vidasSalvas)
            {
                GerenciadorPerfis.SetInt(chaveVidas, vidasAtuais);
            }
            PlayerPrefs.Save();

            if (!faseResolvidaPeloProfessor)
                TelaLetrinhas.MostrarRecompensa(perfil, estrelasAntes, ResgateLetrinhas.EstrelasDoAluno(perfil));

            Invoke("MostrarTelaFaseConcluida", 1.0f);
        }
        else
        {
            RegistrarResposta(false);
            vidasAtuais--;
            AtualizarUIVidas();

            if (sfxSource != null && somErro != null) sfxSource.PlayOneShot(somErro);

            if (vidasAtuais <= 0)
            {
                podeInteragir = false;
                ApoioGameplay.Esconder();
                EnviarHabilidadesDaFase(); // a fase não concluída também mostra o que precisa de reforço
                Invoke("MostrarTelaGameOver", 0.5f);
            }
            else
            {
                string dica = GerarDica(out string dicaFalada);
                ApoioGameplay.MostrarDica(dica, conteudoAlfabetizacao ? dicaFalada : null);
            }
        }
    }

    /// <summary>Dica depois de um erro: mostra o caminho sem entregar a resposta.</summary>
    private string GerarDica(out string falada)
    {
        falada = "";
        if (jogandoMatematica)
        {
            string pergunta = UltimaPerguntaMatematica ?? "";
            if (conteudoAlfabetizacao)
            {
                string d;
                if (pergunta.Contains("dobro")) d = "Dobro é somar o número com ele mesmo.";
                else if (pergunta.Contains("metade")) d = "Metade é dividir em duas partes iguais.";
                else if (perguntaComTexto) d = "Ganhou é juntar (somar). Sobrou é tirar (diminuir).";
                else if (pergunta.Contains(" - ")) d = "Comece no primeiro número e volte nos dedos.";
                else d = "Comece pelo número maior e conte o outro nos dedos.";
                falada = d;
                return d;
            }
            // Mostra o caminho, sem os números da conta (o passo a passo completo aparece no fim, na tela de explicação)
            if (perguntaComTexto) return "Separe em etapas: primeiro descubra o total, depois o que mudou (ganhou, gastou, sobrou).";
            if (pergunta.Contains("R$")) return "Coloque vírgula embaixo de vírgula: some ou tire os centavos e depois os reais.";
            if (pergunta.Contains("%")) return "10% é dividir por 10; 50% é a metade; 25% é a metade da metade.";
            if (pergunta.Contains("/")) return "Divida o total pelo número de baixo da fração e multiplique pelo de cima.";
            if (pergunta.Contains("(")) return "Resolva primeiro o que está dentro dos parênteses.";
            if (pergunta.Contains("÷")) return "Pense na tabuada ao contrário: que número, vezes o divisor, dá o número da conta?";
            if (pergunta.Contains(" x ")) return "Multiplicar é somar o mesmo número várias vezes. Use a tabuada.";
            if (pergunta.Contains(" - ")) return "Tire primeiro as unidades e depois as dezenas. Se faltar, empreste uma dezena.";
            return "Some primeiro as unidades e depois as dezenas. Passou de 9? Vai um para as dezenas.";
        }

        if (conteudoAlfabetizacao)
        {
            falada = palavraCompleta.ToLowerInvariant();
            if (tipoDesafioAlfabetizacao == "silaba") return "Bata uma palma para cada pedacinho da palavra. Qual pedacinho está faltando?";
            if (tipoDesafioAlfabetizacao == "consoante") return "Fale a palavra bem devagar. Que som aparece no lugar do tracinho?";
            return "Fale a palavra devagar e escute: que vogal está faltando?";
        }

        if (perguntaInterpretacao) return "Leia o texto de novo: a resposta está escrita nele.";

        if (nivelAtualID == 2)
        {
            return etapaDificilAtual == 0
                ? "Leia a frase trocando o tracinho por cada opção. Qual faz sentido?"
                : "Substantivo dá nome; verbo é ação; adjetivo é característica; advérbio diz como, quando ou onde; pronome substitui o nome; conjunção liga ideias.";
        }

        switch (respostaCorreta)
        {
            case "SS": return "Entre duas vogais, o som de S forte se escreve com SS (como em PASSO).";
            case "Ç": return "O Ç só aparece antes de A, O e U (como em PALHAÇO).";
            case "C": return "Antes de E e I, a letra C tem som de S (como em CINEMA).";
            case "S": return "Entre duas vogais, um S sozinho tem som de Z (como em CASA).";
            case "Z": return "Pense em palavras da mesma família: elas mantêm a mesma letra.";
            case "CH":
            case "X": return "CH e X podem ter o mesmo som. Lembre de palavras da mesma família.";
            case "A": case "E": case "I": case "O": case "U": return "Fale a palavra devagar e preste atenção no som da vogal.";
            default: return "Fale a palavra devagar e preste atenção no som que está faltando.";
        }
    }

    void MostrarTelaFaseConcluida()
    {
        if (actionScript != null)
        {
            actionScript.PararMusica();
        }

        if (sfxSource != null && somVitoria != null) sfxSource.PlayOneShot(somVitoria);

        if (actionScript != null && gerenciadorFases != null)
        {
            CanvasGroup telaJogoAtual = GetTelaJogoAtual();
            string materia = jogandoMatematica ? "Math" : "Port";

            Debug.Log($"[VITORIA] Fase Finalizada: ID {faseAtualID} | Nível: {nivelAtualID} | Matéria: {materia}");

            if (faseAtualID == 29)
            {
                if (nivelAtualID == 0)
                {
                    if (jogandoMatematica) GerenciadorPerfis.SetInt("Desbloqueio_Medio_Math", 1);
                    else { GerenciadorPerfis.SetInt("Desbloqueio_Medio_Port", 1); GerenciadorPerfis.SetInt("Desbloqueio_Medio", 1); }
                }
                else if (nivelAtualID == 1)
                {
                    if (jogandoMatematica) GerenciadorPerfis.SetInt("Desbloqueio_Dificil_Math", 1);
                    else { GerenciadorPerfis.SetInt("Desbloqueio_Dificil_Port", 1); GerenciadorPerfis.SetInt("Desbloqueio_Dificil", 1); }
                }

                int vidasPreservadas = 0;
                for (int i = 0; i < 30; i++)
                {
                    vidasPreservadas += Mathf.Clamp(GerenciadorPerfis.GetInt($"VidasRestantes_{materia}_{nivelAtualID}_{i}", 1), 0, vidasMaximas);
                }

                // Cálculo de pontuação proporcional com base em 2 Vidas máximas (total 60 corações)
                int totalCoracoesPossiveis = 30 * vidasMaximas; 
                int coracoesPerdidos = totalCoracoesPossiveis - vidasPreservadas;
                float porcentagemPerdida = (coracoesPerdidos / (float)totalCoracoesPossiveis) * 100f;

                int rankConquistado = 1; 
                CanvasGroup painelDestino;

                if (coracoesPerdidos == 0)
                {
                    rankConquistado = 3; 
                    painelDestino = actionScript.groupProxFaseOuro;
                }
                else if (porcentagemPerdida <= 50f)
                {
                    rankConquistado = 2; 
                    painelDestino = actionScript.groupProxFasePrata;
                }
                else
                {
                    rankConquistado = 1; 
                    painelDestino = actionScript.groupProxFaseBronze;
                }

                string chaveTrofeu = $"TrofeuRank_{materia}_{nivelAtualID}";
                int rankSalvo = GerenciadorPerfis.GetInt(chaveTrofeu, 0);
                if (rankConquistado > rankSalvo)
                {
                    GerenciadorPerfis.SetInt(chaveTrofeu, rankConquistado);
                }

                PlayerPrefs.Save();
                Debug.Log($"[SALVOU TROFEU] Chave: {chaveTrofeu} -> Gravado Rank: {rankConquistado}");

                actionScript.VerificarDesbloqueios();
                actionScript.ConfigurarBotoesPainelFaseConcluida();

                if (actionScript.VerificarJogo100PorCento())
                {
                    actionScript.ExecutarFinalDeJogo(telaJogoAtual);
                    return;
                }

                actionScript.TrocarTela(telaJogoAtual, painelDestino);
            }
            else
            {
                actionScript.ConfigurarBotoesPainelFaseConcluida();
                actionScript.TrocarTela(telaJogoAtual, actionScript.groupFaseConcluida);
            }
        }
    }

    void MostrarTelaGameOver()
    {
        if (actionScript != null)
        {
            actionScript.PararMusica();
        }

        if (sfxSource != null && somGameOver != null) sfxSource.PlayOneShot(somGameOver);

        if (actionScript != null)
        {
            CanvasGroup telaJogoAtual = GetTelaJogoAtual();
            actionScript.TrocarTela(telaJogoAtual, actionScript.groupGameOver);
        }

        GerenciadorVideoExplicativo gve = FindFirstObjectByType<GerenciadorVideoExplicativo>();
        if (gve != null)
        {
            gve.AbrirTelaVideo();
        }
    }

    CanvasGroup GetTelaJogoAtual()
    {
        if (jogandoMatematica)
        {
            switch (nivelAtualID)
            {
                case 0: return actionScript.groupJogoMathFacil;
                case 1: return actionScript.groupJogoMathMedio;
                case 2: return actionScript.groupJogoMathDificil;
                default: return actionScript.groupJogoMathFacil;
            }
        }
        else
        {
            switch (nivelAtualID)
            {
                case 0: return actionScript.groupJogoFacil;
                case 1: return actionScript.groupJogoMedio;
                case 2: return actionScript.groupJogoDificil;
                default: return actionScript.groupJogoFacil;
            }
        }
    }

    public void BotaoReiniciarFase()
    {
        if (actionScript != null)
        {
            actionScript.TocarSomClique();
            actionScript.TocarMusicaPersonagens();
            actionScript.TrocarTela(actionScript.groupGameOver, GetTelaJogoAtual());
        }

        IniciarFaseEspecifica(faseAtualID, nivelAtualID, jogandoMatematica);
    }

    public void AvancarParaProximaFase()
    {
        if (gerenciadorFases == null) gerenciadorFases = FindFirstObjectByType<GerenciadorFases>();
        if (gerenciadorFases != null)
        {
            gerenciadorFases.BotaoProximaFase();
        }
        else
        {
            int proxima = faseAtualID + 1;
            if (proxima < 30)
            {
                IniciarFaseEspecifica(proxima, nivelAtualID, jogandoMatematica);
            }
        }
    }

    public void ResolverFaseProfessor()
    {
        // "Auto-completar" do professor: a fase é liberada, mas não vai para o desempenho do aluno no servidor
        faseResolvidaPeloProfessor = true;
        if (jogandoMatematica)
        {
            VerificarResposta(respostaCorreta);
        }
        else
        {
            if (nivelAtualID == 2 && !conteudoAlfabetizacao && !perguntaInterpretacao)
            {
                if (etapaDificilAtual == 0)
                {
                    VerificarResposta(fraseDificilAtual.palavraCorreta);
                }
                else
                {
                    VerificarResposta(fraseDificilAtual.classeGramatical);
                }
            }
            else
            {
                VerificarResposta(respostaCorreta);
            }
        }
    }

    private void CarregarPalavrasFaceis()
    {
        palavrasFaceis = new List<string>
        {
            // --- GRUPO 0: SS (30 Palavras) ---
            "PÁSSARO", "VASSOURA", "OSSO", "MASSA", "PASSEIO",
            "BÚSSOLA", "PÊSSEGO", "DINOSSAURO", "DISCUSSÃO", "TOSSE",
            "PASSO", "CARROSSEL", "SOSSEGO", "MISSÃO", "DEPRESSA",
            "CONDESSA", "AMASSAR", "PROFISSÃO", "REMESSA", "COLOSSO",
            "TRAVESSURA", "ACESSO", "BARRACÃO", "ASSOBIO", "PASSAGEM",
            "PÉSSIMO", "AMASSO", "AVESSO", "CLASSE", "PROGRESSO",

            // --- GRUPO 1: CH (30 Palavras) ---
            "CHAVE", "CHUVA", "CHAPÉU", "CHÃO", "BICHO",
            "CHOCOLATE", "CHAMINÉ", "CHUVEIRO", "MOCHILA", "CACHORRO",
            "BOLACHA", "CHINELO", "CHUPETA", "CHUTEIRA", "CHURRASCO",
            "RECHEIO", "MANCHA", "TOCHA", "FLECHA", "CHORO",
            "CHALEIRA", "FECHADURA", "GANCHOS", "CHUVISCO", "BROCHE",
            "RECHEADO", "CHUPETINHA", "CHICOTE", "CHINELINHO", "CHAMA",

            // --- GRUPO 2: X (30 Palavras) ---
            "CAIXA", "LIXO", "XÍCARA", "PEIXE", "XAROPE",
            "ABACAXI", "XADREZ", "ENXADA", "CAIXOTE", "LIXEIRA",
            "BAIXO", "AMEIXA", "FEIXE", "COXA", "LIXA",
            "XALE", "BEXIGA", "ROXO", "GRAXA", "FAXINA",
            "PEIXINHO", "MEXER", "ENXAME", "ENXAGUAR", "XERIFE",
            "XAMPU", "CAIXINHA", "FAIXA", "BRUXA", "LUXO",

            // --- GRUPO 3: Z (30 Palavras) ---
            "ZEBRA", "BUZINA", "AZEITE", "DOZE", "RAIZ",
            "AMIZADE", "NATUREZA", "BATIZADO", "HORIZONTE", "BELEZA",
            "CERTEZA", "RAPAZ", "FELIZ", "NARIZ", "ARROZ",
            "COZINHA", "AZEITONA", "VIZINHO", "AZUL", "REALEZA",
            "TRISTEZA", "LIMPEZA", "FORTALEZA", "DEZENOVE", "ZOOLÓGICO",
            "ZANGADO", "VAZIO", "CRUZEIRO", "COZIDO", "VELOZ",

            // --- GRUPO 4: Ç (30 Palavras) ---
            "MAÇÃ", "ONÇA", "TAÇA", "MOÇA", "LAÇO",
            "CRIANÇA", "CORAÇÃO", "BALANÇA", "PALHAÇO", "CARROÇA",
            "PRAÇA", "PEDAÇO", "BRAÇO", "CABEÇA", "GRAÇA",
            "FUMAÇA", "CALÇADA", "ALIANÇA", "ESPERANÇA", "AÇÚCAR",
            "RAÇA", "POÇO", "TRANÇA", "LENÇOL", "LIÇÃO",
            "CORAÇÕES", "AÇUCARADO", "PALHAÇADA", "CARROÇEL", "PEDAÇINHO",

            // --- GRUPO 5: C / S (30 Palavras) ---
            "CASA", "CEBOLA", "CENOURA", "CIDADE", "BICO",
            "CINEMA", "BACIA", "OFICINA", "PACIÊNCIA", "CEREJA",
            "MESA", "ROSA", "VASO", "CASACO", "CAMISA",
            "ASA", "TESOURA", "BESOURO", "VISITA", "PARAFUSO",
            "CEGONHA", "CIRCO", "CENTRO", "DOCE", "VACINA",
            "CAPACETE", "CINTO", "BONECA", "COROA", "CAMA"
        };
    }

    private void CarregarPalavrasMedias()
    {
        palavrasMedias = new List<string>
        {
            // --- GRUPO 0: SS Complexo (30 Palavras) ---
            "PROFESSOR", "ASSINATURA", "TRAVESSEIRO", "GIRASSOL", "COMPASSO",
            "DINOSSAURO", "PROFISSIONAL", "NECESSÁRIO", "CONCESSÃO", "DISCUSSÃO",
            "COMPROMISSO", "POSSÍVEL", "ASSUSTADO", "MASSACRE", "IMPOSSÍVEL",
            "CARROSSEL", "PROGRESSISTA", "DEPRESSIVO", "INTERESSE", "AGRESSIVO",
            "REMESSA", "TRAVESSÃO", "SUBMISSÃO", "PERMISSÃO", "DISCUSSÕES",
            "ACESSÍVEL", "CONFISSÃO", "SUCESSO", "PRESSÃO", "CONGRESSO",

            // --- GRUPO 1: CH e Dígrafos Complexos (30 Palavras) ---
            "CHOCOLATE", "CHAMINÉ", "CHUVEIRO", "MOCHILA", "CACHORRO",
            "SANDUÍCHE", "CACHOEIRA", "FÓSFORO", "CHUVISCO", "CHIQUERINHO",
            "RECHEADO", "GUILHOTINA", "CHURRASQUEIRA", "ALCACHOFRA", "BORRACHA",
            "BOLACHINHA", "CHUVEIRADA", "ENCHARCADO", "GANCHINHO", "BICHINHO",
            "FECHADURA", "CHALEIRA", "MANCHADO", "CHUPETINHA", "BICHARADA",
            "BARRACÃO", "CHAVEIRO", "ACHEGADO", "CHINELADA", "CHILIQUE",

            // --- GRUPO 2: X Complexo (30 Palavras) ---
            "ABACAXI", "XADREZ", "ENXADA", "CAIXOTE", "LIXEIRA",
            "CRUCIFIXO", "COMPLEXO", "TÓXICO", "OXIGÊNIO", "REFLEXO",
            "ANEXO", "EXAME", "EXERCÍCIO", "EXEMPLO", "ÊXITO",
            "EXATO", "MÁXIMO", "PRÓXIMO", "AUXÍLIO", "ASFIXIA",
            "TÁXI", "TÓRAX", "CLÍMAX", "LÉXICO", "ENXURRADA",
            "ENXOFRE", "EXAGERADO", "EXECUÇÃO", "EXCELENTE", "EXCLUSIVO",

            // --- GRUPO 3: Z e Sons de Z (30 Palavras) ---
            "AMIZADE", "NATUREZA", "TEXTO", "BATIZADO", "HORIZONTE",
            "COZINHEIRO", "VIZINHANÇA", "FORMOSURA", "ESPERTEZA", "GENTILEZA",
            "APRENDIZADO", "DELICADEZA", "CICATRIZ", "NOBREZA", "REALEZA",
            "CRUELDADE", "RAPIDEZ", "ESCASSEZ", "TIMIDEZ", "AZULEJO",
            "VIZINHO", "LUZES", "RAPAZES", "CAPAZES", "VELOCIDADE",
            "PRODUZIR", "ORGANIZADO", "RAÍZES", "JUÍZES", "REORGANIZAR",

            // --- GRUPO 4: Ç e Sufixos -ção (30 Palavras) ---
            "CRIANÇA", "CORAÇÃO", "BALANÇA", "PALHAÇO", "CARROÇA",
            "POLUIÇÃO", "EDUCAÇÃO", "ATENÇÃO", "CONSTRUÇÃO", "PROTEÇÃO",
            "ILUSTRAÇÃO", "EMBARCAÇÃO", "DIREÇÃO", "SENSAÇÃO", "INSPIRAÇÃO",
            "REFEIÇÃO", "EXPLICAÇÃO", "CORAÇÕES", "ARMAÇÃO", "REDAÇÃO",
            "FUNDAÇÃO", "DECORAÇÃO", "SITUAÇÃO", "ANIMAÇÃO", "TRANSFORMAÇÃO",
            "PREPARAÇÃO", "PRECAUÇÃO", "COMBINAÇÃO", "DEDICAÇÃO", "PARTICIPAÇÃO",

            // --- GRUPO 5: C com som de S (30 Palavras) ---
            "CINEMA", "BACIA", "OFICINA", "PACIÊNCIA", "CEREJA",
            "FELICIDADE", "MELANCIA", "CAPACETE", "POLÍCIA", "PRECIOSO",
            "EXPERIÊNCIA", "ANÚNCIO", "PALÁCIO", "SILÊNCIO", "DIFERENÇA",
            "EXCELÊNCIA", "PRINCÍPIO", "BENEFÍCIO", "JUSTIÇA", "NEGÓCIO",
            "EXERCÍCIO", "DISPONÍVEL", "DELÍCIA", "NOTÍCIA", "INOCÊNCIA",
            "ESPÉCIE", "AUDIÊNCIA", "EFICIÊNCIA", "CONSCIÊNCIA", "COMÉRCIO"
        };
    }

    private void CarregarFrasesDificeis()
    {
        frasesDificeis = new List<FraseDificil>
        {
            // --- TEMA 0: Os 4 Porquês (6 Frases) ---
            new FraseDificil { fraseComLacuna = "Eu não sei o _____ de tanta confusão.", palavraCorreta = "PORQUÊ", opcaoErrada1 = "POR QUE", opcaoErrada2 = "PORQUE", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Verbo", "Conjunção" } },
            new FraseDificil { fraseComLacuna = "Gostaria de saber _____ você não foi à aula.", palavraCorreta = "POR QUE", opcaoErrada1 = "PORQUE", opcaoErrada2 = "PORQUÊ", classeGramatical = "Pronome", opcoesGramaticais = new string[] { "Pronome", "Substantivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Não vou sair, _____ está chovendo bastante.", palavraCorreta = "PORQUE", opcaoErrada1 = "POR QUÊ", opcaoErrada2 = "POR QUE", classeGramatical = "Conjunção", opcoesGramaticais = new string[] { "Conjunção", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Você não almoçou quase nada, por _____?", palavraCorreta = "QUÊ", opcaoErrada1 = "QUE", opcaoErrada2 = "QUEM", classeGramatical = "Pronome", opcoesGramaticais = new string[] { "Pronome", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Descobri o _____ daquele barulho estranho.", palavraCorreta = "PORQUÊ", opcaoErrada1 = "PORQUE", opcaoErrada2 = "POR QUE", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Ela se atrasou _____ o trânsito estava parado.", palavraCorreta = "PORQUE", opcaoErrada1 = "POR QUÊ", opcaoErrada2 = "PORQUÊ", classeGramatical = "Conjunção", opcoesGramaticais = new string[] { "Conjunção", "Substantivo", "Adjetivo" } },

            // --- TEMA 1: MAU vs MAL (6 Frases) ---
            new FraseDificil { fraseComLacuna = "Ele é um _____ exemplo para os colegas.", palavraCorreta = "MAU", opcaoErrada1 = "MAL", opcaoErrada2 = "MÁL", classeGramatical = "Adjetivo", opcoesGramaticais = new string[] { "Adjetivo", "Advérbio", "Substantivo" } },
            new FraseDificil { fraseComLacuna = "Fui _____ na prova de matemática hoje.", palavraCorreta = "MAL", opcaoErrada1 = "MAU", opcaoErrada2 = "MAIS", classeGramatical = "Advérbio", opcoesGramaticais = new string[] { "Advérbio", "Adjetivo", "Substantivo" } },
            new FraseDificil { fraseComLacuna = "O lobo _____ vive nas histórias infantis.", palavraCorreta = "MAU", opcaoErrada1 = "MAL", opcaoErrada2 = "MÁL", classeGramatical = "Adjetivo", opcoesGramaticais = new string[] { "Adjetivo", "Advérbio", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Aquele menino se comportou muito _____ ontem.", palavraCorreta = "MAL", opcaoErrada1 = "MAU", opcaoErrada2 = "MAUZINHO", classeGramatical = "Advérbio", opcoesGramaticais = new string[] { "Advérbio", "Adjetivo", "Substantivo" } },
            new FraseDificil { fraseComLacuna = "O chefe acordou de _____ humor esta manhã.", palavraCorreta = "MAU", opcaoErrada1 = "MAL", opcaoErrada2 = "MAUS", classeGramatical = "Adjetivo", opcoesGramaticais = new string[] { "Adjetivo", "Substantivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "O motorista dirigiu muito _____, com descuido.", palavraCorreta = "MAL", opcaoErrada1 = "MAU", opcaoErrada2 = "MÁ", classeGramatical = "Advérbio", opcoesGramaticais = new string[] { "Advérbio", "Conjunção", "Adjetivo" } },

            // --- TEMA 2: SESSÃO vs SEÇÃO vs CESSÃO (6 Frases) ---
            new FraseDificil { fraseComLacuna = "A _____ de cinema começa exatamente às 20h.", palavraCorreta = "SESSÃO", opcaoErrada1 = "SEÇÃO", opcaoErrada2 = "CESSÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Fui à _____ de eletrodomésticos para comprar uma TV.", palavraCorreta = "SEÇÃO", opcaoErrada1 = "SESSÃO", opcaoErrada2 = "CESSÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "A _____ dos bens para a caridade foi assinada.", palavraCorreta = "CESSÃO", opcaoErrada1 = "SEÇÃO", opcaoErrada2 = "SESSÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "A _____ de fotos dos formandos durou a tarde toda.", palavraCorreta = "SESSÃO", opcaoErrada1 = "SEÇÃO", opcaoErrada2 = "CESSÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Procure os livros novos na _____ de literatura.", palavraCorreta = "SEÇÃO", opcaoErrada1 = "SESSÃO", opcaoErrada2 = "CESSÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "O autor fez a _____ de direitos para o filme.", palavraCorreta = "CESSÃO", opcaoErrada1 = "SEÇÃO", opcaoErrada2 = "SESSÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },

            // --- TEMA 3: FLAGRANTE vs FRAGRANTE (6 Frases) ---
            new FraseDificil { fraseComLacuna = "O suspeito foi pego em _____ pela polícia.", palavraCorreta = "FLAGRANTE", opcaoErrada1 = "FRAGRANTE", opcaoErrada2 = "FLAGANTE", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "O perfume das rosas é muito _____.", palavraCorreta = "FRAGRANTE", opcaoErrada1 = "FLAGRANTE", opcaoErrada2 = "FRAGANTE", classeGramatical = "Adjetivo", opcoesGramaticais = new string[] { "Adjetivo", "Substantivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "O guarda registrou o motorista em _____ delito.", palavraCorreta = "FLAGRANTE", opcaoErrada1 = "FRAGRANTE", opcaoErrada2 = "FLAGRADO", classeGramatical = "Adjetivo", opcoesGramaticais = new string[] { "Adjetivo", "Substantivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "As flores do jardim deixaram o ar muito _____.", palavraCorreta = "FRAGRANTE", opcaoErrada1 = "FLAGRANTE", opcaoErrada2 = "FRAGOSO", classeGramatical = "Adjetivo", opcoesGramaticais = new string[] { "Adjetivo", "Substantivo", "Advérbio" } },
            new FraseDificil { fraseComLacuna = "Eles foram flagrados e a prova foi em _____.", palavraCorreta = "FLAGRANTE", opcaoErrada1 = "FRAGRANTE", opcaoErrada2 = "FLORANTE", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Um aroma suave e _____ tomou conta da casa.", palavraCorreta = "FRAGRANTE", opcaoErrada1 = "FLAGRANTE", opcaoErrada2 = "FRANGANTE", classeGramatical = "Adjetivo", opcoesGramaticais = new string[] { "Adjetivo", "Substantivo", "Verbo" } },

            // --- TEMA 4: CONSERTO vs CONCERTO (6 Frases) ---
            new FraseDificil { fraseComLacuna = "Meu relógio quebrou e precisa de um _____.", palavraCorreta = "CONSERTO", opcaoErrada1 = "CONCERTO", opcaoErrada2 = "CONÇERTO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Ontem assistimos a um lindo _____ de piano.", palavraCorreta = "CONCERTO", opcaoErrada1 = "CONSERTO", opcaoErrada2 = "CONÇERTO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "O sapateiro fez um excelente _____ na sandália.", palavraCorreta = "CONSERTO", opcaoErrada1 = "CONCERTO", opcaoErrada2 = "CONÇERTO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "A orquestra sinfônica brilhou no _____ de Natal.", palavraCorreta = "CONCERTO", opcaoErrada1 = "CONSERTO", opcaoErrada2 = "CONSERTOU", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "A bicicleta furou e precisa de um rápido _____.", palavraCorreta = "CONSERTO", opcaoErrada1 = "CONCERTO", opcaoErrada2 = "CONSETO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Os músicos ensaiaram muito para o grande _____.", palavraCorreta = "CONCERTO", opcaoErrada1 = "CONSERTO", opcaoErrada2 = "CONSSERTO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },

            // --- TEMA 5: TAXA vs TACHA vs TACHAR (6 Frases) ---
            new FraseDificil { fraseComLacuna = "O governo prevê uma alta na _____ de importação.", palavraCorreta = "TAXA", opcaoErrada1 = "TACHA", opcaoErrada2 = "TÂXA", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Não deixe ninguém te _____ de preguiçoso.", palavraCorreta = "TACHAR", opcaoErrada1 = "TAXAR", opcaoErrada2 = "TÂCHAR", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "Pagamos uma _____ bancária para a transferência.", palavraCorreta = "TAXA", opcaoErrada1 = "TACHA", opcaoErrada2 = "TAXAÇÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Fixamos o pôster na parede com uma _____ pequena.", palavraCorreta = "TACHA", opcaoErrada1 = "TAXA", opcaoErrada2 = "TAXINHA", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "A prefeitura cobra uma _____ para coleta de lixo.", palavraCorreta = "TAXA", opcaoErrada1 = "TACHA", opcaoErrada2 = "TAXAR", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Costumam _____ o jogador de fominha sem motivo.", palavraCorreta = "TACHAR", opcaoErrada1 = "TAXAR", opcaoErrada2 = "TACHADO", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },

            // --- TEMA 6: INFRINGIU vs INFLIGIU (6 Frases) ---
            new FraseDificil { fraseComLacuna = "O motorista _____ as leis de trânsito.", palavraCorreta = "INFRINGIU", opcaoErrada1 = "INFLIGIU", opcaoErrada2 = "INFLINGIU", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "O juiz _____ uma pena severa ao réu.", palavraCorreta = "INFLIGIU", opcaoErrada1 = "INFRINGIU", opcaoErrada2 = "INFLINGIU", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "O atleta _____ o regulamento ao cometer falta.", palavraCorreta = "INFRINGIU", opcaoErrada1 = "INFLIGIU", opcaoErrada2 = "INFRINGIDO", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "A derrota inesperada _____ tristeza aos torcedores.", palavraCorreta = "INFLIGIU", opcaoErrada1 = "INFRINGIU", opcaoErrada2 = "INFLINGIU", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "Quem quebra o contrato _____ as regras combinadas.", palavraCorreta = "INFRINGIU", opcaoErrada1 = "INFLIGIU", opcaoErrada2 = "INFRATOR", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "O castigo _____ ao guerreiro foi bastante duro.", palavraCorreta = "INFLIGIDO", opcaoErrada1 = "INFRINGIDO", opcaoErrada2 = "INFLIGIDO", classeGramatical = "Adjetivo", opcoesGramaticais = new string[] { "Adjetivo", "Substantivo", "Verbo" } },

            // --- TEMA 7: DISCRIÇÃO vs DESCRIÇÃO (6 Frases) ---
            new FraseDificil { fraseComLacuna = "Ele agiu com muita _____, ninguém percebeu.", palavraCorreta = "DISCRIÇÃO", opcaoErrada1 = "DESCRIÇÃO", opcaoErrada2 = "DESCREÇÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "A _____ da paisagem no livro é perfeita.", palavraCorreta = "DESCRIÇÃO", opcaoErrada1 = "DISCRIÇÃO", opcaoErrada2 = "DESCREÇÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Para guardar um segredo, é preciso ter _____.", palavraCorreta = "DISCRIÇÃO", opcaoErrada1 = "DESCRIÇÃO", opcaoErrada2 = "DISCRETO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Faça uma _____ detalhada do objeto perdido.", palavraCorreta = "DESCRIÇÃO", opcaoErrada1 = "DISCRIÇÃO", opcaoErrada2 = "DESCRITOR", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "O detetive trabalhou com total _____ e cuidado.", palavraCorreta = "DISCRIÇÃO", opcaoErrada1 = "DESCRIÇÃO", opcaoErrada2 = "DISCRITO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "A testemunha fez a _____ do carro azul.", palavraCorreta = "DESCRIÇÃO", opcaoErrada1 = "DISCRIÇÃO", opcaoErrada2 = "DESCRITO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },

            // --- TEMA 8: MAS vs MAIS (6 Frases) ---
            new FraseDificil { fraseComLacuna = "Eles queriam sair, _____ estava chovendo.", palavraCorreta = "MAS", opcaoErrada1 = "MAIS", opcaoErrada2 = "MÁS", classeGramatical = "Conjunção", opcoesGramaticais = new string[] { "Conjunção", "Advérbio", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "Precisamos de _____ tempo para terminar o projeto.", palavraCorreta = "MAIS", opcaoErrada1 = "MAS", opcaoErrada2 = "MÁS", classeGramatical = "Advérbio", opcoesGramaticais = new string[] { "Advérbio", "Conjunção", "Verbo" } },
            new FraseDificil { fraseComLacuna = "O time jogou muito bem, _____ não conseguiu vencer.", palavraCorreta = "MAS", opcaoErrada1 = "MAIS", opcaoErrada2 = "MÁS", classeGramatical = "Conjunção", opcoesGramaticais = new string[] { "Conjunção", "Advérbio", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "Gostaria de colocar _____ queijo na minha macarronada.", palavraCorreta = "MAIS", opcaoErrada1 = "MAS", opcaoErrada2 = "MAYS", classeGramatical = "Advérbio", opcoesGramaticais = new string[] { "Advérbio", "Substantivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Eu tentei ligar, _____ a linha estava ocupada.", palavraCorreta = "MAS", opcaoErrada1 = "MAIS", opcaoErrada2 = "MAZZ", classeGramatical = "Conjunção", opcoesGramaticais = new string[] { "Conjunção", "Adjetivo", "Advérbio" } },
            new FraseDificil { fraseComLacuna = "Quanto _____ lemos livros, melhor escrevemos.", palavraCorreta = "MAIS", opcaoErrada1 = "MAS", opcaoErrada2 = "MÁS", classeGramatical = "Advérbio", opcoesGramaticais = new string[] { "Advérbio", "Conjunção", "Substantivo" } },

            // --- TEMA 9: CAVALHEIRO vs CAVALEIRO (6 Frases) ---
            new FraseDificil { fraseComLacuna = "O homem cedeu o lugar, ele é um verdadeiro _____.", palavraCorreta = "CAVALHEIRO", opcaoErrada1 = "CAVALEIRO", opcaoErrada2 = "CABALERO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "O _____ montou em seu alazão e cavalgou.", palavraCorreta = "CAVALEIRO", opcaoErrada1 = "CAVALHEIRO", opcaoErrada2 = "CAVAJEIRO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Ele puxou a cadeira para a amiga como um bom _____.", palavraCorreta = "CAVALHEIRO", opcaoErrada1 = "CAVALEIRO", opcaoErrada2 = "CAVALEIROSO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Na Idade Média, o nobre _____ usava armadura de ferro.", palavraCorreta = "CAVALEIRO", opcaoErrada1 = "CAVALHEIRO", opcaoErrada2 = "CAVALO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Aquele senhor educado é o típico _____ gentil.", palavraCorreta = "CAVALHEIRO", opcaoErrada1 = "CAVALEIRO", opcaoErrada2 = "CAVALEIRICE", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "O valente _____ disputou o torneio a cavalo.", palavraCorreta = "CAVALEIRO", opcaoErrada1 = "CAVALHEIRO", opcaoErrada2 = "CAVALARIA", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },

            // --- TEMA 10: SOAR vs SUAR (6 Frases) ---
            new FraseDificil { fraseComLacuna = "O sino da igreja costuma _____ às 18 horas.", palavraCorreta = "SOAR", opcaoErrada1 = "SUAR", opcaoErrada2 = "ZUAR", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "Comecei a _____ muito após a corrida no parque.", palavraCorreta = "SUAR", opcaoErrada1 = "SOAR", opcaoErrada2 = "SOOAR", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "O alarme da escola começou a _____ bem alto.", palavraCorreta = "SOAR", opcaoErrada1 = "SUAR", opcaoErrada2 = "SOANTE", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "No calor intenso do verão, é natural a gente _____.", palavraCorreta = "SUAR", opcaoErrada1 = "SOAR", opcaoErrada2 = "SUOR", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "O trompete afinado fez um lindo som _____ na sala.", palavraCorreta = "SOAR", opcaoErrada1 = "SUAR", opcaoErrada2 = "SOARES", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "A partida de futebol fez todos os jogadores _____.", palavraCorreta = "SUAR", opcaoErrada1 = "SOAR", opcaoErrada2 = "SUADOS", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },

            // --- TEMA 11: COMPRIMENTO vs CUMPRIMENTO (6 Frases) ---
            new FraseDificil { fraseComLacuna = "O _____ do tecido da sala é de três metros.", palavraCorreta = "COMPRIMENTO", opcaoErrada1 = "CUMPRIMENTO", opcaoErrada2 = "CONPRIMENTO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Dei um _____ amigável no meu novo vizinho.", palavraCorreta = "CUMPRIMENTO", opcaoErrada1 = "COMPRIMENTO", opcaoErrada2 = "CUMPRIMENTTO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Medimos a fita métrica e conferimos seu _____.", palavraCorreta = "COMPRIMENTO", opcaoErrada1 = "CUMPRIMENTO", opcaoErrada2 = "COMPRIDÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Ao entrar na sala, o aluno fez um educado _____.", palavraCorreta = "CUMPRIMENTO", opcaoErrada1 = "COMPRIMENTO", opcaoErrada2 = "CUMPRIR", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "A ponte nova tem cem metros de _____ total.", palavraCorreta = "COMPRIMENTO", opcaoErrada1 = "CUMPRIMENTO", opcaoErrada2 = "COMPRIMIDO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Receba meus parabéns e meu caloroso _____!", palavraCorreta = "CUMPRIMENTO", opcaoErrada1 = "COMPRIMENTO", opcaoErrada2 = "CUMPRIMENTAR", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } }
        };
    }
}
