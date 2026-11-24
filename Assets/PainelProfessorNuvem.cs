using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Área do Professor (nova): desempenho da sala e de cada aluno, com os dados do SERVIDOR
/// (somam todos os computadores da escola). Um professor de sala vê só a sua sala;
/// o acesso geral da escola vê todas.
/// </summary>
public class PainelProfessorNuvem : MonoBehaviour
{
    // =====================================================================
    // PALETA
    // =====================================================================
    private static readonly Color COR_OVERLAY = Hex("0F172A", 0.85f);
    private static readonly Color COR_JANELA = Hex("F1F5F9");
    private static readonly Color COR_CARD = Color.white;
    private static readonly Color COR_TITULO = Hex("1E293B");
    private static readonly Color COR_TEXTO = Hex("334155");
    private static readonly Color COR_SUAVE = Hex("64748B");
    private static readonly Color COR_LINHA = Hex("F8FAFC");
    private static readonly Color COR_LINHA_HOVER = Hex("EEF2FF");
    private static readonly Color COR_PRIMARIA = Hex("4F46E5");
    private static readonly Color COR_PRIMARIA_ESCURA = Hex("3730A3");
    private static readonly Color COR_PORT = Hex("3B82F6");
    private static readonly Color COR_MATH = Hex("10B981");
    private static readonly Color COR_ESTRELA = Hex("F59E0B");
    private static readonly Color COR_ERRO = Hex("EF4444");
    private static readonly Color COR_ROXO = Hex("8B5CF6");
    private static readonly Color COR_VAZIO = Hex("E2E8F0");

    private const string TODAS = "";
    private const int FASES_POR_NIVEL = 30;
    private const int FASES_POR_MATERIA = 90;

    private enum Aba { Sala, Alunos, Habilidades }

    // =====================================================================
    // ESTADO
    // =====================================================================
    private AreaDoProfessor area;
    private TMP_FontAsset fonte;
    private TMP_FontAsset fonteTitulo;
    private Sprite spriteRedondo;
    private Sprite spriteRedondoPequeno;
    private Sprite spriteCirculo;

    private GameObject raiz;
    private TextMeshProUGUI txtSubtitulo;
    private Image imgAbaSala, imgAbaAlunos, imgAbaHab;
    private TextMeshProUGUI txtAbaSala, txtAbaAlunos, txtAbaHab;
    private RectTransform areaChips;
    private Transform chipsContent;
    private TextMeshProUGUI txtSalaFixa;
    private RectTransform viewSala, viewAlunos, viewHabilidades, viewMensagem;
    private TextMeshProUGUI txtMensagem;
    private GameObject btnTentarNovamente;
    private Transform listaAlunosContent;
    private RectTransform detalheAluno;
    private TMP_InputField inputBusca;

    private ServicoNuvem.DtoResumoSalasRoot dados;
    private readonly Dictionary<string, ServicoNuvem.DtoProgressoAlunoRoot> cacheDetalhe = new Dictionary<string, ServicoNuvem.DtoProgressoAlunoRoot>();
    private Aba abaAtual = Aba.Sala;
    private string salaSelecionadaId = TODAS;
    private string alunoSelecionadoId = "";
    private string filtroBusca = "";
    private bool carregando;

    public bool Aberto => raiz != null && raiz.activeSelf;

    // Linha de aluno já com a sala, usada nas listas
    private class AlunoComSala
    {
        public ServicoNuvem.DtoAlunoSalaDetalhe aluno;
        public ServicoNuvem.DtoSalaResumo sala;
    }

    // =====================================================================
    // ABERTURA / FECHAMENTO
    // =====================================================================
    public void Configurar(AreaDoProfessor areaProfessor, TMP_FontAsset fonteTexto, TMP_FontAsset fonteDeTitulo)
    {
        area = areaProfessor;
        fonte = fonteTexto != null ? fonteTexto : TMP_Settings.defaultFontAsset;
        fonteTitulo = fonteDeTitulo != null ? fonteDeTitulo : fonte;
    }

    public void Abrir()
    {
        if (raiz == null) Construir();
        raiz.SetActive(true);
        raiz.transform.SetAsLastSibling();

        salaSelecionadaId = TODAS;
        alunoSelecionadoId = "";
        filtroBusca = "";
        if (inputBusca != null) inputBusca.SetTextWithoutNotify("");

        AtualizarCabecalho();
        AtualizarAbas();
        Recarregar();
    }

    public void Fechar()
    {
        if (raiz != null) raiz.SetActive(false);
    }

    private void Recarregar()
    {
        ServicoNuvem sn = ServicoNuvem.Instancia;
        cacheDetalhe.Clear();
        if (sn == null)
        {
            MostrarMensagem("O serviço de conexão do jogo não está disponível.", true);
            return;
        }

        carregando = true;
        MostrarMensagem("Carregando os dados da escola no servidor...\n<size=18>(se o servidor estava parado, isso pode levar até 1 minuto)</size>", false);
        sn.BuscarResumoSalas(resultado =>
        {
            carregando = false;
            if (raiz == null || !raiz.activeSelf) return;
            if (resultado == null || resultado.salas == null)
            {
                MostrarMensagem("Não foi possível carregar os dados do servidor.\nVerifique a internet e tente novamente.", true);
                return;
            }

            dados = resultado;
            List<ServicoNuvem.DtoSalaResumo> salas = SalasPermitidas();
            if (salas.Count == 0)
            {
                MostrarMensagem(ProfessorRestrito
                    ? $"A sala \"{AreaDoProfessor.TurmaRestritaNome}\" não foi encontrada no servidor.\nConfira o cadastro no painel da escola."
                    : "Esta escola ainda não tem salas cadastradas no servidor.", true);
                return;
            }

            // Professor de uma sala: abre direto nela. Acesso geral: começa vendo todas.
            salaSelecionadaId = salas.Count == 1 ? salas[0].turmaId : TODAS;
            viewMensagem.gameObject.SetActive(false);
            AtualizarCabecalho();
            RenderChips();
            RenderAbaAtual();
        });
    }

    private bool ProfessorRestrito =>
        !string.IsNullOrEmpty(AreaDoProfessor.TurmaRestritaNome) && AreaDoProfessor.TurmaRestritaNome != "Todas as Salas";

    /// <summary>Salas que o professor logado pode ver.</summary>
    private List<ServicoNuvem.DtoSalaResumo> SalasPermitidas()
    {
        List<ServicoNuvem.DtoSalaResumo> todas = dados?.salas ?? new List<ServicoNuvem.DtoSalaResumo>();
        if (!ProfessorRestrito) return todas;

        return todas.Where(s =>
            (!string.IsNullOrEmpty(AreaDoProfessor.TurmaRestritaId) && s.turmaId == AreaDoProfessor.TurmaRestritaId) ||
            string.Equals(s.nomeTurma?.Trim(), AreaDoProfessor.TurmaRestritaNome.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
    }

    /// <summary>Salas exibidas agora (uma, ou todas as permitidas).</summary>
    private List<ServicoNuvem.DtoSalaResumo> SalasExibidas()
    {
        List<ServicoNuvem.DtoSalaResumo> permitidas = SalasPermitidas();
        if (salaSelecionadaId == TODAS) return permitidas;
        List<ServicoNuvem.DtoSalaResumo> uma = permitidas.Where(s => s.turmaId == salaSelecionadaId).ToList();
        return uma.Count > 0 ? uma : permitidas;
    }

    private List<AlunoComSala> AlunosExibidos()
    {
        List<AlunoComSala> lista = new List<AlunoComSala>();
        foreach (var sala in SalasExibidas())
        {
            if (sala.alunos == null) continue;
            foreach (var a in sala.alunos) lista.Add(new AlunoComSala { aluno = a, sala = sala });
        }
        return lista;
    }

    // =====================================================================
    // CONSTRUÇÃO DA JANELA
    // =====================================================================
    private void Construir()
    {
        spriteRedondo = GerarSprite(64, 18, true);
        spriteRedondoPequeno = GerarSprite(32, 6, true);
        spriteCirculo = GerarSprite(128, 64, false);

        // Fundo escuro que bloqueia o jogo
        RectTransform rtRaiz = NovoRect("PainelProfessorNuvem", transform);
        Esticar(rtRaiz);
        Image overlay = rtRaiz.gameObject.AddComponent<Image>();
        overlay.color = COR_OVERLAY;
        raiz = rtRaiz.gameObject;

        // Janela
        Image janela = Caixa(rtRaiz, "Janela", COR_JANELA);
        RectTransform rtJanela = janela.rectTransform;
        rtJanela.anchorMin = rtJanela.anchorMax = new Vector2(0.5f, 0.5f);
        // Um pouco abaixo do topo para não ficar sob os botões de minimizar/fechar do jogo
        rtJanela.sizeDelta = new Vector2(1780f, 950f);
        rtJanela.anchoredPosition = new Vector2(0f, -40f);

        ConstruirCabecalho(rtJanela);
        ConstruirBarraFerramentas(rtJanela);

        // Área de conteúdo
        RectTransform conteudo = NovoRect("Conteudo", rtJanela);
        Esticar(conteudo, 28, 24, 28, 214);

        viewSala = NovoRect("VisaoSala", conteudo);
        Esticar(viewSala);
        viewAlunos = NovoRect("VisaoAlunos", conteudo);
        Esticar(viewAlunos);
        viewHabilidades = NovoRect("VisaoHabilidades", conteudo);
        Esticar(viewHabilidades);
        ConstruirEstruturaAlunos();

        // Mensagens (carregando / erro)
        viewMensagem = NovoRect("Mensagem", conteudo);
        Esticar(viewMensagem);
        Image fundoMsg = viewMensagem.gameObject.AddComponent<Image>();
        fundoMsg.color = COR_JANELA;
        txtMensagem = Texto(viewMensagem, "", 26, COR_TEXTO, TextAlignmentOptions.Center);
        Posicionar(txtMensagem.rectTransform, new Vector2(0.1f, 0.45f), new Vector2(0.9f, 0.8f), Vector2.zero, Vector2.zero);
        Button btnTentar = Botao(viewMensagem, "BtnTentarNovamente", "Tentar novamente", COR_PRIMARIA, Color.white, 22, Recarregar);
        RectTransform rtTentar = (RectTransform)btnTentar.transform;
        rtTentar.anchorMin = rtTentar.anchorMax = new Vector2(0.5f, 0.35f);
        rtTentar.sizeDelta = new Vector2(300f, 60f);
        btnTentarNovamente = btnTentar.gameObject;

        raiz.SetActive(false);
    }

    private void ConstruirCabecalho(RectTransform janela)
    {
        Image cab = Caixa(janela, "Cabecalho", COR_PRIMARIA);
        Posicionar(cab.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -124), Vector2.zero);

        TextMeshProUGUI titulo = Texto(cab.transform, "Área do Professor", 42, Color.white, TextAlignmentOptions.BottomLeft, true);
        titulo.font = fonteTitulo;
        Posicionar(titulo.rectTransform, new Vector2(0, 0.5f), new Vector2(0.62f, 1), new Vector2(40, -2), new Vector2(0, -14));

        txtSubtitulo = Texto(cab.transform, "", 20, new Color(1, 1, 1, 0.88f), TextAlignmentOptions.TopLeft);
        Posicionar(txtSubtitulo.rectTransform, new Vector2(0, 0), new Vector2(0.62f, 0.5f), new Vector2(42, 10), new Vector2(0, -4));
        txtSubtitulo.textWrappingMode = TextWrappingModes.NoWrap;
        txtSubtitulo.overflowMode = TextOverflowModes.Ellipsis;

        // Botões à direita
        RectTransform botoes = NovoRect("Botoes", cab.transform);
        Posicionar(botoes, new Vector2(0.55f, 0), new Vector2(1, 1), new Vector2(0, 30), new Vector2(-28, -30));
        HorizontalLayoutGroup hl = botoes.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment = TextAnchor.MiddleRight;
        hl.spacing = 14;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = true;

        AdicionarLargura(Botao(botoes, "BtnAtualizar", "Atualizar", COR_PRIMARIA_ESCURA, Color.white, 20, Recarregar).gameObject, 170);
        // A senha da coordenação agora é definida no painel de administração (servidor), não mais por computador.
        AdicionarLargura(Botao(botoes, "BtnSair", "Sair da sessão", Hex("DC2626"), Color.white, 20, () =>
        {
            if (area != null) area.EncerrarSessaoProfessor();
        }).gameObject, 210);
        AdicionarLargura(Botao(botoes, "BtnFechar", "X", Color.white, COR_PRIMARIA, 26, () =>
        {
            if (area != null) area.FecharAreaDoProfessor();
            else Fechar();
        }).gameObject, 64);
    }

    private void ConstruirBarraFerramentas(RectTransform janela)
    {
        RectTransform barra = NovoRect("BarraFerramentas", janela);
        Posicionar(barra, new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -200), new Vector2(-28, -140));

        Button abaSala = Botao(barra, "AbaSala", "Visão da sala", COR_PRIMARIA, Color.white, 22, () => TrocarAba(Aba.Sala));
        Posicionar((RectTransform)abaSala.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(230, 0));
        imgAbaSala = abaSala.GetComponent<Image>();
        txtAbaSala = abaSala.GetComponentInChildren<TextMeshProUGUI>();

        Button abaAlunos = Botao(barra, "AbaAlunos", "Alunos", COR_CARD, COR_PRIMARIA, 22, () => TrocarAba(Aba.Alunos));
        Posicionar((RectTransform)abaAlunos.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(246, 0), new Vector2(476, 0));
        imgAbaAlunos = abaAlunos.GetComponent<Image>();
        txtAbaAlunos = abaAlunos.GetComponentInChildren<TextMeshProUGUI>();

        Button abaHab = Botao(barra, "AbaHabilidades", "Habilidades BNCC", COR_CARD, COR_PRIMARIA, 22, () => TrocarAba(Aba.Habilidades));
        Posicionar((RectTransform)abaHab.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(492, 0), new Vector2(752, 0));
        imgAbaHab = abaHab.GetComponent<Image>();
        txtAbaHab = abaHab.GetComponentInChildren<TextMeshProUGUI>();

        // Seletor de salas (acesso geral) ou nome da sala (professor da sala)
        areaChips = NovoRect("SeletorSalas", barra);
        Posicionar(areaChips, new Vector2(0, 0), new Vector2(1, 1), new Vector2(770, 0), Vector2.zero);
        ScrollRect sr = areaChips.gameObject.AddComponent<ScrollRect>();
        sr.vertical = false;
        sr.horizontal = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30;
        RectTransform vp = NovoRect("Viewport", areaChips);
        Esticar(vp);
        vp.gameObject.AddComponent<RectMask2D>();
        Image vpImg = vp.gameObject.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0);
        RectTransform content = NovoRect("Chips", vp);
        content.anchorMin = new Vector2(0, 0);
        content.anchorMax = new Vector2(0, 1);
        content.pivot = new Vector2(0, 0.5f);
        content.sizeDelta = Vector2.zero;
        HorizontalLayoutGroup hl = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 10;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;
        hl.padding = new RectOffset(0, 0, 6, 6);
        ContentSizeFitter csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.viewport = vp;
        sr.content = content;
        chipsContent = content;

        txtSalaFixa = Texto(barra, "", 24, COR_TITULO, TextAlignmentOptions.MidlineRight, true);
        Posicionar(txtSalaFixa.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(770, 0), Vector2.zero);
    }

    private void ConstruirEstruturaAlunos()
    {
        // Coluna da esquerda: busca + lista
        Image colLista = Caixa(viewAlunos, "ColunaLista", COR_CARD);
        Posicionar(colLista.rectTransform, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(440, 0));

        TextMeshProUGUI tituloLista = Texto(colLista.transform, "Alunos", 24, COR_TITULO, TextAlignmentOptions.MidlineLeft, true);
        Posicionar(tituloLista.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -56), new Vector2(-22, -12));

        inputBusca = CriarCampoBusca(colLista.transform);
        Posicionar((RectTransform)inputBusca.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -120), new Vector2(-18, -64));

        RectTransform rtScroll = NovoRect("ScrollAlunos", colLista.transform);
        Posicionar(rtScroll, new Vector2(0, 0), new Vector2(1, 1), new Vector2(12, 12), new Vector2(-12, -132));
        listaAlunosContent = CriarScrollVertical(rtScroll, 8);

        // Detalhe à direita
        detalheAluno = NovoRect("DetalheAluno", viewAlunos);
        Posicionar(detalheAluno, new Vector2(0, 0), new Vector2(1, 1), new Vector2(460, 0), Vector2.zero);
    }

    // =====================================================================
    // CABEÇALHO, ABAS E SELETOR DE SALAS
    // =====================================================================
    private void AtualizarCabecalho()
    {
        if (txtSubtitulo == null) return;
        ServicoNuvem sn = ServicoNuvem.Instancia;
        string escola = dados != null && !string.IsNullOrEmpty(dados.nomeEscola)
            ? $"{dados.nomeEscola} ({dados.municipio})"
            : (sn != null && !string.IsNullOrEmpty(sn.nomeEscolaConectada) ? $"{sn.nomeEscolaConectada} ({sn.municipioConectado})" : "Escola");
        string escopo = ProfessorRestrito ? $"Sala {AreaDoProfessor.TurmaRestritaNome}" : "Todas as salas";
        string aviso = "";
        if (sn != null && sn.temExpiracaoContrato && sn.diasRestantesContrato <= 30)
        {
            aviso = sn.diasRestantesContrato <= 0
                ? "  ·  <color=#FDE68A><b>Contrato vence hoje</b></color>"
                : $"  ·  <color=#FDE68A><b>Contrato vence em {sn.diasRestantesContrato} dia(s)</b></color>";
        }
        txtSubtitulo.text = $"{escola}  ·  {escopo}  ·  {AreaDoProfessor.NomeProfessorLogado}{aviso}";
    }

    private void TrocarAba(Aba nova)
    {
        abaAtual = nova;
        AtualizarAbas();
        if (!carregando && dados != null) RenderAbaAtual();
    }

    private void AtualizarAbas()
    {
        bool sala = abaAtual == Aba.Sala, alunos = abaAtual == Aba.Alunos, hab = abaAtual == Aba.Habilidades;
        imgAbaSala.color = sala ? COR_PRIMARIA : COR_CARD;
        txtAbaSala.color = sala ? Color.white : COR_PRIMARIA;
        imgAbaAlunos.color = alunos ? COR_PRIMARIA : COR_CARD;
        txtAbaAlunos.color = alunos ? Color.white : COR_PRIMARIA;
        imgAbaHab.color = hab ? COR_PRIMARIA : COR_CARD;
        txtAbaHab.color = hab ? Color.white : COR_PRIMARIA;
        viewSala.gameObject.SetActive(sala);
        viewAlunos.gameObject.SetActive(alunos);
        viewHabilidades.gameObject.SetActive(hab);
    }

    private void RenderChips()
    {
        Limpar(chipsContent);
        List<ServicoNuvem.DtoSalaResumo> salas = SalasPermitidas();
        bool mostrarChips = !ProfessorRestrito && salas.Count > 1;
        areaChips.gameObject.SetActive(mostrarChips);
        txtSalaFixa.gameObject.SetActive(!mostrarChips);

        if (!mostrarChips)
        {
            txtSalaFixa.text = salas.Count == 1 ? $"Sala: {salas[0].nomeTurma}" : "";
            return;
        }

        CriarChip("Todas as salas", TODAS);
        foreach (var s in salas) CriarChip(s.nomeTurma, s.turmaId);
    }

    private void CriarChip(string rotulo, string id)
    {
        bool ativo = salaSelecionadaId == id;
        Button chip = Botao(chipsContent, "Chip_" + rotulo, rotulo, ativo ? COR_TITULO : COR_CARD, ativo ? Color.white : COR_TEXTO, 19, () =>
        {
            salaSelecionadaId = id;
            alunoSelecionadoId = "";
            RenderChips();
            RenderAbaAtual();
        });
        TextMeshProUGUI t = chip.GetComponentInChildren<TextMeshProUGUI>();
        float largura = t.GetPreferredValues(rotulo).x + 44f;
        LayoutElement le = chip.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = Mathf.Max(110f, largura);
        le.preferredHeight = 48f;
    }

    private void RenderAbaAtual()
    {
        if (abaAtual == Aba.Sala) RenderVisaoSala();
        else if (abaAtual == Aba.Alunos) RenderVisaoAlunos();
        else RenderHabilidades();
    }

    private void MostrarMensagem(string msg, bool erro)
    {
        if (viewMensagem == null) return;
        viewMensagem.gameObject.SetActive(true);
        viewMensagem.SetAsLastSibling();
        txtMensagem.text = msg;
        txtMensagem.color = erro ? Hex("B91C1C") : COR_TEXTO;
        btnTentarNovamente.SetActive(erro);
    }

    // =====================================================================
    // ABA 3: HABILIDADES DA BNCC
    // =====================================================================
    private int pedidoHabilidades;

    private void RenderHabilidades()
    {
        Limpar(viewHabilidades);
        ServicoNuvem sn = ServicoNuvem.Instancia;
        if (sn == null) return;

        // Uma sala (escolhida ou a do professor) ou a escola toda (coordenação em "Todas as salas")
        List<ServicoNuvem.DtoSalaResumo> exibidas = SalasExibidas();
        string turmaId = (!ProfessorRestrito && salaSelecionadaId == TODAS) ? "" : (exibidas.Count > 0 ? exibidas[0].turmaId : "");
        string escopo = string.IsNullOrEmpty(turmaId) ? "toda a escola" : $"sala {exibidas[0].nomeTurma}";

        TextMeshProUGUI titulo = Texto(viewHabilidades, $"Habilidades da BNCC — {escopo}", 26, COR_TITULO, TextAlignmentOptions.MidlineLeft, true);
        Posicionar(titulo.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(4, -44), new Vector2(-4, 0));
        TextMeshProUGUI legenda = Texto(viewHabilidades,
            "Percentual de acerto em cada habilidade (todas as respostas, inclusive de fases não concluídas). " +
            "<color=#B91C1C>Precisam de atenção</color>: 4 respostas ou mais e menos de 60% de acerto.", 17, COR_SUAVE, TextAlignmentOptions.TopLeft);
        Posicionar(legenda.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(4, -94), new Vector2(-4, -48));

        RectTransform rtScroll = NovoRect("ScrollHabilidades", viewHabilidades);
        Posicionar(rtScroll, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, new Vector2(0, -100));
        Transform lista = CriarScrollVertical(rtScroll, 10);
        TextMeshProUGUI carregando = Texto(lista, "Carregando...", 22, COR_SUAVE, TextAlignmentOptions.Center);
        carregando.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;

        int pedido = ++pedidoHabilidades;
        sn.BuscarRelatorioHabilidades(turmaId, dados =>
        {
            if (raiz == null || !raiz.activeSelf || abaAtual != Aba.Habilidades || pedido != pedidoHabilidades) return;
            Limpar(lista);
            if (dados == null)
            {
                TextMeshProUGUI erro = Texto(lista, "Não foi possível carregar o relatório. Verifique a internet.", 22, COR_ERRO, TextAlignmentOptions.Center);
                erro.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
                return;
            }
            if (dados.habilidades == null || dados.habilidades.Count == 0)
            {
                TextMeshProUGUI vazio = Texto(lista, "Ainda não há respostas registradas. Os dados aparecem conforme os alunos jogam.", 22, COR_SUAVE, TextAlignmentOptions.Center);
                vazio.gameObject.AddComponent<LayoutElement>().preferredHeight = 80;
                return;
            }
            foreach (var h in dados.habilidades) LinhaHabilidade(lista, h);
        });
    }

    private void LinhaHabilidade(Transform lista, ServicoNuvem.DtoHabilidadeRelatorio h)
    {
        Image card = Caixa(lista, "Hab_" + h.codigo, COR_CARD);
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = 118;
        Color corComp = h.componente == "Matemática" ? COR_MATH : COR_PORT;
        Color corTaxa = h.taxa >= 80 ? Hex("16A34A") : h.taxa >= 60 ? Hex("CA8A04") : Hex("DC2626");

        Image faixa = Caixa(card.transform, "Faixa", corComp, false);
        Posicionar(faixa.rectTransform, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(8, 0));

        TextMeshProUGUI cod = Texto(card.transform, $"<b>{h.codigo}</b>\n<size=15><color=#64748B>{h.componente}\n{h.ano} ano</color></size>", 22, COR_TITULO, TextAlignmentOptions.MidlineLeft);
        Posicionar(cod.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(24, 8), new Vector2(200, -8));

        TextMeshProUGUI desc = Texto(card.transform, $"<b>{h.resumo}</b>\n<size=15><color=#64748B>{h.descricao}</color></size>", 19, COR_TEXTO, TextAlignmentOptions.MidlineLeft);
        desc.enableAutoSizing = true;
        desc.fontSizeMin = 12;
        desc.fontSizeMax = 19;
        Posicionar(desc.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(210, 8), new Vector2(-640, -8));

        Image barra = Caixa(card.transform, "Barra", COR_VAZIO, true, spriteRedondoPequeno);
        Posicionar(barra.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-620, 2), new Vector2(-400, 18));
        Image preench = Caixa(barra.transform, "Preenchimento", corTaxa, true, spriteRedondoPequeno);
        Posicionar(preench.rectTransform, Vector2.zero, new Vector2(Mathf.Clamp01(h.taxa / 100f), 1), Vector2.zero, Vector2.zero);
        TextMeshProUGUI taxa = Texto(card.transform, $"<b>{h.taxa}%</b> <size=15><color=#64748B>({h.acertos} de {h.respostas} · {h.alunosAvaliados} aluno(s))</color></size>", 22, corTaxa, TextAlignmentOptions.MidlineLeft);
        Posicionar(taxa.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-620, -44), new Vector2(-380, -2));

        string nomes = h.alunosAtencao != null && h.alunosAtencao.Count > 0 ? string.Join(", ", h.alunosAtencao) : "<color=#16A34A>ninguém</color>";
        TextMeshProUGUI atencao = Texto(card.transform, $"<size=15><color=#B91C1C><b>Precisam de atenção:</b></color></size>\n{nomes}", 17, COR_TEXTO, TextAlignmentOptions.MidlineLeft);
        atencao.enableAutoSizing = true;
        atencao.fontSizeMin = 11;
        atencao.fontSizeMax = 17;
        Posicionar(atencao.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-370, 8), new Vector2(-16, -8));
    }

    // =====================================================================
    // ABA 1: VISÃO DA SALA
    // =====================================================================
    private void RenderVisaoSala()
    {
        Limpar(viewSala);
        List<ServicoNuvem.DtoSalaResumo> salas = SalasExibidas();
        List<AlunoComSala> alunos = AlunosExibidos();
        bool variasSalas = salas.Count > 1;

        int totalAlunos = alunos.Count;
        int ativos = alunos.Count(a => a.aluno.totalFases > 0);
        int fases = alunos.Sum(a => a.aluno.totalFases);
        int fasesPort = alunos.Sum(a => a.aluno.fasesPort);
        int fasesMath = alunos.Sum(a => a.aluno.fasesMath);
        int erros = alunos.Sum(a => a.aluno.errosTotais);
        float mediaEstrelas = ativos > 0 ? alunos.Where(a => a.aluno.totalFases > 0).Average(a => a.aluno.mediaEstrelas) : 0f;
        int acerto = Acerto(fases, erros);

        // ---- Linha 1: indicadores
        RectTransform linhaKpi = NovoRect("Indicadores", viewSala);
        Posicionar(linhaKpi, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -140), Vector2.zero);
        LayoutHorizontal(linhaKpi, 18);
        CardIndicador(linhaKpi, "Alunos", totalAlunos.ToString(), $"{ativos} já jogaram", COR_PRIMARIA);
        CardIndicador(linhaKpi, "Fases concluídas", fases.ToString(), "somando todos os alunos", COR_ROXO);
        CardIndicador(linhaKpi, "Média por aluno", totalAlunos > 0 ? ((float)fases / totalAlunos).ToString("0.0") : "0", "fases concluídas", COR_PORT);
        CardIndicador(linhaKpi, "Média de estrelas", mediaEstrelas.ToString("0.0"), "de 3 estrelas por fase", COR_ESTRELA);
        CardIndicador(linhaKpi, "Taxa de acerto", fases > 0 ? acerto + "%" : "-", fases > 0 ? $"{erros} erro(s) em {fases} fases" : "sem fases ainda", COR_MATH);

        // ---- Linha 2: matérias + situação
        RectTransform linha2 = NovoRect("Materias", viewSala);
        Posicionar(linha2, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -318), new Vector2(0, -158));
        LayoutHorizontal(linha2, 18);
        CardProgressoMateria(linha2, "Português", fasesPort, totalAlunos * FASES_POR_MATERIA, COR_PORT);
        CardProgressoMateria(linha2, "Matemática", fasesMath, totalAlunos * FASES_POR_MATERIA, COR_MATH);
        CardSituacao(linha2, alunos);

        // ---- Linha 3: ranking/salas + atenção
        RectTransform linha3 = NovoRect("Detalhes", viewSala);
        Posicionar(linha3, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, new Vector2(0, -336));
        LayoutHorizontal(linha3, 18);

        if (variasSalas) CardComparativoSalas(linha3, salas, 1.7f);
        else CardRanking(linha3, alunos, 1.7f);
        CardAtencao(linha3, alunos, 1f);
    }

    private void CardIndicador(Transform pai, string rotulo, string valor, string detalhe, Color cor)
    {
        Image card = Caixa(pai, "Kpi_" + rotulo, COR_CARD);
        Flex(card.gameObject, 1);

        Image barra = Caixa(card.transform, "Destaque", cor, true, spriteRedondoPequeno);
        Posicionar(barra.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 14), new Vector2(8, -14));

        TextMeshProUGUI tRot = Texto(card.transform, rotulo, 19, COR_SUAVE);
        Posicionar(tRot.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -44), new Vector2(-12, -12));
        TextMeshProUGUI tVal = Texto(card.transform, valor, 46, COR_TITULO, TextAlignmentOptions.MidlineLeft, true);
        Posicionar(tVal.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(28, 34), new Vector2(-12, -40));
        TextMeshProUGUI tDet = Texto(card.transform, detalhe, 16, COR_SUAVE);
        Posicionar(tDet.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(28, 10), new Vector2(-12, 38));
    }

    private void CardProgressoMateria(Transform pai, string materia, int feitas, int possiveis, Color cor)
    {
        Image card = Caixa(pai, "Materia_" + materia, COR_CARD);
        Flex(card.gameObject, 1);
        float pct = possiveis > 0 ? Mathf.Clamp01((float)feitas / possiveis) : 0f;

        TextMeshProUGUI t = Texto(card.transform, materia, 24, COR_TITULO, TextAlignmentOptions.MidlineLeft, true);
        Posicionar(t.rectTransform, new Vector2(0, 1), new Vector2(0.6f, 1), new Vector2(26, -58), new Vector2(0, -16));
        TextMeshProUGUI p = Texto(card.transform, Mathf.RoundToInt(pct * 100) + "%", 34, cor, TextAlignmentOptions.MidlineRight, true);
        Posicionar(p.rectTransform, new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(-26, -12));

        RectTransform rtBarra = NovoRect("Barra", card.transform);
        Posicionar(rtBarra, new Vector2(0, 0), new Vector2(1, 0), new Vector2(26, 56), new Vector2(-26, 78));
        BarraProgresso(rtBarra, pct, cor);

        TextMeshProUGUI d = Texto(card.transform, possiveis > 0 ? $"{feitas} de {possiveis} fases concluídas pela turma" : "Nenhum aluno nesta seleção", 17, COR_SUAVE);
        Posicionar(d.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(26, 14), new Vector2(-26, 46));
    }

    private void CardSituacao(Transform pai, List<AlunoComSala> alunos)
    {
        Image card = Caixa(pai, "Situacao", COR_CARD);
        Flex(card.gameObject, 1.25f);
        TextMeshProUGUI t = Texto(card.transform, "Situação dos alunos", 24, COR_TITULO, TextAlignmentOptions.MidlineLeft, true);
        Posicionar(t.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -58), new Vector2(-26, -16));

        string[] status = { "Excelente", "Bom", "Em Progresso", "Precisa de Apoio", "Sem Atividade" };
        RectTransform grade = NovoRect("Pilulas", card.transform);
        Posicionar(grade, new Vector2(0, 0), new Vector2(1, 1), new Vector2(22, 14), new Vector2(-22, -62));
        GridLayoutGroup gl = grade.gameObject.AddComponent<GridLayoutGroup>();
        gl.cellSize = new Vector2(186, 34);
        gl.spacing = new Vector2(8, 8);
        foreach (var s in status)
        {
            int qtd = alunos.Count(a => a.aluno.statusDesempenho == s);
            Image pilula = Caixa(grade, "Status_" + s, CorStatusFundo(s));
            TextMeshProUGUI tp = Texto(pilula.transform, $"{s}: <b>{qtd}</b>", 18, CorStatus(s), TextAlignmentOptions.Center);
            Esticar(tp.rectTransform, 8, 0, 8, 0);
        }
    }

    private void CardRanking(Transform pai, List<AlunoComSala> alunos, float flex)
    {
        Image card = Caixa(pai, "Ranking", COR_CARD);
        Flex(card.gameObject, flex);
        TextMeshProUGUI t = Texto(card.transform, "Desempenho de cada aluno  <size=17><color=#64748B>(clique para ver a ficha)</color></size>", 24, COR_TITULO, TextAlignmentOptions.MidlineLeft, true);
        Posicionar(t.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -58), new Vector2(-26, -14));

        float[] col = { 0.06f, 0.36f, 0.47f, 0.58f, 0.68f, 0.79f, 1f };
        string[] titulos = { "#", "Aluno", "Port.", "Mat.", "Total", "Acerto", "Situação" };
        RectTransform cab = NovoRect("CabecalhoTabela", card.transform);
        Posicionar(cab, new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -96), new Vector2(-22, -62));
        CelulasLinha(cab, col, titulos, 17, COR_SUAVE, true);

        RectTransform rtScroll = NovoRect("ScrollRanking", card.transform);
        Posicionar(rtScroll, new Vector2(0, 0), new Vector2(1, 1), new Vector2(14, 12), new Vector2(-14, -100));
        Transform content = CriarScrollVertical(rtScroll, 6);

        if (alunos.Count == 0)
        {
            AdicionarTextoLista(content, "Nenhum aluno cadastrado nesta sala ainda.");
            return;
        }

        int pos = 1;
        foreach (var a in alunos.OrderByDescending(x => x.aluno.totalFases).ThenByDescending(x => x.aluno.mediaEstrelas).ThenBy(x => x.aluno.nomeJogo))
        {
            AlunoComSala capt = a;
            Button linha = Botao(content, "Linha_" + a.aluno.nomeJogo, "", pos % 2 == 0 ? COR_LINHA : COR_CARD, COR_TEXTO, 18, () => AbrirFichaAluno(capt.aluno.id));
            Destroy(linha.GetComponentInChildren<TextMeshProUGUI>().gameObject);
            LayoutElement le = linha.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            RectTransform rtLinha = NovoRect("Celulas", linha.transform);
            Esticar(rtLinha, 8, 0, 8, 0);
            string[] valores =
            {
                pos.ToString(),
                $"<b>{a.aluno.nomeJogo}</b>",
                a.aluno.fasesPort.ToString(),
                a.aluno.fasesMath.ToString(),
                a.aluno.totalFases.ToString(),
                a.aluno.totalFases > 0 ? Acerto(a.aluno.totalFases, a.aluno.errosTotais) + "%" : "-",
                ""
            };
            CelulasLinha(rtLinha, col, valores, 19, COR_TEXTO, false);
            PilulaStatus(rtLinha, a.aluno.statusDesempenho, new Vector2(col[5], 0.18f), new Vector2(col[6], 0.82f), 16);
            pos++;
        }
    }

    private void CardComparativoSalas(Transform pai, List<ServicoNuvem.DtoSalaResumo> salas, float flex)
    {
        Image card = Caixa(pai, "ComparativoSalas", COR_CARD);
        Flex(card.gameObject, flex);
        TextMeshProUGUI t = Texto(card.transform, "Comparativo das salas  <size=17><color=#64748B>(clique para abrir a sala)</color></size>", 24, COR_TITULO, TextAlignmentOptions.MidlineLeft, true);
        Posicionar(t.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -58), new Vector2(-26, -14));

        float[] col = { 0.24f, 0.47f, 0.57f, 0.69f, 0.81f, 0.9f, 1f };
        string[] titulos = { "Sala", "Professor(a)", "Alunos", "Português", "Matemática", "Estrelas", "Fases" };
        RectTransform cab = NovoRect("CabecalhoTabela", card.transform);
        Posicionar(cab, new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -96), new Vector2(-22, -62));
        CelulasLinha(cab, col, titulos, 17, COR_SUAVE, true);

        RectTransform rtScroll = NovoRect("ScrollSalas", card.transform);
        Posicionar(rtScroll, new Vector2(0, 0), new Vector2(1, 1), new Vector2(14, 12), new Vector2(-14, -100));
        Transform content = CriarScrollVertical(rtScroll, 6);

        int i = 0;
        foreach (var s in salas.OrderBy(x => x.nomeTurma))
        {
            string id = s.turmaId;
            Button linha = Botao(content, "Sala_" + s.nomeTurma, "", i % 2 == 0 ? COR_CARD : COR_LINHA, COR_TEXTO, 18, () =>
            {
                salaSelecionadaId = id;
                RenderChips();
                RenderAbaAtual();
            });
            Destroy(linha.GetComponentInChildren<TextMeshProUGUI>().gameObject);
            LayoutElement le = linha.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 50;
            RectTransform rtLinha = NovoRect("Celulas", linha.transform);
            Esticar(rtLinha, 8, 0, 8, 0);
            string[] valores =
            {
                $"<b>{s.nomeTurma}</b>",
                string.IsNullOrEmpty(s.nomeProfessor) ? "-" : s.nomeProfessor,
                s.totalAlunos.ToString(),
                Mathf.RoundToInt(s.taxaConclusaoPort) + "%",
                Mathf.RoundToInt(s.taxaConclusaoMath) + "%",
                s.mediaEstrelas.ToString("0.0"),
                s.totalFasesConcluidas.ToString()
            };
            CelulasLinha(rtLinha, col, valores, 19, COR_TEXTO, false);
            i++;
        }
    }

    private void CardAtencao(Transform pai, List<AlunoComSala> alunos, float flex)
    {
        Image card = Caixa(pai, "Atencao", COR_CARD);
        Flex(card.gameObject, flex);
        TextMeshProUGUI t = Texto(card.transform, "Precisam de atenção", 24, COR_TITULO, TextAlignmentOptions.MidlineLeft, true);
        Posicionar(t.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -58), new Vector2(-26, -14));

        RectTransform rtScroll = NovoRect("ScrollAtencao", card.transform);
        Posicionar(rtScroll, new Vector2(0, 0), new Vector2(1, 1), new Vector2(14, 12), new Vector2(-14, -66));
        Transform content = CriarScrollVertical(rtScroll, 8);

        var lista = new List<KeyValuePair<AlunoComSala, string>>();
        foreach (var a in alunos)
        {
            string motivo = MotivoAtencao(a.aluno);
            if (motivo != null) lista.Add(new KeyValuePair<AlunoComSala, string>(a, motivo));
        }

        if (lista.Count == 0)
        {
            AdicionarTextoLista(content, alunos.Count == 0 ? "Nenhum aluno nesta seleção." : "Tudo certo! Nenhum aluno com dificuldade ou parado.");
            return;
        }

        foreach (var par in lista.OrderBy(p => p.Key.aluno.statusDesempenho == "Precisa de Apoio" ? 0 : 1))
        {
            AlunoComSala a = par.Key;
            Button item = Botao(content, "Atencao_" + a.aluno.nomeJogo, "", COR_LINHA, COR_TEXTO, 18, () => AbrirFichaAluno(a.aluno.id));
            Destroy(item.GetComponentInChildren<TextMeshProUGUI>().gameObject);
            LayoutElement le = item.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 76;

            Image marca = Caixa(item.transform, "Marca", CorStatus(a.aluno.statusDesempenho), true, spriteRedondoPequeno);
            Posicionar(marca.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 10), new Vector2(6, -10));
            string salaTxt = SalasExibidas().Count > 1 ? $"  <size=16><color=#64748B>({a.sala.nomeTurma})</color></size>" : "";
            TextMeshProUGUI nome = Texto(item.transform, $"<b>{a.aluno.nomeJogo}</b>{salaTxt}", 20, COR_TITULO);
            Posicionar(nome.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(20, 0), new Vector2(-12, -6));
            TextMeshProUGUI motivo = Texto(item.transform, par.Value, 16, COR_SUAVE);
            Posicionar(motivo.rectTransform, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(20, 6), new Vector2(-12, 0));
        }
    }

    private static string MotivoAtencao(ServicoNuvem.DtoAlunoSalaDetalhe a)
    {
        if (a.totalFases == 0) return "Ainda não concluiu nenhuma fase.";
        int dias = DiasDesde(a.ultimaAtividade);
        if (a.statusDesempenho == "Precisa de Apoio")
            return $"Erra com frequência: {Acerto(a.totalFases, a.errosTotais)}% de acerto.";
        if (dias >= 14) return $"Está há {dias} dias sem jogar.";
        return null;
    }

    // =====================================================================
    // ABA 2: ALUNOS (LISTA + FICHA INDIVIDUAL)
    // =====================================================================
    private void AbrirFichaAluno(string alunoId)
    {
        alunoSelecionadoId = alunoId;
        abaAtual = Aba.Alunos;
        AtualizarAbas();
        RenderVisaoAlunos();
    }

    private void RenderVisaoAlunos()
    {
        List<AlunoComSala> alunos = AlunosExibidos();
        if (alunos.Count > 0 && !alunos.Any(a => a.aluno.id == alunoSelecionadoId))
        {
            alunoSelecionadoId = alunos.OrderBy(a => a.aluno.nomeJogo).First().aluno.id;
        }
        RenderListaAlunos();
        RenderFicha();
    }

    private void RenderListaAlunos()
    {
        Limpar(listaAlunosContent);
        List<AlunoComSala> alunos = AlunosExibidos()
            .Where(a => string.IsNullOrEmpty(filtroBusca) ||
                        (a.aluno.nomeJogo ?? "").IndexOf(filtroBusca, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (a.aluno.nomeCompleto ?? "").IndexOf(filtroBusca, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(a => a.aluno.nomeJogo)
            .ToList();

        if (alunos.Count == 0)
        {
            AdicionarTextoLista(listaAlunosContent, string.IsNullOrEmpty(filtroBusca) ? "Nenhum aluno nesta sala." : "Nenhum aluno encontrado.");
            return;
        }

        bool variasSalas = SalasExibidas().Count > 1;
        foreach (var a in alunos)
        {
            AlunoComSala capt = a;
            bool selecionado = a.aluno.id == alunoSelecionadoId;
            Button item = Botao(listaAlunosContent, "Aluno_" + a.aluno.nomeJogo, "", selecionado ? COR_LINHA_HOVER : COR_LINHA, COR_TEXTO, 18, () =>
            {
                alunoSelecionadoId = capt.aluno.id;
                RenderListaAlunos();
                RenderFicha();
            });
            Destroy(item.GetComponentInChildren<TextMeshProUGUI>().gameObject);
            LayoutElement le = item.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 76;

            if (selecionado)
            {
                Image marca = Caixa(item.transform, "Selecionado", COR_PRIMARIA, true, spriteRedondoPequeno);
                Posicionar(marca.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 10), new Vector2(6, -10));
            }

            Avatar(item.transform, a.aluno.nomeJogo, CorStatus(a.aluno.statusDesempenho), 48, new Vector2(16, 0));
            TextMeshProUGUI nome = Texto(item.transform, $"<b>{a.aluno.nomeJogo}</b>", 20, COR_TITULO);
            Posicionar(nome.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(78, 0), new Vector2(-10, -8));
            nome.textWrappingMode = TextWrappingModes.NoWrap;
            nome.overflowMode = TextOverflowModes.Ellipsis;
            string sub = variasSalas ? $"{a.sala.nomeTurma} · {a.aluno.totalFases} fases" : $"{a.aluno.totalFases} fases · {a.aluno.statusDesempenho}";
            TextMeshProUGUI tSub = Texto(item.transform, sub, 16, COR_SUAVE);
            Posicionar(tSub.rectTransform, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(78, 8), new Vector2(-10, 0));
        }
    }

    private void RenderFicha()
    {
        Limpar(detalheAluno);
        AlunoComSala atual = AlunosExibidos().FirstOrDefault(a => a.aluno.id == alunoSelecionadoId);
        if (atual == null)
        {
            Image vazio = Caixa(detalheAluno, "SemAluno", COR_CARD);
            Esticar(vazio.rectTransform);
            TextMeshProUGUI t = Texto(vazio.transform, "Selecione um aluno na lista para ver o desempenho dele.", 24, COR_SUAVE, TextAlignmentOptions.Center);
            Esticar(t.rectTransform, 40, 0, 40, 0);
            return;
        }

        ServicoNuvem.DtoAlunoSalaDetalhe a = atual.aluno;

        // ---- Cabeçalho da ficha
        Image cab = Caixa(detalheAluno, "CabecalhoFicha", COR_CARD);
        Posicionar(cab.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -124), Vector2.zero);
        Avatar(cab.transform, a.nomeJogo, CorStatus(a.statusDesempenho), 84, new Vector2(24, 0));
        TextMeshProUGUI nome = Texto(cab.transform, a.nomeJogo, 34, COR_TITULO, TextAlignmentOptions.BottomLeft, true);
        Posicionar(nome.rectTransform, new Vector2(0, 0.5f), new Vector2(0.6f, 1), new Vector2(128, 0), new Vector2(0, -16));
        nome.textWrappingMode = TextWrappingModes.NoWrap;
        nome.overflowMode = TextOverflowModes.Ellipsis;
        string completo = !string.IsNullOrEmpty(a.nomeCompleto) && a.nomeCompleto != a.nomeJogo ? $"{a.nomeCompleto} · " : "";
        TextMeshProUGUI sub = Texto(cab.transform, $"{completo}Sala {atual.sala.nomeTurma} · Última atividade: {FormatarData(a.ultimaAtividade)}", 18, COR_SUAVE, TextAlignmentOptions.TopLeft);
        Posicionar(sub.rectTransform, new Vector2(0, 0), new Vector2(0.72f, 0.5f), new Vector2(130, 12), new Vector2(0, -6));
        PilulaStatus(cab.transform, a.statusDesempenho, new Vector2(1, 0.5f), new Vector2(1, 0.5f), 18, new Vector2(-470, -22), new Vector2(-260, 22));
        Button copiar = Botao(cab.transform, "BtnCopiar", "Copiar relatório", COR_PRIMARIA, Color.white, 19, () => CopiarRelatorio(atual));
        Posicionar((RectTransform)copiar.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-240, -28), new Vector2(-24, 28));

        // ---- Indicadores do aluno
        RectTransform kpis = NovoRect("IndicadoresAluno", detalheAluno);
        Posicionar(kpis, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -262), new Vector2(0, -140));
        LayoutHorizontal(kpis, 14);
        CardMini(kpis, "Português", $"{a.fasesPort}<size=22>/{FASES_POR_MATERIA}</size>", COR_PORT);
        CardMini(kpis, "Matemática", $"{a.fasesMath}<size=22>/{FASES_POR_MATERIA}</size>", COR_MATH);
        CardMini(kpis, "Pontuação", a.pontuacaoTotal.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "."), COR_ROXO);
        CardMini(kpis, "Estrelas (média)", a.totalFases > 0 ? a.mediaEstrelas.ToString("0.0") + "<size=22> de 3</size>" : "-", COR_ESTRELA);
        CardMini(kpis, "Acerto", a.totalFases > 0 ? Acerto(a.totalFases, a.errosTotais) + "%" : "-", COR_MATH);
        CardMini(kpis, "Tempo jogado", FormatarTempo(a.tempoTotalSegundos), COR_PRIMARIA);

        // ---- Mapa das fases (Português e Matemática) + parecer
        RectTransform mapas = NovoRect("Mapas", detalheAluno);
        Posicionar(mapas, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 136), new Vector2(0, -280));
        LayoutHorizontal(mapas, 18);
        RectTransform mapaPort = CardMapa(mapas, "Português", COR_PORT);
        RectTransform mapaMath = CardMapa(mapas, "Matemática", COR_MATH);

        Image parecer = Caixa(detalheAluno, "Parecer", COR_CARD);
        Posicionar(parecer.rectTransform, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, 118));
        TextMeshProUGUI tPar = Texto(parecer.transform, "<b>Parecer pedagógico</b>\n" + GerarParecer(atual, null), 19, COR_TEXTO, TextAlignmentOptions.TopLeft);
        Esticar(tPar.rectTransform, 26, 14, 26, 14);

        // Detalhe fase a fase vem do servidor
        if (cacheDetalhe.TryGetValue(a.id, out var detalhe))
        {
            PreencherMapa(mapaPort, detalhe, "Portugues", COR_PORT);
            PreencherMapa(mapaMath, detalhe, "Matematica", COR_MATH);
            tPar.text = "<b>Parecer pedagógico</b>\n" + GerarParecer(atual, detalhe);
            return;
        }

        TextoCarregandoMapa(mapaPort);
        TextoCarregandoMapa(mapaMath);
        string idPedido = a.id;
        ServicoNuvem.Instancia?.BuscarProgressoAluno(idPedido, resp =>
        {
            if (resp != null) cacheDetalhe[idPedido] = resp;
            if (raiz == null || !raiz.activeSelf || abaAtual != Aba.Alunos || alunoSelecionadoId != idPedido) return;
            if (resp == null)
            {
                Limpar(mapaPort.Find("Grade"));
                Limpar(mapaMath.Find("Grade"));
                AdicionarAvisoMapa(mapaPort, "Não foi possível carregar as fases.");
                AdicionarAvisoMapa(mapaMath, "Não foi possível carregar as fases.");
                return;
            }
            RenderFicha();
        });
    }

    private void CardMini(Transform pai, string rotulo, string valor, Color cor)
    {
        Image card = Caixa(pai, "Mini_" + rotulo, COR_CARD);
        Flex(card.gameObject, 1);
        Image barra = Caixa(card.transform, "Destaque", cor, true, spriteRedondoPequeno);
        Posicionar(barra.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -8), new Vector2(-16, -2));
        TextMeshProUGUI tRot = Texto(card.transform, rotulo, 17, COR_SUAVE, TextAlignmentOptions.TopLeft);
        Posicionar(tRot.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(18, -46), new Vector2(-10, -16));
        TextMeshProUGUI tVal = Texto(card.transform, valor, 34, COR_TITULO, TextAlignmentOptions.BottomLeft, true);
        Posicionar(tVal.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(18, 12), new Vector2(-10, -46));
        tVal.textWrappingMode = TextWrappingModes.NoWrap;
        tVal.enableAutoSizing = true;
        tVal.fontSizeMin = 20;
        tVal.fontSizeMax = 34;
    }

    private RectTransform CardMapa(Transform pai, string materia, Color cor)
    {
        Image card = Caixa(pai, "Mapa_" + materia, COR_CARD);
        Flex(card.gameObject, 1);
        TextMeshProUGUI t = Texto(card.transform, $"{materia}  <size=16><color=#64748B>mapa das 90 fases</color></size>", 24, COR_TITULO, TextAlignmentOptions.MidlineLeft, true);
        t.name = "Titulo";
        Posicionar(t.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -52), new Vector2(-22, -12));

        RectTransform grade = NovoRect("Grade", card.transform);
        Posicionar(grade, new Vector2(0, 0), new Vector2(1, 1), new Vector2(22, 40), new Vector2(-22, -58));

        // Legenda
        RectTransform legenda = NovoRect("Legenda", card.transform);
        Posicionar(legenda, new Vector2(0, 0), new Vector2(1, 0), new Vector2(22, 10), new Vector2(-22, 36));
        LayoutHorizontal(legenda, 14);
        ItemLegenda(legenda, COR_VAZIO, "não feita");
        ItemLegenda(legenda, CorEstrelas(1, cor), "1 estrela");
        ItemLegenda(legenda, CorEstrelas(2, cor), "2 estrelas");
        ItemLegenda(legenda, CorEstrelas(3, cor), "3 estrelas");
        return (RectTransform)card.transform;
    }

    private void ItemLegenda(Transform pai, Color cor, string texto)
    {
        RectTransform item = NovoRect("Legenda_" + texto, pai);
        Flex(item.gameObject, 1);
        Image q = Caixa(item, "Quadrado", cor, true, spriteRedondoPequeno);
        Posicionar(q.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, -9), new Vector2(18, 9));
        TextMeshProUGUI t = Texto(item, texto, 15, COR_SUAVE);
        Posicionar(t.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(24, 0), Vector2.zero);
    }

    private void TextoCarregandoMapa(RectTransform card)
    {
        AdicionarAvisoMapa(card, "Carregando as fases do aluno...");
    }

    private void AdicionarAvisoMapa(RectTransform card, string msg)
    {
        Transform grade = card.Find("Grade");
        TextMeshProUGUI t = Texto(grade, msg, 20, COR_SUAVE, TextAlignmentOptions.Center);
        Esticar(t.rectTransform);
    }

    /// <summary>
    /// Desenha, para cada nível, as 30 fases em 2 linhas de 15 quadradinhos coloridos pelas estrelas.
    /// </summary>
    private void PreencherMapa(RectTransform card, ServicoNuvem.DtoProgressoAlunoRoot detalhe, string materia, Color cor)
    {
        Transform grade = card.Find("Grade");
        Limpar(grade);
        string[] niveis = { "Facil", "Medio", "Dificil" };
        string[] nomes = { "Fácil", "Médio", "Difícil" };

        for (int n = 0; n < 3; n++)
        {
            Dictionary<int, ServicoNuvem.DtoFaseAluno> porFase = new Dictionary<int, ServicoNuvem.DtoFaseAluno>();
            if (detalhe?.fases != null)
            {
                foreach (var f in detalhe.fases)
                {
                    if (f.materia == materia && f.nivel == niveis[n] && f.concluida) porFase[f.numeroFase] = f;
                }
            }

            RectTransform bloco = NovoRect("Nivel_" + niveis[n], grade);
            bloco.anchorMin = new Vector2(0, 1f - (n + 1) / 3f);
            bloco.anchorMax = new Vector2(1, 1f - n / 3f);
            bloco.offsetMin = new Vector2(0, 4);
            bloco.offsetMax = new Vector2(0, -4);

            TextMeshProUGUI rotulo = Texto(bloco, $"<b>{nomes[n]}</b>\n<size=16><color=#64748B>{porFase.Count}/{FASES_POR_NIVEL}</color></size>", 19, COR_TITULO, TextAlignmentOptions.MidlineLeft);
            Posicionar(rotulo.rectTransform, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(84, 0));

            RectTransform quadros = NovoRect("Fases", bloco);
            Posicionar(quadros, new Vector2(0, 0), new Vector2(1, 1), new Vector2(90, 0), Vector2.zero);
            GridLayoutGroup gl = quadros.gameObject.AddComponent<GridLayoutGroup>();
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 15;
            gl.spacing = new Vector2(5, 5);
            gl.childAlignment = TextAnchor.MiddleLeft;
            gl.cellSize = new Vector2(24, 24);

            for (int fase = 1; fase <= FASES_POR_NIVEL; fase++)
            {
                Color c = porFase.TryGetValue(fase, out var dadoFase) ? CorEstrelas(dadoFase.estrelas, cor) : COR_VAZIO;
                Caixa(quadros, "Fase_" + fase, c, true, spriteRedondoPequeno);
            }
        }

        // Ajusta o tamanho dos quadradinhos ao espaço disponível (calcula o layout antes de medir)
        if (card.parent is RectTransform linhaMapas) LayoutRebuilder.ForceRebuildLayoutImmediate(linhaMapas);
        float largura = ((RectTransform)grade).rect.width - 90f;
        float lado = Mathf.Clamp((largura - 14 * 5f) / 15f, 14f, 30f);
        float altura = ((RectTransform)grade).rect.height / 3f - 8f;
        lado = Mathf.Min(lado, (altura - 5f) / 2f);
        foreach (GridLayoutGroup gl in grade.GetComponentsInChildren<GridLayoutGroup>())
        {
            gl.cellSize = new Vector2(lado, lado);
        }
    }

    // =====================================================================
    // TEXTOS: PARECER E RELATÓRIO
    // =====================================================================
    private string GerarParecer(AlunoComSala atual, ServicoNuvem.DtoProgressoAlunoRoot detalhe)
    {
        var a = atual.aluno;
        if (a.totalFases == 0)
            return $"{a.nomeJogo} ainda não concluiu nenhuma fase. Incentive o primeiro acesso e acompanhe as próximas aulas.";

        StringBuilder sb = new StringBuilder();
        int acerto = Acerto(a.totalFases, a.errosTotais);
        if (acerto >= 90) sb.Append($"{a.nomeJogo} tem ótima precisão ({acerto}% de acerto). ");
        else if (acerto >= 70) sb.Append($"{a.nomeJogo} tem bom aproveitamento ({acerto}% de acerto). ");
        else sb.Append($"{a.nomeJogo} erra com frequência ({acerto}% de acerto): vale revisar os conteúdos com ele(a). ");

        if (a.fasesPort > a.fasesMath + 5) sb.Append("Avança mais em Português; estimule Matemática. ");
        else if (a.fasesMath > a.fasesPort + 5) sb.Append("Avança mais em Matemática; estimule Português. ");
        else sb.Append("Avança de forma equilibrada nas duas matérias. ");

        if (detalhe?.fases != null)
        {
            string[] niveis = { "Dificil", "Medio" };
            string[] nomes = { "Difícil", "Médio" };
            for (int i = 0; i < niveis.Length; i++)
            {
                if (detalhe.fases.Any(f => f.concluida && f.nivel == niveis[i]))
                {
                    sb.Append($"Já chegou ao nível {nomes[i]}. ");
                    break;
                }
            }
        }

        int dias = DiasDesde(a.ultimaAtividade);
        if (dias >= 14) sb.Append($"Atenção: está há {dias} dias sem jogar.");
        else if (dias >= 0) sb.Append(dias == 0 ? "Jogou hoje." : $"Jogou há {dias} dia(s).");
        return sb.ToString().Trim();
    }

    private void CopiarRelatorio(AlunoComSala atual)
    {
        var a = atual.aluno;
        cacheDetalhe.TryGetValue(a.id, out var detalhe);
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("RELATÓRIO DO ALUNO - A REVOLTA DAS LETRINHAS");
        if (dados != null) sb.AppendLine($"Escola: {dados.nomeEscola} ({dados.municipio})");
        sb.AppendLine($"Sala: {atual.sala.nomeTurma}");
        sb.AppendLine($"Aluno: {a.nomeJogo}" + (!string.IsNullOrEmpty(a.nomeCompleto) && a.nomeCompleto != a.nomeJogo ? $" ({a.nomeCompleto})" : ""));
        sb.AppendLine($"Situação: {a.statusDesempenho}");
        sb.AppendLine($"Português: {a.fasesPort}/{FASES_POR_MATERIA} fases | Matemática: {a.fasesMath}/{FASES_POR_MATERIA} fases");
        sb.AppendLine($"Pontuação: {a.pontuacaoTotal} | Estrelas (média): {a.mediaEstrelas:0.0} de 3 | Acerto: {(a.totalFases > 0 ? Acerto(a.totalFases, a.errosTotais) + "%" : "-")}");
        sb.AppendLine($"Tempo jogado: {FormatarTempo(a.tempoTotalSegundos)} | Última atividade: {FormatarData(a.ultimaAtividade)}");
        if (detalhe?.fases != null)
        {
            string[] materias = { "Portugues", "Matematica" };
            string[] nomesMat = { "Português", "Matemática" };
            string[] niveis = { "Facil", "Medio", "Dificil" };
            string[] nomesNiv = { "Fácil", "Médio", "Difícil" };
            for (int m = 0; m < 2; m++)
            {
                StringBuilder linha = new StringBuilder($"{nomesMat[m]} por nível: ");
                for (int n = 0; n < 3; n++)
                {
                    int qtd = detalhe.fases.Count(f => f.concluida && f.materia == materias[m] && f.nivel == niveis[n]);
                    linha.Append($"{nomesNiv[n]} {qtd}/{FASES_POR_NIVEL}" + (n < 2 ? " | " : ""));
                }
                sb.AppendLine(linha.ToString());
            }
        }
        sb.AppendLine();
        sb.AppendLine("Parecer: " + GerarParecer(atual, detalhe));
        GUIUtility.systemCopyBuffer = sb.ToString();
        MostrarAvisoRapido("Relatório copiado! Cole (Ctrl+V) em um documento ou mensagem.");
    }

    private GameObject avisoRapido;
    private void MostrarAvisoRapido(string msg)
    {
        if (avisoRapido != null) Destroy(avisoRapido);
        Image aviso = Caixa(raiz.transform, "AvisoRapido", COR_TITULO);
        RectTransform rt = aviso.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(760, 64);
        rt.anchoredPosition = new Vector2(0, 70);
        TextMeshProUGUI t = Texto(aviso.transform, msg, 20, Color.white, TextAlignmentOptions.Center);
        Esticar(t.rectTransform, 16, 0, 16, 0);
        avisoRapido = aviso.gameObject;
        Destroy(avisoRapido, 3f);
    }

    // =====================================================================
    // COMPONENTES VISUAIS REUTILIZÁVEIS
    // =====================================================================
    private void BarraProgresso(RectTransform rt, float valor01, Color cor)
    {
        Image fundo = Caixa(rt, "Fundo", COR_VAZIO, true, spriteRedondoPequeno);
        Esticar(fundo.rectTransform);
        if (valor01 <= 0f) return;
        Image fill = Caixa(fundo.transform, "Preenchimento", cor, true, spriteRedondoPequeno);
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(valor01), 1);
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;
    }

    private void PilulaStatus(Transform pai, string status, Vector2 aMin, Vector2 aMax, float tamFonte, Vector2? offMin = null, Vector2? offMax = null)
    {
        Image p = Caixa(pai, "Status", CorStatusFundo(status));
        Posicionar(p.rectTransform, aMin, aMax, offMin ?? Vector2.zero, offMax ?? Vector2.zero);
        TextMeshProUGUI t = Texto(p.transform, status, tamFonte, CorStatus(status), TextAlignmentOptions.Center, true);
        Esticar(t.rectTransform, 6, 0, 6, 0);
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.enableAutoSizing = true;
        t.fontSizeMin = 12;
        t.fontSizeMax = tamFonte;
    }

    private void Avatar(Transform pai, string nome, Color cor, float tam, Vector2 posEsquerda)
    {
        RectTransform rt = NovoRect("Avatar", pai);
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        rt.pivot = new Vector2(0, 0.5f);
        rt.sizeDelta = new Vector2(tam, tam);
        rt.anchoredPosition = posEsquerda;
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = spriteCirculo;
        img.color = cor;
        img.raycastTarget = false;
        string inicial = string.IsNullOrEmpty(nome) ? "?" : nome.Trim().Substring(0, 1).ToUpperInvariant();
        TextMeshProUGUI t = Texto(rt, inicial, tam * 0.48f, Color.white, TextAlignmentOptions.Center, true);
        Esticar(t.rectTransform);
    }

    private void CelulasLinha(RectTransform linha, float[] limites, string[] textos, float tam, Color cor, bool negrito)
    {
        float inicio = 0f;
        for (int i = 0; i < limites.Length && i < textos.Length; i++)
        {
            if (!string.IsNullOrEmpty(textos[i]))
            {
                bool nomeOuTexto = i == 1 || (i == 0 && limites[0] > 0.1f);
                TextMeshProUGUI t = Texto(linha, textos[i], tam, cor, nomeOuTexto ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center, negrito);
                Posicionar(t.rectTransform, new Vector2(inicio, 0), new Vector2(limites[i], 1), new Vector2(6, 0), new Vector2(-6, 0));
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.overflowMode = TextOverflowModes.Ellipsis;
            }
            inicio = limites[i];
        }
    }

    private void AdicionarTextoLista(Transform content, string msg)
    {
        RectTransform rt = NovoRect("Aviso", content);
        LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 90;
        TextMeshProUGUI t = Texto(rt, msg, 20, COR_SUAVE, TextAlignmentOptions.Center);
        Esticar(t.rectTransform, 10, 0, 10, 0);
    }

    private TMP_InputField CriarCampoBusca(Transform pai)
    {
        Image fundo = Caixa(pai, "CampoBusca", COR_LINHA);
        TMP_InputField input = fundo.gameObject.AddComponent<TMP_InputField>();
        RectTransform areaTexto = NovoRect("AreaTexto", fundo.transform);
        Esticar(areaTexto, 18, 6, 18, 6);
        areaTexto.gameObject.AddComponent<RectMask2D>();
        TextMeshProUGUI ph = Texto(areaTexto, "Buscar aluno pelo nome...", 19, COR_SUAVE);
        ph.fontStyle = FontStyles.Italic;
        Esticar(ph.rectTransform);
        TextMeshProUGUI txt = Texto(areaTexto, "", 19, COR_TITULO);
        Esticar(txt.rectTransform);
        input.textViewport = areaTexto;
        input.textComponent = txt;
        input.placeholder = ph;
        input.fontAsset = fonte;
        input.characterLimit = 40;
        input.onValueChanged.AddListener(v =>
        {
            filtroBusca = v ?? "";
            RenderListaAlunos();
        });
        return input;
    }

    private Transform CriarScrollVertical(RectTransform rt, float espacamento)
    {
        ScrollRect sr = rt.gameObject.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 35;
        RectTransform vp = NovoRect("Viewport", rt);
        Esticar(vp);
        vp.gameObject.AddComponent<RectMask2D>();
        Image vpImg = vp.gameObject.AddComponent<Image>();
        vpImg.color = new Color(0, 0, 0, 0);
        RectTransform content = NovoRect("Content", vp);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        VerticalLayoutGroup vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
        vl.spacing = espacamento;
        vl.padding = new RectOffset(4, 4, 4, 4);
        vl.childControlHeight = true;
        vl.childControlWidth = true;
        vl.childForceExpandHeight = false;
        vl.childForceExpandWidth = true;
        ContentSizeFitter csf = content.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.viewport = vp;
        sr.content = content;
        return content;
    }

    private Button Botao(Transform pai, string nome, string texto, Color fundo, Color corTexto, float tamFonte, UnityAction acao)
    {
        Image img = Caixa(pai, nome, fundo);
        img.raycastTarget = true;
        Button btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cores = btn.colors;
        cores.normalColor = Color.white;
        cores.highlightedColor = new Color(0.9f, 0.92f, 1f, 1f);
        cores.pressedColor = new Color(0.78f, 0.8f, 0.9f, 1f);
        cores.selectedColor = Color.white;
        cores.fadeDuration = 0.08f;
        btn.colors = cores;
        TextMeshProUGUI t = Texto(img.transform, texto, tamFonte, corTexto, TextAlignmentOptions.Center, true);
        Esticar(t.rectTransform, 10, 0, 10, 0);
        btn.onClick.AddListener(() =>
        {
            if (area != null) area.TocarSomCliquePublico();
            acao?.Invoke();
        });
        return btn;
    }

    private Image Caixa(Transform pai, string nome, Color cor, bool arredondado = true, Sprite sprite = null)
    {
        RectTransform rt = NovoRect(nome, pai);
        Image img = rt.gameObject.AddComponent<Image>();
        img.color = cor;
        img.raycastTarget = false;
        if (arredondado)
        {
            img.sprite = sprite != null ? sprite : spriteRedondo;
            img.type = Image.Type.Sliced;
        }
        return img;
    }

    private TextMeshProUGUI Texto(Transform pai, string texto, float tamanho, Color cor, TextAlignmentOptions alinhamento = TextAlignmentOptions.MidlineLeft, bool negrito = false)
    {
        RectTransform rt = NovoRect("Texto", pai);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (fonte != null) t.font = fonte;
        t.text = texto;
        t.fontSize = tamanho;
        t.color = cor;
        t.alignment = alinhamento;
        t.fontStyle = negrito ? FontStyles.Bold : FontStyles.Normal;
        t.raycastTarget = false;
        t.richText = true;
        return t;
    }

    private static RectTransform NovoRect(string nome, Transform pai)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        return (RectTransform)go.transform;
    }

    private static void Esticar(RectTransform rt, float esq = 0, float baixo = 0, float dir = 0, float cima = 0)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(esq, baixo);
        rt.offsetMax = new Vector2(-dir, -cima);
    }

    private static void Posicionar(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }

    private static void LayoutHorizontal(RectTransform rt, float espacamento)
    {
        HorizontalLayoutGroup hl = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = espacamento;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = true;
    }

    private static void Flex(GameObject go, float flex)
    {
        LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.flexibleWidth = flex;
        le.minWidth = 0;
    }

    private static void AdicionarLargura(GameObject go, float largura)
    {
        LayoutElement le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.preferredWidth = largura;
    }

    private static void Limpar(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            GameObject filho = t.GetChild(i).gameObject;
            filho.SetActive(false); // some do layout já neste quadro
            Destroy(filho);
        }
    }

    // =====================================================================
    // UTILITÁRIOS
    // =====================================================================
    private static int Acerto(int fases, int erros)
    {
        if (fases <= 0) return 0;
        return Mathf.Clamp(Mathf.RoundToInt(100f * (1f - (float)erros / fases)), 0, 100);
    }

    private static Color CorStatus(string status)
    {
        switch (status)
        {
            case "Excelente": return Hex("059669");
            case "Bom": return Hex("2563EB");
            case "Em Progresso": return Hex("D97706");
            case "Precisa de Apoio": return Hex("DC2626");
            default: return Hex("64748B");
        }
    }

    private static Color CorStatusFundo(string status)
    {
        switch (status)
        {
            case "Excelente": return Hex("D1FAE5");
            case "Bom": return Hex("DBEAFE");
            case "Em Progresso": return Hex("FEF3C7");
            case "Precisa de Apoio": return Hex("FEE2E2");
            default: return Hex("F1F5F9");
        }
    }

    private static Color CorEstrelas(int estrelas, Color corMateria)
    {
        if (estrelas >= 3) return corMateria;
        if (estrelas == 2) return Color.Lerp(corMateria, Color.white, 0.4f);
        return Color.Lerp(corMateria, Color.white, 0.7f);
    }

    private static DateTime? LerData(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return null;
        if (DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime dt))
            return dt;
        return null;
    }

    private static string FormatarData(string iso)
    {
        DateTime? dt = LerData(iso);
        return dt.HasValue ? dt.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "nunca jogou";
    }

    private static int DiasDesde(string iso)
    {
        DateTime? dt = LerData(iso);
        if (!dt.HasValue) return -1;
        return Mathf.Max(0, (int)(DateTime.UtcNow - dt.Value).TotalDays);
    }

    private static string FormatarTempo(int segundos)
    {
        if (segundos <= 0) return "-";
        int min = segundos / 60;
        if (min < 60) return $"{Mathf.Max(1, min)} min";
        return $"{min / 60}h {min % 60:00}min";
    }

    private static Color Hex(string hex, float alfa = 1f)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        c.a = alfa;
        return c;
    }

    /// <summary>Gera um sprite branco com cantos arredondados (fatiado) ou um círculo.</summary>
    private static Sprite GerarSprite(int tamanho, int raio, bool fatiado)
    {
        Texture2D tex = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        Color32[] px = new Color32[tamanho * tamanho];
        for (int y = 0; y < tamanho; y++)
        {
            for (int x = 0; x < tamanho; x++)
            {
                float px0 = x + 0.5f, py0 = y + 0.5f;
                float cx = Mathf.Clamp(px0, raio, tamanho - raio);
                float cy = Mathf.Clamp(py0, raio, tamanho - raio);
                float d = Vector2.Distance(new Vector2(px0, py0), new Vector2(cx, cy));
                byte a = (byte)(Mathf.Clamp01(raio - d + 0.5f) * 255f);
                px[y * tamanho + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        Vector4 borda = fatiado ? new Vector4(raio, raio, raio, raio) : Vector4.zero;
        return Sprite.Create(tex, new Rect(0, 0, tamanho, tamanho), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, borda);
    }
}
