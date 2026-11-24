using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Galeria de personagens de perfil. Com 2 troféus (níveis inteiros concluídos) o aluno resgata um personagem
/// só dele: cada personagem tem um único dono na escola (o servidor garante). O personagem vira a foto do aluno
/// no Lobby e na lista "Quem vai jogar hoje?". Imagens: Noto Emoji v2.042 (Google), licença Apache 2.0.
/// </summary>
public class TelaPersonagens : MonoBehaviour
{
    [Serializable] private class ItemCatalogo { public string codigo; public string nome; public string categoria; }
    [Serializable] private class CatalogoRoot { public List<ItemCatalogo> personagens; }

    private class Celula
    {
        public ItemCatalogo item;
        public GameObject go;
        public Image fundo;
        public Image imagem;
        public GameObject marcaDono;
        public GameObject marcaMeu;
    }

    private static TelaPersonagens _instancia;
    private const int ORDEM = 993; // acima do álbum (990), abaixo da Área do Professor (998)
    private static readonly string[] CATEGORIAS = { "Todos", "Animais", "Heróis e magia", "Profissões", "Aventura e esporte", "Divertidos" };

    private GameObject raiz;
    private TextMeshProUGUI txtSubtitulo;
    private TextMeshProUGUI txtMensagem;
    private Image imgMeu;
    private TextMeshProUGUI txtMeu;
    private RectTransform conteudoGrade;
    private readonly List<Celula> celulas = new List<Celula>();
    private readonly List<Image> abas = new List<Image>();
    private string categoriaAtual = "Todos";
    private List<ItemCatalogo> catalogo;
    private bool carregando;

    private GameObject confirmacao;
    private Image imgConfirmacao;
    private TextMeshProUGUI txtConfirmacao;
    private ItemCatalogo escolhido;

    public static bool Aberta => _instancia != null && _instancia.raiz != null && _instancia.raiz.activeSelf;

    public static void Abrir()
    {
        if (_instancia == null)
        {
            GameObject go = InterfaceUtil.CriarCanvas("[TelaPersonagens]", ORDEM);
            _instancia = go.AddComponent<TelaPersonagens>();
            _instancia.Construir();
        }
        _instancia.raiz.SetActive(true);
        _instancia.confirmacao.SetActive(false);
        _instancia.txtMensagem.text = "";
        _instancia.categoriaAtual = "Todos";
        _instancia.AtualizarTudo();
        if (_instancia.catalogo == null && !_instancia.carregando) _instancia.StartCoroutine(_instancia.CarregarCatalogo());
        var sn = ServicoNuvem.Instancia;
        if (sn != null)
        {
            sn.OnPersonagensAtualizados -= _instancia.AtualizarTudo;
            sn.OnPersonagensAtualizados += _instancia.AtualizarTudo;
            sn.AtualizarPersonagens();
        }
    }

    public static void Fechar()
    {
        if (_instancia != null) _instancia.raiz.SetActive(false);
    }

    // ================= Catálogo =================

    private string ArquivoCatalogo => Path.Combine(Application.persistentDataPath, "imagens", "personagens", "catalogo.json");

    private IEnumerator CarregarCatalogo()
    {
        carregando = true;
        txtMensagem.text = "Carregando personagens...";
        string json = null;
        var sn = ServicoNuvem.Instancia;
        if (sn != null)
        {
            using (UnityWebRequest req = UnityWebRequest.Get($"{sn.servidorUrl}/imagens/personagens/catalogo.json"))
            {
                req.timeout = 20;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    json = req.downloadHandler.text;
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(ArquivoCatalogo));
                        File.WriteAllText(ArquivoCatalogo, json);
                    }
                    catch { }
                }
            }
        }
        if (json == null && File.Exists(ArquivoCatalogo)) json = File.ReadAllText(ArquivoCatalogo);

        CatalogoRoot root = null;
        if (!string.IsNullOrEmpty(json)) { try { root = JsonUtility.FromJson<CatalogoRoot>(json); } catch { } }
        carregando = false;
        if (root == null || root.personagens == null || root.personagens.Count == 0)
        {
            txtMensagem.text = "Não foi possível carregar os personagens. Conecte o computador à internet e abra de novo.";
            yield break;
        }
        catalogo = root.personagens;
        txtMensagem.text = "";
        CriarCelulas();
        AtualizarTudo();
    }

    private void CriarCelulas()
    {
        foreach (var item in catalogo)
        {
            Celula c = new Celula { item = item };
            Image fundo = InterfaceUtil.Caixa(conteudoGrade, "Personagem_" + item.codigo, Color.white);
            fundo.raycastTarget = true;
            Button b = fundo.gameObject.AddComponent<Button>();
            b.targetGraphic = fundo;
            ItemCatalogo capturado = item;
            b.onClick.AddListener(() => ClicarPersonagem(capturado));

            RectTransform rtImg = InterfaceUtil.NovoRect("Imagem", fundo.transform);
            InterfaceUtil.Posicionar(rtImg, new Vector2(0, 0.24f), new Vector2(1, 1), new Vector2(14, 0), new Vector2(-14, -10));
            Image img = rtImg.gameObject.AddComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = new Color(1, 1, 1, 0);
            ImagensRemotas.ImagemDoPersonagem(item.codigo, s => { if (s != null && img != null) { img.sprite = s; img.color = Color.white; } });

            TextMeshProUGUI nome = InterfaceUtil.Texto(fundo.transform, item.nome, 16, InterfaceUtil.Hex("#334155"), TextAlignmentOptions.Center);
            nome.enableAutoSizing = true;
            nome.fontSizeMin = 10;
            nome.fontSizeMax = 16;
            InterfaceUtil.Posicionar(nome.rectTransform, new Vector2(0, 0), new Vector2(1, 0.25f), new Vector2(4, 2), new Vector2(-4, 0));

            // "Tem dono": véu cinza com cadeado escrito
            Image veu = InterfaceUtil.Caixa(fundo.transform, "TemDono", new Color(0.35f, 0.38f, 0.45f, 0.72f));
            InterfaceUtil.Esticar(veu.rectTransform, 0, 0, 0, 0);
            TextMeshProUGUI txtDono = InterfaceUtil.Texto(veu.transform, "Tem dono", 18, Color.white, TextAlignmentOptions.Center);
            txtDono.fontStyle = FontStyles.Bold;
            InterfaceUtil.Esticar(txtDono.rectTransform, 0, 0, 0, 0);

            // "Seu!": faixa dourada
            Image meu = InterfaceUtil.Caixa(fundo.transform, "Meu", InterfaceUtil.Hex("#F59E0B"));
            InterfaceUtil.Posicionar(meu.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -26), Vector2.zero);
            TextMeshProUGUI txtMeuSelo = InterfaceUtil.Texto(meu.transform, "É SEU!", 16, Color.white, TextAlignmentOptions.Center);
            txtMeuSelo.fontStyle = FontStyles.Bold;
            InterfaceUtil.Esticar(txtMeuSelo.rectTransform, 0, 0, 0, 0);

            c.go = fundo.gameObject;
            c.fundo = fundo;
            c.imagem = img;
            c.marcaDono = veu.gameObject;
            c.marcaMeu = meu.gameObject;
            celulas.Add(c);
        }
    }

    // ================= Estado =================

    private string Aluno => GerenciadorPerfis.PerfilAtivo;
    private string MeuCodigo => ServicoNuvem.Instancia != null ? ServicoNuvem.Instancia.ObterPersonagemDoAluno(Aluno) : "";
    private int TrofeusNecessarios => ServicoNuvem.Instancia != null ? ServicoNuvem.Instancia.TrofeusParaPersonagem : 2;

    private void AtualizarTudo()
    {
        if (raiz == null || !raiz.activeSelf) return;
        string meu = MeuCodigo;
        int trofeus = ResgateLetrinhas.TrofeusDoAluno(Aluno);
        int precisa = TrofeusNecessarios;

        if (!string.IsNullOrEmpty(meu))
            txtSubtitulo.text = $"<b>{Aluno}</b>, este é o seu personagem! Ele aparece na sua foto de perfil e é só seu na escola.";
        else if (trofeus >= precisa)
            txtSubtitulo.text = $"<b>{Aluno}</b>, você tem {trofeus} troféus: escolha um personagem só seu! Cada personagem só pode ter um dono na escola.";
        else
            txtSubtitulo.text = $"<b>{Aluno}</b>, você tem {trofeus} de {precisa} troféus. Complete níveis inteiros (30 fases) para ganhar troféus e resgatar um personagem só seu!";

        imgMeu.gameObject.SetActive(false);
        txtMeu.text = string.IsNullOrEmpty(meu) ? "Sem personagem" : NomeDo(meu);
        if (!string.IsNullOrEmpty(meu))
            ImagensRemotas.ImagemDoPersonagem(meu, s => { if (s != null) { imgMeu.sprite = s; imgMeu.gameObject.SetActive(true); } });

        var sn = ServicoNuvem.Instancia;
        foreach (var c in celulas)
        {
            bool visivel = categoriaAtual == "Todos" || c.item.categoria == categoriaAtual;
            if (c.go.activeSelf != visivel) c.go.SetActive(visivel);
            bool eMeu = c.item.codigo == meu;
            bool temDono = !eMeu && sn != null && sn.PersonagemOcupado(c.item.codigo);
            c.marcaMeu.SetActive(eMeu);
            c.marcaDono.SetActive(temDono);
            c.fundo.color = eMeu ? InterfaceUtil.Hex("#FEF3C7") : Color.white;
        }
        for (int i = 0; i < abas.Count; i++)
            abas[i].color = CATEGORIAS[i] == categoriaAtual ? InterfaceUtil.Hex("#7C3AED") : InterfaceUtil.Hex("#A78BFA");
    }

    private string NomeDo(string codigo)
    {
        if (catalogo != null) foreach (var i in catalogo) if (i.codigo == codigo) return i.nome;
        return "Seu personagem";
    }

    private void ClicarPersonagem(ItemCatalogo item)
    {
        string meu = MeuCodigo;
        var sn = ServicoNuvem.Instancia;
        int trofeus = ResgateLetrinhas.TrofeusDoAluno(Aluno);
        if (item.codigo == meu) { Avisar("Este já é o seu personagem!"); return; }
        if (sn != null && sn.PersonagemOcupado(item.codigo)) { Avisar("Esse personagem já tem dono na sua escola. Escolha outro!"); return; }
        if (!string.IsNullOrEmpty(meu)) { Avisar("Você já resgatou o seu personagem. Cada criança tem um só."); return; }
        if (trofeus < TrofeusNecessarios) { Avisar($"Você precisa de {TrofeusNecessarios} troféus para resgatar (tem {trofeus}). Complete níveis inteiros!"); return; }
        if (sn == null || !sn.conectadoAoServidor) { Avisar("Para resgatar um personagem o computador precisa de internet."); return; }

        escolhido = item;
        txtConfirmacao.text = $"Resgatar <b>{item.nome}</b>?\n<size=75%>Ele será só seu na escola e vira sua foto de perfil. Depois não dá para trocar.</size>";
        imgConfirmacao.sprite = null;
        imgConfirmacao.color = new Color(1, 1, 1, 0);
        ImagensRemotas.ImagemDoPersonagem(item.codigo, s => { if (s != null) { imgConfirmacao.sprite = s; imgConfirmacao.color = Color.white; } });
        confirmacao.SetActive(true);
    }

    private void ConfirmarResgate()
    {
        confirmacao.SetActive(false);
        if (escolhido == null || ServicoNuvem.Instancia == null) return;
        ItemCatalogo item = escolhido;
        Avisar("Resgatando...");
        ServicoNuvem.Instancia.ResgatarPersonagem(Aluno, item.codigo, (ok, msg) =>
        {
            if (ok)
            {
                Avisar($"Oba! Agora {item.nome} é só seu!");
                LeitorDeVoz.Falar($"Oba! Agora o personagem {item.nome} é só seu!");
                GerenciadorPerfis.NotificarProgressoAtualizado(); // atualiza a foto no Lobby
            }
            else Avisar(msg);
            AtualizarTudo();
        });
    }

    private void Avisar(string texto) => txtMensagem.text = texto;

    // ================= Construção =================

    private void Construir()
    {
        RectTransform rtRaiz = InterfaceUtil.NovoRect("Personagens", transform);
        InterfaceUtil.Esticar(rtRaiz, 0, 0, 0, 0);
        Image fundo = rtRaiz.gameObject.AddComponent<Image>();
        fundo.color = new Color(0.05f, 0.07f, 0.14f, 0.9f);
        raiz = rtRaiz.gameObject;

        Image card = InterfaceUtil.Caixa(rtRaiz, "Cartao", new Color(0.98f, 0.97f, 1f));
        InterfaceUtil.Fixar(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1720f, 1000f));

        TextMeshProUGUI titulo = InterfaceUtil.Texto(card.transform, "Personagens", 60, InterfaceUtil.Hex("#4C1D95"), TextAlignmentOptions.Left, true);
        InterfaceUtil.Posicionar(titulo.rectTransform, new Vector2(0, 1), new Vector2(0.6f, 1), new Vector2(40, -100), new Vector2(0, -20));
        txtSubtitulo = InterfaceUtil.Texto(card.transform, "", 24, InterfaceUtil.Hex("#334155"), TextAlignmentOptions.TopLeft);
        InterfaceUtil.Posicionar(txtSubtitulo.rectTransform, new Vector2(0, 1), new Vector2(0.72f, 1), new Vector2(40, -170), new Vector2(0, -100));

        // Meu personagem (canto superior direito)
        Image caixaMeu = InterfaceUtil.Caixa(card.transform, "MeuPersonagem", InterfaceUtil.Hex("#EDE9FE"));
        InterfaceUtil.Fixar(caixaMeu.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-30, -20), new Vector2(380f, 150f));
        RectTransform rtMeu = InterfaceUtil.NovoRect("Imagem", caixaMeu.transform);
        InterfaceUtil.Fixar(rtMeu, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(120f, 120f));
        rtMeu.pivot = new Vector2(0, 0.5f);
        imgMeu = rtMeu.gameObject.AddComponent<Image>();
        imgMeu.preserveAspect = true;
        imgMeu.raycastTarget = false;
        TextMeshProUGUI rot = InterfaceUtil.Texto(caixaMeu.transform, "Meu personagem", 18, InterfaceUtil.Hex("#6D28D9"), TextAlignmentOptions.TopLeft);
        InterfaceUtil.Posicionar(rot.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(150, 0), new Vector2(-10, -18));
        txtMeu = InterfaceUtil.Texto(caixaMeu.transform, "", 26, InterfaceUtil.Hex("#1E1B4B"), TextAlignmentOptions.Left, true);
        txtMeu.enableAutoSizing = true;
        txtMeu.fontSizeMin = 16;
        txtMeu.fontSizeMax = 26;
        InterfaceUtil.Posicionar(txtMeu.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(150, 10), new Vector2(-10, -50));

        // Abas de categoria
        RectTransform rtAbas = InterfaceUtil.NovoRect("Abas", card.transform);
        InterfaceUtil.Posicionar(rtAbas, new Vector2(0, 1), new Vector2(1, 1), new Vector2(40, -240), new Vector2(-40, -186));
        HorizontalLayoutGroup hl = rtAbas.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = 10;
        hl.childForceExpandWidth = false;
        hl.childControlWidth = false;
        hl.childControlHeight = true;
        foreach (string cat in CATEGORIAS)
        {
            string c = cat;
            Button aba = InterfaceUtil.Botao(rtAbas, "Aba_" + cat, cat, InterfaceUtil.Hex("#A78BFA"), 22, () => { categoriaAtual = c; AtualizarTudo(); }, out _);
            ((RectTransform)aba.transform).sizeDelta = new Vector2(cat.Length > 12 ? 250f : 180f, 54f);
            abas.Add(aba.GetComponent<Image>());
        }

        // Grade rolável
        RectTransform rtScroll = InterfaceUtil.NovoRect("Rolagem", card.transform);
        InterfaceUtil.Posicionar(rtScroll, new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 130), new Vector2(-40, -252));
        Image fundoScroll = rtScroll.gameObject.AddComponent<Image>();
        fundoScroll.color = InterfaceUtil.Hex("#F1F5F9");
        ScrollRect sr = rtScroll.gameObject.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.scrollSensitivity = 40f;
        rtScroll.gameObject.AddComponent<RectMask2D>();
        conteudoGrade = InterfaceUtil.NovoRect("Conteudo", rtScroll);
        conteudoGrade.anchorMin = new Vector2(0, 1);
        conteudoGrade.anchorMax = new Vector2(1, 1);
        conteudoGrade.pivot = new Vector2(0.5f, 1);
        conteudoGrade.anchoredPosition = Vector2.zero;
        conteudoGrade.sizeDelta = Vector2.zero; // mesma largura da área visível (sem isso a 1ª coluna ficava cortada)
        GridLayoutGroup grid = conteudoGrade.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(132f, 156f);
        grid.spacing = new Vector2(12f, 12f);
        grid.padding = new RectOffset(14, 14, 14, 14);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 11;
        grid.childAlignment = TextAnchor.UpperCenter;
        ContentSizeFitter ajuste = conteudoGrade.gameObject.AddComponent<ContentSizeFitter>();
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = conteudoGrade;
        sr.viewport = rtScroll;

        // Mensagem e botão Fechar
        txtMensagem = InterfaceUtil.Texto(card.transform, "", 26, InterfaceUtil.Hex("#B45309"), TextAlignmentOptions.MidlineLeft);
        txtMensagem.fontStyle = FontStyles.Bold;
        InterfaceUtil.Posicionar(txtMensagem.rectTransform, new Vector2(0, 0), new Vector2(0.75f, 0), new Vector2(40, 30), new Vector2(0, 110));
        Button fechar = InterfaceUtil.Botao(card.transform, "Fechar", "Fechar", InterfaceUtil.Hex("#EA580C"), 30, Fechar, out _);
        InterfaceUtil.Fixar((RectTransform)fechar.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 32), new Vector2(260f, 72f));
        TextMeshProUGUI creditos = InterfaceUtil.Texto(card.transform, "Imagens: Noto Emoji © Google — licença Apache 2.0", 14, InterfaceUtil.Hex("#94A3B8"), TextAlignmentOptions.BottomLeft);
        InterfaceUtil.Posicionar(creditos.rectTransform, new Vector2(0, 0), new Vector2(0.6f, 0), new Vector2(40, 6), new Vector2(0, 28));

        // Confirmação do resgate
        RectTransform rtConf = InterfaceUtil.NovoRect("Confirmacao", transform);
        InterfaceUtil.Esticar(rtConf, 0, 0, 0, 0);
        Image veu = rtConf.gameObject.AddComponent<Image>();
        veu.color = new Color(0, 0, 0, 0.55f);
        Image caixa = InterfaceUtil.Caixa(rtConf, "Caixa", Color.white);
        InterfaceUtil.Fixar(caixa.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 520f));
        RectTransform rtImgC = InterfaceUtil.NovoRect("Imagem", caixa.transform);
        InterfaceUtil.Fixar(rtImgC, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(180f, 180f));
        imgConfirmacao = rtImgC.gameObject.AddComponent<Image>();
        imgConfirmacao.preserveAspect = true;
        imgConfirmacao.raycastTarget = false;
        txtConfirmacao = InterfaceUtil.Texto(caixa.transform, "", 34, InterfaceUtil.Hex("#1E293B"), TextAlignmentOptions.Center);
        InterfaceUtil.Posicionar(txtConfirmacao.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(40, 120), new Vector2(-40, -220));
        Button sim = InterfaceUtil.Botao(caixa.transform, "Resgatar", "Resgatar!", InterfaceUtil.Hex("#16A34A"), 30, ConfirmarResgate, out _);
        InterfaceUtil.Fixar((RectTransform)sim.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-150, 30), new Vector2(260f, 72f));
        Button nao = InterfaceUtil.Botao(caixa.transform, "Cancelar", "Cancelar", InterfaceUtil.Hex("#64748B"), 30, () => confirmacao.SetActive(false), out _);
        InterfaceUtil.Fixar((RectTransform)nao.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(150, 30), new Vector2(260f, 72f));
        confirmacao = rtConf.gameObject;
        confirmacao.SetActive(false);

        raiz.SetActive(false);
    }
}
