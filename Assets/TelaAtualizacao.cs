using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aviso de atualização do jogo: mostra que há uma versão nova, o progresso do download
/// e, ao terminar, oferece "Reiniciar agora" (instala na hora) ou "Continuar jogando".
/// </summary>
public class TelaAtualizacao : MonoBehaviour
{
    private static TelaAtualizacao _instancia;

    private GameObject raiz;
    private TextMeshProUGUI txtTitulo;
    private TextMeshProUGUI txtNovidades;
    private TextMeshProUGUI txtStatus;
    private RectTransform barraFundo;
    private RectTransform barraPreenchimento;
    private GameObject areaBotoes;
    private Button btnPrincipal;
    private TextMeshProUGUI txtBtnPrincipal;
    private Button btnSecundario;
    private TextMeshProUGUI txtBtnSecundario;
    private TMP_FontAsset fonte;
    private Sprite spriteRedondo;
    private System.Action acaoPrincipal;
    private System.Action acaoSecundaria;

    private static readonly Color COR_PRIMARIA = new Color(0.31f, 0.27f, 0.9f);
    private static readonly Color COR_SUCESSO = new Color(0.06f, 0.6f, 0.4f);
    private static readonly Color COR_NEUTRA = new Color(0.4f, 0.45f, 0.53f);
    private static readonly Color COR_ERRO = new Color(0.86f, 0.15f, 0.15f);

    public static bool Visivel => _instancia != null && _instancia.raiz != null && _instancia.raiz.activeSelf;

    private static TelaAtualizacao Obter()
    {
        if (_instancia == null)
        {
            GameObject go = new GameObject("[TelaAtualizacao]");
            DontDestroyOnLoad(go);
            _instancia = go.AddComponent<TelaAtualizacao>();
            _instancia.Construir();
        }
        return _instancia;
    }

    /// <summary>Versão nova encontrada: mostra o download começando.</summary>
    public static void MostrarBaixando(string versaoNova, string versaoAtual, string novidades, long tamanhoBytes)
    {
        TelaAtualizacao t = Obter();
        t.txtTitulo.text = "Nova versão disponível!";
        t.txtNovidades.text =
            $"<b>Versão {versaoNova}</b>  <color=#64748B>(você está na {versaoAtual})</color>\n" +
            (string.IsNullOrEmpty(novidades) ? "" : $"<size=22><color=#475569>{novidades}</color></size>");
        t.barraFundo.gameObject.SetActive(true);
        t.areaBotoes.SetActive(false);
        t.AjustarProgresso(0f);
        t.txtStatus.color = new Color(0.2f, 0.25f, 0.33f);
        t.txtStatus.text = "Baixando a atualização... aguarde um instante.";
        t.raiz.SetActive(true);
    }

    public static void AtualizarProgresso(float progresso01, long baixados, long total)
    {
        if (!Visivel) return;
        float p = Mathf.Clamp01(progresso01);
        _instancia.AjustarProgresso(p);
        string tamanho = total > 0 ? $" ({FormatarKb(baixados)} de {FormatarKb(total)})" : "";
        _instancia.txtStatus.text = $"Baixando a atualização... {Mathf.RoundToInt(p * 100)}%{tamanho}";
    }

    /// <summary>Download concluído (ou baixado numa vez anterior): oferece reiniciar para instalar.</summary>
    public static void MostrarPronta(string versaoNova, System.Action reiniciar, System.Action continuar)
    {
        TelaAtualizacao t = Obter();
        t.txtTitulo.text = "Atualização pronta!";
        if (string.IsNullOrEmpty(t.txtNovidades.text))
            t.txtNovidades.text = $"<b>Versão {versaoNova}</b>";
        t.barraFundo.gameObject.SetActive(true);
        t.AjustarProgresso(1f);
        t.txtStatus.color = new Color(0.2f, 0.45f, 0.25f);
        t.txtStatus.text = "Clique em \"Reiniciar agora\" para instalar (leva poucos segundos).\n" +
                           "<size=20><color=#64748B>Se preferir continuar, ela será instalada quando você sair pelo botão de sair do jogo.</color></size>";
        t.ConfigurarBotoes("Reiniciar agora", COR_SUCESSO, reiniciar, "Continuar jogando", COR_NEUTRA, continuar);
        t.raiz.SetActive(true);
    }

    public static void MostrarErro(string mensagem)
    {
        TelaAtualizacao t = Obter();
        t.txtTitulo.text = "Não foi possível atualizar agora";
        t.barraFundo.gameObject.SetActive(false);
        t.txtStatus.color = COR_ERRO;
        t.txtStatus.text = mensagem + "\n<size=20><color=#64748B>O jogo continua normalmente e tenta de novo na próxima vez que for aberto.</color></size>";
        t.ConfigurarBotoes("Continuar", COR_PRIMARIA, Esconder, null, COR_NEUTRA, null);
        t.raiz.SetActive(true);
    }

    public static void Esconder()
    {
        if (_instancia != null && _instancia.raiz != null) _instancia.raiz.SetActive(false);
    }

    private void ConfigurarBotoes(string textoPrincipal, Color corPrincipal, System.Action principal,
                                  string textoSecundario, Color corSecundaria, System.Action secundaria)
    {
        acaoPrincipal = principal;
        acaoSecundaria = secundaria;
        txtBtnPrincipal.text = textoPrincipal;
        btnPrincipal.GetComponent<Image>().color = corPrincipal;

        bool temSecundario = !string.IsNullOrEmpty(textoSecundario);
        btnSecundario.gameObject.SetActive(temSecundario);
        if (temSecundario)
        {
            txtBtnSecundario.text = textoSecundario;
            btnSecundario.GetComponent<Image>().color = corSecundaria;
        }

        RectTransform rtP = (RectTransform)btnPrincipal.transform;
        rtP.anchorMin = rtP.anchorMax = new Vector2(0.5f, 0.5f);
        rtP.sizeDelta = new Vector2(340f, 72f);
        rtP.anchoredPosition = temSecundario ? new Vector2(-185f, 0f) : Vector2.zero;
        RectTransform rtS = (RectTransform)btnSecundario.transform;
        rtS.anchorMin = rtS.anchorMax = new Vector2(0.5f, 0.5f);
        rtS.sizeDelta = new Vector2(340f, 72f);
        rtS.anchoredPosition = new Vector2(185f, 0f);

        areaBotoes.SetActive(true);
    }

    private void AjustarProgresso(float p)
    {
        barraPreenchimento.anchorMax = new Vector2(Mathf.Max(0.02f, p), 1f);
    }

    private static string FormatarKb(long bytes) => bytes >= 1024 * 1024
        ? $"{bytes / (1024f * 1024f):0.0} MB"
        : $"{Mathf.Max(1, bytes / 1024)} KB";

    private void Construir()
    {
        fonte = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset;
        spriteRedondo = GerarSpriteArredondado(64, 18);

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Acima do "Carregando" (32760) e abaixo da tela de bloqueio da licença (32765)
        canvas.sortingOrder = 32762;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        RectTransform rtRaiz = NovoRect("Atualizacao", transform);
        Esticar(rtRaiz, 0, 0, 0, 0);
        Image fundo = rtRaiz.gameObject.AddComponent<Image>();
        fundo.color = new Color(0.06f, 0.09f, 0.16f, 0.93f); // também bloqueia cliques enquanto baixa
        raiz = rtRaiz.gameObject;

        Image card = Caixa(rtRaiz, "Cartao", Color.white);
        card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        card.rectTransform.sizeDelta = new Vector2(1000f, 600f);

        Image faixa = Caixa(card.transform, "Faixa", COR_PRIMARIA);
        Posicionar(faixa.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -16), Vector2.zero);

        txtTitulo = Texto(card.transform, "", 44, new Color(0.12f, 0.16f, 0.23f), TextAlignmentOptions.Center, true);
        Posicionar(txtTitulo.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(50, -120), new Vector2(-50, -40));

        txtNovidades = Texto(card.transform, "", 28, new Color(0.2f, 0.25f, 0.33f), TextAlignmentOptions.Top, false);
        Posicionar(txtNovidades.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(70, -290), new Vector2(-70, -130));

        barraFundo = Caixa(card.transform, "Barra", new Color(0.89f, 0.91f, 0.94f)).rectTransform;
        Posicionar(barraFundo, new Vector2(0, 1), new Vector2(1, 1), new Vector2(90, -336), new Vector2(-90, -306));
        Image preenchimento = Caixa(barraFundo, "Preenchimento", COR_PRIMARIA);
        barraPreenchimento = preenchimento.rectTransform;
        barraPreenchimento.anchorMin = Vector2.zero;
        barraPreenchimento.anchorMax = new Vector2(0.02f, 1f);
        barraPreenchimento.offsetMin = Vector2.zero;
        barraPreenchimento.offsetMax = Vector2.zero;

        txtStatus = Texto(card.transform, "", 24, new Color(0.2f, 0.25f, 0.33f), TextAlignmentOptions.Top, false);
        Posicionar(txtStatus.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(60, -460), new Vector2(-60, -352));

        RectTransform rtBotoes = NovoRect("Botoes", card.transform);
        Posicionar(rtBotoes, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 30), new Vector2(0, 120));
        areaBotoes = rtBotoes.gameObject;
        btnPrincipal = Botao(rtBotoes, "Principal", COR_SUCESSO, () => acaoPrincipal?.Invoke(), out txtBtnPrincipal);
        btnSecundario = Botao(rtBotoes, "Secundario", COR_NEUTRA, () => acaoSecundaria?.Invoke(), out txtBtnSecundario);
        areaBotoes.SetActive(false);

        raiz.SetActive(false);
    }

    // ===== Componentes de interface =====

    private Button Botao(Transform pai, string nome, Color cor, System.Action acao, out TextMeshProUGUI texto)
    {
        Image img = Caixa(pai, "Botao_" + nome, cor);
        img.raycastTarget = true;
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        texto = Texto(img.transform, "", 26, Color.white, TextAlignmentOptions.Center, true);
        Esticar(texto.rectTransform, 8, 0, 8, 0);
        b.onClick.AddListener(() => acao());
        return b;
    }

    private Image Caixa(Transform pai, string nome, Color cor)
    {
        RectTransform rt = NovoRect(nome, pai);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = spriteRedondo;
        img.type = Image.Type.Sliced;
        img.color = cor;
        img.raycastTarget = false;
        return img;
    }

    private TextMeshProUGUI Texto(Transform pai, string texto, float tamanho, Color cor, TextAlignmentOptions alinhamento, bool negrito)
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
        return t;
    }

    private static RectTransform NovoRect(string nome, Transform pai)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        return (RectTransform)go.transform;
    }

    private static void Esticar(RectTransform rt, float esq, float baixo, float dir, float cima)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(esq, baixo);
        rt.offsetMax = new Vector2(-dir, -cima);
    }

    private static void Posicionar(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = oMin;
        rt.offsetMax = oMax;
    }

    private static Sprite GerarSpriteArredondado(int tamanho, int raio)
    {
        Texture2D tex = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[tamanho * tamanho];
        for (int y = 0; y < tamanho; y++)
        {
            for (int x = 0; x < tamanho; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, raio, tamanho - raio);
                float cy = Mathf.Clamp(y + 0.5f, raio, tamanho - raio);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                px[y * tamanho + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(raio - d + 0.5f) * 255f));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, tamanho, tamanho), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(raio, raio, raio, raio));
    }
}
