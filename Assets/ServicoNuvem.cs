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

    [Header("Configurações do Servidor")]
    public string servidorUrl = "https://api.kairosstudios.site";
    public string tokenEscola = "ESC-CAMPINAS-01";
    public bool conectadoAoServidor { get; private set; } = false;
    public string nomeEscolaConectada { get; private set; } = "";
    public string municipioConectado { get; private set; } = "";
    public string validadeContrato { get; private set; } = "";
    public int diasRestantesContrato { get; private set; } = 999;
    public bool contratoValido { get; private set; } = true;

    // Evento disparado quando a escola ou alunos são atualizados
    public delegate void EscolaSincronizadaHandler();
    public static event EscolaSincronizadaHandler OnEscolaSincronizada;

    // Mapeamento local de alunos (Nome -> Guid do Banco de Dados)
    private Dictionary<string, string> mapaAlunosIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    // Mapeamento local de Turmas (Nome Aluno -> Nome da Turma)
    public List<DtoTurmaRecebida> turmasDisponiveis = new List<DtoTurmaRecebida>();
    private Dictionary<string, string> mapaAlunoTurma = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    // Fila offline para retentativas automáticas
    private Queue<DtoProgressoEnvio> filaOffline = new Queue<DtoProgressoEnvio>();
    private bool enviandoFila = false;

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
        public string nomeCompleto;
        public string nomeJogo;
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
        public string statusDesempenho;
    }

    [System.Serializable]
    public class DtoSalaResumo
    {
        public string turmaId;
        public string nomeTurma;
        public int anoLetivo;
        public int totalAlunos;
        public int totalFasesConcluidas;
        public float mediaFasesPorAluno;
        public float mediaEstrelas;
        public int totalErros;
        public float taxaConclusaoPort;
        public float taxaConclusaoMath;
        public List<DtoAlunoSalaDetalhe> alunos;
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
    }

    void Start()
    {
        StartCoroutine(ConectarESincronizarCoroutine());
        StartCoroutine(RotinaFilaOfflineCoroutine());
        StartCoroutine(VerificarAtualizacaoCoroutine());
    }

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
                    if (!string.IsNullOrEmpty(config.ServidorUrl)) servidorUrl = config.ServidorUrl.TrimEnd('/');
                    if (!string.IsNullOrEmpty(config.TokenEscola)) tokenEscola = config.TokenEscola.Trim();
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
        tokenEscola = novoToken.Trim();
        SalvarConfiguracaoLocal();
        StartCoroutine(ConectarESincronizarCoroutine());
    }

    public IEnumerator ConectarESincronizarCoroutine()
    {
        string url = $"{servidorUrl}/api/jogo/conectar?token={tokenEscola}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("X-Escola-Token", tokenEscola);
            req.timeout = 5;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                conectadoAoServidor = true;
                string jsonResposta = req.downloadHandler.text;
                DtoInfoEscolaRecebida info = JsonUtility.FromJson<DtoInfoEscolaRecebida>(jsonResposta);

                if (info != null)
                {
                    nomeEscolaConectada = info.nomeEscola;
                    municipioConectado = $"{info.municipio}/{info.uf}";
                    validadeContrato = !string.IsNullOrEmpty(info.dataExpiracaoContrato) ? info.dataExpiracaoContrato : "Sem expiração";
                    diasRestantesContrato = info.diasRestantesContrato;
                    contratoValido = info.contratoValido;

                    turmasDisponiveis.Clear();
                    mapaAlunosIds.Clear();
                    mapaAlunoTurma.Clear();
                    List<string> nomesRecebidos = new List<string>();

                    if (info.turmas != null)
                    {
                        turmasDisponiveis.AddRange(info.turmas);
                        foreach (var turma in info.turmas)
                        {
                            if (turma.alunos != null)
                            {
                                foreach (var aluno in turma.alunos)
                                {
                                    string nomeExibicao = !string.IsNullOrEmpty(aluno.nomeJogo) ? aluno.nomeJogo : aluno.nomeCompleto;
                                    mapaAlunosIds[nomeExibicao] = aluno.id;
                                    mapaAlunosIds[aluno.nomeCompleto] = aluno.id;
                                    mapaAlunoTurma[nomeExibicao] = turma.nome;
                                    mapaAlunoTurma[aluno.nomeCompleto] = turma.nome;
                                    PlayerPrefs.SetString($"Aluno_Turma_{nomeExibicao}", turma.nome);
                                    nomesRecebidos.Add(nomeExibicao);
                                }
                            }
                        }
                    }

                    // Define no jogo ESTRITAMENTE os alunos desta escola, isolando completamente de outras!
                    GerenciadorPerfis.DefinirAlunosDaEscola(nomesRecebidos);

                    Debug.Log($"[ServicoNuvem] Conectado com sucesso à escola '{nomeEscolaConectada}' ({municipioConectado})! Alunos carregados: {nomesRecebidos.Count}");
                    OnEscolaSincronizada?.Invoke();
                }
            }
            else
            {
                conectadoAoServidor = false;
                Debug.LogWarning($"[ServicoNuvem] Falha ao conectar ao servidor ({req.error}). Usando dados locais salvos.");
            }
        }
    }

    public void EnviarFaseConcluida(string nomeAluno, string materia, int nivelIndex, int numeroFase, int estrelas = 3, int pontuacao = 1000, int erros = 0, int tempo = 60)
    {
        string nivelStr = nivelIndex == 0 ? "Facil" : (nivelIndex == 1 ? "Medio" : "Dificil");
        string materiaStr = materia.ToLower().Contains("math") ? "Matematica" : "Portugues";

        DtoProgressoEnvio dto = new DtoProgressoEnvio
        {
            alunoNome = nomeAluno,
            materia = materiaStr,
            nivel = nivelStr,
            numeroFase = numeroFase,
            concluida = true,
            estrelas = estrelas,
            pontuacao = pontuacao,
            quantidadeErros = erros,
            tempoGastoSegundos = tempo
        };

        StartCoroutine(EnviarProgressoCoroutine(dto));
    }

    private IEnumerator EnviarProgressoCoroutine(DtoProgressoEnvio dto)
    {
        string alunoId = "";
        if (mapaAlunosIds.ContainsKey(dto.alunoNome))
        {
            alunoId = mapaAlunosIds[dto.alunoNome];
        }
        else
        {
            yield return StartCoroutine(CadastrarAlunoCoroutine(dto.alunoNome, "", (id) => alunoId = id));
        }

        if (string.IsNullOrEmpty(alunoId))
        {
            filaOffline.Enqueue(dto);
            yield break;
        }

        dto.alunoId = alunoId;
        string jsonPayload = JsonUtility.ToJson(dto);
        string url = $"{servidorUrl}/api/jogo/progresso?token={tokenEscola}";

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Escola-Token", tokenEscola);
            req.timeout = 5;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[ServicoNuvem] Progresso salvo na nuvem: {dto.alunoNome} | {dto.materia} Fase {dto.numeroFase}");
            }
            else
            {
                filaOffline.Enqueue(dto);
            }
        }
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

        string tokenFormatado = token.Trim();
        string url = $"{servidorUrl}/api/jogo/conectar?token={tokenFormatado}";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("X-Escola-Token", tokenFormatado);
            req.timeout = 6;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                DtoInfoEscolaRecebida info = JsonUtility.FromJson<DtoInfoEscolaRecebida>(json);

                if (info != null && !string.IsNullOrEmpty(info.nomeEscola))
                {
                    tokenEscola = tokenFormatado;
                    conectadoAoServidor = true;
                    nomeEscolaConectada = info.nomeEscola;
                    municipioConectado = $"{info.municipio}/{info.uf}";
                    validadeContrato = !string.IsNullOrEmpty(info.dataExpiracaoContrato) ? info.dataExpiracaoContrato : "Sem expiração";
                    diasRestantesContrato = info.diasRestantesContrato;
                    contratoValido = info.contratoValido;

                    SalvarConfiguracaoLocal();

                    turmasDisponiveis.Clear();
                    mapaAlunosIds.Clear();
                    mapaAlunoTurma.Clear();
                    List<string> nomesRecebidos = new List<string>();

                    if (info.turmas != null)
                    {
                        turmasDisponiveis.AddRange(info.turmas);
                        foreach (var turma in info.turmas)
                        {
                            if (turma.alunos != null)
                            {
                                foreach (var aluno in turma.alunos)
                                {
                                    string nomeExibicao = !string.IsNullOrEmpty(aluno.nomeJogo) ? aluno.nomeJogo : aluno.nomeCompleto;
                                    mapaAlunosIds[nomeExibicao] = aluno.id;
                                    mapaAlunosIds[aluno.nomeCompleto] = aluno.id;
                                    mapaAlunoTurma[nomeExibicao] = turma.nome;
                                    mapaAlunoTurma[aluno.nomeCompleto] = turma.nome;
                                    PlayerPrefs.SetString($"Aluno_Turma_{nomeExibicao}", turma.nome);
                                    nomesRecebidos.Add(nomeExibicao);
                                }
                            }
                        }
                    }

                    GerenciadorPerfis.DefinirAlunosDaEscola(nomesRecebidos);
                    OnEscolaSincronizada?.Invoke();

                    callback?.Invoke(true, $"{info.nomeEscola} ({info.municipio}/{info.uf})");
                    yield break;
                }
            }

            callback?.Invoke(false, "Token inválido ou não cadastrado no servidor.");
        }
    }

    public void CadastrarAlunoNuvem(string nomeAluno, string turmaId = "", System.Action<string> onIdRecebido = null)
    {
        StartCoroutine(CadastrarAlunoCoroutine(nomeAluno, turmaId, onIdRecebido));
    }

    private IEnumerator CadastrarAlunoCoroutine(string nomeAluno, string turmaId, System.Action<string> onIdRecebido)
    {
        DtoCriarAlunoEnvio dto = new DtoCriarAlunoEnvio
        {
            turmaId = turmaId,
            nomeCompleto = nomeAluno,
            nomeJogo = nomeAluno
        };

        string json = JsonUtility.ToJson(dto);
        string url = $"{servidorUrl}/api/jogo/criar-aluno?token={tokenEscola}";

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Escola-Token", tokenEscola);
            req.timeout = 5;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                DtoAlunoRecebido criado = JsonUtility.FromJson<DtoAlunoRecebido>(req.downloadHandler.text);
                if (criado != null && !string.IsNullOrEmpty(criado.id))
                {
                    mapaAlunosIds[nomeAluno] = criado.id;
                    if (!string.IsNullOrEmpty(criado.turmaNome))
                    {
                        mapaAlunoTurma[nomeAluno] = criado.turmaNome;
                        PlayerPrefs.SetString($"Aluno_Turma_{nomeAluno}", criado.turmaNome);
                    }
                    onIdRecebido?.Invoke(criado.id);
                    Debug.Log($"[ServicoNuvem] Aluno '{nomeAluno}' cadastrado na turma '{criado.turmaNome}' com ID: {criado.id}");
                    yield break;
                }
            }
        }
        onIdRecebido?.Invoke("");
    }

    /// <summary>
    /// Consulta o resumo de turmas e desempenho de cada sala diretamente no servidor .NET.
    /// </summary>
    public void BuscarResumoSalas(System.Action<DtoResumoSalasRoot> callback)
    {
        StartCoroutine(BuscarResumoSalasCoroutine(callback));
    }

    private IEnumerator BuscarResumoSalasCoroutine(System.Action<DtoResumoSalasRoot> callback)
    {
        string url = $"{servidorUrl}/api/jogo/salas-resumo?token={tokenEscola}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("X-Escola-Token", tokenEscola);
            req.timeout = 6;
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
    /// Notifica o servidor .NET para excluir o aluno do banco de dados na nuvem!
    /// </summary>
    public void ExcluirAlunoNuvem(string nomeAluno)
    {
        StartCoroutine(ExcluirAlunoCoroutine(nomeAluno));
    }

    private IEnumerator ExcluirAlunoCoroutine(string nomeAluno)
    {
        DtoExcluirAlunoEnvio dto = new DtoExcluirAlunoEnvio { nomeAluno = nomeAluno };
        string json = JsonUtility.ToJson(dto);
        string url = $"{servidorUrl}/api/jogo/excluir-aluno?token={tokenEscola}";

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Escola-Token", tokenEscola);
            req.timeout = 5;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                if (mapaAlunosIds.ContainsKey(nomeAluno)) mapaAlunosIds.Remove(nomeAluno);
                Debug.Log($"[ServicoNuvem] Aluno '{nomeAluno}' excluído com sucesso do servidor .NET!");
            }
            else
            {
                Debug.LogWarning($"[ServicoNuvem] Falha ao excluir aluno no servidor: {req.error}");
            }
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
            req.timeout = 5;
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
        string url = $"{servidorUrl}/api/jogo/login-professor?token={tokenEscola}";
        string payload = JsonUtility.ToJson(new DtoLoginProfessorEnvio { login = login, senha = senha });

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Escola-Token", tokenEscola);
            req.timeout = 7;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string json = req.downloadHandler.text;
                DtoLoginProfessorResposta res = JsonUtility.FromJson<DtoLoginProfessorResposta>(json);
                callback?.Invoke(true, res);
            }
            else
            {
                callback?.Invoke(false, null);
            }
        }
    }

    private IEnumerator RotinaFilaOfflineCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(20f);

            if (filaOffline.Count > 0 && !enviandoFila)
            {
                enviandoFila = true;
                int count = filaOffline.Count;
                for (int i = 0; i < count; i++)
                {
                    if (filaOffline.Count == 0) break;
                    var item = filaOffline.Dequeue();
                    yield return StartCoroutine(EnviarProgressoCoroutine(item));
                }
                enviandoFila = false;
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

    public int versaoLocalCodigo = 101;
    public string versaoLocalNome = "1.0.1";
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
            req.timeout = 6;
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

                int versaoAtual = PlayerPrefs.GetInt("VersaoPatchCodigo", 100);

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
            req.timeout = 30;
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
