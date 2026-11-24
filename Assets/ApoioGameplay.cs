using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Apoio ao aluno durante a fase:
/// - botão "Ouvir": lê a pergunta em voz alta (no 1º e 2º ano, lê sozinho a cada pergunta);
/// - dica depois de um erro (explica o caminho sem dar a resposta).
/// </summary>
public class ApoioGameplay : MonoBehaviour
{
    private static ApoioGameplay _instancia;

    // Acima da barra da fase (500) e abaixo da Área do Professor (998)
    private const int ORDEM = 600;

    private GameObject botaoOuvir;

    // Desenho da palavra: fica DENTRO do balão de fala (filho do texto do balão), à direita do texto.
    // O balão nunca cobre o personagem da fase, então o desenho também não cobre.
    private class FiguraNoBalao
    {
        public GameObject go;
        public Image imagem;
        public Vector4 margemBase;
        public bool ativa;
    }
    private readonly Dictionary<TextMeshProUGUI, FiguraNoBalao> figuras = new Dictionary<TextMeshProUGUI, FiguraNoBalao>();
    private TextMeshProUGUI balaoComFigura;
    private string figuraPalavra;
    private GameObject dica;
    private CanvasGroup dicaGrupo;
    private TextMeshProUGUI txtDica;
    private Coroutine rotinaDica;
    private Action action;
    private GameManager gm;
    private float proximaChecagem;

    private static ApoioGameplay Obter()
    {
        if (_instancia == null)
        {
            GameObject go = InterfaceUtil.CriarCanvas("[ApoioGameplay]", ORDEM);
            _instancia = go.AddComponent<ApoioGameplay>();
            _instancia.Construir();
        }
        return _instancia;
    }

    /// <summary>Antes de montar a pergunta: tira a dica e o desenho da pergunta anterior (e devolve a margem do texto).</summary>
    public static void LimparPergunta()
    {
        if (_instancia == null) return;
        _instancia.EsconderDica();
        _instancia.EsconderFigura();
    }

    /// <summary>Pergunta nova na tela: se pedido, lê a pergunta em voz alta.</summary>
    public static void NovaPergunta(bool lerAutomaticamente)
    {
        ApoioGameplay a = Obter();
        a.EsconderDica();
        if (lerAutomaticamente) a.StartCoroutine(a.LerDepois(0.6f));
    }

    /// <summary>Mostra o desenho da palavra (ex.: uma bola em "B_LA") dentro do balão da pergunta, à direita do texto.</summary>
    public static void MostrarFigura(string palavra, TextMeshProUGUI balao)
    {
        if (string.IsNullOrEmpty(palavra) || balao == null || !ImagensRemotas.TemDesenho(palavra)) return;
        ApoioGameplay a = Obter();
        a.figuraPalavra = palavra;
        ImagensRemotas.DesenhoDaPalavra(palavra, sprite =>
        {
            if (sprite == null || a.figuraPalavra != palavra || balao == null) return; // a pergunta já mudou
            a.ColocarFiguraNoBalao(balao, sprite);
        });
    }

    private void ColocarFiguraNoBalao(TextMeshProUGUI balao, Sprite sprite)
    {
        EsconderFigura();
        if (!figuras.TryGetValue(balao, out FiguraNoBalao f))
        {
            RectTransform rt = InterfaceUtil.NovoRect("DesenhoDaPalavra", balao.transform);
            Image img = rt.gameObject.AddComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            f = new FiguraNoBalao { go = rt.gameObject, imagem = img };
            figuras[balao] = f;
        }

        // Tamanho pela altura útil do balão (sem as margens do texto)
        Rect area = balao.rectTransform.rect;
        Vector4 m = balao.margin; // x=esq, y=topo, z=dir, w=baixo
        float alturaUtil = area.height - m.y - m.w;
        float lado = Mathf.Clamp(alturaUtil * 0.92f, 50f, Mathf.Min(200f, area.width * 0.4f));

        RectTransform rtF = (RectTransform)f.go.transform;
        rtF.anchorMin = rtF.anchorMax = new Vector2(1f, 0.5f);
        rtF.pivot = new Vector2(1f, 0.5f);
        rtF.sizeDelta = new Vector2(lado, lado);
        rtF.anchoredPosition = new Vector2(-(m.z + 4f), (m.w - m.y) / 2f);
        f.imagem.sprite = sprite;
        f.margemBase = m;
        f.ativa = true;
        f.go.SetActive(true);

        // Afasta o texto para não ficar embaixo do desenho
        balao.margin = new Vector4(m.x, m.y, m.z + lado + 10f, m.w);
        balaoComFigura = balao;
    }

    private void EsconderFigura()
    {
        figuraPalavra = null;
        if (balaoComFigura != null && figuras.TryGetValue(balaoComFigura, out FiguraNoBalao f) && f.ativa)
        {
            f.go.SetActive(false);
            balaoComFigura.margin = f.margemBase;
            f.ativa = false;
        }
        balaoComFigura = null;
    }

    public static void MostrarDica(string texto, string textoFalado)
    {
        if (string.IsNullOrEmpty(texto)) return;
        ApoioGameplay a = Obter();
        if (a.rotinaDica != null) a.StopCoroutine(a.rotinaDica);
        a.rotinaDica = a.StartCoroutine(a.DicaCoroutine(texto, textoFalado));
    }

    public static void Esconder()
    {
        if (_instancia != null) _instancia.EsconderDica();
    }

    private IEnumerator LerDepois(float segundos)
    {
        yield return new WaitForSecondsRealtime(segundos);
        if (gm == null) gm = FindFirstObjectByType<GameManager>();
        if (gm != null) LeitorDeVoz.Falar(gm.TextoParaLeitura);
    }

    private IEnumerator DicaCoroutine(string texto, string falado)
    {
        txtDica.text = "<b>Dica:</b> " + texto;
        dica.SetActive(true);
        dicaGrupo.alpha = 1f;
        if (!string.IsNullOrEmpty(falado)) LeitorDeVoz.Falar(falado);
        yield return new WaitForSecondsRealtime(7f);
        for (float t = 0; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            dicaGrupo.alpha = 1f - t / 0.5f;
            yield return null;
        }
        dica.SetActive(false);
        rotinaDica = null;
    }

    private void EsconderDica()
    {
        if (rotinaDica != null) StopCoroutine(rotinaDica);
        rotinaDica = null;
        if (dica != null) dica.SetActive(false);
    }

    private void Update()
    {
        if (Time.unscaledTime < proximaChecagem) return;
        proximaChecagem = Time.unscaledTime + 0.2f;
        if (action == null) action = FindFirstObjectByType<Action>();
        bool emJogo = InterfaceUtil.EmGameplay(action);
        if (botaoOuvir.activeSelf != (emJogo && LeitorDeVoz.Disponivel)) botaoOuvir.SetActive(emJogo && LeitorDeVoz.Disponivel);
        if (!emJogo && dica.activeSelf) EsconderDica();
    }

    private void Construir()
    {
        // Botão "Ouvir" no canto superior esquerdo, ao lado dos ícones da fase
        Image fundo = InterfaceUtil.Caixa(transform, "BotaoOuvir", new Color(0.12f, 0.16f, 0.32f, 0.9f));
        fundo.raycastTarget = true;
        // À direita dos ícones da fase (casinha, engrenagem e lâmpada) e antes do indicador da fase
        InterfaceUtil.Fixar(fundo.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(390f, -12f), new Vector2(190f, 66f));
        Button b = fundo.gameObject.AddComponent<Button>();
        b.targetGraphic = fundo;
        b.onClick.AddListener(() =>
        {
            if (gm == null) gm = FindFirstObjectByType<GameManager>();
            if (gm != null) LeitorDeVoz.Falar(gm.TextoParaLeitura);
        });

        Action a = FindFirstObjectByType<Action>();
        Sprite iconeSom = a != null ? a.soundOnSprite : null;
        if (iconeSom != null)
        {
            RectTransform rtIcone = InterfaceUtil.NovoRect("Icone", fundo.transform);
            Image icone = rtIcone.gameObject.AddComponent<Image>();
            icone.sprite = iconeSom;
            icone.preserveAspect = true;
            icone.color = Color.white;
            icone.raycastTarget = false;
            InterfaceUtil.Fixar(rtIcone, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(50f, 50f));
        }
        TextMeshProUGUI rotulo = InterfaceUtil.Texto(fundo.transform, "OUVIR", 30, Color.white, TextAlignmentOptions.Center, true);
        InterfaceUtil.Posicionar(rotulo.rectTransform, Vector2.zero, Vector2.one, new Vector2(iconeSom != null ? 66 : 8, 0), new Vector2(-8, 0));
        botaoOuvir = fundo.gameObject;
        botaoOuvir.SetActive(false);

        // Caixa de dica na faixa de baixo da tela (sobre as mãos e a borda das cartas): não cobre os personagens
        // nem o texto das cartas; os cliques passam por ela
        Image caixa = InterfaceUtil.Caixa(transform, "Dica", new Color(0.99f, 0.95f, 0.78f, 0.97f));
        InterfaceUtil.Fixar(caixa.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(1240f, 104f));
        Outline contorno = caixa.gameObject.AddComponent<Outline>();
        contorno.effectColor = new Color(0.7f, 0.45f, 0.05f, 0.9f);
        contorno.effectDistance = new Vector2(3f, -3f);
        dicaGrupo = caixa.gameObject.AddComponent<CanvasGroup>();
        dicaGrupo.blocksRaycasts = false;
        txtDica = InterfaceUtil.Texto(caixa.transform, "", 30, InterfaceUtil.Hex("#422006"), TextAlignmentOptions.Center);
        txtDica.enableAutoSizing = true;
        txtDica.fontSizeMin = 20;
        txtDica.fontSizeMax = 30;
        InterfaceUtil.Esticar(txtDica.rectTransform, 24, 8, 24, 8);
        dica = caixa.gameObject;
        dica.SetActive(false);
    }
}
