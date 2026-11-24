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
        "MANDO", "SOLTO", "SALTA", "BOLSO", "BALSA", "GALINHO", "COELHA", "AMIGA"
    };

    // Palavras que NÃO podem se formar trocando a vogal (palavrões e afins).
    private static readonly HashSet<string> Proibidas = new HashSet<string>
    {
        "FODA", "PUTA", "PUTO", "PICA", "PIPI", "BOSTA", "MERDA", "PORRA", "BUNDA", "ROLA", "PINTO",
        "CU", "PIROCA", "BUCETA", "CACETE", "CUZAO", "TETA", "BOSTO"
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
