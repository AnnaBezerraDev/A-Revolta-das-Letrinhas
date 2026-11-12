using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public class ReferenciasDoPainel
    {
        public TextMeshProUGUI balaoTexto;
        public CartaBotao[] botoes;
        public GameObject painelObjeto;
        public Image imagemFundoCarta;

        [Header("Arraste os 3 corações DESTA tela aqui")]
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

    [Header("Sistema de Vidas")]
    public int vidasMaximas = 3;
    private int vidasAtuais;

    [Header("Visual das Cartas")]
    public Sprite[] temasDasCartas;

    [Header("Banco de Palavras")]
    public List<string> palavrasFaceis = new List<string>();
    public List<string> palavrasMedias = new List<string>();
    public List<FraseDificil> frasesDificeis = new List<FraseDificil>();

    private ReferenciasDoPainel uiAtual;
    private int nivelAtualID;
    public int NivelAtualID => nivelAtualID;
    public bool JogandoMatematica => jogandoMatematica;
    private int faseAtualID = 0;
    private bool jogandoMatematica = false;
    private string respostaCorreta;
    private string palavraCompleta;
    private bool podeInteragir = true;
    private string alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    // Controle de 2 Etapas da Fase Difícil de Português (0 = Palavra, 1 = Classe Gramatical)
    private int etapaDificilAtual = 0;
    private string fraseOriginalDificil = "";
    private FraseDificil fraseDificilAtual;

    private float tempoUltimoClique = 0f;

    private Action actionScript;
    private GerenciadorFases gerenciadorFases;

    void Awake()
    {
        if (palavrasFaceis == null || palavrasFaceis.Count == 0) CarregarPalavrasFaceis();
        if (palavrasMedias == null || palavrasMedias.Count == 0) CarregarPalavrasMedias();
        if (frasesDificeis == null || frasesDificeis.Count == 0) CarregarFrasesDificeis();
    }

    void Start()
    {
        actionScript = FindFirstObjectByType<Action>();
        gerenciadorFases = FindFirstObjectByType<GerenciadorFases>();
    }

    public void IniciarFaseEspecifica(int numeroFase, int nivelDificuldade, bool isMath)
    {
        nivelAtualID = nivelDificuldade;
        faseAtualID = numeroFase;
        jogandoMatematica = isMath;

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

        podeInteragir = true;
        tempoUltimoClique = 0f;
        etapaDificilAtual = 0;

        if (isMath)
        {
            ConfigurarFaseMatematica(numeroFase, nivelDificuldade);
        }
        else
        {
            if (nivelDificuldade == 2)
            {
                ConfigurarFaseFrase(numeroFase);
            }
            else
            {
                string palavraDaFase = "";
                if (nivelDificuldade == 0 && numeroFase < palavrasFaceis.Count) palavraDaFase = palavrasFaceis[numeroFase];
                if (nivelDificuldade == 1 && numeroFase < palavrasMedias.Count) palavraDaFase = palavrasMedias[numeroFase];

                if (!string.IsNullOrEmpty(palavraDaFase))
                {
                    palavraCompleta = palavraDaFase.Trim().ToUpper();
                    AplicarRegrasDeLacuna();
                    ConfigurarBotoesComRegras();
                }
                else
                {
                    Debug.LogError($"Fase {numeroFase} não possui palavra cadastrada no nível {nivelAtualID}!");
                }
            }
        }
    }

    void AtualizarUIVidas()
    {
        if (uiAtual == null || uiAtual.iconesVidasPainel == null) return;

        for (int i = 0; i < uiAtual.iconesVidasPainel.Length; i++)
        {
            if (uiAtual.iconesVidasPainel[i] != null)
            {
                uiAtual.iconesVidasPainel[i].SetActive(i < vidasAtuais);
            }
        }
    }

    void AtualizarIndicadorFase(int numeroFase)
    {
        if (uiAtual == null) return;

        // Ocultar / remover qualquer indicador de fase na gameplay conforme solicitado
        if (uiAtual.textoIndicadorFase != null)
        {
            uiAtual.textoIndicadorFase.text = "";
            uiAtual.textoIndicadorFase.gameObject.SetActive(false);
        }

        if (uiAtual.painelObjeto != null)
        {
            Transform tExistente = uiAtual.painelObjeto.transform.Find("IndicadorFase");
            if (tExistente != null)
            {
                tExistente.gameObject.SetActive(false);
            }
        }
    }

    void ConfigurarFaseMatematica(int numeroFase, int nivelDificuldade)
    {
        string pergunta = "";
        int correta = 0;

        Random.InitState(numeroFase + (nivelDificuldade * 100) + 1234);

        if (nivelDificuldade == 0)
        {
            if (numeroFase < 10)
            {
                int a = Random.Range(10, 50); int b = Random.Range(10, 50);
                pergunta = $"{a} + {b} = ?"; correta = a + b;
            }
            else if (numeroFase < 20)
            {
                int a = Random.Range(30, 99); int b = Random.Range(10, a);
                pergunta = $"{a} - {b} = ?"; correta = a - b;
            }
            else
            {
                int a = Random.Range(2, 11); int b = Random.Range(2, 11);
                pergunta = $"{a} x {b} = ?"; correta = a * b;
            }
        }
        else if (nivelDificuldade == 1)
        {
            if (numeroFase < 10)
            {
                int divisor = Random.Range(2, 10); int quociente = Random.Range(5, 20); int dividendo = divisor * quociente;
                pergunta = $"{dividendo} ÷ {divisor} = ?"; correta = quociente;
            }
            else if (numeroFase < 20)
            {
                int a = Random.Range(10, 50); int b = Random.Range(2, 10); int c = Random.Range(2, 10);
                pergunta = $"{a} + ({b} x {c}) = ?"; correta = a + (b * c);
            }
            else
            {
                int a = Random.Range(15, 99); int b = Random.Range(5, 20);
                pergunta = $"({a} x 2) - {b} = ?"; correta = (a * 2) - b;
            }
        }
        else
        {
            if (numeroFase < 10)
            {
                int a = Random.Range(2, 9);
                pergunta = $"{a}² = ?"; correta = a * a;
            }
            else if (numeroFase < 20)
            {
                int a = Random.Range(3, 10); int b = Random.Range(2, 6); int c = Random.Range(2, 8);
                pergunta = $"({a} x {b}) + {c}² = ?"; correta = (a * b) + (c * c);
            }
            else
            {
                int a = Random.Range(20, 50); int b = Random.Range(2, 10); int c = Random.Range(2, 5);
                pergunta = $"({a} + {b}) x {c} = ?"; correta = (a + b) * c;
            }
        }

        respostaCorreta = correta.ToString();
        if (uiAtual.balaoTexto != null) uiAtual.balaoTexto.text = pergunta;

        List<string> opcoes = new List<string> { respostaCorreta };
        int errada1 = correta + Random.Range(1, 10);
        int errada2 = correta - Random.Range(1, 10);
        if (errada2 == correta || errada2 == errada1) errada2 = correta + 15;

        opcoes.Add(errada1.ToString());
        opcoes.Add(errada2.ToString());

        opcoes = EmbaralharLista(opcoes);

        for (int i = 0; i < uiAtual.botoes.Length; i++)
        {
            if (i < opcoes.Count && uiAtual.botoes[i] != null)
                uiAtual.botoes[i].DefinirLetra(opcoes[i]);
        }

        Random.InitState((int)System.DateTime.Now.Ticks);
    }

    void ConfigurarFaseFrase(int numeroFase)
    {
        if (numeroFase < frasesDificeis.Count)
        {
            etapaDificilAtual = 0;
            fraseDificilAtual = frasesDificeis[numeroFase];
            fraseOriginalDificil = fraseDificilAtual.fraseComLacuna;

            respostaCorreta = fraseDificilAtual.palavraCorreta.Trim().ToUpper();
            palavraCompleta = fraseOriginalDificil.Replace("_____", respostaCorreta);

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
            // Para outras letras, pegar letras visualmente e foneticamente próximas
            candidatosConfusos.AddRange(new string[] { "B", "P", "D", "T", "V", "F", "M", "N" });
        }

        // Embaralha intensamente os candidatos confusos
        candidatosConfusos = EmbaralharLista(candidatosConfusos);

        foreach (var c in candidatosConfusos)
        {
            if (c != respostaCorreta && !opcoes.Contains(c) && opcoes.Count < uiAtual.botoes.Length)
            {
                opcoes.Add(c);
            }
        }

        PreencherComLetrasAleatorias(opcoes);

        // Embaralha completamente a ordem das opções nos botões para confundir o jogador
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
        // Embaralhamento robusto com múltiplas rodadas Fisher-Yates e semente de alta entropia
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
            entradaNormalizada = Regex.Replace(entradaNormalizada, "[^0-9-]", "");
            corretaNormalizada = Regex.Replace(corretaNormalizada, "[^0-9-]", "");
        }
        else
        {
            entradaNormalizada = entradaNormalizada.Replace("\u200B", "").Replace("\u00A0", "").Trim().ToUpper();
            corretaNormalizada = corretaNormalizada.Replace("\u200B", "").Replace("\u00A0", "").Trim().ToUpper();
        }

        if (entradaNormalizada == corretaNormalizada && !string.IsNullOrEmpty(corretaNormalizada))
        {
            
            if (!jogandoMatematica && nivelAtualID == 2 && etapaDificilAtual == 0)
            {
             
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

            if (sfxSource != null && somAcerto != null) sfxSource.PlayOneShot(somAcerto);

            if (gerenciadorFases != null) gerenciadorFases.MarcarFaseComoCompleta();

            string materia = jogandoMatematica ? "Math" : "Port";
            string chaveVidas = $"VidasRestantes_{materia}_{nivelAtualID}_{faseAtualID}";
            int vidasSalvas = GerenciadorPerfis.GetInt(chaveVidas, 0);
            if (vidasAtuais > vidasSalvas)
            {
                GerenciadorPerfis.SetInt(chaveVidas, vidasAtuais);
                PlayerPrefs.Save();
            }

            if (uiAtual.balaoTexto != null)
            {
                if (jogandoMatematica) uiAtual.balaoTexto.text = uiAtual.balaoTexto.text.Replace("?", corretaNormalizada);
                else if (nivelAtualID == 2)
                {
                    uiAtual.balaoTexto.enableAutoSizing = true;
                    uiAtual.balaoTexto.fontSizeMin = 36;
                    uiAtual.balaoTexto.fontSizeMax = 68;
                    uiAtual.balaoTexto.text = $"Muito bem!\n\"<color=#007700>{fraseDificilAtual.palavraCorreta}</color>\" é <color=#007700>{fraseDificilAtual.classeGramatical.ToUpper()}</color>!";
                }
                else uiAtual.balaoTexto.text = palavraCompleta;
            }

            Invoke("MostrarTelaFaseConcluida", 1.0f);
        }
        else
        {
            vidasAtuais--;
            AtualizarUIVidas();

            if (sfxSource != null && somErro != null) sfxSource.PlayOneShot(somErro);

            if (vidasAtuais <= 0)
            {
                podeInteragir = false;
                Invoke("MostrarTelaGameOver", 0.5f);
            }
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
                    vidasPreservadas += GerenciadorPerfis.GetInt($"VidasRestantes_{materia}_{nivelAtualID}_{i}", 1);
                }

                int totalCoracoesPossiveis = 30 * 3; 
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

                if (actionScript.VerificarJogo100PorCento())
                {
                    actionScript.ExecutarFinalDeJogo(telaJogoAtual);
                    return;
                }

                actionScript.TrocarTela(telaJogoAtual, painelDestino);
            }
            else
            {
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

    private void CarregarPalavrasFaceis()
    {
        
        palavrasFaceis = new List<string>
        {
            
            "PÁSSARO", "CHAVE", "CAIXA", "ZEBRA", "MAÇÃ", "CASA",
            
            "VASSOURA", "CHUVA", "LIXO", "BUZINA", "ONÇA", "CEBOLA",
           
            "OSSO", "CHAPÉU", "XÍCARA", "AZEITE", "TAÇA", "CENOURA",
           
            "MASSA", "CHÃO", "PEIXE", "DOZE", "MOÇA", "CIDADE",
            
            "PASSEIO", "BICHO", "XAROPE", "RAIZ", "LAÇO", "BICO"
        };
    }

    private void CarregarPalavrasMedias()
    {
        
        palavrasMedias = new List<string>
        {
          
            "PROFESSOR", "CHOCOLATE", "ABACAXI", "AMIZADE", "CRIANÇA", "CINEMA",
          
            "ASSINATURA", "DEZENA", "XADREZ", "NATUREZA", "CORAÇÃO", "BACIA",
          
            "TRAVESSEIRO", "CHAMINÉ", "ENXADA", "TEXTO", "BALANÇA", "OFICINA",
           
            "GIRASSOL", "CHUVEIRO", "CAIXOTE", "BATIZADO", "PALHAÇO", "PACIÊNCIA",
           
            "COMPASSO", "MOCHILA", "LIXEIRA", "HORIZONTE", "CARROÇA", "CEREJA"
        };
    }

    private void CarregarFrasesDificeis()
    {
       
        frasesDificeis = new List<FraseDificil>
        {
           
            new FraseDificil { fraseComLacuna = "Eu não sei o _____ de tanta confusão.", palavraCorreta = "PORQUÊ", opcaoErrada1 = "POR QUE", opcaoErrada2 = "PORQUE", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Verbo", "Conjunção" } },
            new FraseDificil { fraseComLacuna = "Ele é um _____ exemplo para os colegas.", palavraCorreta = "MAU", opcaoErrada1 = "MAL", opcaoErrada2 = "MÁL", classeGramatical = "Adjetivo", opcoesGramaticais = new string[] { "Adjetivo", "Advérbio", "Substantivo" } },
            new FraseDificil { fraseComLacuna = "A _____ de cinema começa exatamente às 20h.", palavraCorreta = "SESSÃO", opcaoErrada1 = "SEÇÃO", opcaoErrada2 = "CESSÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "O suspeito foi pego em _____ pela polícia.", palavraCorreta = "FLAGRANTE", opcaoErrada1 = "FRAGRANTE", opcaoErrada2 = "FLAGANTE", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Meu relógio quebrou e precisa de um _____.", palavraCorreta = "CONSERTO", opcaoErrada1 = "CONCERTO", opcaoErrada2 = "CONÇERTO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "O governo prevê uma alta na _____ de importação.", palavraCorreta = "TAXA", opcaoErrada1 = "TACHA", opcaoErrada2 = "TÂXA", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },

            
            new FraseDificil { fraseComLacuna = "O motorista _____ as leis de trânsito.", palavraCorreta = "INFRINGIU", opcaoErrada1 = "INFLIGIU", opcaoErrada2 = "INFLINGIU", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "Ele agiu com muita _____, ninguém percebeu.", palavraCorreta = "DISCRIÇÃO", opcaoErrada1 = "DESCRIÇÃO", opcaoErrada2 = "DESCREÇÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Eles queriam sair, _____ estava chovendo.", palavraCorreta = "MAS", opcaoErrada1 = "MAIS", opcaoErrada2 = "MÁS", classeGramatical = "Conjunção", opcoesGramaticais = new string[] { "Conjunção", "Advérbio", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "O homem cedeu o lugar, ele é um verdadeiro _____.", palavraCorreta = "CAVALHEIRO", opcaoErrada1 = "CAVALEIRO", opcaoErrada2 = "CABALERO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Fui à _____ de eletrodomésticos para comprar uma TV.", palavraCorreta = "SEÇÃO", opcaoErrada1 = "SESSÃO", opcaoErrada2 = "CESSÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Gostaria de saber _____ você não foi à aula.", palavraCorreta = "POR QUE", opcaoErrada1 = "PORQUE", opcaoErrada2 = "PORQUÊ", classeGramatical = "Pronome", opcoesGramaticais = new string[] { "Pronome", "Substantivo", "Verbo" } },

           
            new FraseDificil { fraseComLacuna = "Fui _____ na prova de matemática hoje.", palavraCorreta = "MAL", opcaoErrada1 = "MAU", opcaoErrada2 = "MAIS", classeGramatical = "Advérbio", opcoesGramaticais = new string[] { "Advérbio", "Adjetivo", "Substantivo" } },
            new FraseDificil { fraseComLacuna = "O _____ do pneu do carro de corrida furou.", palavraCorreta = "CÂMARA", opcaoErrada1 = "CÂMERA", opcaoErrada2 = "CAMARA", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "O sino da igreja costuma _____ às 18 horas.", palavraCorreta = "SOAR", opcaoErrada1 = "SUAR", opcaoErrada2 = "ZUAR", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "O _____ do tecido da sala é de três metros.", palavraCorreta = "COMPRIMENTO", opcaoErrada1 = "CUMPRIMENTO", opcaoErrada2 = "CONPRIMENTO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Não vou sair, _____ está chovendo bastante.", palavraCorreta = "PORQUE", opcaoErrada1 = "POR QUÊ", opcaoErrada2 = "POR QUE", classeGramatical = "Conjunção", opcoesGramaticais = new string[] { "Conjunção", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "O carro estava na beira do abismo, o perigo era _____.", palavraCorreta = "IMINENTE", opcaoErrada1 = "EMINENTE", opcaoErrada2 = "INIMENTE", classeGramatical = "Adjetivo", opcoesGramaticais = new string[] { "Adjetivo", "Substantivo", "Verbo" } },

         
            new FraseDificil { fraseComLacuna = "A _____ dos bens para a caridade foi assinada.", palavraCorreta = "CESSÃO", opcaoErrada1 = "SEÇÃO", opcaoErrada2 = "SESSÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "O perfume dela é muito _____.", palavraCorreta = "FRAGRANTE", opcaoErrada1 = "FLAGRANTE", opcaoErrada2 = "FRAGANTE", classeGramatical = "Adjetivo", opcoesGramaticais = new string[] { "Adjetivo", "Substantivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Ontem assistimos a um lindo _____ de piano.", palavraCorreta = "CONCERTO", opcaoErrada1 = "CONSERTO", opcaoErrada2 = "CONÇERTO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Precisamos de _____ tempo para terminar o projeto.", palavraCorreta = "MAIS", opcaoErrada1 = "MAS", opcaoErrada2 = "MÁS", classeGramatical = "Advérbio", opcoesGramaticais = new string[] { "Advérbio", "Conjunção", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Não deixe ninguém te _____ de preguiçoso.", palavraCorreta = "TACHAR", opcaoErrada1 = "TAXAR", opcaoErrada2 = "TÂCHAR", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "O juiz _____ uma pena severa ao réu.", palavraCorreta = "INFLIGIU", opcaoErrada1 = "INFRINGIU", opcaoErrada2 = "INFLINGIU", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },

           
            new FraseDificil { fraseComLacuna = "A _____ da paisagem no livro é perfeita.", palavraCorreta = "DESCRIÇÃO", opcaoErrada1 = "DISCRIÇÃO", opcaoErrada2 = "DESCREÇÃO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Comecei a _____ muito após a corrida no parque.", palavraCorreta = "SUAR", opcaoErrada1 = "SOAR", opcaoErrada2 = "SOOAR", classeGramatical = "Verbo", opcoesGramaticais = new string[] { "Verbo", "Substantivo", "Adjetivo" } },
            new FraseDificil { fraseComLacuna = "O _____ montou em seu alazão e cavalgou.", palavraCorreta = "CAVALEIRO", opcaoErrada1 = "CAVALHEIRO", opcaoErrada2 = "CAVAJEIRO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Ele sorriu quando viu a _____ fotográfica.", palavraCorreta = "CÂMERA", opcaoErrada1 = "CÂMARA", opcaoErrada2 = "CAMÂRA", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Dei um _____ amigável no meu novo vizinho.", palavraCorreta = "CUMPRIMENTO", opcaoErrada1 = "COMPRIMENTO", opcaoErrada2 = "CUMPRIMENTTO", classeGramatical = "Substantivo", opcoesGramaticais = new string[] { "Substantivo", "Adjetivo", "Verbo" } },
            new FraseDificil { fraseComLacuna = "Aquele cientista é uma figura _____ no mundo.", palavraCorreta = "EMINENTE", opcaoErrada1 = "IMINENTE", opcaoErrada2 = "ENIMENTE", classeGramatical = "Adjetivo", opcoesGramaticais = new string[] { "Adjetivo", "Substantivo", "Verbo" } }
        };
    }
}