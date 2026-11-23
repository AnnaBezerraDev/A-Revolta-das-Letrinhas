using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ServicoNuvem : MonoBehaviour
{
    private static ServicoNuvem _instancia;
    public static ServicoNuvem Instancia
    {
        get
        {
            if (_instancia == null)
            {
                CriarInstanciaAutomatica();
            }
            return _instancia;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CriarInstanciaAutomatica()
    {
        if (_instancia == null)
        {
            GameObject go = new GameObject("[ServicoNuvem]");
            _instancia = go.AddComponent<ServicoNuvem>();
            DontDestroyOnLoad(go);
            Debug.Log("[ServicoNuvem] Inicializado automaticamente antes de carregar a cena.");
        }
    }

    public class ForceAcceptCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

    [Header("Configurações do Servidor")]
    public string servidorUrl = "https://api.kairosstudios.site";
    public string tokenEscola = "ESC-CAMPINAS-01";
    public bool conectadoAoServidor { get; private set; } = false;
    public string nomeEscolaConectada { get; private set; } = "";
    public string municipioConectado { get; private set; } = "";
    public string validadeContrato { get; private set; } = "";
    public int diasRestantesContrato { get; private set; } = 999;
    public bool contratoValido { get; private set; } = true;

    // Tempo máximo de espera da conexão inicial. O servidor na nuvem pode levar ~1 minuto
    // para "acordar" depois de um período parado, por isso não pode ser curto demais.
    private const int TIMEOUT_CONEXAO = 30;
    private const int TIMEOUT_ENVIO = 25;

    // O servidor espera um GUID no campo turmaId. Enviar "" fazia o cadastro de aluno ser recusado (HTTP 400).
    private const string TURMA_ID_VAZIO = "00000000-0000-0000-0000-000000000000";

    // Evento disparado quando a escola ou alunos são atualizados
    public delegate void EscolaSincronizadaHandler();
    public static event EscolaSincronizadaHandler OnEscolaSincronizada;

    // Mapeamento local de alunos (Nome -> Guid do Banco de Dados)
    private Dictionary<string, string> mapaAlunosIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    // Mapeamento local de Turmas (Nome Aluno -> Nome da Turma)
    public List<DtoTurmaRecebida> turmasDisponiveis = new List<DtoTurmaRecebida>();
    private Dictionary<string, string> mapaAlunoTurma = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    // =========================================================
    // FILA DE SINCRONIZAÇÃO (salva no PlayerPrefs)
    // Tudo o que o jogo precisa mandar para o servidor (alunos, salas,
    // exclusões e fases concluídas) entra nesta fila e é enviado EM ORDEM.
    // Se a internet ou o servidor falhar, a operação fica guardada e é
    // reenviada depois - inclusive se o jogo for fechado e aberto de novo.
    // =========================================================
    private const string KEY_FILA = "Nuvem_FilaSincronizacao_v2";
    private const string KEY_MIGRACAO_PERFIS = "Nuvem_MigracaoPerfisLocais_v2";
    private const string KEY_MIGRACAO_PROGRESSO = "Nuvem_MigracaoProgressoLocal_v1";

    private const string OP_CRIAR_ALUNO = "criarAluno";
    private const string OP_MOVER_ALUNO = "moverAluno";
    private const string OP_EXCLUIR_ALUNO = "excluirAluno";
    private const string OP_CRIAR_SALA = "criarSala";
    private const string OP_PROGRESSO = "progresso";
    private const string OP_PROGRESSO_LOTE = "progressoLote";

    private enum ResultadoEnvio { Sucesso, Descartar, TentarDepois }

    [System.Serializable]
    public class OperacaoNuvem
    {
        public string tipo;
        public string token;
        public string aluno;
        public string sala;
        public DtoProgressoEnvio progresso;
        public List<DtoProgressoEnvio> lote = new List<DtoProgressoEnvio>();
        public int tentativas;
    }

    [System.Serializable]
    public class DtoProgressoLoteEnvio
    {
        public string nomeAluno;
        public string nomeTurma;
        public List<DtoProgressoEnvio> itens;
    }

    [System.Serializable]
    public class FilaOperacoesSalva
    {
        public List<OperacaoNuvem> itens = new List<OperacaoNuvem>();
    }

    private List<OperacaoNuvem> fila = new List<OperacaoNuvem>();
    private readonly Dictionary<OperacaoNuvem, Action<string>> callbacksCriacao = new Dictionary<OperacaoNuvem, Action<string>>();
    private bool processandoFila = false;
    private bool sincronizando = false;

    /// <summary>Quantidade de envios aguardando internet/servidor.</summary>
    public int OperacoesPendentes => fila.Count;

    [System.Serializable]
    public class ConfigEscolaData
    {
        public string ServidorUrl;
        public string TokenEscola;
    }

    [System.Serializable]
    public class DtoProgressoEnvio
    {
        public string alunoId;
        public string alunoNome;
        public string materia;
        public string nivel;
        public int numeroFase;
        public bool concluida;
        public int estrelas;
        public int pontuacao;
        public int quantidadeErros;
        public int tempoGastoSegundos;
    }

    [System.Serializable]
    public class DtoCriarAlunoEnvio
    {
        public string turmaId;
        public string nomeTurma;
        public string nomeCompleto;
        public string nomeJogo;
    }

    [System.Serializable]
    public class DtoAtualizarTurmaAlunoEnvio
    {
        public string nomeAluno;
        public string nomeTurma;
    }

    [System.Serializable]
    public class DtoCriarTurmaEnvio
    {
        public string nomeTurma;
    }

    [System.Serializable]
    public class DtoExcluirAlunoEnvio
    {
        public string nomeAluno;
    }

    [System.Serializable]
    public class DtoAlunoRecebido
    {
        public string id;
        public string nomeCompleto;
        public string nomeJogo;
        public string turmaId;
        public string turmaNome;
    }

    [System.Serializable]
    public class DtoTurmaRecebida
    {
        public string turmaId;
        public string nome;
        public string nomeProfessor;
        public string loginProfessor;
        public List<DtoAlunoRecebido> alunos;
    }

    [System.Serializable]
    public class DtoInfoEscolaRecebida
    {
        public string escolaId;
        public string nomeEscola;
        public string municipio;
        public string uf;
        public string dataExpiracaoContrato;
        public string numeroConvenio;
        public int diasRestantesContrato;
        public bool contratoValido;
        public string tokenAcesso;
        public List<DtoTurmaRecebida> turmas;
    }

    [System.Serializable]
    public class DtoEscolaItem
    {
        public string token;
        public string nome;
        public string municipio;
        public string uf;
        public int totalAlunos;
    }

    [System.Serializable]
    public class DtoEscolasWrapper
    {
        public List<DtoEscolaItem> items;
    }

    [System.Serializable]
    public class DtoAlunoSalaDetalhe
    {
        public string id;
        public string nomeCompleto;
        public string nomeJogo;
        public int fasesPort;
        public int fasesMath;
        public int totalFases;
        public int pontuacaoTotal;
        public float mediaEstrelas;
        public int errosTotais;
        public int tempoTotalSegundos;
        public string statusDesempenho;
        public string ultimaAtividade;
    }

    [System.Serializable]
    public class DtoSalaResumo
    {
        public string turmaId;
        public string nomeTurma;
        public string nomeProfessor;
        public int anoLetivo;
        public int totalAlunos;
        public int alunosAtivos;
        public int totalFasesConcluidas;
        public float mediaFasesPorAluno;
        public float mediaEstrelas;
        public int totalErros;
        public float taxaConclusaoPort;
        public float taxaConclusaoMath;
        public string ultimaAtividade;
        public List<DtoAlunoSalaDetalhe> alunos;
    }

    [System.Serializable]
    public class DtoFaseAluno
    {
        public string materia;
        public string nivel;
        public int numeroFase;
        public bool concluida;
        public int estrelas;
        public int pontuacao;
        public int quantidadeErros;
        public int tempoGastoSegundos;
        public string ultimaAtualizacao;
    }

    [System.Serializable]
    public class DtoProgressoAlunoRoot
    {
        public string alunoId;
        public string nomeJogo;
        public string nomeCompleto;
        public string turmaNome;
        public DtoAlunoSalaDetalhe resumo;
        public List<DtoFaseAluno> fases;
    }

    [System.Serializable]
    public class DtoResumoSalasRoot
    {
        public string escolaId;
        public string nomeEscola;
        public string municipio;
        public string tokenAcesso;
        public List<DtoSalaResumo> salas;
    }

    [System.Serializable]
    public class DtoLoginProfessorEnvio
    {
        public string login;
        public string senha;
    }

    [System.Serializable]
    public class DtoLoginProfessorResposta
    {
        public bool sucesso;
        public string tipo;
        public string nome;
        public string turmaId;
        public string turmaNome;
        public string erro;
    }

    void Awake()
    {
        if (_instancia != null && _instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        _instancia = this;
        DontDestroyOnLoad(gameObject);

        CarregarConfiguracaoLocal();
        CarregarFila();
    }

    void Start()
    {
        StartCoroutine(ConectarESincronizarCoroutine());
        StartCoroutine(RotinaSincronizacaoCoroutine());
        StartCoroutine(VerificarAtualizacaoCoroutine());
    }

    void OnApplicationQuit()
    {
        SalvarFila();
    }

    // =========================================================
    // CONFIGURAÇÃO (config_escola.json)
    // =========================================================

    public void CarregarConfiguracaoLocal()
    {
        try
        {
            string pathStreaming = Path.Combine(Application.streamingAssetsPath, "config_escola.json");
            string pathRaiz = Path.Combine(Application.dataPath, "..", "config_escola.json");
            string pathFinal = File.Exists(pathRaiz) ? pathRaiz : (File.Exists(pathStreaming) ? pathStreaming : "");

            if (!string.IsNullOrEmpty(pathFinal))
            {
                string json = File.ReadAllText(pathFinal, Encoding.UTF8);
                ConfigEscolaData config = JsonUtility.FromJson<ConfigEscolaData>(json);
                if (config != null)
                {
                    if (!string.IsNullOrEmpty(config.ServidorUrl)) servidorUrl = config.ServidorUrl.Trim().TrimEnd('/');
                    if (!string.IsNullOrEmpty(config.TokenEscola)) tokenEscola = config.TokenEscola.Trim().ToUpperInvariant();
                    Debug.Log($"[ServicoNuvem] Config carregada: {servidorUrl} | Token: {tokenEscola}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ServicoNuvem] Falha ao ler config_escola.json: {ex.Message}");
        }
    }

    public void SalvarConfiguracaoLocal()
    {
        try
        {
            string json = JsonUtility.ToJson(new ConfigEscolaData { ServidorUrl = servidorUrl, TokenEscola = tokenEscola }, true);
            string pathStreaming = Path.Combine(Application.streamingAssetsPath, "config_escola.json");
            string pathRaiz = Path.Combine(Application.dataPath, "..", "config_escola.json");

            string dirStream = Path.GetDirectoryName(pathStreaming);
            if (!Directory.Exists(dirStream)) Directory.CreateDirectory(dirStream);

            File.WriteAllText(pathStreaming, json, Encoding.UTF8);
            File.WriteAllText(pathRaiz, json, Encoding.UTF8);
            Debug.Log($"[ServicoNuvem] Nova configuração salva localmente com token: {tokenEscola}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ServicoNuvem] Erro ao salvar config_escola.json: {ex.Message}");
        }
    }

    public void TrocarEscola(string novoToken)
    {
        if (string.IsNullOrEmpty(novoToken)) return;
        tokenEscola = novoToken.Trim().ToUpperInvariant();
        SalvarConfiguracaoLocal();
        StartCoroutine(ConectarESincronizarCoroutine());
    }

    // =========================================================
    // CONEXÃO E SINCRONIZAÇÃO DA LISTA DE ALUNOS/SALAS
    // =========================================================

    /// <summary>
    /// Busca de novo no servidor a lista de salas e alunos da escola (ex.: ao abrir a tela "Quem vai jogar hoje?").
    /// </summary>
    public void SincronizarAgora()
    {
        if (!sincronizando) StartCoroutine(ConectarESincronizarCoroutine());
    }

    public IEnumerator ConectarESincronizarCoroutine()
    {
        if (sincronizando) yield break;
        sincronizando = true;

        // 1. Antes de baixar a lista, envia o que está pendente (alunos/salas criados sem internet)
        yield return StartCoroutine(ProcessarFilaCoroutine());

        string token = tokenEscola;
        string url = $"{servidorUrl}/api/jogo/conectar?token={UnityWebRequest.EscapeURL(token)}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.certificateHandler = new ForceAcceptCertificate();
            req.SetRequestHeader("X-Escola-Token", token);
            req.timeout = TIMEOUT_CONEXAO;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                DtoInfoEscolaRecebida info = null;
                try
                {
                    info = JsonUtility.FromJson<DtoInfoEscolaRecebida>(req.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ServicoNuvem] Resposta inválida do servidor: {ex.Message}");
                }

                if (info != null && !string.IsNullOrEmpty(info.nomeEscola))
                {
                    conectadoAoServidor = true;
                    AplicarInfoEscola(info, token, true);
                    Debug.Log($"[ServicoNuvem] Conectado à escola '{nomeEscolaConectada}' ({municipioConectado}). Envios pendentes: {fila.Count}");
                }
            }
            else
            {
                conectadoAoServidor = false;
                if (req.responseCode == 404)
                    Debug.LogWarning($"[ServicoNuvem] A escola com token '{token}' não existe no servidor {servidorUrl}. Cadastre-a no painel ou corrija o config_escola.json.");
                else
                    Debug.LogWarning($"[ServicoNuvem] Falha ao conectar ao servidor ({req.error}). Usando dados locais; nova tentativa em instantes.");
            }
        }

        sincronizando = false;
    }

    /// <summary>
    /// Aplica a lista de salas/alunos recebida do servidor, sem apagar o que ainda está na fila de envio.
    /// </summary>
    private void AplicarInfoEscola(DtoInfoEscolaRecebida info, string token, bool permitirMigracao)
    {
        nomeEscolaConectada = info.nomeEscola;
        municipioConectado = $"{info.municipio}/{info.uf}";
        validadeContrato = !string.IsNullOrEmpty(info.dataExpiracaoContrato) ? info.dataExpiracaoContrato : "Sem expiração";
        diasRestantesContrato = info.diasRestantesContrato;
        contratoValido = info.contratoValido;

        List<string> perfisLocaisAntes = GerenciadorPerfis.ObterListaPerfis();

        turmasDisponiveis.Clear();
        mapaAlunosIds.Clear();
        mapaAlunoTurma.Clear();
        List<string> nomes = new List<string>();

        if (info.turmas != null)
        {
            turmasDisponiveis.AddRange(info.turmas);
            foreach (var turma in info.turmas)
            {
                if (turma.alunos == null) continue;
                foreach (var aluno in turma.alunos)
                {
                    string nomeExibicao = !string.IsNullOrEmpty(aluno.nomeJogo) ? aluno.nomeJogo : aluno.nomeCompleto;
                    mapaAlunosIds[nomeExibicao] = aluno.id;
                    if (!string.IsNullOrEmpty(aluno.nomeCompleto)) mapaAlunosIds[aluno.nomeCompleto] = aluno.id;
                    mapaAlunoTurma[nomeExibicao] = turma.nome;
                    PlayerPrefs.SetString($"Aluno_Turma_{nomeExibicao}", turma.nome);
                    if (!ContemNome(nomes, nomeExibicao)) nomes.Add(nomeExibicao);
                }
            }
        }

        // 2. O que foi feito no jogo e ainda não chegou ao servidor continua valendo (na ordem em que aconteceu)
        foreach (var op in fila)
        {
            if (!MesmoToken(op.token, token) || string.IsNullOrEmpty(op.aluno)) continue;

            if (op.tipo == OP_CRIAR_ALUNO || op.tipo == OP_MOVER_ALUNO)
            {
                if (!ContemNome(nomes, op.aluno)) nomes.Add(op.aluno);
                if (!string.IsNullOrEmpty(op.sala))
                {
                    mapaAlunoTurma[op.aluno] = op.sala;
                    PlayerPrefs.SetString($"Aluno_Turma_{op.aluno}", op.sala);
                }
            }
            else if (op.tipo == OP_EXCLUIR_ALUNO)
            {
                nomes.RemoveAll(n => string.Equals(n, op.aluno, StringComparison.OrdinalIgnoreCase));
            }
        }

        // 3. Migração única: alunos criados no jogo ANTES desta correção só existiam neste computador.
        //    Eles são enviados ao servidor uma única vez para não sumirem da lista.
        if (permitirMigracao && PlayerPrefs.GetInt(KEY_MIGRACAO_PERFIS, 0) == 0)
        {
            int migrados = 0;
            foreach (var local in perfisLocaisAntes)
            {
                if (string.IsNullOrWhiteSpace(local) || local == "Aluno 1" || ContemNome(nomes, local)) continue;
                string sala = PlayerPrefs.GetString($"Aluno_Turma_{local}", "");
                fila.Add(new OperacaoNuvem { tipo = OP_CRIAR_ALUNO, token = token, aluno = local, sala = sala });
                nomes.Add(local);
                migrados++;
            }
            PlayerPrefs.SetInt(KEY_MIGRACAO_PERFIS, 1);
            SalvarFila();
            if (migrados > 0) Debug.Log($"[ServicoNuvem] {migrados} aluno(s) que existiam só neste computador serão enviados ao servidor.");
        }

        // 4. Envio único do progresso que só existia neste computador (versões antigas não mandavam as fases)
        if (permitirMigracao && PlayerPrefs.GetInt(KEY_MIGRACAO_PROGRESSO, 0) == 0)
        {
            int alunosComProgresso = 0;
            foreach (var nome in nomes)
            {
                List<DtoProgressoEnvio> lote = LerProgressoLocal(nome);
                if (lote.Count == 0) continue;
                string sala = mapaAlunoTurma.TryGetValue(nome, out string s) ? s : PlayerPrefs.GetString($"Aluno_Turma_{nome}", "");
                fila.Add(new OperacaoNuvem { tipo = OP_PROGRESSO_LOTE, token = token, aluno = nome, sala = sala, lote = lote });
                alunosComProgresso++;
            }
            PlayerPrefs.SetInt(KEY_MIGRACAO_PROGRESSO, 1);
            SalvarFila();
            if (alunosComProgresso > 0) Debug.Log($"[ServicoNuvem] Progresso salvo neste computador de {alunosComProgresso} aluno(s) será enviado ao servidor.");
        }

        // As salas deste PC passam a ser as do servidor (somem as salas que não existem no banco)
        List<string> salasServidor = new List<string>();
        foreach (var t in turmasDisponiveis)
        {
            if (!string.IsNullOrWhiteSpace(t.nome)) salasServidor.Add(t.nome.Trim());
        }
        GerenciadorPerfis.DefinirSalasDaEscola(salasServidor, ObterSalasPendentes());

        PlayerPrefs.Save();

        // Define no jogo os alunos desta escola (isolando de outras escolas)
        GerenciadorPerfis.DefinirAlunosDaEscola(nomes);
        OnEscolaSincronizada?.Invoke();

        if (fila.Count > 0) StartCoroutine(ProcessarFilaCoroutine());
    }

    /// <summary>
    /// Salas criadas/escolhidas no jogo que ainda estão na fila de envio para o servidor.
    /// </summary>
    public List<string> ObterSalasPendentes()
    {
        List<string> salas = new List<string>();
        foreach (var op in fila)
        {
            if (!MesmoToken(op.token, tokenEscola) || string.IsNullOrWhiteSpace(op.sala)) continue;
            if (op.tipo != OP_CRIAR_SALA && op.tipo != OP_CRIAR_ALUNO && op.tipo != OP_MOVER_ALUNO) continue;
            if (!ContemNome(salas, op.sala)) salas.Add(op.sala.Trim());
        }
        return salas;
    }

    /// <summary>
    /// Lê as fases concluídas de um aluno salvas neste computador (PlayerPrefs) para enviá-las ao servidor.
    /// </summary>
    private List<DtoProgressoEnvio> LerProgressoLocal(string nomeAluno)
    {
        List<DtoProgressoEnvio> itens = new List<DtoProgressoEnvio>();
        string pfx = $"P_{GerenciadorPerfis.SanitizarNome(nomeAluno)}_";
        string[] materias = { "Port", "Math" };
        string[] niveis = { "Facil", "Medio", "Dificil" };

        foreach (var mat in materias)
        {
            for (int nivel = 0; nivel < 3; nivel++)
            {
                for (int fase = 0; fase < 30; fase++)
                {
                    if (PlayerPrefs.GetInt($"{pfx}FaseCompleta_{mat}_{nivel}_{fase}", 0) != 1) continue;
                    // Vidas que sobraram ao concluir (2 = nenhum erro). Sem o registro, considera sem erros.
                    int vidas = Mathf.Clamp(PlayerPrefs.GetInt($"{pfx}VidasRestantes_{mat}_{nivel}_{fase}", VIDAS_POR_FASE), 1, VIDAS_POR_FASE);
                    itens.Add(CriarProgresso(nomeAluno, mat, niveis[nivel], fase + 1, vidas, VIDAS_POR_FASE, 0));
                }
            }
        }
        return itens;
    }

    // Cada fase do jogo começa com 2 vidas (GameManager.vidasMaximas)
    private const int VIDAS_POR_FASE = 2;

    /// <summary>
    /// Converte o resultado de uma fase (vidas que sobraram) em estrelas, pontos e erros.
    /// Sem erros = 3 estrelas e 1000 pontos; cada vida perdida tira 1 estrela e reduz a pontuação.
    /// </summary>
    private static DtoProgressoEnvio CriarProgresso(string nomeAluno, string materia, string nivel, int numeroFase, int vidasRestantes, int vidasMaximas, int tempoSegundos)
    {
        vidasMaximas = Mathf.Max(1, vidasMaximas);
        vidasRestantes = Mathf.Clamp(vidasRestantes, 1, vidasMaximas);
        int erros = vidasMaximas - vidasRestantes;
        return new DtoProgressoEnvio
        {
            alunoNome = nomeAluno,
            materia = materia.ToLower().Contains("math") ? "Matematica" : "Portugues",
            nivel = nivel,
            numeroFase = numeroFase,
            concluida = true,
            estrelas = Mathf.Clamp(3 - erros, 1, 3),
            pontuacao = Mathf.RoundToInt(1000f * vidasRestantes / vidasMaximas),
            quantidadeErros = erros,
            tempoGastoSegundos = Mathf.Max(0, tempoSegundos)
        };
    }

    public string ObterTurmaDoAluno(string nomeAluno)
    {
        if (string.IsNullOrEmpty(nomeAluno)) return "";
        if (mapaAlunoTurma.TryGetValue(nomeAluno, out string turma)) return turma;
        return PlayerPrefs.GetString($"Aluno_Turma_{nomeAluno}", "");
    }

    public void ValidarEVincularToken(string token, System.Action<bool, string> callback)
    {
        StartCoroutine(ValidarTokenCoroutine(token, callback));
    }

    private IEnumerator ValidarTokenCoroutine(string token, System.Action<bool, string> callback)
    {
        if (string.IsNullOrEmpty(token))
        {
            callback?.Invoke(false, "Token não pode ser vazio.");
            yield break;
        }

        string tokenFormatado = token.Trim().ToUpperInvariant();
        string url = $"{servidorUrl}/api/jogo/conectar?token={UnityWebRequest.EscapeURL(tokenFormatado)}";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.certificateHandler = new ForceAcceptCertificate();
            req.SetRequestHeader("X-Escola-Token", tokenFormatado);
            req.timeout = TIMEOUT_CONEXAO;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                DtoInfoEscolaRecebida info = null;
                try
                {
                    info = JsonUtility.FromJson<DtoInfoEscolaRecebida>(req.downloadHandler.text);
                }
                catch { }

                if (info != null && !string.IsNullOrEmpty(info.nomeEscola))
                {
                    tokenEscola = tokenFormatado;
                    conectadoAoServidor = true;
                    SalvarConfiguracaoLocal();

                    // Ao trocar de escola não há migração: os alunos deste PC pertencem à escola anterior.
                    AplicarInfoEscola(info, tokenFormatado, false);

                    callback?.Invoke(true, $"{info.nomeEscola} ({info.municipio}/{info.uf})");
                    yield break;
                }
            }

            string erroMsg = "Token inválido ou não cadastrado no servidor.";
            if (req.result == UnityWebRequest.Result.ConnectionError)
            {
                erroMsg = "Não foi possível conectar ao servidor. Verifique a internet.";
            }
            else if (req.responseCode == 404)
            {
                erroMsg = "Escola não encontrada para este token.";
            }

            callback?.Invoke(false, erroMsg);
        }
    }

    // =========================================================
    // OPERAÇÕES PÚBLICAS (chamadas pelo jogo) -> entram na fila
    // =========================================================

    /// <summary>
    /// Envia ao servidor uma fase concluída com o desempenho REAL (vidas que sobraram e tempo gasto).
    /// Antes eram enviados sempre 3 estrelas, 1000 pontos e 0 erros, e o professor via dados irreais.
    /// </summary>
    public void EnviarFaseConcluida(string nomeAluno, string materia, int nivelIndex, int numeroFase, int vidasRestantes, int vidasMaximas, int tempoSegundos)
    {
        if (string.IsNullOrEmpty(nomeAluno)) return;
        string nivelStr = nivelIndex == 0 ? "Facil" : (nivelIndex == 1 ? "Medio" : "Dificil");
        DtoProgressoEnvio dto = CriarProgresso(nomeAluno, materia, nivelStr, numeroFase, vidasRestantes, vidasMaximas, tempoSegundos);

        Enfileirar(new OperacaoNuvem
        {
            tipo = OP_PROGRESSO,
            token = tokenEscola,
            aluno = nomeAluno,
            sala = ObterTurmaDoAluno(nomeAluno),
            progresso = dto
        });
    }

    /// <summary>
    /// Cadastra no servidor um aluno criado no jogo (na sala informada). Se já existir, o servidor apenas devolve o mesmo aluno.
    /// </summary>
    public void CadastrarAlunoNuvem(string nomeAluno, string turmaId = "", string nomeTurma = "", System.Action<string> onIdRecebido = null)
    {
        if (string.IsNullOrWhiteSpace(nomeAluno)) return;
        string nome = nomeAluno.Trim();
        if (!string.IsNullOrEmpty(nomeTurma))
        {
            mapaAlunoTurma[nome] = nomeTurma.Trim();
        }

        var op = new OperacaoNuvem { tipo = OP_CRIAR_ALUNO, token = tokenEscola, aluno = nome, sala = nomeTurma?.Trim() ?? "" };
        if (onIdRecebido != null) callbacksCriacao[op] = onIdRecebido;
        Enfileirar(op);
    }

    /// <summary>
    /// Cadastra no servidor uma sala criada no jogo.
    /// </summary>
    public void CadastrarSalaNuvem(string nomeSala)
    {
        if (string.IsNullOrWhiteSpace(nomeSala)) return;
        Enfileirar(new OperacaoNuvem { tipo = OP_CRIAR_SALA, token = tokenEscola, sala = nomeSala.Trim() });
    }

    public void AtualizarTurmaAlunoNuvem(string nomeAluno, string nomeTurma)
    {
        if (string.IsNullOrEmpty(nomeAluno) || string.IsNullOrEmpty(nomeTurma)) return;
        Enfileirar(new OperacaoNuvem { tipo = OP_MOVER_ALUNO, token = tokenEscola, aluno = nomeAluno.Trim(), sala = nomeTurma.Trim() });
    }

    /// <summary>
    /// Define a sala do aluno neste computador e (por padrão) envia a mudança ao servidor.
    /// </summary>
    public void DefinirTurmaDoAluno(string nomeAluno, string nomeTurma, bool enviarParaNuvem = true)
    {
        if (string.IsNullOrEmpty(nomeAluno) || string.IsNullOrEmpty(nomeTurma)) return;
        mapaAlunoTurma[nomeAluno] = nomeTurma;
        PlayerPrefs.SetString($"Aluno_Turma_{nomeAluno}", nomeTurma);
        if (enviarParaNuvem) AtualizarTurmaAlunoNuvem(nomeAluno, nomeTurma);
    }

    /// <summary>
    /// Exclui o aluno do servidor (chamado quando o perfil é excluído no jogo).
    /// </summary>
    public void ExcluirAlunoNuvem(string nomeAluno)
    {
        if (string.IsNullOrWhiteSpace(nomeAluno)) return;
        string nome = nomeAluno.Trim();
        mapaAlunosIds.Remove(nome);
        mapaAlunoTurma.Remove(nome);
        Enfileirar(new OperacaoNuvem { tipo = OP_EXCLUIR_ALUNO, token = tokenEscola, aluno = nome });
    }

    // =========================================================
    // PROCESSAMENTO DA FILA
    // =========================================================

    private void Enfileirar(OperacaoNuvem op)
    {
        fila.Add(op);
        SalvarFila();
        if (!processandoFila) StartCoroutine(ProcessarFilaCoroutine());
    }

    private void CarregarFila()
    {
        try
        {
            string json = PlayerPrefs.GetString(KEY_FILA, "");
            if (!string.IsNullOrEmpty(json))
            {
                FilaOperacoesSalva salva = JsonUtility.FromJson<FilaOperacoesSalva>(json);
                if (salva != null && salva.itens != null) fila = salva.itens;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ServicoNuvem] Não foi possível ler a fila de envios salva: {ex.Message}");
            fila = new List<OperacaoNuvem>();
        }
        if (fila.Count > 0) Debug.Log($"[ServicoNuvem] {fila.Count} envio(s) pendente(s) da última vez serão enviados ao servidor.");
    }

    private void SalvarFila()
    {
        try
        {
            PlayerPrefs.SetString(KEY_FILA, JsonUtility.ToJson(new FilaOperacoesSalva { itens = fila }));
            PlayerPrefs.Save();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ServicoNuvem] Não foi possível salvar a fila de envios: {ex.Message}");
        }
    }

    private IEnumerator ProcessarFilaCoroutine()
    {
        if (processandoFila) yield break;
        processandoFila = true;

        // Se uma operação de uma escola falhar, as próximas DESSA escola esperam (para manter a ordem).
        HashSet<string> tokensEmEspera = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int i = 0;
        while (i < fila.Count)
        {
            OperacaoNuvem op = fila[i];
            string tokenOp = string.IsNullOrEmpty(op.token) ? tokenEscola : op.token;
            if (tokensEmEspera.Contains(tokenOp))
            {
                i++;
                continue;
            }

            ResultadoEnvio resultado = ResultadoEnvio.TentarDepois;
            yield return StartCoroutine(ExecutarOperacaoCoroutine(op, tokenOp, r => resultado = r));

            if (resultado == ResultadoEnvio.TentarDepois)
            {
                op.tentativas++;
                tokensEmEspera.Add(tokenOp);
                i++;
            }
            else
            {
                fila.Remove(op);
                callbacksCriacao.Remove(op);
            }
            SalvarFila();
        }

        processandoFila = false;
    }

    private IEnumerator ExecutarOperacaoCoroutine(OperacaoNuvem op, string token, Action<ResultadoEnvio> concluir)
    {
        ResultadoEnvio resultado = ResultadoEnvio.TentarDepois;

        switch (op.tipo)
        {
            case OP_CRIAR_ALUNO:
            {
                string json = JsonUtility.ToJson(new DtoCriarAlunoEnvio { turmaId = TURMA_ID_VAZIO, nomeTurma = op.sala ?? "", nomeCompleto = op.aluno, nomeJogo = op.aluno });
                yield return StartCoroutine(PostJsonCoroutine("/api/jogo/criar-aluno", token, json, req =>
                {
                    resultado = Classificar(req, op, false);
                    if (resultado == ResultadoEnvio.Sucesso)
                    {
                        string id = RegistrarAlunoConfirmado(token, op.aluno, req.downloadHandler.text);
                        if (callbacksCriacao.TryGetValue(op, out var cb)) cb?.Invoke(id);
                        Debug.Log($"[ServicoNuvem] Aluno '{op.aluno}' salvo no servidor (sala '{ObterTurmaDoAluno(op.aluno)}').");
                    }
                }));
                break;
            }

            case OP_MOVER_ALUNO:
            {
                string json = JsonUtility.ToJson(new DtoAtualizarTurmaAlunoEnvio { nomeAluno = op.aluno, nomeTurma = op.sala });
                yield return StartCoroutine(PostJsonCoroutine("/api/jogo/atualizar-turma-aluno", token, json, req =>
                {
                    resultado = Classificar(req, op, false);
                    if (resultado == ResultadoEnvio.Sucesso)
                    {
                        RegistrarAlunoConfirmado(token, op.aluno, req.downloadHandler.text);
                        Debug.Log($"[ServicoNuvem] Sala do aluno '{op.aluno}' atualizada para '{op.sala}' no servidor.");
                    }
                }));
                break;
            }

            case OP_EXCLUIR_ALUNO:
            {
                string json = JsonUtility.ToJson(new DtoExcluirAlunoEnvio { nomeAluno = op.aluno });
                yield return StartCoroutine(PostJsonCoroutine("/api/jogo/excluir-aluno", token, json, req =>
                {
                    // 404 = o aluno já não existe no servidor: objetivo cumprido
                    resultado = Classificar(req, op, true);
                    if (resultado != ResultadoEnvio.TentarDepois)
                        Debug.Log($"[ServicoNuvem] Aluno '{op.aluno}' excluído do servidor.");
                }));
                break;
            }

            case OP_CRIAR_SALA:
            {
                string json = JsonUtility.ToJson(new DtoCriarTurmaEnvio { nomeTurma = op.sala });
                yield return StartCoroutine(PostJsonCoroutine("/api/jogo/criar-turma", token, json, req =>
                {
                    resultado = Classificar(req, op, false);
                    if (resultado == ResultadoEnvio.Sucesso)
                        Debug.Log($"[ServicoNuvem] Sala '{op.sala}' salva no servidor.");
                }));
                break;
            }

            case OP_PROGRESSO:
            {
                if (op.progresso == null)
                {
                    resultado = ResultadoEnvio.Descartar;
                    break;
                }

                // Descobre o Id do aluno no servidor (cadastrando-o se ainda não existir lá)
                string alunoId = "";
                if (MesmoToken(token, tokenEscola) && mapaAlunosIds.TryGetValue(op.aluno, out string idConhecido))
                {
                    alunoId = idConhecido;
                }

                if (string.IsNullOrEmpty(alunoId))
                {
                    string jsonAluno = JsonUtility.ToJson(new DtoCriarAlunoEnvio { turmaId = TURMA_ID_VAZIO, nomeTurma = op.sala ?? "", nomeCompleto = op.aluno, nomeJogo = op.aluno });
                    ResultadoEnvio resultadoCadastro = ResultadoEnvio.TentarDepois;
                    yield return StartCoroutine(PostJsonCoroutine("/api/jogo/criar-aluno", token, jsonAluno, req =>
                    {
                        resultadoCadastro = Classificar(req, op, false);
                        if (resultadoCadastro == ResultadoEnvio.Sucesso)
                            alunoId = RegistrarAlunoConfirmado(token, op.aluno, req.downloadHandler.text);
                    }));

                    if (string.IsNullOrEmpty(alunoId))
                    {
                        // Sem o aluno não dá para salvar a fase. Só desiste se o servidor recusar os dados de vez.
                        resultado = resultadoCadastro == ResultadoEnvio.Descartar ? ResultadoEnvio.Descartar : ResultadoEnvio.TentarDepois;
                        break;
                    }
                }

                op.progresso.alunoId = alunoId;
                string json = JsonUtility.ToJson(op.progresso);
                yield return StartCoroutine(PostJsonCoroutine("/api/jogo/progresso", token, json, req =>
                {
                    if (req.responseCode == 404)
                    {
                        // Id desatualizado (aluno excluído/recriado no servidor): na próxima tentativa o Id é buscado de novo
                        mapaAlunosIds.Remove(op.aluno);
                        op.tentativas++;
                        resultado = op.tentativas > 5 ? ResultadoEnvio.Descartar : ResultadoEnvio.TentarDepois;
                        return;
                    }
                    resultado = Classificar(req, op, false);
                    if (resultado == ResultadoEnvio.Sucesso)
                        Debug.Log($"[ServicoNuvem] Progresso salvo na nuvem: {op.aluno} | {op.progresso.materia} Fase {op.progresso.numeroFase}");
                }));
                break;
            }

            case OP_PROGRESSO_LOTE:
            {
                if (op.lote == null || op.lote.Count == 0)
                {
                    resultado = ResultadoEnvio.Descartar;
                    break;
                }
                string json = JsonUtility.ToJson(new DtoProgressoLoteEnvio { nomeAluno = op.aluno, nomeTurma = op.sala ?? "", itens = op.lote });
                yield return StartCoroutine(PostJsonCoroutine("/api/jogo/progresso-lote", token, json, req =>
                {
                    resultado = Classificar(req, op, false);
                    if (resultado == ResultadoEnvio.Sucesso)
                        Debug.Log($"[ServicoNuvem] Progresso antigo de '{op.aluno}' ({op.lote.Count} fases) enviado ao servidor.");
                }));
                break;
            }

            default:
                resultado = ResultadoEnvio.Descartar;
                break;
        }

        concluir?.Invoke(resultado);
    }

    private IEnumerator PostJsonCoroutine(string caminho, string token, string json, Action<UnityWebRequest> aoTerminar)
    {
        string url = $"{servidorUrl}{caminho}?token={UnityWebRequest.EscapeURL(token)}";
        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.certificateHandler = new ForceAcceptCertificate();
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Escola-Token", token);
            req.timeout = TIMEOUT_ENVIO;

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[ServicoNuvem] {caminho} falhou (HTTP {req.responseCode} {req.error}): {req.downloadHandler?.text}");
            }
            aoTerminar?.Invoke(req);
        }
    }

    /// <summary>
    /// Decide o que fazer com uma operação depois da resposta do servidor.
    /// </summary>
    private static ResultadoEnvio Classificar(UnityWebRequest req, OperacaoNuvem op, bool naoEncontradoEhSucesso)
    {
        if (req.result == UnityWebRequest.Result.Success) return ResultadoEnvio.Sucesso;

        long codigo = req.responseCode;
        if (codigo == 404 && naoEncontradoEhSucesso) return ResultadoEnvio.Descartar;

        // Dados recusados pelo servidor: tenta algumas vezes (o servidor pode estar sendo atualizado) e depois desiste
        if ((codigo == 400 || codigo == 409 || codigo == 415) && op.tentativas >= 20) return ResultadoEnvio.Descartar;

        // Escola não encontrada (token inválido): tenta por um bom tempo (a escola pode ser cadastrada depois)
        if ((codigo == 401 || codigo == 404) && op.tentativas >= 200) return ResultadoEnvio.Descartar;

        // Sem internet, servidor acordando, erro 5xx...: tenta de novo mais tarde
        return ResultadoEnvio.TentarDepois;
    }

    /// <summary>
    /// Guarda o Id/sala do aluno devolvidos pelo servidor. Retorna o Id.
    /// </summary>
    private string RegistrarAlunoConfirmado(string token, string nomeLocal, string jsonResposta)
    {
        DtoAlunoRecebido aluno = null;
        try { aluno = JsonUtility.FromJson<DtoAlunoRecebido>(jsonResposta); } catch { }
        if (aluno == null || string.IsNullOrEmpty(aluno.id)) return "";

        if (MesmoToken(token, tokenEscola))
        {
            mapaAlunosIds[nomeLocal] = aluno.id;
            if (!string.IsNullOrEmpty(aluno.nomeJogo)) mapaAlunosIds[aluno.nomeJogo] = aluno.id;

            if (!string.IsNullOrEmpty(aluno.turmaNome))
            {
                mapaAlunoTurma[nomeLocal] = aluno.turmaNome;
                PlayerPrefs.SetString($"Aluno_Turma_{nomeLocal}", aluno.turmaNome);
            }
        }
        return aluno.id;
    }

    private static bool MesmoToken(string a, string b) =>
        string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static bool ContemNome(List<string> lista, string nome)
    {
        foreach (var n in lista)
        {
            if (string.Equals(n?.Trim(), nome?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private IEnumerator RotinaSincronizacaoCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(20f);

            // Reenvia o que ficou pendente
            if (fila.Count > 0 && !processandoFila)
            {
                yield return StartCoroutine(ProcessarFilaCoroutine());
            }

            // Se o jogo abriu sem conseguir falar com o servidor (sem internet / servidor acordando), tenta de novo
            if (!conectadoAoServidor && !sincronizando)
            {
                yield return StartCoroutine(ConectarESincronizarCoroutine());
            }
        }
    }

    // =========================================================
    // CONSULTAS (Área do Professor / Lista de Escolas / Login)
    // =========================================================

    /// <summary>
    /// Consulta o resumo de turmas e desempenho de cada sala diretamente no servidor .NET.
    /// </summary>
    public void BuscarResumoSalas(System.Action<DtoResumoSalasRoot> callback)
    {
        StartCoroutine(BuscarResumoSalasCoroutine(callback));
    }

    private IEnumerator BuscarResumoSalasCoroutine(System.Action<DtoResumoSalasRoot> callback)
    {
        string url = $"{servidorUrl}/api/jogo/salas-resumo?token={UnityWebRequest.EscapeURL(tokenEscola)}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.certificateHandler = new ForceAcceptCertificate();
            req.SetRequestHeader("X-Escola-Token", tokenEscola);
            req.timeout = TIMEOUT_CONEXAO;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                DtoResumoSalasRoot dados = JsonUtility.FromJson<DtoResumoSalasRoot>(json);
                callback?.Invoke(dados);
            }
            else
            {
                Debug.LogWarning($"[ServicoNuvem] Falha ao buscar resumo de salas: {req.error}");
                callback?.Invoke(null);
            }
        }
    }

    /// <summary>
    /// Busca no servidor o desempenho fase a fase de um aluno (Área do Professor).
    /// </summary>
    public void BuscarProgressoAluno(string alunoId, System.Action<DtoProgressoAlunoRoot> callback)
    {
        StartCoroutine(BuscarProgressoAlunoCoroutine(alunoId, callback));
    }

    private IEnumerator BuscarProgressoAlunoCoroutine(string alunoId, System.Action<DtoProgressoAlunoRoot> callback)
    {
        if (string.IsNullOrEmpty(alunoId))
        {
            callback?.Invoke(null);
            yield break;
        }

        string url = $"{servidorUrl}/api/jogo/progresso-aluno/{UnityWebRequest.EscapeURL(alunoId)}?token={UnityWebRequest.EscapeURL(tokenEscola)}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.certificateHandler = new ForceAcceptCertificate();
            req.SetRequestHeader("X-Escola-Token", tokenEscola);
            req.timeout = TIMEOUT_CONEXAO;
            yield return req.SendWebRequest();

            DtoProgressoAlunoRoot dados = null;
            if (req.result == UnityWebRequest.Result.Success)
            {
                try { dados = JsonUtility.FromJson<DtoProgressoAlunoRoot>(req.downloadHandler.text); } catch { }
            }
            else
            {
                Debug.LogWarning($"[ServicoNuvem] Falha ao buscar progresso do aluno: {req.error}");
            }
            callback?.Invoke(dados);
        }
    }

    /// <summary>
    /// Busca todas as escolas disponíveis no servidor para seleção no jogo.
    /// </summary>
    public void BuscarEscolasDisponiveis(System.Action<List<DtoEscolaItem>> callback)
    {
        StartCoroutine(BuscarEscolasCoroutine(callback));
    }

    private IEnumerator BuscarEscolasCoroutine(System.Action<List<DtoEscolaItem>> callback)
    {
        string url = $"{servidorUrl}/api/jogo/escolas-disponiveis";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.certificateHandler = new ForceAcceptCertificate();
            req.timeout = 15;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = "{\"items\":" + req.downloadHandler.text + "}";
                DtoEscolasWrapper wrap = JsonUtility.FromJson<DtoEscolasWrapper>(json);
                callback?.Invoke(wrap != null ? wrap.items : new List<DtoEscolaItem>());
            }
            else
            {
                callback?.Invoke(new List<DtoEscolaItem>());
            }
        }
    }

    /// <summary>
    /// Autentica o professor de uma sala específica ou geral no servidor .NET.
    /// </summary>
    public void AutenticarProfessor(string login, string senha, System.Action<bool, DtoLoginProfessorResposta> callback)
    {
        StartCoroutine(AutenticarProfessorCoroutine(login, senha, callback));
    }

    private IEnumerator AutenticarProfessorCoroutine(string login, string senha, System.Action<bool, DtoLoginProfessorResposta> callback)
    {
        string loginLimpo = login != null ? login.Trim().ToLowerInvariant() : "";
        string senhaLimpa = senha != null ? senha.Trim() : "";
        string url = $"{servidorUrl}/api/jogo/login-professor?token={UnityWebRequest.EscapeURL(tokenEscola)}";
        string payload = JsonUtility.ToJson(new DtoLoginProfessorEnvio { login = loginLimpo, senha = senhaLimpa });

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.certificateHandler = new ForceAcceptCertificate();
            byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Escola-Token", tokenEscola);
            req.timeout = 15;

            yield return req.SendWebRequest();

            DtoLoginProfessorResposta res = null;
            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string json = req.downloadHandler.text;
                    res = JsonUtility.FromJson<DtoLoginProfessorResposta>(json);
                }
                catch { }

                callback?.Invoke(res != null && res.sucesso, res);
            }
            else
            {
                // Diferencia "senha errada" de "sem conexão", para a tela de login avisar corretamente
                string motivo;
                if (req.responseCode == 401) motivo = "Usuário ou senha do professor incorretos.";
                else if (req.responseCode == 404) motivo = "A escola deste computador não foi encontrada no servidor.";
                else motivo = "Sem conexão com o servidor. Verifique a internet e tente novamente.";
                callback?.Invoke(false, new DtoLoginProfessorResposta { sucesso = false, erro = motivo, tipo = req.responseCode == 401 ? "credenciais" : "conexao" });
            }
        }
    }

    // =========================================================
    // SISTEMA DE AUTO-ATUALIZAÇÃO REMOTA (PATCH DE ESCOLAS)
    // Permite que qualquer escola receba melhorias e correções
    // sem precisar baixar o jogo inteiro de 200MB novamente.
    // =========================================================

    [System.Serializable]
    public class DtoVersaoResposta
    {
        public string versao;
        public int versaoCodigo;
        public bool temPatch;
        public long tamanhoBytes;
        public string urlDownloadPatch;
        public string novidades;
    }

    // Versão do código DESTE build. Aumente a cada novo build publicado (e use o mesmo número no servidor ao enviar o patch).
    public int versaoLocalCodigo = 103;
    public string versaoLocalNome = "1.0.3";
    public bool patchProntoParaAplicar { get; private set; } = false;
    public string novidadesPatch { get; private set; } = "";

    public void VerificarAtualizacaoManual(System.Action<bool, string> onConcluido = null)
    {
        StartCoroutine(VerificarAtualizacaoCoroutine(onConcluido));
    }

    public IEnumerator VerificarAtualizacaoCoroutine(System.Action<bool, string> onConcluido = null)
    {
        string url = $"{servidorUrl}/api/jogo/versao";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.certificateHandler = new ForceAcceptCertificate();
            req.timeout = TIMEOUT_CONEXAO;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                DtoVersaoResposta info = null;
                try
                {
                    string json = req.downloadHandler.text;
                    info = JsonUtility.FromJson<DtoVersaoResposta>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AutoUpdater] Erro ao processar versão: {ex.Message}");
                }

                // Considera a versão do próprio build: sem isso, um PC novo baixava um patch MAIS ANTIGO
                // que o jogo instalado e voltava o código para trás ao sair do jogo.
                int versaoAtual = Math.Max(PlayerPrefs.GetInt("VersaoPatchCodigo", 0), versaoLocalCodigo);

                if (info != null && info.temPatch && info.versaoCodigo > versaoAtual)
                {
                    Debug.Log($"[AutoUpdater] Nova versão encontrada: v{info.versao} (código {info.versaoCodigo}). Baixando patch...");
                    yield return StartCoroutine(BaixarEAplicarPatchCoroutine(info, onConcluido));
                    yield break;
                }
                else
                {
                    onConcluido?.Invoke(false, "O jogo já está com a versão mais recente da prefeitura!");
                }
            }
            else
            {
                onConcluido?.Invoke(false, "Servidor de atualizações offline ou inacessível no momento.");
            }
        }
    }

    private IEnumerator BaixarEAplicarPatchCoroutine(DtoVersaoResposta info, System.Action<bool, string> onConcluido = null)
    {
        string downloadUrl = info.urlDownloadPatch.StartsWith("http") ? info.urlDownloadPatch : $"{servidorUrl}{info.urlDownloadPatch}";

        using (UnityWebRequest req = UnityWebRequest.Get(downloadUrl))
        {
            req.certificateHandler = new ForceAcceptCertificate();
            req.timeout = 35;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                byte[] bytes = req.downloadHandler.data;
                if (bytes != null && bytes.Length > 20000)
                {
                    try
                    {
                        string managedDir = Path.Combine(Application.dataPath, "Managed");
                        if (!Directory.Exists(managedDir)) Directory.CreateDirectory(managedDir);

                        string patchNovo = Path.Combine(managedDir, "Assembly-CSharp.dll.novo");
                        File.WriteAllBytes(patchNovo, bytes);

                        string batPath = Path.Combine(Application.dataPath, "..", "atualizar_jogo.bat");
                        string batConteudo = "@echo off\r\n" +
                            "timeout /t 1 /nobreak >nul\r\n" +
                            "set \"BASE_DIR=%~dp0\"\r\n" +
                            "if exist \"%BASE_DIR%RevoltaDasLetrinhas_Data\\Managed\\Assembly-CSharp.dll.novo\" (\r\n" +
                            "    copy /y \"%BASE_DIR%RevoltaDasLetrinhas_Data\\Managed\\Assembly-CSharp.dll.novo\" \"%BASE_DIR%RevoltaDasLetrinhas_Data\\Managed\\Assembly-CSharp.dll\" >nul\r\n" +
                            "    del \"%BASE_DIR%RevoltaDasLetrinhas_Data\\Managed\\Assembly-CSharp.dll.novo\" >nul\r\n" +
                            ")\r\n" +
                            "if \"%1\"==\"reiniciar\" (\r\n" +
                            "    start \"\" \"%BASE_DIR%RevoltaDasLetrinhas.exe\"\r\n" +
                            ")\r\n" +
                            "exit\r\n";

                        File.WriteAllText(batPath, batConteudo, Encoding.UTF8);

                        PlayerPrefs.SetInt("VersaoPatchCodigo", info.versaoCodigo);
                        PlayerPrefs.SetString("VersaoPatchNome", info.versao);
                        PlayerPrefs.Save();

                        patchProntoParaAplicar = true;
                        novidadesPatch = info.novidades;
                        Debug.Log($"[AutoUpdater] Patch v{info.versao} pronto para ser aplicado na saída ou reinício do jogo!");
                        onConcluido?.Invoke(true, $"Atualização v{info.versao} baixada com sucesso! Será aplicada ao fechar o jogo.");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[AutoUpdater] Falha ao salvar patch local: {ex.Message}");
                        onConcluido?.Invoke(false, "Erro ao gravar arquivo de atualização.");
                    }
                }
                else
                {
                    onConcluido?.Invoke(false, "Arquivo de atualização corrompido ou incompleto.");
                }
            }
            else
            {
                onConcluido?.Invoke(false, "Falha no download da atualização.");
            }
        }
    }

    /// <summary>
    /// Reinicia o jogo imediatamente aplicando a atualização baixada
    /// </summary>
    public void ReiniciarComAtualizacao()
    {
        try
        {
            PlayerPrefs.Save();
            string batPath = Path.Combine(Application.dataPath, "..", "atualizar_jogo.bat");
            if (File.Exists(batPath))
            {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = batPath,
                    Arguments = "reiniciar",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(psi);
            }
        }
        catch { }

        Application.Quit();
        try
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
        catch { }
    }
}
