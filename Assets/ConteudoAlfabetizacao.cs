using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Conteúdo das fases para alunos do 1º e 2º ano (ciclo de alfabetização).
/// - Português: completar a VOGAL que falta em palavras pequenas (ex.: B_LA → BOLA).
/// - Matemática: somas só com números de 1 algarismo (ex.: 2 + 6).
/// Do 3º ano em diante, o jogo continua usando o conteúdo original do GameManager.
/// </summary>
public static class ConteudoAlfabetizacao
{
    // ==========================================
    // ANO ESCOLAR A PARTIR DO NOME DA SALA
    // ==========================================

    /// <summary>Ano escolar escrito no nome da sala ("1º Ano A", "2°ANO B", "Ano 1"...). 0 = não identificado.</summary>
    public static int AnoDaSala(string sala)
    {
        if (string.IsNullOrWhiteSpace(sala)) return 0;
        string s = sala.Trim().ToLowerInvariant();

        Match m = Regex.Match(s, @"(\d+)\s*[º°ªo]?\s*\.?\s*ano");           // "1º ano", "3°ano", "2 ano", "1o ano"
        if (!m.Success) m = Regex.Match(s, @"ano\s*(\d+)");                  // "ano 1"
        if (!m.Success) m = Regex.Match(s, @"^(\d+)\s*[º°ª]");               // "2ºA", "1ª série"
        if (m.Success && int.TryParse(m.Groups[1].Value, out int ano)) return ano;

        string[] ordinais = { "primeiro", "segundo", "terceiro", "quarto", "quinto" };
        for (int i = 0; i < ordinais.Length; i++)
        {
            if (s.Contains(ordinais[i])) return i + 1;
        }
        return 0;
    }

    /// <summary>true se a sala for do 1º ou 2º ano.</summary>
    public static bool SalaDeAlfabetizacao(string sala)
    {
        int ano = AnoDaSala(sala);
        return ano >= 1 && ano < 3;
    }

    // ==========================================
    // PORTUGUÊS: COMPLETAR A VOGAL
    // ==========================================

    private const string VOGAIS = "AEIOU";

    // Fácil: 3 a 4 letras, sílabas simples
    private static readonly string[] PalavrasFaceis =
    {
        "UVA", "OVO", "LUA", "RUA", "FIO", "BOLA", "CASA", "GATO", "PATO", "SAPO",
        "DADO", "MALA", "VACA", "FACA", "LOBO", "SUCO", "BOCA", "MESA", "PIPA", "BOLO",
        "FOGO", "DEDO", "TATU", "MULA", "RATO", "CAMA", "LATA", "PENA", "VELA", "MOTO",
        "DOCE", "FOCA", "SINO", "MAPA", "LAGO", "FADA", "GALO", "BULE", "COCO", "NABO"
    };

    // Médio: 4 a 5 letras
    private static readonly string[] PalavrasMedias =
    {
        "BOTA", "SOPA", "FLOR", "URSO", "NUVEM", "LIVRO", "PRATO", "CARRO", "MANGA", "BANCO",
        "PORTA", "FESTA", "LEITE", "PERNA", "PEDRA", "PULGA", "PIANO", "VENTO", "VERDE", "FORNO",
        "GARFO", "BARCO", "CORDA", "PRAIA", "FRUTA", "TIGRE", "ZEBRA", "MOSCA", "PORCO", "TURMA",
        "CINTO", "PENTE", "PASTA", "DENTE", "MUNDO", "GORRO", "SALTO", "BOLSA", "TERRA", "NARIZ"
    };

    // Difícil: 5 a 7 letras (ainda palavras do dia a dia da criança)
    private static readonly string[] PalavrasDificeis =
    {
        "SAPATO", "CABELO", "JANELA", "PANELA", "BONECA", "MACACO", "CAVALO", "GIRAFA", "TOMATE", "BANANA",
        "ESCOLA", "PIPOCA", "CANETA", "BALEIA", "BIGODE", "CAMELO", "CEBOLA", "TAPETE", "PERUCA", "GALINHA",
        "MOCHILA", "CENOURA", "BATATA", "CADEIRA", "BONECO", "COELHO", "ABELHA", "ANIMAL", "CORUJA", "GAVETA",
        "MALETA", "MENINO", "MENINA", "PIRATA", "SACOLA", "TUCANO", "FORMIGA", "MORANGO", "PEPINO", "AMIGO"
    };

    // Palavras comuns usadas só para evitar duas respostas certas (ex.: B_LO pode ser BOLO ou BOLA).
    private static readonly string[] OutrasPalavrasComuns =
    {
        "GELO", "PATA", "BALA", "BELA", "MATO", "SECO", "SACO", "LOGO", "VILA", "PAPA", "COMA", "COPA",
        "COPO", "GOTA", "GATA", "LOBA", "RATA", "TIA", "TIO", "RIO", "MEIA", "MAIO", "MEIO", "PELO",
        "POLO", "PULO", "MOLE", "BELO", "BOTE", "VOTO", "MITO", "MATA", "LADO", "LODO", "MEDO", "MODO",
        "NADA", "SALA", "SELA", "SOLA", "CEDO", "CADA", "COLA", "CALA", "MAGO", "PODE", "PEDE", "LUVA",
        "LAVA", "LEVA", "NOVE", "NEVE", "NAVE", "FICA", "CACO", "CUCO", "CASO", "LIGA", "RITO", "MOLA",
        "MALO", "SAPA", "PETO", "FADO", "MACACA", "BICA", "CANTO", "CONTO", "PINTA", "PONTE", "VENDA",
        "PASTO", "PRATA", "PORTO", "PERTO", "BURRO", "MESMO", "AVO", "OVA", "RIA", "LIA",
        "BULA", "DADA", "FACE", "SOCO", "FIGO", "FOGE", "TATO", "RETO", "ROTO", "CIMA", "LUTA", "LATE",
        "VALA", "FOCO", "SONO", "LEGO", "LIGO", "GALA", "BOTO", "BATA", "URSA", "LIVRE", "PRETO", "CORRO",
        "BANCA", "PEDRO", "BARCA", "FRUTO", "FRITA", "FROTA", "PORCA", "CENTO", "CINTA", "PISTA", "POSTA",
        "MANDO", "SOLTO", "SALTA", "BOLSO", "BALSA", "GALINHO", "COELHA", "AMIGA",
        // trocas de sílaba/consoante do 2º ano que formam palavras de verdade
        "BORA", "JATO", "GADO", "PAPO", "MANA", "VAGA", "ROBO", "DOCA", "MUSA", "TUTU", "NULA", "CANA",
        "GAMA", "NATA", "LAPA", "LADA", "PELA", "VERA", "SILO", "LACO", "CALO", "PULE", "COCA", "NADO",
        "PAMELA", "CAVALA", "CAMILO", "TOPETE", "BATUTA", "MULETA", "COMODA", "COMIDO", "SARADA", "CASE",
        "CATO", "BATO", "MELA", "SENO", "SINA", "LOTA", "BOLE", "DOLO", "BORO", "LOBA", "GOLA", "TOCA",
        "FATO", "MATO", "TATO", "LUTO", "SELO", "MOLA", "BOTE", "PATE", "BECA", "CAPELO", "MACUCO",
        "BOBA", "BODA", "TOLA", "LOLA", "CAPA", "CARA", "CATA", "CAVA", "RASA", "NASA", "GAGO", "GAMO",
        "PAGO", "PANO", "PARO", "DANO", "FALA", "MAMA", "PALA", "RALA", "TALA", "MAGA", "MACA", "PACA",
        "SACA", "VARA", "FAMA", "FAVA", "BOBO", "LOTO", "SUMO", "MUCO", "MOCA", "ROCA", "SOCA", "LOCA",
        "MEGA", "MERA", "META", "PESA", "PISA", "RIPA", "PILA", "PIRA", "ROLO", "SOLO", "TOLO", "FOFO",
        "FOTO", "TABU", "GULA", "MUDA", "PULA", "RALO", "RAMO", "RARO", "RASO", "DAMA", "LAMA", "DATA",
        "LARA", "PEGA", "PERA", "RENA", "TELA", "GELA", "MOFO", "MORO", "FOFA", "FORA", "FINO", "DINO",
        "PINO", "SIGO", "TAPA", "VAGO", "LAVO", "TALO", "COLO", "COMO", "CORO", "TOCO", "NAMO", "PAGA",
        "PARA", "LUPA", "LUNA", "PATELA", "SONECA", "CAVADO", "GIRAVA", "CANELA", "CARETA", "REBOLA",
        "BARATA", "PALETA", "SALETA", "MERINO", "MEDINA", "TUTANO", "COSIDA", "BALADA", "RALADA"
    };

    // Palavras que NÃO podem se formar trocando a vogal (palavrões e afins).
    private static readonly HashSet<string> Proibidas = new HashSet<string>
    {
        "FODA", "PUTA", "PUTO", "PICA", "PIPI", "BOSTA", "MERDA", "PORRA", "BUNDA", "ROLA", "PINTO",
        "CU", "PIROCA", "BUCETA", "CACETE", "CUZAO", "TETA", "BOSTO", "PUTECA", "PUTINHA",
        "CAGA", "FODO", "PEDO", "LULA", "RABO", "FALO", "CARALO", "CAGALO", "PITOCA", "CAPETA", "CAGETA",
        "MANETA", "SAFADA"
    };

    private static HashSet<string> _conhecidas;
    private static HashSet<string> Conhecidas
    {
        get
        {
            if (_conhecidas == null)
            {
                _conhecidas = new HashSet<string>();
                foreach (var p in PalavrasFaceis) _conhecidas.Add(p);
                foreach (var p in PalavrasMedias) _conhecidas.Add(p);
                foreach (var p in PalavrasDificeis) _conhecidas.Add(p);
                foreach (var p in OutrasPalavrasComuns) _conhecidas.Add(p);
                foreach (var p in PalavrasSilabas) _conhecidas.Add(p.Replace("-", ""));
            }
            return _conhecidas;
        }
    }

    public struct DesafioVogal
    {
        public string palavra;      // BOLA
        public string comLacuna;    // B_LA
        public string vogal;        // O
        public List<string> opcoes; // O, I, U (já embaralhadas)
    }

    // Última palavra de cada fase, para a criança ver uma palavra diferente ao jogar de novo
    private static readonly Dictionary<string, string> ultimaPalavraDaFase = new Dictionary<string, string>();

    /// <summary>Sorteia a palavra da fase e esconde uma vogal. qtdOpcoes = número de cartas de resposta.</summary>
    public static DesafioVogal SortearDesafioVogal(int nivel, int numeroFase, int qtdOpcoes)
    {
        string[] banco = nivel <= 0 ? PalavrasFaceis : (nivel == 1 ? PalavrasMedias : PalavrasDificeis);
        qtdOpcoes = Mathf.Clamp(qtdOpcoes, 2, VOGAIS.Length);

        string chave = nivel + "_" + numeroFase;
        ultimaPalavraDaFase.TryGetValue(chave, out string ultima);

        List<string> candidatas = new List<string>(banco);
        if (candidatas.Count > 1 && !string.IsNullOrEmpty(ultima)) candidatas.Remove(ultima);
        Embaralhar(candidatas);

        foreach (string palavra in candidatas)
        {
            if (TentarMontar(palavra, qtdOpcoes, true, out DesafioVogal desafio))
            {
                ultimaPalavraDaFase[chave] = palavra;
                return desafio;
            }
        }

        // Não deve acontecer; se acontecer, aceita alternativas que formam outra palavra comum
        TentarMontar(candidatas[0], qtdOpcoes, false, out DesafioVogal reserva);
        ultimaPalavraDaFase[chave] = candidatas[0];
        return reserva;
    }

    private static bool TentarMontar(string palavra, int qtdOpcoes, bool evitarOutraPalavra, out DesafioVogal desafio)
    {
        desafio = default;
        List<int> posicoes = new List<int>();
        for (int i = 0; i < palavra.Length; i++)
        {
            if (VOGAIS.IndexOf(palavra[i]) >= 0) posicoes.Add(i);
        }
        Embaralhar(posicoes);

        foreach (int pos in posicoes)
        {
            char certa = palavra[pos];
            List<string> erradas = new List<string>();
            foreach (char v in VOGAIS)
            {
                if (v == certa) continue;
                char[] letras = palavra.ToCharArray();
                letras[pos] = v;
                string formada = new string(letras);
                if (Proibidas.Contains(formada)) continue;
                if (evitarOutraPalavra && Conhecidas.Contains(formada)) continue;
                erradas.Add(v.ToString());
            }
            if (erradas.Count < qtdOpcoes - 1) continue;

            Embaralhar(erradas);
            List<string> opcoes = new List<string> { certa.ToString() };
            for (int i = 0; i < qtdOpcoes - 1; i++) opcoes.Add(erradas[i]);
            Embaralhar(opcoes);

            char[] comLacuna = palavra.ToCharArray();
            comLacuna[pos] = '_';
            desafio = new DesafioVogal
            {
                palavra = palavra,
                comLacuna = new string(comLacuna),
                vogal = certa.ToString(),
                opcoes = opcoes
            };
            return true;
        }
        return false;
    }

    // ==========================================
    // 2º ANO: SÍLABA E CONSOANTE QUE FALTAM
    // ==========================================

    // Palavras de sílabas simples (consoante + vogal), já separadas
    private static readonly string[] PalavrasSilabas =
    {
        "BO-LA", "CA-SA", "GA-TO", "PA-TO", "SA-PO", "DA-DO", "MA-LA", "VA-CA", "FA-CA", "LO-BO",
        "SU-CO", "BO-CA", "ME-SA", "PI-PA", "BO-LO", "FO-GO", "DE-DO", "TA-TU", "MU-LA", "RA-TO",
        "CA-MA", "LA-TA", "PE-NA", "VE-LA", "MO-TO", "FO-CA", "SI-NO", "MA-PA", "LA-GO", "GA-LO",
        "BU-LE", "CO-CO", "NA-BO", "PA-TA", "LU-VA",
        "MA-CA-CO", "JA-NE-LA", "PA-NE-LA", "BO-NE-CA", "CA-VA-LO", "GI-RA-FA", "TO-MA-TE", "BA-NA-NA",
        "PI-PO-CA", "CA-NE-TA", "CA-BE-LO", "SA-PA-TO", "CA-ME-LO", "CE-BO-LA", "TA-PE-TE", "PE-RU-CA",
        "BA-TA-TA", "MA-LE-TA", "ME-NI-NO", "ME-NI-NA", "GA-VE-TA", "SA-CO-LA", "TU-CA-NO", "PE-PI-NO",
        "CO-RU-JA", "BI-GO-DE", "PE-TE-CA", "CO-MI-DA", "SA-LA-DA"
    };

    // Letras que as crianças costumam trocar (B/P, D/T, F/V, M/N, L/R...): viram as opções erradas
    private static readonly Dictionary<char, string> ConsoantesParecidas = new Dictionary<char, string>
    {
        { 'B', "PD" }, { 'P', "BT" }, { 'D', "TB" }, { 'T', "DP" }, { 'F', "V" }, { 'V', "FB" },
        { 'G', "CJ" }, { 'C', "G" }, { 'J', "G" }, { 'L', "RN" }, { 'R', "L" }, { 'M', "N" },
        { 'N', "ML" }, { 'S', "Z" }, { 'Z', "S" }
    };

    /// <summary>Desafio de completar a palavra: vale para vogal, sílaba ou consoante.</summary>
    public struct DesafioLacuna
    {
        public string palavra;      // BOLA
        public string comLacuna;    // B_LA, BO__ ou BO_A
        public string resposta;     // O, LA ou L
        public List<string> opcoes; // já embaralhadas
        public string tipo;         // "vogal", "silaba" ou "consoante"
    }

    /// <summary>
    /// Desafio de Português para o 1º e 2º ano.
    /// 1º ano: vogal em todos os níveis. 2º ano: Fácil = vogal (palavras maiores), Médio = sílaba, Difícil = consoante.
    /// </summary>
    public static DesafioLacuna SortearDesafio(int ano, int nivel, int numeroFase, int qtdOpcoes)
    {
        qtdOpcoes = Mathf.Clamp(qtdOpcoes, 2, 5);
        if (ano == 2 && nivel == 1) return SortearDesafioPalavraSilabas(nivel, numeroFase, qtdOpcoes, true);
        if (ano == 2 && nivel >= 2) return SortearDesafioPalavraSilabas(nivel, numeroFase, qtdOpcoes, false);

        int nivelBanco = ano == 2 ? 1 + (numeroFase % 2) : nivel; // 2º ano Fácil usa as palavras maiores
        DesafioVogal v = SortearDesafioVogal(nivelBanco, numeroFase, qtdOpcoes);
        return new DesafioLacuna { palavra = v.palavra, comLacuna = v.comLacuna, resposta = v.vogal, opcoes = v.opcoes, tipo = "vogal" };
    }

    private static DesafioLacuna SortearDesafioPalavraSilabas(int nivel, int numeroFase, int qtdOpcoes, bool silaba)
    {
        string chave = (silaba ? "S" : "C") + nivel + "_" + numeroFase;
        ultimaPalavraDaFase.TryGetValue(chave, out string ultima);
        List<string> candidatas = new List<string>(PalavrasSilabas);
        if (!string.IsNullOrEmpty(ultima)) candidatas.Remove(ultima);
        Embaralhar(candidatas);

        foreach (string separada in candidatas)
        {
            bool ok = silaba ? TentarMontarSilaba(separada, qtdOpcoes, out DesafioLacuna d) : TentarMontarConsoante(separada, qtdOpcoes, out d);
            if (ok)
            {
                ultimaPalavraDaFase[chave] = separada;
                return d;
            }
        }
        // Não deve acontecer: cai para a vogal
        DesafioVogal v = SortearDesafioVogal(1, numeroFase, qtdOpcoes);
        return new DesafioLacuna { palavra = v.palavra, comLacuna = v.comLacuna, resposta = v.vogal, opcoes = v.opcoes, tipo = "vogal" };
    }

    private static bool FormaPalavraProibidaOuConhecida(string formada) => Proibidas.Contains(formada) || Conhecidas.Contains(formada);

    private static bool TentarMontarSilaba(string separada, int qtdOpcoes, out DesafioLacuna desafio)
    {
        desafio = default;
        string[] silabas = separada.Split('-');
        List<int> indices = new List<int>();
        for (int i = 0; i < silabas.Length; i++) indices.Add(i);
        Embaralhar(indices);

        foreach (int i in indices)
        {
            string certa = silabas[i];
            if (certa.Length != 2) continue;
            char c = certa[0], v = certa[1];

            // Uma opção com a mesma consoante e outra vogal, e outra com consoante parecida e a mesma vogal
            List<string> trocaVogal = new List<string>();
            foreach (char o in VOGAIS) if (o != v) trocaVogal.Add("" + c + o);
            List<string> trocaConsoante = new List<string>();
            if (ConsoantesParecidas.TryGetValue(c, out string parecidas))
                foreach (char o in parecidas) trocaConsoante.Add("" + o + v);
            Embaralhar(trocaVogal);
            Embaralhar(trocaConsoante);

            List<string> erradas = new List<string>();
            foreach (string op in IntercalarListas(trocaConsoante, trocaVogal))
            {
                string[] copia = (string[])silabas.Clone();
                copia[i] = op;
                if (FormaPalavraProibidaOuConhecida(string.Join("", copia))) continue;
                if (!erradas.Contains(op)) erradas.Add(op);
                if (erradas.Count == qtdOpcoes - 1) break;
            }
            if (erradas.Count < qtdOpcoes - 1) continue;

            List<string> opcoes = new List<string> { certa };
            opcoes.AddRange(erradas);
            Embaralhar(opcoes);

            string[] lacuna = (string[])silabas.Clone();
            lacuna[i] = "__";
            desafio = new DesafioLacuna
            {
                palavra = string.Join("", silabas),
                comLacuna = string.Join("", lacuna),
                resposta = certa,
                opcoes = opcoes,
                tipo = "silaba"
            };
            return true;
        }
        return false;
    }

    private static bool TentarMontarConsoante(string separada, int qtdOpcoes, out DesafioLacuna desafio)
    {
        desafio = default;
        string palavra = separada.Replace("-", "");
        List<int> posicoes = new List<int>();
        for (int i = 0; i < palavra.Length; i++)
        {
            if (VOGAIS.IndexOf(palavra[i]) < 0 && ConsoantesParecidas.ContainsKey(palavra[i])) posicoes.Add(i);
        }
        Embaralhar(posicoes);

        const string OUTRAS = "BDFGLMNPRSTV";
        foreach (int pos in posicoes)
        {
            char certa = palavra[pos];
            List<char> preferidas = new List<char>(ConsoantesParecidas[certa]);
            List<char> outras = new List<char>(OUTRAS);
            Embaralhar(preferidas);
            Embaralhar(outras);

            List<string> erradas = new List<string>();
            foreach (char o in IntercalarListas(preferidas, outras))
            {
                if (o == certa || erradas.Contains(o.ToString())) continue;
                char[] letras = palavra.ToCharArray();
                letras[pos] = o;
                if (FormaPalavraProibidaOuConhecida(new string(letras))) continue;
                erradas.Add(o.ToString());
                if (erradas.Count == qtdOpcoes - 1) break;
            }
            if (erradas.Count < qtdOpcoes - 1) continue;

            List<string> opcoes = new List<string> { certa.ToString() };
            opcoes.AddRange(erradas);
            Embaralhar(opcoes);

            char[] lacuna = palavra.ToCharArray();
            lacuna[pos] = '_';
            desafio = new DesafioLacuna
            {
                palavra = palavra,
                comLacuna = new string(lacuna),
                resposta = certa.ToString(),
                opcoes = opcoes,
                tipo = "consoante"
            };
            return true;
        }
        return false;
    }

    /// <summary>Primeiro um da lista A, depois um da B, e assim por diante (a primeira opção errada vem da lista A).</summary>
    private static List<T> IntercalarListas<T>(List<T> a, List<T> b)
    {
        List<T> r = new List<T>();
        for (int i = 0; i < Mathf.Max(a.Count, b.Count); i++)
        {
            if (i < a.Count) r.Add(a[i]);
            if (i < b.Count) r.Add(b[i]);
        }
        return r;
    }

    // ==========================================
    // MATEMÁTICA DO 1º E 2º ANO
    // ==========================================

    /// <summary>
    /// Conta da fase. 1º ano: somas de 1 algarismo (ver SortearSoma).
    /// 2º ano: Fácil = somas e subtrações até 10; Médio = dezenas sem "vai um"/"empresta";
    /// Difícil = dobro, metade e probleminhas com dinheiro e objetos.
    /// </summary>
    public static void SortearConta(int ano, int nivel, out string pergunta, out int correta, out string passo, out bool comTexto)
    {
        comTexto = false;
        if (ano != 2)
        {
            SortearSoma(nivel, out int a1, out int b1);
            pergunta = $"{a1} + {b1} = ?";
            correta = a1 + b1;
            passo = PassoAPassoSoma(a1, b1);
            return;
        }

        if (nivel <= 0)
        {
            if (Random.value < 0.5f)
            {
                int a = Random.Range(1, 10), b = Random.Range(1, 10);
                pergunta = $"{a} + {b} = ?"; correta = a + b; passo = PassoAPassoSoma(a, b);
            }
            else
            {
                int a = Random.Range(3, 11), b = Random.Range(1, a);
                pergunta = $"{a} - {b} = ?"; correta = a - b;
                passo = $"• <b>Comece no {a}</b> e volte {b} nos dedos: {Contagem(a - 1, a - b, -1)}\n• <b>Resultado:</b> <color=#00E676>{correta}</color>";
            }
            return;
        }

        if (nivel == 1)
        {
            int dezA = Random.Range(1, 8), uniA = Random.Range(0, 10);
            if (Random.value < 0.5f)
            {
                int dezB = Random.Range(1, 10 - dezA), uniB = Random.Range(0, 10 - uniA);
                int a = dezA * 10 + uniA, b = dezB * 10 + uniB;
                pergunta = $"{a} + {b} = ?"; correta = a + b;
                passo = $"• <b>Unidades:</b> {uniA} + {uniB} = {uniA + uniB}\n• <b>Dezenas:</b> {dezA} + {dezB} = {dezA + dezB}\n• <b>Resultado:</b> <color=#00E676>{correta}</color>";
            }
            else
            {
                dezA = Random.Range(2, 10);
                int dezB = Random.Range(1, dezA), uniB = Random.Range(0, uniA + 1);
                int a = dezA * 10 + uniA, b = dezB * 10 + uniB;
                pergunta = $"{a} - {b} = ?"; correta = a - b;
                passo = $"• <b>Unidades:</b> {uniA} - {uniB} = {uniA - uniB}\n• <b>Dezenas:</b> {dezA} - {dezB} = {dezA - dezB}\n• <b>Resultado:</b> <color=#00E676>{correta}</color>";
            }
            return;
        }

        int tipo = Random.Range(0, 4);
        if (tipo == 0)
        {
            int n = Random.Range(2, 16);
            pergunta = $"O dobro de {n} é ?"; correta = n * 2;
            passo = $"• <b>Dobro</b> é somar o número com ele mesmo: {n} + {n}\n• <b>Resultado:</b> <color=#00E676>{correta}</color>";
        }
        else if (tipo == 1)
        {
            int n = Random.Range(2, 16) * 2;
            pergunta = $"A metade de {n} é ?"; correta = n / 2;
            passo = $"• <b>Metade</b> é dividir em 2 partes iguais: {n / 2} + {n / 2} = {n}\n• <b>Resultado:</b> <color=#00E676>{correta}</color>";
        }
        else if (tipo == 2)
        {
            string[] nomes = { "Ana", "Léo", "Bia", "Davi", "Lia", "Theo" };
            string nome = nomes[Random.Range(0, nomes.Length)];
            int a = Random.Range(3, 13), b = Random.Range(2, 9);
            pergunta = $"{nome} tem {a} reais e ganhou mais {b} reais. Quantos reais tem agora?";
            correta = a + b; comTexto = true;
            passo = $"• <b>Ganhou</b> é juntar: {a} + {b}\n• <b>Resultado:</b> <color=#00E676>{correta} reais</color>";
        }
        else
        {
            string[] coisas = { "balas", "figurinhas", "bolinhas de gude", "lápis de cor", "biscoitos" };
            bool[] feminino = { true, true, true, false, false };
            int k = Random.Range(0, coisas.Length);
            int a = Random.Range(8, 21), b = Random.Range(2, a - 1);
            pergunta = feminino[k]
                ? $"Havia {a} {coisas[k]}. Foram usadas {b}. Quantas sobraram?"
                : $"Havia {a} {coisas[k]}. Foram usados {b}. Quantos sobraram?";
            correta = a - b; comTexto = true;
            passo = $"• <b>Sobraram</b> é tirar: {a} - {b}\n• <b>Resultado:</b> <color=#00E676>{correta}</color>";
        }
    }

    private static string Contagem(int de, int ate, int passo)
    {
        StringBuilder sb = new StringBuilder();
        for (int n = de; passo > 0 ? n <= ate : n >= ate; n += passo)
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(n);
        }
        return sb.ToString();
    }

    // ==========================================
    // MATEMÁTICA: SOMAS COM 1 ALGARISMO
    // ==========================================

    /// <summary>
    /// Fácil: parcelas de 1 a 5. Médio: de 1 a 9, resultado até 10. Difícil: de 2 a 9, resultado acima de 10.
    /// </summary>
    public static void SortearSoma(int nivel, out int a, out int b)
    {
        if (nivel <= 0)
        {
            a = Random.Range(1, 6);
            b = Random.Range(1, 6);
        }
        else if (nivel == 1)
        {
            a = Random.Range(1, 10);
            b = Random.Range(1, 11 - a); // a + b <= 10
            if (b > 9) b = 9;
        }
        else
        {
            a = Random.Range(2, 10);
            int minimoB = Mathf.Max(2, 11 - a); // a + b >= 11
            b = Random.Range(minimoB, 10);
        }
    }

    /// <summary>Explicação para a criança: começa no número maior e conta o menor nos dedos.</summary>
    public static string PassoAPassoSoma(int a, int b)
    {
        int maior = Mathf.Max(a, b);
        int menor = Mathf.Min(a, b);
        StringBuilder contagem = new StringBuilder();
        for (int i = 1; i <= menor; i++)
        {
            if (i > 1) contagem.Append(", ");
            contagem.Append(maior + i);
        }
        return $"• <b>Comece pelo número maior:</b> {maior}\n" +
               $"• <b>Conte mais {menor} nos dedos:</b> {contagem}\n" +
               $"• <b>Resultado:</b> <color=#00E676>{a + b}</color>";
    }

    /// <summary>Resposta certa + alternativas próximas (nunca negativas), já embaralhadas.</summary>
    public static List<string> OpcoesDaSoma(int correta, int qtdOpcoes)
    {
        List<int> vizinhos = new List<int>();
        for (int d = 1; d <= 3; d++)
        {
            if (correta - d >= 0) vizinhos.Add(correta - d);
            vizinhos.Add(correta + d);
        }
        Embaralhar(vizinhos);

        List<string> opcoes = new List<string> { correta.ToString() };
        for (int i = 0; i < vizinhos.Count && opcoes.Count < qtdOpcoes; i++) opcoes.Add(vizinhos[i].ToString());
        Embaralhar(opcoes);
        return opcoes;
    }

    private static void Embaralhar<T>(List<T> lista)
    {
        for (int n = lista.Count - 1; n > 0; n--)
        {
            int k = Random.Range(0, n + 1);
            T tmp = lista[k];
            lista[k] = lista[n];
            lista[n] = tmp;
        }
    }
}
