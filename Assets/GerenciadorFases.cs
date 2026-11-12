using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GerenciadorFases : MonoBehaviour
{
    [Header("Configuração do Nível")]
    public int nivelAtual = 0;

    [Header("Referência para Botões (Português)")]
    public List<Button> botoesFaseFacil = new List<Button>();
    public List<Button> botoesFaseMedio = new List<Button>();
    public List<Button> botoesFaseDificil = new List<Button>();

    [Header("Referência para Botões (Matemática)")]
    public List<Button> botoesFaseMathFacil = new List<Button>();
    public List<Button> botoesFaseMathMedio = new List<Button>();
    public List<Button> botoesFaseMathDificil = new List<Button>();

    private List<Button> botoesFaseAtivos = new List<Button>();

    [Header("Sistema de Troféus")]
    public GameObject[] iconesTrofeus;
    public Color corFaseBloqueada = new Color(0.28f, 0.28f, 0.28f, 0.45f);
    public Color corFaseDisponivel = Color.white;
    public Color corFaseCompleta = Color.white;

    [Header("Sprites de Indicadores")]
    public Sprite spriteCadeado;
    public Sprite spriteCheckVerde;
    public Sprite spriteBordaAtual;

    public int faseAtualJogando = -1;

    void OnEnable()
    {
        GerenciadorPerfis.OnPerfilAlterado -= AoMudarPerfil;
        GerenciadorPerfis.OnPerfilAlterado += AoMudarPerfil;
    }

    void OnDisable()
    {
        GerenciadorPerfis.OnPerfilAlterado -= AoMudarPerfil;
    }

    void AoMudarPerfil()
    {
        DefinirNivel(nivelAtual);
    }

    void Start()
    {
        AutoVincularBotoesSeVazio();
        DefinirNivel(nivelAtual);
    }

    private void AutoVincularBotoesSeVazio()
    {
        Action act = FindFirstObjectByType<Action>();
        if (botoesFaseFacil == null || botoesFaseFacil.Count == 0)
            botoesFaseFacil = ObterBotoesDeCanvas(act != null ? act.groupFasesFacil : null, "Canvas_Facil");
        if (botoesFaseMedio == null || botoesFaseMedio.Count == 0)
            botoesFaseMedio = ObterBotoesDeCanvas(act != null ? act.groupFasesMedio : null, "Canvas_Medio");
        if (botoesFaseDificil == null || botoesFaseDificil.Count == 0)
            botoesFaseDificil = ObterBotoesDeCanvas(act != null ? act.groupFasesDificil : null, "Canvas_Dificil");

        if (botoesFaseMathFacil == null || botoesFaseMathFacil.Count == 0)
            botoesFaseMathFacil = ObterBotoesDeCanvas(act != null ? act.groupFasesMathFacil : null, "Canvas_Facil_Matematica");
        if (botoesFaseMathMedio == null || botoesFaseMathMedio.Count == 0)
            botoesFaseMathMedio = ObterBotoesDeCanvas(act != null ? act.groupFasesMathMedio : null, "Canvas_Medio_Matematica");
        if (botoesFaseMathDificil == null || botoesFaseMathDificil.Count == 0)
            botoesFaseMathDificil = ObterBotoesDeCanvas(act != null ? act.groupFasesMathDificil : null, "Canvas_Dificil_Matematica");
    }

    private List<Button> ObterBotoesDeCanvas(CanvasGroup cg, string nomeObjetoFallback)
    {
        List<Button> lista = new List<Button>();
        Transform root = null;
        if (cg != null) root = cg.transform;
        else
        {
            GameObject g = GameObject.Find(nomeObjetoFallback);
            if (g != null) root = g.transform;
        }

        if (root == null) return lista;

        Transform tMenu = root.Find("Menu_Fases");
        if (tMenu == null)
        {
            foreach (Transform c in root)
            {
                if (c.name.Contains("Menu_Fases") || c.name.Contains("Fases")) { tMenu = c; break; }
            }
        }

        if (tMenu != null)
        {
            foreach (Transform c in tMenu)
            {
                Button b = c.GetComponent<Button>();
                if (b != null) lista.Add(b);
            }
        }
        else
        {
            Button[] todos = root.GetComponentsInChildren<Button>(true);
            foreach (Button b in todos)
            {
                if (!b.name.ToLower().Contains("voltar") && !b.name.ToLower().Contains("menu"))
                    lista.Add(b);
            }
        }
        return lista;
    }

    private string GetPrefixoSave()
    {
        Action actionScript = FindFirstObjectByType<Action>();
        bool isMath = (actionScript != null && actionScript.telaDificuldadeAtual == actionScript.groupDificuldadeMatematica);
        return isMath ? "Math" : "Port";
    }

    public void DefinirNivel(int nivel)
    {
        nivelAtual = nivel;

        Action actionScript = FindFirstObjectByType<Action>();
        bool isMath = (actionScript != null && actionScript.telaDificuldadeAtual == actionScript.groupDificuldadeMatematica);

        if (isMath)
        {
            if (nivel == 0) botoesFaseAtivos = botoesFaseMathFacil;
            else if (nivel == 1) botoesFaseAtivos = botoesFaseMathMedio;
            else if (nivel == 2) botoesFaseAtivos = botoesFaseMathDificil;
        }
        else
        {
            if (nivel == 0) botoesFaseAtivos = botoesFaseFacil;
            else if (nivel == 1) botoesFaseAtivos = botoesFaseMedio;
            else if (nivel == 2) botoesFaseAtivos = botoesFaseDificil;
        }

        ConfigurarTodosBotoes();
        AtualizarTrofeus();
    }

    void AtualizarTrofeus()
    {
        int fasesCompletas = ObterFasesCompletas(nivelAtual);
        int qtdTrofeus = 0;
        if (fasesCompletas >= 30) qtdTrofeus = 3;
        else if (fasesCompletas >= 20) qtdTrofeus = 2;
        else if (fasesCompletas >= 10) qtdTrofeus = 1;

        for (int i = 0; i < iconesTrofeus.Length; i++)
        {
            if (iconesTrofeus[i] != null) iconesTrofeus[i].SetActive(i < qtdTrofeus);
        }
    }

    void GarantirFase1Completa()
    {
        
    }

    void ConfigurarTodosBotoes()
    {
        if (botoesFaseAtivos == null || botoesFaseAtivos.Count == 0) return;

        if (spriteCadeado == null) spriteCadeado = Resources.Load<Sprite>("cadeado");
        if (spriteCheckVerde == null) spriteCheckVerde = Resources.Load<Sprite>("check_verde");
        if (spriteBordaAtual == null) spriteBordaAtual = Resources.Load<Sprite>("borda_atual");

        int totalFases = Mathf.Min(botoesFaseAtivos.Count, 30);
        int faseAtualIndex = -1;
        int fasesCompletasContagem = 0;

        for (int i = 0; i < totalFases; i++)
        {
            bool comp = VerificarFaseCompleta(i);
            if (comp) fasesCompletasContagem++;
            else if (faseAtualIndex == -1 && VerificarFaseDisponivel(i))
            {
                faseAtualIndex = i;
            }
        }
        if (faseAtualIndex == -1) faseAtualIndex = totalFases - 1;

        for (int i = 0; i < totalFases; i++)
        {
            bool isAtual = (i == faseAtualIndex);
            ConfigurarBotaoFase(i, isAtual);
        }

        AtualizarBannerTelaFases(GetTelaFases(), faseAtualIndex + 1, fasesCompletasContagem);
    }

    void ConfigurarBotaoFase(int indiceFase, bool isFaseAtual = false)
    {
        if (indiceFase >= botoesFaseAtivos.Count || botoesFaseAtivos[indiceFase] == null) return;

        Button botao = botoesFaseAtivos[indiceFase];
        bool faseDisponivel = VerificarFaseDisponivel(indiceFase);
        bool faseCompleta = VerificarFaseCompleta(indiceFase);

        botao.interactable = faseDisponivel;

      
        SpriteRenderer sr = botao.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (faseCompleta)
            {
                sr.color = Color.white;
            }
            else if (!faseDisponivel)
            {
                sr.color = corFaseBloqueada; 
            }
            else
            {
                sr.color = Color.white;
            }
        }

        Image imagem = botao.GetComponent<Image>();
        if (imagem != null && imagem.sprite != null)
        {
            if (faseCompleta) imagem.color = Color.white;
            else if (!faseDisponivel) imagem.color = corFaseBloqueada;
            else imagem.color = Color.white;
        }

        AtualizarBadgesVisuaisFase(botao, faseCompleta, faseDisponivel, isFaseAtual);

        botao.onClick.RemoveAllListeners();
        int faseIndex = indiceFase;
        botao.onClick.AddListener(() => {
            IniciarFaseEspecifica(faseIndex);

            Action actionScript = FindFirstObjectByType<Action>();
            if (actionScript != null)
            {
                actionScript.TrocarTela(GetTelaFases(), GetTelaJogo());
            }
        });
    }

    void AtualizarBadgesVisuaisFase(Button botao, bool faseCompleta, bool faseDisponivel, bool isFaseAtual)
    {
        if (botao == null) return;

      
        Transform tCadeado = botao.transform.Find("BadgeCadeado");
        if (!faseDisponivel)
        {
            if (tCadeado == null)
            {
                GameObject g = new GameObject("BadgeCadeado");
                g.transform.SetParent(botao.transform, false);
                RectTransform rt = g.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(1.8f, 1.8f);

                Image img = g.AddComponent<Image>();
                img.sprite = spriteCadeado;
                img.raycastTarget = false;
            }
            else
            {
                tCadeado.gameObject.SetActive(true);
            }
        }
        else
        {
            if (tCadeado != null) tCadeado.gameObject.SetActive(false);
        }

       
        Transform tCheck = botao.transform.Find("BadgeCheck");
        if (faseCompleta)
        {
            if (tCheck == null)
            {
                GameObject g = new GameObject("BadgeCheck");
                g.transform.SetParent(botao.transform, false);
                RectTransform rt = g.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0.7f, 0.7f);
                rt.sizeDelta = new Vector2(1.1f, 1.1f);

                Image img = g.AddComponent<Image>();
                img.sprite = spriteCheckVerde;
                img.raycastTarget = false;
            }
            else
            {
                tCheck.gameObject.SetActive(true);
            }
        }
        else
        {
            if (tCheck != null) tCheck.gameObject.SetActive(false);
        }

      
        Transform tAtual = botao.transform.Find("BadgeAtual");
        if (isFaseAtual && !faseCompleta)
        {
            if (tAtual == null)
            {
                GameObject g = new GameObject("BadgeAtual");
                g.transform.SetParent(botao.transform, false);
                RectTransform rt = g.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(2.5f, 2.5f);

                Image img = g.AddComponent<Image>();
                img.sprite = spriteBordaAtual;
                img.raycastTarget = false;
            }
            else
            {
                tAtual.gameObject.SetActive(true);
            }
        }
        else
        {
            if (tAtual != null) tAtual.gameObject.SetActive(false);
        }

      
        Transform tAntigo = botao.transform.Find("BadgeStatus");
        if (tAntigo != null) tAntigo.gameObject.SetActive(false);
    }

    void AtualizarBannerTelaFases(CanvasGroup canvasFases, int numeroFaseAtual, int completas)
    {
        if (canvasFases == null) return;

        Transform tBanner = canvasFases.transform.Find("BannerIndicadorFase");
        GameObject gBanner;
        TextMeshProUGUI tmp;

        if (tBanner == null)
        {
            gBanner = new GameObject("BannerIndicadorFase");
            gBanner.transform.SetParent(canvasFases.transform, false);

            RectTransform rt = gBanner.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -40f);
            rt.sizeDelta = new Vector2(800f, 60f);

            tmp = gBanner.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 32;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }
        else
        {
            gBanner = tBanner.gameObject;
            tmp = gBanner.GetComponent<TextMeshProUGUI>();
        }

        string materia = GetPrefixoSave() == "Math" ? "MATEMTICA" : "PORTUGUS";
        string nivelStr = "FCIL";
        if (nivelAtual == 1) nivelStr = "MDIO";
        else if (nivelAtual == 2) nivelStr = "DIFCIL";

        if (tmp != null)
        {
            tmp.text = $"{materia} • {nivelStr}  |  FASE ATUAL: {numeroFaseAtual}  ({completas}/30 CONCLUDAS)";
            if (nivelAtual == 0) tmp.color = new Color(1f, 0.9f, 0.2f, 1f); 
            else if (nivelAtual == 1) tmp.color = new Color(0.3f, 0.85f, 1f, 1f); 
            else tmp.color = new Color(1f, 0.4f, 0.6f, 1f); 
        }
    }

    bool VerificarFaseDisponivel(int numeroFase)
    {
        if (numeroFase == 0) return true;
        return GerenciadorPerfis.GetInt($"FaseCompleta_{GetPrefixoSave()}_{nivelAtual}_{numeroFase - 1}", 0) == 1;
    }

    bool VerificarFaseCompleta(int numeroFase)
    {
        return GerenciadorPerfis.GetInt($"FaseCompleta_{GetPrefixoSave()}_{nivelAtual}_{numeroFase}", 0) == 1;
    }

    public int ObterUltimaFaseDesbloqueada(int nivel)
    {
        for (int i = 29; i >= 0; i--)
        {
            if (GerenciadorPerfis.GetInt($"FaseCompleta_{GetPrefixoSave()}_{nivel}_{i}", 0) == 1)
            {
                if (i < 29) return i + 1;
                return 29;
            }
        }
        return 0;
    }

    public void IniciarFaseDireto(int numeroFase)
    {
        faseAtualJogando = numeroFase;
        Action actionScript = FindFirstObjectByType<Action>();

       
        if (actionScript != null)
        {
            actionScript.TocarSomClique();
            actionScript.TocarMusicaPersonagens();
        }

        bool isMath = (actionScript != null && actionScript.telaDificuldadeAtual == actionScript.groupDificuldadeMatematica);

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null) gameManager.IniciarFaseEspecifica(numeroFase, nivelAtual, isMath);

        if (actionScript != null)
        {
            CanvasGroup telaJogo = GetTelaJogo();
            if (telaJogo != null) actionScript.TrocarTela(actionScript.telaDificuldadeAtual, telaJogo);
        }
    }

    public void IniciarFaseEspecifica(int numeroFase)
    {
        faseAtualJogando = numeroFase;
        Action actionScript = FindFirstObjectByType<Action>();

       
        if (actionScript != null)
        {
            actionScript.TocarSomClique();
            actionScript.TocarMusicaPersonagens();
        }

        bool isMath = (actionScript != null && actionScript.telaDificuldadeAtual == actionScript.groupDificuldadeMatematica);

        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null) gameManager.IniciarFaseEspecifica(numeroFase, nivelAtual, isMath);
    }

    public void MarcarFaseComoCompleta()
    {
        if (faseAtualJogando >= 0)
        {
            GerenciadorPerfis.SetInt($"FaseCompleta_{GetPrefixoSave()}_{nivelAtual}_{faseAtualJogando}", 1);
            PlayerPrefs.Save();

            ConfigurarTodosBotoes();

            AtualizarTrofeus();
        }
    }

    public int ObterFasesCompletas(int nivel)
    {
        int completas = 0;
        for (int i = 0; i < 30; i++)
        {
            if (GerenciadorPerfis.GetInt($"FaseCompleta_{GetPrefixoSave()}_{nivel}_{i}", 0) == 1) completas++;
        }
        return completas;
    }

    public void BotaoProximaFase()
    {
        Action actionScript = FindFirstObjectByType<Action>();

        if (actionScript != null)
        {
            actionScript.TocarSomClique();
            actionScript.TocarMusicaPersonagens();
        }

        CanvasGroup painelAtivo = actionScript.GetPainelConclusaoAtivo();

        if (faseAtualJogando == 29)
        {
            bool isMath = (actionScript.telaDificuldadeAtual == actionScript.groupDificuldadeMatematica);

            if (nivelAtual == 0)
            {
                GerenciadorPerfis.SetInt($"Desbloqueio_Medio_{GetPrefixoSave()}", 1);
                PlayerPrefs.Save();
                DefinirNivel(1);

                CanvasGroup telaDestino = isMath ? actionScript.groupFasesMathMedio : actionScript.groupFasesMedio;
                actionScript.TrocarTela(painelAtivo, telaDestino);
            }
            else if (nivelAtual == 1)
            {
                GerenciadorPerfis.SetInt($"Desbloqueio_Dificil_{GetPrefixoSave()}", 1);
                PlayerPrefs.Save();
                DefinirNivel(2);

                CanvasGroup telaDestino = isMath ? actionScript.groupFasesMathDificil : actionScript.groupFasesDificil;
                actionScript.TrocarTela(painelAtivo, telaDestino);
            }
            else if (nivelAtual == 2)
            {
                actionScript.TrocarTela(painelAtivo, actionScript.telaDificuldadeAtual);
            }
            return;
        }

        actionScript.TrocarTela(painelAtivo, GetTelaJogo());
        IniciarFaseEspecifica(faseAtualJogando + 1);
    }

    public void BotaoTentarNovamente()
    {
        Action actionScript = FindFirstObjectByType<Action>();

        if (actionScript != null)
        {
            actionScript.TocarSomClique();
            actionScript.TocarMusicaPersonagens();
        }

        actionScript.TrocarTela(actionScript.groupGameOver, GetTelaJogo());
        IniciarFaseEspecifica(faseAtualJogando);
    }

    public void BotaoSairParaFases()
    {
        Action actionScript = FindFirstObjectByType<Action>();

        if (actionScript != null)
        {
            actionScript.TocarSomClique();
            actionScript.TocarMusicaPersonagens();
        }

        CanvasGroup painelAtivo = actionScript.GetPainelConclusaoAtivo();
        CanvasGroup telaDestino = GetTelaFases();

        if (painelAtivo != null && painelAtivo.alpha > 0) actionScript.TrocarTela(painelAtivo, telaDestino);
        else actionScript.TrocarTela(actionScript.groupGameOver, telaDestino);

        DefinirNivel(nivelAtual);
    }

    CanvasGroup GetTelaJogo()
    {
        Action action = FindFirstObjectByType<Action>();
        if (action == null) return null;

        bool isMath = (action.telaDificuldadeAtual == action.groupDificuldadeMatematica);

        if (isMath)
        {
            switch (nivelAtual)
            {
                case 0: return action.groupJogoMathFacil;
                case 1: return action.groupJogoMathMedio;
                case 2: return action.groupJogoMathDificil;
                default: return null;
            }
        }
        else
        {
            switch (nivelAtual)
            {
                case 0: return action.groupJogoFacil;
                case 1: return action.groupJogoMedio;
                case 2: return action.groupJogoDificil;
                default: return null;
            }
        }
    }

    CanvasGroup GetTelaFases()
    {
        Action action = FindFirstObjectByType<Action>();
        if (action == null) return null;

        bool isMath = (action.telaDificuldadeAtual == action.groupDificuldadeMatematica);

        if (isMath)
        {
            switch (nivelAtual)
            {
                case 0: return action.groupFasesMathFacil;
                case 1: return action.groupFasesMathMedio;
                case 2: return action.groupFasesMathDificil;
                default: return null;
            }
        }
        else
        {
            switch (nivelAtual)
            {
                case 0: return action.groupFasesFacil;
                case 1: return action.groupFasesMedio;
                case 2: return action.groupFasesDificil;
                default: return null;
            }
        }
    }
}