using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Peças de interface criadas por código (as telas novas não dependem de arquivos de imagem,
/// por isso chegam às escolas pela atualização automática, que só troca o código do jogo).
/// </summary>
public static class InterfaceUtil
{
    private static Sprite _spriteArredondado;
    private static TMP_FontAsset _fonteJogo;
    private static TMP_FontAsset _fonteTexto;

    /// <summary>Fonte "de giz" do jogo (títulos e letras grandes).</summary>
    public static TMP_FontAsset FonteJogo
    {
        get
        {
            if (_fonteJogo == null) _fonteJogo = Resources.Load<TMP_FontAsset>("CrayonHandRegular2016Demo SDF") ?? FonteTexto;
            return _fonteJogo;
        }
    }

    /// <summary>Fonte de leitura fácil (textos longos).</summary>
    public static TMP_FontAsset FonteTexto
    {
        get
        {
            if (_fonteTexto == null) _fonteTexto = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF") ?? TMP_Settings.defaultFontAsset;
            return _fonteTexto;
        }
    }

    /// <summary>
    /// A fonte "de giz" do jogo não tem ponto, vírgula nem interrogação ("R$ 3,00" aparecia como "3 00").
    /// Coloca a fonte de leitura como reserva: só esses sinais que faltam passam a ser desenhados com ela.
    /// </summary>
    public static void GarantirFonteReserva()
    {
        TMP_FontAsset reserva = FonteTexto;
        if (reserva == null) return;
        foreach (TMP_FontAsset f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (f == null || f == reserva || f.characterLookupTable == null) continue;
            bool alterou = false;
            // Na fonte de demonstração esses sinais existem, mas o desenho está vazio (tamanho zero):
            // tirando-os da tabela da fonte, o TextMeshPro usa a fonte reserva só para eles.
            foreach (char c in ".,?;:!")
            {
                if (f.characterLookupTable.TryGetValue(c, out TMP_Character ch) && ch != null && ch.glyph != null &&
                    (ch.glyph.glyphRect.width == 0 || ch.glyph.glyphRect.height == 0))
                {
                    f.characterLookupTable.Remove(c);
                    alterou = true;
                }
            }
            if (!alterou) continue;
            if (f.fallbackFontAssetTable == null) f.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            if (!f.fallbackFontAssetTable.Contains(reserva)) f.fallbackFontAssetTable.Add(reserva);
            Debug.Log($"[Fonte] '{f.name}': pontuação vazia passa a usar a fonte reserva.");
        }
    }

    public static Sprite SpriteArredondado
    {
        get
        {
            if (_spriteArredondado == null) _spriteArredondado = GerarSpriteArredondado(64, 18);
            return _spriteArredondado;
        }
    }

    /// <summary>Canvas próprio (tela cheia, 1920x1080 de referência) em um objeto que sobrevive à troca de cena.</summary>
    public static GameObject CriarCanvas(string nome, int ordem)
    {
        GameObject go = new GameObject(nome);
        Object.DontDestroyOnLoad(go);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ordem;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    public static RectTransform NovoRect(string nome, Transform pai)
    {
        GameObject go = new GameObject(nome, typeof(RectTransform));
        go.transform.SetParent(pai, false);
        return (RectTransform)go.transform;
    }

    public static Image Caixa(Transform pai, string nome, Color cor)
    {
        RectTransform rt = NovoRect(nome, pai);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = SpriteArredondado;
        img.type = Image.Type.Sliced;
        img.color = cor;
        img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI Texto(Transform pai, string texto, float tamanho, Color cor, TextAlignmentOptions alinhamento, bool fonteDoJogo = false)
    {
        RectTransform rt = NovoRect("Texto", pai);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = fonteDoJogo ? FonteJogo : FonteTexto;
        t.text = texto;
        t.fontSize = tamanho;
        t.color = cor;
        t.alignment = alinhamento;
        t.raycastTarget = false;
        return t;
    }

    public static Button Botao(Transform pai, string nome, string texto, Color cor, float tamanhoTexto, System.Action acao, out TextMeshProUGUI rotulo)
    {
        Image img = Caixa(pai, "Botao_" + nome, cor);
        img.raycastTarget = true;
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        rotulo = Texto(img.transform, texto, tamanhoTexto, Color.white, TextAlignmentOptions.Center);
        rotulo.fontStyle = FontStyles.Bold;
        Esticar(rotulo.rectTransform, 8, 0, 8, 0);
        b.onClick.AddListener(() => acao?.Invoke());
        return b;
    }

    public static void Esticar(RectTransform rt, float esq, float baixo, float dir, float cima)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(esq, baixo);
        rt.offsetMax = new Vector2(-dir, -cima);
    }

    public static void Posicionar(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = oMin;
        rt.offsetMax = oMax;
    }

    /// <summary>Âncora num ponto da tela, com tamanho fixo.</summary>
    public static void Fixar(RectTransform rt, Vector2 ancora, Vector2 pivo, Vector2 posicao, Vector2 tamanho)
    {
        rt.anchorMin = rt.anchorMax = ancora;
        rt.pivot = pivo;
        rt.anchoredPosition = posicao;
        rt.sizeDelta = tamanho;
    }

    public static Color Hex(string hex)
    {
        return ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out Color c) ? c : Color.magenta;
    }

    /// <summary>Gameplay (alguma tela de fase) visível agora?</summary>
    public static bool EmGameplay(Action action)
    {
        if (action == null) return false;
        return Visivel(action.groupJogoFacil) || Visivel(action.groupJogoMedio) || Visivel(action.groupJogoDificil) ||
               Visivel(action.groupJogoMathFacil) || Visivel(action.groupJogoMathMedio) || Visivel(action.groupJogoMathDificil);
    }

    public static bool Visivel(CanvasGroup g) => g != null && g.gameObject.activeInHierarchy && g.alpha > 0.5f;

    private static Sprite _spriteEstrela;

    /// <summary>Estrela de 5 pontas desenhada por código (sem arquivo de imagem).</summary>
    public static Sprite SpriteEstrela
    {
        get
        {
            if (_spriteEstrela == null) _spriteEstrela = GerarSpriteEstrela(128);
            return _spriteEstrela;
        }
    }

    public static Image Estrela(Transform pai, Color cor, float tamanho)
    {
        RectTransform rt = NovoRect("Estrela", pai);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = SpriteEstrela;
        img.color = cor;
        img.preserveAspect = true;
        img.raycastTarget = false;
        rt.sizeDelta = new Vector2(tamanho, tamanho);
        return img;
    }

    private static Sprite GerarSpriteEstrela(int tamanho)
    {
        Vector2[] pontos = new Vector2[10];
        float centro = tamanho / 2f, raioExt = tamanho * 0.48f, raioInt = tamanho * 0.2f;
        for (int i = 0; i < 10; i++)
        {
            float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float r = i % 2 == 0 ? raioExt : raioInt;
            pontos[i] = new Vector2(centro + Mathf.Cos(ang) * r, centro + Mathf.Sin(ang) * r);
        }
        Texture2D tex = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] px = new Color32[tamanho * tamanho];
        for (int y = 0; y < tamanho; y++)
        {
            for (int x = 0; x < tamanho; x++)
            {
                // 4 amostras por pixel para suavizar a borda
                int dentro = 0;
                for (int s = 0; s < 4; s++)
                {
                    Vector2 p = new Vector2(x + 0.25f + 0.5f * (s % 2), y + 0.25f + 0.5f * (s / 2));
                    if (DentroDoPoligono(p, pontos)) dentro++;
                }
                px[y * tamanho + x] = new Color32(255, 255, 255, (byte)(dentro * 255 / 4));
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, tamanho, tamanho), new Vector2(0.5f, 0.5f), 100f);
    }

    private static bool DentroDoPoligono(Vector2 p, Vector2[] poli)
    {
        bool dentro = false;
        for (int i = 0, j = poli.Length - 1; i < poli.Length; j = i++)
        {
            if (((poli[i].y > p.y) != (poli[j].y > p.y)) &&
                (p.x < (poli[j].x - poli[i].x) * (p.y - poli[i].y) / (poli[j].y - poli[i].y) + poli[i].x))
                dentro = !dentro;
        }
        return dentro;
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
