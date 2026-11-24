using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Conteúdo extra para o 5º ano (metade das perguntas do nível Difícil):
/// - Português: leitura de um texto curto com pergunta de interpretação.
/// - Matemática: frações e porcentagem de quantidades, dinheiro com centavos e problemas de duas etapas.
/// </summary>
public static class ConteudoQuintoAno
{
    public struct Interpretacao
    {
        public string texto;
        public string pergunta;
        public string correta;
        public string errada1;
        public string errada2;

        public Interpretacao(string texto, string pergunta, string correta, string errada1, string errada2)
        {
            this.texto = texto; this.pergunta = pergunta; this.correta = correta; this.errada1 = errada1; this.errada2 = errada2;
        }
    }

    private static readonly Interpretacao[] Textos =
    {
        new Interpretacao("Marina acordou cedo, tomou café e foi de bicicleta para a escola.", "Como Marina foi para a escola?", "DE BICICLETA", "DE ÔNIBUS", "A PÉ"),
        new Interpretacao("O cachorro Totó late quando ouve o carteiro. Hoje ele latiu três vezes.", "Quantas vezes Totó latiu hoje?", "TRÊS", "DUAS", "NENHUMA"),
        new Interpretacao("Pedro levou o guarda-chuva porque o céu estava cinza e cheio de nuvens.", "Por que Pedro levou o guarda-chuva?", "PODIA CHOVER", "FAZIA SOL", "ERA PRESENTE"),
        new Interpretacao("A feira de ciências será no sábado, às nove horas, na quadra da escola.", "Onde será a feira?", "NA QUADRA", "NA SALA", "NO PARQUE"),
        new Interpretacao("Luana plantou uma semente e regou todos os dias. Depois de uma semana, nasceu um broto.", "O que nasceu depois de uma semana?", "UM BROTO", "UMA FLOR", "UMA FRUTA"),
        new Interpretacao("O sorvete derreteu porque ficou muito tempo no sol, em cima do banco.", "Por que o sorvete derreteu?", "FICOU NO SOL", "CAIU NO CHÃO", "ESTAVA GELADO"),
        new Interpretacao("Caio tem 10 anos. Sua irmã Bia é dois anos mais nova do que ele.", "Quantos anos tem Bia?", "OITO", "DOZE", "DEZ"),
        new Interpretacao("Na biblioteca, todos falam baixo para não atrapalhar quem está lendo.", "O que é preciso fazer na biblioteca?", "FALAR BAIXO", "CORRER", "CANTAR"),
        new Interpretacao("Os gatos dormem muitas horas por dia e costumam caçar à noite.", "Quando os gatos costumam caçar?", "À NOITE", "DE MANHÃ", "AO MEIO-DIA"),
        new Interpretacao("Ana guardou os brinquedos na caixa azul e os livros na estante do quarto.", "Onde Ana guardou os livros?", "NA ESTANTE", "NA CAIXA", "NA MOCHILA"),
        new Interpretacao("O ônibus da excursão saiu atrasado porque um dos pneus furou no caminho.", "Por que o ônibus atrasou?", "O PNEU FUROU", "CHOVEU", "SEM GASOLINA"),
        new Interpretacao("Rafael comeu todo o lanche, mas ainda estava com fome. Então pegou uma maçã.", "O que Rafael pegou?", "UMA MAÇÃ", "UM SUCO", "UM BOLO"),
        new Interpretacao("A tartaruga venceu a corrida porque não parou de andar, enquanto a lebre dormia.", "Quem venceu a corrida?", "A TARTARUGA", "A LEBRE", "NINGUÉM"),
        new Interpretacao("Hoje a temperatura caiu muito. Todos os alunos vieram de casaco e gorro.", "Como estava o tempo?", "FRIO", "QUENTE", "ABAFADO"),
        new Interpretacao("Joana adora ler histórias de mistério antes de dormir.", "De que histórias Joana gosta?", "DE MISTÉRIO", "DE PIRATA", "DE PRINCESA"),
        new Interpretacao("O bolo precisa ficar 40 minutos no forno. Ele foi colocado às 3 horas.", "A que horas o bolo fica pronto?", "3H40", "4H", "3H"),
        new Interpretacao("Theo perdeu o lápis, mas um colega emprestou outro para ele terminar a prova.", "Quem ajudou Theo?", "UM COLEGA", "A PROFESSORA", "A MÃE"),
        new Interpretacao("As abelhas levam o pólen de uma flor para outra e assim ajudam as plantas a dar frutos.", "Como as abelhas ajudam as plantas?", "LEVAM PÓLEN", "COMEM FOLHAS", "FAZEM SOMBRA"),
        new Interpretacao("No campeonato com 8 times, só a Escola Azul ganhou todos os jogos.", "Que time ganhou todos os jogos?", "ESCOLA AZUL", "ESCOLA VERDE", "NENHUM"),
        new Interpretacao("Lia não foi à aula porque estava com febre. A mãe a levou ao médico.", "Por que Lia faltou?", "ESTAVA DOENTE", "CHOVEU", "VIAJOU"),
        new Interpretacao("O rio da cidade ficou cheio de lixo. Os alunos fizeram um mutirão para limpá-lo.", "O que os alunos fizeram?", "LIMPARAM O RIO", "NADARAM", "PESCARAM"),
        new Interpretacao("Davi juntou moedas durante três meses para comprar uma bola nova.", "O que Davi queria comprar?", "UMA BOLA", "UM LIVRO", "UM JOGO"),
        new Interpretacao("Quando a luz do semáforo fica vermelha, os carros devem parar.", "O que a luz vermelha indica?", "PARAR", "ANDAR", "BUZINAR"),
        new Interpretacao("A coruja dorme durante o dia e fica acordada à noite para caçar.", "Quando a coruja dorme?", "DE DIA", "À NOITE", "NUNCA"),
        new Interpretacao("Bruno mora no terceiro andar. A avó dele mora dois andares acima.", "Em que andar mora a avó?", "QUINTO", "PRIMEIRO", "TERCEIRO"),
        new Interpretacao("Na receita, primeiro se mistura a farinha, depois o leite e, por último, os ovos.", "O que entra por último?", "OS OVOS", "O LEITE", "A FARINHA"),
        new Interpretacao("Na votação da turma, 12 alunos escolheram o museu e 9 escolheram o zoológico.", "Qual passeio venceu?", "MUSEU", "ZOOLÓGICO", "EMPATE"),
        new Interpretacao("Manu esqueceu a garrafa de água em casa, então bebeu água no bebedouro da escola.", "Onde Manu bebeu água?", "NO BEBEDOURO", "EM CASA", "NA CANTINA"),
        new Interpretacao("O jornal da escola sai toda sexta-feira com notícias de todas as turmas.", "Quando o jornal sai?", "SEXTA-FEIRA", "SEGUNDA-FEIRA", "TODO DIA"),
        new Interpretacao("Clara acha que reciclar é importante, porque diminui o lixo do planeta.", "Por que Clara acha importante reciclar?", "DIMINUI O LIXO", "É DIVERTIDO", "DÁ DINHEIRO")
    };

    private static readonly Dictionary<int, int> ultimoTextoDaFase = new Dictionary<int, int>();

    public static Interpretacao SortearInterpretacao(int numeroFase)
    {
        ultimoTextoDaFase.TryGetValue(numeroFase, out int ultimo);
        int i = Random.Range(0, Textos.Length);
        if (Textos.Length > 1 && i == ultimo - 1) i = (i + 1) % Textos.Length;
        ultimoTextoDaFase[numeroFase] = i + 1;
        return Textos[i];
    }

    public static int QuantidadeDeTextos => Textos.Length;

    // ==========================================
    // MATEMÁTICA
    // ==========================================

    /// <summary>Valor com vírgula e dois centavos (ex.: 4,25), sem depender do idioma do Windows.</summary>
    public static string Dinheiro(decimal valor) => valor.ToString("0.00", CultureInfo.InvariantCulture).Replace('.', ',');

    /// <summary>Pergunta do 5º ano. correta/opcoes são texto (podem ter centavos, ex.: "4,25").</summary>
    public static void SortearProblema(out string pergunta, out string correta, out List<string> opcoes, out string passo, out bool comTexto, out string habilidade)
    {
        int tipo = Random.Range(0, 4);
        comTexto = false;
        // Habilidade da BNCC de cada tipo: frações, porcentagem, decimais (dinheiro) e problemas de multiplicação/divisão
        habilidade = tipo == 0 ? "EF05MA03" : tipo == 1 ? "EF05MA06" : tipo == 2 ? "EF05MA07" : "EF05MA08";

        if (tipo == 0)
        {
            int den = new[] { 2, 3, 4, 5, 10 }[Random.Range(0, 5)];
            int num = den == 2 ? 1 : Random.Range(1, den);
            int total = den * Random.Range(2, 13);
            int r = total / den * num;
            pergunta = $"{num}/{den} de {total} = ?";
            correta = r.ToString();
            passo = $"• <b>Divida em {den} partes:</b> {total} ÷ {den} = {total / den}\n• <b>Pegue {num} parte(s):</b> {total / den} x {num} = <color=#00E676>{r}</color>";
            opcoes = OpcoesInteiras(r, total / den);
        }
        else if (tipo == 1)
        {
            int p = new[] { 10, 20, 25, 50 }[Random.Range(0, 4)];
            int passoBase = p == 25 ? 4 : (p == 50 ? 2 : 10);
            int total = passoBase * Random.Range(2, 16);
            int r = total * p / 100;
            pergunta = $"{p}% de {total} = ?";
            correta = r.ToString();
            string como = p == 50 ? "50% é a metade" : p == 25 ? "25% é a quarta parte" : p == 10 ? "10% é dividir por 10" : "20% é o dobro de 10%";
            passo = $"• <b>{como}</b>\n• <b>Resultado:</b> <color=#00E676>{r}</color>";
            opcoes = OpcoesInteiras(r, Mathf.Max(2, r / 2));
        }
        else if (tipo == 2)
        {
            decimal a = Random.Range(2, 30) * 0.25m, b = Random.Range(1, 20) * 0.25m;
            bool soma = Random.value < 0.5f;
            if (!soma && b > a) { decimal t = a; a = b; b = t; }
            decimal r = soma ? a + b : a - b;
            pergunta = soma ? $"R$ {Dinheiro(a)} + R$ {Dinheiro(b)} = ?" : $"R$ {Dinheiro(a)} - R$ {Dinheiro(b)} = ?";
            correta = Dinheiro(r);
            passo = $"• <b>Some ou tire os centavos e depois os reais</b>, como em uma conta armada com vírgula embaixo de vírgula\n• <b>Resultado:</b> <color=#00E676>R$ {correta}</color>";
            opcoes = new List<string> { correta };
            decimal[] desvios = { 0.25m, 0.5m, 1m, -0.25m, -0.5m, -1m };
            List<decimal> lista = new List<decimal>(desvios);
            Embaralhar(lista);
            foreach (decimal d in lista)
            {
                if (opcoes.Count >= 3) break;
                if (r + d < 0) continue;
                string s = Dinheiro(r + d);
                if (!opcoes.Contains(s)) opcoes.Add(s);
            }
        }
        else
        {
            comTexto = true;
            if (Random.value < 0.5f)
            {
                int porCaixa = Random.Range(6, 13), caixas = Random.Range(3, 7);
                int deu = Random.Range(5, porCaixa * caixas - 4);
                int r = porCaixa * caixas - deu;
                pergunta = $"Uma caixa tem {porCaixa} lápis. A escola comprou {caixas} caixas e distribuiu {deu} lápis. Quantos lápis sobraram?";
                correta = r.ToString();
                passo = $"• <b>1º Total de lápis:</b> {porCaixa} x {caixas} = {porCaixa * caixas}\n• <b>2º Tire os distribuídos:</b> {porCaixa * caixas} - {deu} = <color=#00E676>{r}</color>";
                opcoes = OpcoesInteiras(r, 6);
            }
            else
            {
                int preco = Random.Range(4, 13), alunos = Random.Range(12, 31), desconto = Random.Range(5, 21);
                int r = preco * alunos - desconto;
                pergunta = $"O ingresso custa {preco} reais. {alunos} alunos vão ao teatro e a escola ganhou {desconto} reais de desconto. Quanto a turma vai pagar?";
                correta = r.ToString();
                passo = $"• <b>1º Preço de todos:</b> {preco} x {alunos} = {preco * alunos}\n• <b>2º Tire o desconto:</b> {preco * alunos} - {desconto} = <color=#00E676>{r}</color>";
                opcoes = OpcoesInteiras(r, 10);
            }
        }
        Embaralhar(opcoes);
    }

    /// <summary>Resposta certa + duas erradas próximas, positivas e diferentes.</summary>
    private static List<string> OpcoesInteiras(int correta, int distancia)
    {
        distancia = Mathf.Max(1, distancia);
        List<string> opcoes = new List<string> { correta.ToString() };
        List<int> candidatos = new List<int> { correta + distancia, correta - distancia, correta + 1, correta - 1, correta + 2 * distancia, correta + 10 };
        Embaralhar(candidatos);
        foreach (int c in candidatos)
        {
            if (opcoes.Count >= 3) break;
            if (c < 0 || opcoes.Contains(c.ToString())) continue;
            opcoes.Add(c.ToString());
        }
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
