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
        public bool temLimiteAlunos;
        public int limiteAlunos;
        public int totalAlunos;
        public int limiteMaquinas;
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

        // Até o servidor responder, vale o que ficou salvo da última verificação da licença
        AvaliarLicencaSemServidor(tokenEscola, false);
    }

    void Start()
    {
        StartCoroutine(RotinaLicencaCoroutine());
        StartCoroutine(ConectarESincronizarCoroutine());
        StartCoroutine(RotinaSincronizacaoCoroutine());
        StartCoroutine(VerificarAtualizacaoCoroutine());
    }

    void OnApplicationQuit()
    {
        SalvarFila();
        EncerrarSessaoDaMaquina();
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
                DtoErroServidor erroLicenca = LerErro(req.downloadHandler?.text);
                if ((req.responseCode == 403 || req.responseCode == 404) && erroLicenca != null && !string.IsNullOrEmpty(erroLicenca.codigo))
                    AplicarBloqueio(token, erroLicenca.codigo, erroLicenca.erro, erroLicenca.codigo != "limite_maquinas");
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
        temExpiracaoContrato = !string.IsNullOrEmpty(info.dataExpiracaoContrato);
        temLimiteAlunos = info.temLimiteAlunos;
        limiteAlunos = info.limiteAlunos;
        if (info.limiteMaquinas > 0) limiteMaquinas = info.limiteMaquinas;

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
                    VerificarLicencaAgora();

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
            else if (req.responseCode == 403)
            {
                DtoErroServidor erroLicenca = LerErro(req.downloadHandler?.text);
                if (erroLicenca != null && !string.IsNullOrEmpty(erroLicenca.erro)) erroMsg = erroLicenca.erro;
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
    private ResultadoEnvio Classificar(UnityWebRequest req, OperacaoNuvem op, bool naoEncontradoEhSucesso)
    {
        if (req.result == UnityWebRequest.Result.Success) return ResultadoEnvio.Sucesso;

        long codigo = req.responseCode;
        string corpo = req.downloadHandler?.text ?? "";

        // A escola atingiu o limite de alunos da licença: o cadastro é cancelado (não adianta reenviar)
        if (codigo == 409 && corpo.Contains("\"limite_alunos\""))
        {
            DtoErroServidor e = LerErro(corpo);
            string motivo = e != null && !string.IsNullOrEmpty(e.erro) ? e.erro : "A escola atingiu o limite de alunos da licença.";
            Debug.LogWarning($"[ServicoNuvem] Cadastro de '{op.aluno}' recusado: {motivo}");
            if (MesmoToken(op.token, tokenEscola)) OnCadastroRecusado?.Invoke(op.aluno, motivo);
            return ResultadoEnvio.Descartar;
        }

        // Licença bloqueada (contrato vencido, escola desativada...): guarda para enviar quando liberar
        if (codigo == 403)
        {
            DtoErroServidor e = LerErro(corpo);
            if (e != null && !string.IsNullOrEmpty(e.codigo) && MesmoToken(op.token, tokenEscola))
                AplicarBloqueio(op.token, e.codigo, e.erro, e.codigo != "limite_maquinas");
            return ResultadoEnvio.TentarDepois;
        }
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
    // LICENÇA: COMPUTADORES EM USO, VENCIMENTO E LIMITE DE ALUNOS
    // O jogo avisa o servidor a cada minuto que este computador está em uso.
    // O servidor recusa quando o contrato venceu, a escola foi desativada,
    // o computador foi bloqueado ou já há o máximo de computadores em uso.
    // Sem internet, vale a última verificação por até DIAS_TOLERANCIA_OFFLINE dias.
    // =========================================================

    public enum EstadoLicenca { Verificando, Liberada, Bloqueada }
    public EstadoLicenca estadoLicenca { get; private set; } = EstadoLicenca.Verificando;
    public string codigoBloqueio { get; private set; } = "";
    public string mensagemBloqueio { get; private set; } = "";
    public bool temLimiteAlunos { get; private set; }
    public int limiteAlunos { get; private set; }
    public int maquinasEmUso { get; private set; }
    public int limiteMaquinas { get; private set; } = 100;
    public bool temExpiracaoContrato { get; private set; }
    public bool licencaValidadaOnline { get; private set; }

    public delegate void CadastroRecusadoHandler(string aluno, string motivo);
    /// <summary>O servidor recusou o cadastro de um aluno (limite de alunos da escola).</summary>
    public static event CadastroRecusadoHandler OnCadastroRecusado;
    public static event System.Action OnLicencaAtualizada;

    private const string KEY_LIC_TOKEN = "Licenca_Token";
    private const string KEY_LIC_VALIDACAO = "Licenca_UltimaValidacaoUtc";
    private const string KEY_LIC_PRIMEIRO_USO = "Licenca_PrimeiroUsoSemValidacaoUtc";
    private const string KEY_LIC_EXPIRACAO = "Licenca_DataExpiracao";
    private const string KEY_LIC_BLOQUEIO_CODIGO = "Licenca_BloqueioCodigo";
    private const string KEY_LIC_BLOQUEIO_MSG = "Licenca_BloqueioMensagem";
    private const string KEY_MAQUINA_ID = "Licenca_IdentificadorMaquina";
    private const int DIAS_TOLERANCIA_OFFLINE = 7;
    private const float ESPERA_BLOQUEADO = 30f;

    private float intervaloSinal = 60f;
    private bool verificandoLicenca = false;
    private bool sessaoEncerrada = false;

    [System.Serializable]
    public class DtoSinalMaquinaEnvio
    {
        public string identificador;
        public string nome;
        public string versaoJogo;
    }

    [System.Serializable]
    public class DtoSinalResposta
    {
        public bool liberado;
        public int maquinasEmUso;
        public int limiteMaquinas;
        public int intervaloSinalSegundos;
        public bool temExpiracao;
        public string dataExpiracao;
        public int diasRestantes;
        public bool temLimiteAlunos;
        public int limiteAlunos;
        public int totalAlunos;
    }

    [System.Serializable]
    public class DtoErroServidor
    {
        public string erro;
        public string codigo;
        public bool temExpiracao;
        public string dataExpiracao;
        public int diasRestantes;
        public int maquinasEmUso;
        public int limiteMaquinas;
    }

    /// <summary>Identificador único deste computador (do hardware; se indisponível, um gerado e salvo).</summary>
    public static string IdentificadorMaquina
    {
        get
        {
            string id = SystemInfo.deviceUniqueIdentifier;
            if (string.IsNullOrEmpty(id) || id == SystemInfo.unsupportedIdentifier)
            {
                id = PlayerPrefs.GetString(KEY_MAQUINA_ID, "");
                if (string.IsNullOrEmpty(id))
                {
                    id = Guid.NewGuid().ToString("N");
                    PlayerPrefs.SetString(KEY_MAQUINA_ID, id);
                    PlayerPrefs.Save();
                }
            }
            return id;
        }
    }

    private static DtoErroServidor LerErro(string corpo)
    {
        if (string.IsNullOrEmpty(corpo) || !corpo.TrimStart().StartsWith("{")) return null;
        try { return JsonUtility.FromJson<DtoErroServidor>(corpo); } catch { return null; }
    }

    /// <summary>Confere a licença com o servidor agora (botão "Tentar novamente", troca de token).</summary>
    public void VerificarLicencaAgora()
    {
        if (!verificandoLicenca) StartCoroutine(EnviarSinalCoroutine());
    }

    /// <summary>
    /// Pode cadastrar mais um aluno? (limite de alunos da licença da escola)
    /// </summary>
    public bool PodeCadastrarAluno(int alunosNoJogo, out string motivo)
    {
        motivo = "";
        if (!temLimiteAlunos || alunosNoJogo < limiteAlunos) return true;
        motivo = $"A escola atingiu o limite de {limiteAlunos} alunos da licença. Fale com a coordenação para liberar mais vagas.";
        return false;
    }

    private IEnumerator RotinaLicencaCoroutine()
    {
        while (true)
        {
            yield return StartCoroutine(EnviarSinalCoroutine());
            // Bloqueado: tenta de novo mais rápido (ex.: alguém fechou o jogo em outro computador)
            float espera = estadoLicenca == EstadoLicenca.Bloqueada ? ESPERA_BLOQUEADO : intervaloSinal;
            yield return new WaitForSecondsRealtime(espera);
        }
    }

    private IEnumerator EnviarSinalCoroutine()
    {
        if (verificandoLicenca) yield break;
        verificandoLicenca = true;

        string token = tokenEscola;
        string json = JsonUtility.ToJson(new DtoSinalMaquinaEnvio
        {
            identificador = IdentificadorMaquina,
            nome = SystemInfo.deviceName,
            versaoJogo = versaoLocalNome
        });

        long codigoHttp = 0;
        string corpo = "";
        yield return StartCoroutine(PostJsonCoroutine("/api/jogo/maquina/sinal", token, json, req =>
        {
            codigoHttp = req.responseCode;
            corpo = req.downloadHandler?.text ?? "";
        }));

        DtoErroServidor erro = LerErro(corpo);
        if (codigoHttp == 200)
        {
            DtoSinalResposta r = null;
            try { r = JsonUtility.FromJson<DtoSinalResposta>(corpo); } catch { }
            if (r != null && r.liberado)
            {
                intervaloSinal = Mathf.Clamp(r.intervaloSinalSegundos > 0 ? r.intervaloSinalSegundos : 60, 20, 300);
                maquinasEmUso = r.maquinasEmUso;
                limiteMaquinas = r.limiteMaquinas;
                temLimiteAlunos = r.temLimiteAlunos;
                limiteAlunos = r.limiteAlunos;
                temExpiracaoContrato = r.temExpiracao;
                if (r.temExpiracao)
                {
                    diasRestantesContrato = r.diasRestantes;
                    validadeContrato = r.dataExpiracao;
                }
                SalvarLicencaValidada(token, r.temExpiracao ? r.dataExpiracao : "");
                Liberar(true);
            }
            else
            {
                AvaliarLicencaSemServidor(token, true);
            }
        }
        else if ((codigoHttp == 403 || codigoHttp == 404) && erro != null && !string.IsNullOrEmpty(erro.codigo))
        {
            // limite de computadores é passageiro: não fica gravado para o modo sem internet
            AplicarBloqueio(token, erro.codigo, erro.erro, erro.codigo != "limite_maquinas");
        }
        else
        {
            // Sem internet, servidor parado ou servidor ainda sem esta verificação
            AvaliarLicencaSemServidor(token, true);
        }

        verificandoLicenca = false;
    }

    private void SalvarLicencaValidada(string token, string dataExpiracao)
    {
        PlayerPrefs.SetString(KEY_LIC_TOKEN, token);
        PlayerPrefs.SetString(KEY_LIC_VALIDACAO, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.SetString(KEY_LIC_EXPIRACAO, dataExpiracao ?? "");
        PlayerPrefs.DeleteKey(KEY_LIC_BLOQUEIO_CODIGO);
        PlayerPrefs.DeleteKey(KEY_LIC_BLOQUEIO_MSG);
        PlayerPrefs.DeleteKey(KEY_LIC_PRIMEIRO_USO);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Decide sem o servidor: último bloqueio informado, data do contrato salva e tempo sem validar pela internet.
    /// </summary>
    private void AvaliarLicencaSemServidor(string token, bool servidorConsultado)
    {
        bool mesmoToken = MesmoToken(PlayerPrefs.GetString(KEY_LIC_TOKEN, ""), token);

        // 1. O último bloqueio informado pelo servidor para esta escola continua valendo
        string codigoSalvo = mesmoToken ? PlayerPrefs.GetString(KEY_LIC_BLOQUEIO_CODIGO, "") : "";
        if (!string.IsNullOrEmpty(codigoSalvo))
        {
            AplicarBloqueio(token, codigoSalvo, PlayerPrefs.GetString(KEY_LIC_BLOQUEIO_MSG, ""), false);
            return;
        }

        // 2. Contrato vencido pela data salva (o contrato vale até o fim do dia)
        string expiracao = mesmoToken ? PlayerPrefs.GetString(KEY_LIC_EXPIRACAO, "") : "";
        if (!string.IsNullOrEmpty(expiracao) &&
            DateTime.TryParseExact(expiracao, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime dataExp) &&
            DateTime.UtcNow >= dataExp.Date.AddDays(1).AddHours(3))
        {
            AplicarBloqueio(token, "contrato_vencido",
                $"O contrato da prefeitura venceu em {dataExp:dd/MM/yyyy}. O jogo volta a funcionar assim que o contrato for renovado.", false);
            return;
        }

        // 3. Tempo sem confirmar a licença pela internet
        DateTime referencia;
        if (mesmoToken && long.TryParse(PlayerPrefs.GetString(KEY_LIC_VALIDACAO, ""), out long ticks))
        {
            referencia = new DateTime(ticks, DateTimeKind.Utc);
        }
        else
        {
            if (!long.TryParse(PlayerPrefs.GetString(KEY_LIC_PRIMEIRO_USO, ""), out long primeiro))
            {
                primeiro = DateTime.UtcNow.Ticks;
                PlayerPrefs.SetString(KEY_LIC_PRIMEIRO_USO, primeiro.ToString());
                PlayerPrefs.Save();
            }
            referencia = new DateTime(primeiro, DateTimeKind.Utc);
        }

        double dias = (DateTime.UtcNow - referencia).TotalDays;
        if (dias > DIAS_TOLERANCIA_OFFLINE || dias < -1)
        {
            AplicarBloqueio(token, "sem_validacao",
                $"Este computador está há mais de {DIAS_TOLERANCIA_OFFLINE} dias sem confirmar a licença pela internet. " +
                "Conecte o computador à internet e clique em \"Tentar novamente\".", false);
            return;
        }

        if (servidorConsultado) Debug.Log($"[Licença] Servidor indisponível; usando a licença salva (validada há {dias:0.0} dia(s)).");
        Liberar(false);
    }

    private void AplicarBloqueio(string token, string codigo, string mensagem, bool persistir)
    {
        if (persistir)
        {
            PlayerPrefs.SetString(KEY_LIC_TOKEN, token);
            PlayerPrefs.SetString(KEY_LIC_BLOQUEIO_CODIGO, codigo);
            PlayerPrefs.SetString(KEY_LIC_BLOQUEIO_MSG, mensagem ?? "");
            PlayerPrefs.Save();
        }

        bool mudou = estadoLicenca != EstadoLicenca.Bloqueada || codigoBloqueio != codigo;
        estadoLicenca = EstadoLicenca.Bloqueada;
        codigoBloqueio = codigo;
        mensagemBloqueio = string.IsNullOrEmpty(mensagem) ? "O uso do jogo não está liberado para esta escola." : mensagem;
        if (mudou) Debug.LogWarning($"[Licença] Jogo bloqueado ({codigo}): {mensagemBloqueio}");
        MostrarTelaBloqueio();
        OnLicencaAtualizada?.Invoke();
    }

    private void Liberar(bool validadaOnline)
    {
        bool estavaBloqueada = estadoLicenca == EstadoLicenca.Bloqueada;
        estadoLicenca = EstadoLicenca.Liberada;
        codigoBloqueio = "";
        mensagemBloqueio = "";
        if (validadaOnline) licencaValidadaOnline = true;
        TelaBloqueioLicenca.Esconder();
        if (estavaBloqueada)
        {
            Debug.Log("[Licença] Jogo liberado.");
            StartCoroutine(ConectarESincronizarCoroutine());
        }
        OnLicencaAtualizada?.Invoke();
    }

    private void MostrarTelaBloqueio()
    {
        string titulo;
        switch (codigoBloqueio)
        {
            case "contrato_vencido": titulo = "Contrato da prefeitura vencido"; break;
            case "escola_inativa": titulo = "Acesso da escola desativado"; break;
            case "maquina_bloqueada": titulo = "Este computador foi bloqueado"; break;
            case "limite_maquinas": titulo = "Limite de computadores atingido"; break;
            case "escola_nao_encontrada": titulo = "Escola não encontrada"; break;
            case "sem_validacao": titulo = "Conecte-se à internet"; break;
            default: titulo = "Jogo bloqueado"; break;
        }

        string mensagem = mensagemBloqueio;
        if (codigoBloqueio == "limite_maquinas") mensagem += "\n<size=22><color=#64748B>Tentando de novo sozinho a cada 30 segundos.</color></size>";

        string escola = !string.IsNullOrEmpty(nomeEscolaConectada) ? nomeEscolaConectada + "  ·  " : "";
        string detalhes = $"{escola}Token: {tokenEscola}  ·  Computador: {SystemInfo.deviceName}";
        TelaBloqueioLicenca.Mostrar(titulo, mensagem, detalhes, codigoBloqueio != "limite_maquinas");
    }

    /// <summary>Usado pela tela de bloqueio: troca o token da escola e revalida a licença.</summary>
    public void TrocarTokenPelaTelaDeBloqueio(string token)
    {
        ValidarEVincularToken(token, (ok, msg) =>
        {
            if (ok)
            {
                TelaBloqueioLicenca.MostrarRetorno($"Token aceito: {msg}. Verificando a licença...", false);
                VerificarLicencaAgora();
            }
            else
            {
                TelaBloqueioLicenca.MostrarRetorno(msg, true);
            }
        });
    }

    /// <summary>
    /// Avisa o servidor que este computador saiu do jogo, liberando a vaga na hora
    /// (sem isso, a vaga fica ocupada por até 3 minutos). Espera no máximo 2 segundos.
    /// </summary>
    public void EncerrarSessaoDaMaquina()
    {
        if (sessaoEncerrada || estadoLicenca != EstadoLicenca.Liberada) return;
        sessaoEncerrada = true;
        try
        {
            string json = JsonUtility.ToJson(new DtoSinalMaquinaEnvio { identificador = IdentificadorMaquina, nome = SystemInfo.deviceName, versaoJogo = versaoLocalNome });
            using (var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) })
            using (var pedido = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post,
                       $"{servidorUrl}/api/jogo/maquina/encerrar?token={Uri.EscapeDataString(tokenEscola)}"))
            {
                pedido.Headers.Add("X-Escola-Token", tokenEscola);
                pedido.Content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
                http.SendAsync(pedido).Wait(2000);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Licença] Não foi possível avisar a saída ao servidor: {ex.Message}");
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
    // O texto de versão do Lobby também lê daqui.
    public const int VersaoCodigoDoBuild = 106;
    public const string VersaoNomeDoBuild = "1.0.6";
    public int versaoLocalCodigo = VersaoCodigoDoBuild;
    public string versaoLocalNome = VersaoNomeDoBuild;
    public bool patchProntoParaAplicar { get; private set; } = false;
    public string novidadesPatch { get; private set; } = "";

    public void VerificarAtualizacaoManual(System.Action<bool, string> onConcluido = null)
    {
        StartCoroutine(VerificarAtualizacaoCoroutine(onConcluido));
    }

    /// <summary>Atualização baixada que ainda não foi instalada.</summary>
    private static string CaminhoPatchNovo() => Path.Combine(Application.dataPath, "Managed", "Assembly-CSharp.dll.novo");

    public IEnumerator VerificarAtualizacaoCoroutine(System.Action<bool, string> onConcluido = null)
    {
#if UNITY_EDITOR
        // No Editor da Unity não há o que atualizar (e gravaria arquivos dentro de Assets)
        onConcluido?.Invoke(false, "Atualização automática desativada no Editor da Unity.");
        yield break;
#else
        string url = $"{servidorUrl}/api/jogo/versao";
        DtoVersaoResposta info = null;
        bool servidorRespondeu = false;
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.certificateHandler = new ForceAcceptCertificate();
            req.timeout = TIMEOUT_CONEXAO;
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                servidorRespondeu = true;
                try
                {
                    info = JsonUtility.FromJson<DtoVersaoResposta>(req.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AutoUpdater] Erro ao processar versão: {ex.Message}");
                }
            }
        }

        // versaoLocalCodigo é a versão do código que está rodando (o patch instalado traz o número dele).
        // Se já existe um patch baixado esperando instalação, conta a versão dele para não baixar de novo.
        // (Só o valor salvo não basta: se o jogo for reinstalado, ele ficaria preso na versão antiga.)
        int versaoBaixada = File.Exists(CaminhoPatchNovo()) ? PlayerPrefs.GetInt("VersaoPatchCodigo", 0) : 0;
        int versaoAtual = Math.Max(versaoBaixada, versaoLocalCodigo);

        if (info != null && info.temPatch && info.versaoCodigo > versaoAtual)
        {
            Debug.Log($"[AutoUpdater] Nova versão encontrada: v{info.versao} (código {info.versaoCodigo}). Baixando patch...");
            yield return StartCoroutine(BaixarEAplicarPatchCoroutine(info, onConcluido));
            yield break;
        }

        // Atualização baixada numa vez anterior e ainda não instalada: oferece instalar agora
        if (File.Exists(CaminhoPatchNovo()))
        {
            if (PlayerPrefs.GetInt("VersaoPatchCodigo", 0) > versaoLocalCodigo)
            {
                try { CriarScriptDeInstalacao(); } catch (Exception ex) { Debug.LogWarning($"[AutoUpdater] {ex.Message}"); }
                patchProntoParaAplicar = true;
                OferecerReinicio(PlayerPrefs.GetString("VersaoPatchNome", "nova"));
                onConcluido?.Invoke(true, "Atualização pronta para instalar.");
                yield break;
            }
            // Arquivo velho (este jogo já é igual ou mais novo que ele): descarta
            try { File.Delete(CaminhoPatchNovo()); } catch { }
        }

        onConcluido?.Invoke(false, servidorRespondeu
            ? "O jogo já está com a versão mais recente da prefeitura!"
            : "Servidor de atualizações offline ou inacessível no momento.");
#endif
    }

    private IEnumerator BaixarEAplicarPatchCoroutine(DtoVersaoResposta info, System.Action<bool, string> onConcluido = null)
    {
        string downloadUrl = info.urlDownloadPatch.StartsWith("http") ? info.urlDownloadPatch : $"{servidorUrl}{info.urlDownloadPatch}";
        TelaAtualizacao.MostrarBaixando(info.versao, versaoLocalNome, info.novidades, info.tamanhoBytes);
        float inicio = Time.realtimeSinceStartup;

        byte[] bytes = null;
        string erroDownload = null;
        using (UnityWebRequest req = UnityWebRequest.Get(downloadUrl))
        {
            req.certificateHandler = new ForceAcceptCertificate();
            req.timeout = 180;
            UnityWebRequestAsyncOperation envio = req.SendWebRequest();
            while (!envio.isDone)
            {
                TelaAtualizacao.AtualizarProgresso(req.downloadProgress, (long)req.downloadedBytes, info.tamanhoBytes);
                yield return null;
            }

            if (req.result == UnityWebRequest.Result.Success) bytes = req.downloadHandler.data;
            else erroDownload = req.error;
        }

        // Deixa a barra visível por um instante (o arquivo é pequeno e baixa muito rápido)
        while (Time.realtimeSinceStartup - inicio < 1.5f)
        {
            TelaAtualizacao.AtualizarProgresso(bytes != null ? 1f : 0f, bytes?.Length ?? 0, info.tamanhoBytes);
            yield return null;
        }

        if (bytes == null)
        {
            Debug.LogWarning($"[AutoUpdater] Falha no download: {erroDownload}");
            TelaAtualizacao.MostrarErro("A atualização não pôde ser baixada (verifique a internet).");
            onConcluido?.Invoke(false, "Falha no download da atualização.");
            yield break;
        }
        if (bytes.Length < 20000 || (info.tamanhoBytes > 0 && bytes.Length != info.tamanhoBytes))
        {
            TelaAtualizacao.MostrarErro("O arquivo da atualização chegou incompleto.");
            onConcluido?.Invoke(false, "Arquivo de atualização corrompido ou incompleto.");
            yield break;
        }

        try
        {
            string managedDir = Path.Combine(Application.dataPath, "Managed");
            if (!Directory.Exists(managedDir)) Directory.CreateDirectory(managedDir);
            File.WriteAllBytes(CaminhoPatchNovo(), bytes);
            CriarScriptDeInstalacao();

            PlayerPrefs.SetInt("VersaoPatchCodigo", info.versaoCodigo);
            PlayerPrefs.SetString("VersaoPatchNome", info.versao);
            PlayerPrefs.Save();

            patchProntoParaAplicar = true;
            novidadesPatch = info.novidades;
            Debug.Log($"[AutoUpdater] Patch v{info.versao} pronto para ser aplicado ao reiniciar ou sair do jogo.");
            OferecerReinicio(info.versao);
            onConcluido?.Invoke(true, $"Atualização v{info.versao} baixada com sucesso!");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AutoUpdater] Falha ao salvar patch local: {ex.Message}");
            TelaAtualizacao.MostrarErro("Não foi possível gravar a atualização nesta pasta (falta permissão?).");
            onConcluido?.Invoke(false, "Erro ao gravar arquivo de atualização.");
        }
    }

    private void OferecerReinicio(string versao)
    {
        TelaAtualizacao.MostrarPronta(versao, ReiniciarComAtualizacao, TelaAtualizacao.Esconder);
    }

    /// <summary>
    /// Script que troca a DLL depois que o jogo fecha (o arquivo em uso não pode ser trocado com o jogo aberto).
    /// Usa o nome real do executável e da pasta de dados deste jogo.
    /// </summary>
    private static void CriarScriptDeInstalacao()
    {
        string pastaDados = Path.GetFileName(Application.dataPath);            // ex.: RevoltaDasLetrinhas_Data
        string executavel = pastaDados.EndsWith("_Data") ? pastaDados.Substring(0, pastaDados.Length - 5) + ".exe" : "RevoltaDasLetrinhas.exe";
        string batPath = Path.Combine(Application.dataPath, "..", "atualizar_jogo.bat");
        // Tenta a troca por até ~30 s: o arquivo fica travado até o jogo terminar de fechar.
        // ("ping" como pausa: o "timeout" não funciona quando o script roda sem janela)
        string batConteudo = "@echo off\r\n" +
            "set \"BASE_DIR=%~dp0\"\r\n" +
            $"set \"MANAGED=%BASE_DIR%{pastaDados}\\Managed\"\r\n" +
            "set TENTATIVAS=0\r\n" +
            ":esperar\r\n" +
            "ping -n 3 127.0.0.1 >nul\r\n" +
            "if not exist \"%MANAGED%\\Assembly-CSharp.dll.novo\" goto iniciar\r\n" +
            "copy /y \"%MANAGED%\\Assembly-CSharp.dll.novo\" \"%MANAGED%\\Assembly-CSharp.dll\" >nul 2>&1\r\n" +
            "if not errorlevel 1 goto copiado\r\n" +
            "set /a TENTATIVAS+=1\r\n" +
            "if %TENTATIVAS% lss 15 goto esperar\r\n" +
            "goto iniciar\r\n" +
            ":copiado\r\n" +
            "del \"%MANAGED%\\Assembly-CSharp.dll.novo\" >nul 2>&1\r\n" +
            ":iniciar\r\n" +
            $"if \"%1\"==\"reiniciar\" start \"\" \"%BASE_DIR%{executavel}\"\r\n" +
            "exit\r\n";

        // Sem BOM: um BOM no começo faz o Windows não reconhecer o "@echo off"
        File.WriteAllText(batPath, batConteudo, new UTF8Encoding(false));
    }

    /// <summary>
    /// Reinicia o jogo imediatamente aplicando a atualização baixada
    /// </summary>
    public void ReiniciarComAtualizacao()
    {
        EncerrarSessaoDaMaquina();
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
