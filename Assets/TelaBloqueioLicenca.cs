using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tela que cobre o jogo quando a licença não permite jogar (contrato vencido, escola desativada,
/// computador bloqueado, limite de computadores ou muito tempo sem validar pela internet).
/// Pausa o jogo enquanto estiver visível.
/// </summary>
public class TelaBloqueioLicenca : MonoBehaviour
{
    private static TelaBloqueioLicenca _instancia;

    private GameObject raiz;
    private TextMeshProUGUI txtTitulo;
    private TextMeshProUGUI txtMensagem;
    private TextMeshProUGUI txtDetalhes;
    private TextMeshProUGUI txtRetorno;
    private GameObject caixaToken;
    private TMP_InputField inputToken;
    private TMP_FontAsset fonte;
    private Sprite spriteRedondo;

    private float escalaTempoAnterior = 1f;
    private bool pausado;

    public static bool Visivel => _instancia != null && _instancia.raiz != null && _instancia.raiz.activeSelf;

    public static void Mostrar(string titulo, string mensagem, string detalhes, bool permitirTrocarToken)
    {
        if (_instancia == null)
        {
            GameObject go = new GameObject("[TelaBloqueioLicenca]");
            DontDestroyOnLoad(go);
            _instancia = go.AddComponent<TelaBloqueioLicenca>();
            _instancia.Construir();
        }
        _instancia.Exibir(titulo, mensagem, detalhes, permitirTrocarToken);
    }

    public static void Esconder()
    {
        if (_instancia != null) _instancia.Ocultar();
    }

    public static void MostrarRetorno(string texto, bool erro)
    {
        if (_instancia == null || _instancia.txtRetorno == null) return;
        _instancia.txtRetorno.text = texto;
        _instancia.txtRetorno.color = erro ? new Color(0.86f, 0.15f, 0.15f) : new Color(0.2f, 0.45f, 0.25f);
    }

    private void Exibir(string titulo, string mensagem, string detalhes, bool permitirTrocarToken)
    {
        txtTitulo.text = titulo;
        txtMensagem.text = mensagem;
        txtDetalhes.text = detalhes;
        txtRetorno.text = "";
        caixaToken.SetActive(permitirTrocarToken);
        raiz.SetActive(true);

        if (!pausado)
        {
            escalaTempoAnterior = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            pausado = true;
        }
    }

    private void Ocultar()
    {
        if (raiz != null) raiz.SetActive(false);
        if (pausado)
        {
            Time.timeScale = escalaTempoAnterior;
            AudioListener.pause = false;
            pausado = false;
        }
    }

    private void Construir()
    {
        fonte = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset;
        spriteRedondo = GerarSpriteArredondado(64, 18);

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Acima de qualquer tela do jogo, inclusive o "Carregando" (32760); abaixo só das faixas de proporção (32767)
        canvas.sortingOrder = 32765;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        // Fundo que bloqueia todos os cliques
        RectTransform rtRaiz = NovoRect("Bloqueio", transform);
        Esticar(rtRaiz, 0, 0, 0, 0);
        Image fundo = rtRaiz.gameObject.AddComponent<Image>();
        fundo.color = new Color(0.06f, 0.09f, 0.16f, 0.94f);
        raiz = rtRaiz.gameObject;

        Image card = Caixa(rtRaiz, "Cartao", Color.white);
        RectTransform rtCard = card.rectTransform;
        rtCard.anchorMin = rtCard.anchorMax = new Vector2(0.5f, 0.5f);
        rtCard.sizeDelta = new Vector2(1100f, 700f);

        Image faixa = Caixa(card.transform, "Faixa", new Color(0.86f, 0.15f, 0.15f));
        Posicionar(faixa.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -16), Vector2.zero);

        txtTitulo = Texto(card.transform, "", 44, new Color(0.12f, 0.16f, 0.23f), TextAlignmentOptions.Center, true);
        Posicionar(txtTitulo.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(50, -120), new Vector2(-50, -40));

        txtMensagem = Texto(card.transform, "", 27, new Color(0.2f, 0.25f, 0.33f), TextAlignmentOptions.Top, false);
        Posicionar(txtMensagem.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(70, -300), new Vector2(-70, -135));

        txtDetalhes = Texto(card.transform, "", 19, new Color(0.39f, 0.45f, 0.55f), TextAlignmentOptions.Top, false);
        Posicionar(txtDetalhes.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(70, -380), new Vector2(-70, -305));

        // Trocar o token da escola (ex.: a escola recebeu um token novo)
        RectTransform rtToken = NovoRect("TrocarToken", card.transform);
        Posicionar(rtToken, new Vector2(0, 0), new Vector2(1, 0), new Vector2(70, 150), new Vector2(-70, 215));
        caixaToken = rtToken.gameObject;
        inputToken = CampoTexto(rtToken, "Novo token da escola (ex.: ESC-SP-1A2B3C)");
        Posicionar((RectTransform)inputToken.transform, new Vector2(0, 0), new Vector2(0.68f, 1), Vector2.zero, Vector2.zero);
        Button btnToken = Botao(rtToken, "Usar este token", new Color(0.31f, 0.27f, 0.9f), () =>
        {
            string token = inputToken.text.Trim();
            if (token.Length < 3)
            {
                MostrarRetorno("Digite o token da escola.", true);
                return;
            }
            MostrarRetorno("Verificando o token...", false);
            ServicoNuvem.Instancia.TrocarTokenPelaTelaDeBloqueio(token);
        });
        Posicionar((RectTransform)btnToken.transform, new Vector2(0.7f, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);

        // Botões
        Button tentar = Botao(card.transform, "Tentar novamente", new Color(0.06f, 0.6f, 0.4f), () =>
        {
            MostrarRetorno("Verificando com o servidor...", false);
            ServicoNuvem.Instancia.VerificarLicencaAgora();
        });
        Posicionar((RectTransform)tentar.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-370, 50), new Vector2(-20, 120));

        Button sair = Botao(card.transform, "Fechar o jogo", new Color(0.4f, 0.45f, 0.53f), () =>
        {
            ServicoNuvem.Instancia.EncerrarSessaoDaMaquina();
            Application.Quit();
        });
        Posicionar((RectTransform)sair.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(20, 50), new Vector2(370, 120));

        txtRetorno = Texto(card.transform, "", 19, new Color(0.2f, 0.45f, 0.25f), TextAlignmentOptions.Center, false);
        Posicionar(txtRetorno.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(50, 10), new Vector2(-50, 46));

        raiz.SetActive(false);
    }

    void OnDestroy()
    {
        if (pausado)
        {
            Time.timeScale = escalaTempoAnterior;
            AudioListener.pause = false;
        }
    }

    // ===== Componentes de interface =====

    private TMP_InputField CampoTexto(Transform pai, string dica)
    {
        Image img = Caixa(pai, "Campo", new Color(0.95f, 0.96f, 0.98f));
        TMP_InputField input = img.gameObject.AddComponent<TMP_InputField>();
        RectTransform area = NovoRect("Area", img.transform);
        Esticar(area, 18, 6, 18, 6);
        area.gameObject.AddComponent<RectMask2D>();
        TextMeshProUGUI ph = Texto(area, dica, 22, new Color(0.55f, 0.6f, 0.68f), TextAlignmentOptions.MidlineLeft, false);
        ph.fontStyle = FontStyles.Italic;
        Esticar(ph.rectTransform, 0, 0, 0, 0);
        TextMeshProUGUI txt = Texto(area, "", 22, new Color(0.12f, 0.16f, 0.23f), TextAlignmentOptions.MidlineLeft, false);
        Esticar(txt.rectTransform, 0, 0, 0, 0);
        input.textViewport = area;
        input.textComponent = txt;
        input.placeholder = ph;
        input.fontAsset = fonte;
        input.characterLimit = 40;
        return input;
    }

    private Button Botao(Transform pai, string texto, Color cor, System.Action acao)
    {
        Image img = Caixa(pai, "Botao_" + texto, cor);
        img.raycastTarget = true;
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        TextMeshProUGUI t = Texto(img.transform, texto, 24, Color.white, TextAlignmentOptions.Center, true);
        Esticar(t.rectTransform, 8, 0, 8, 0);
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
