using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    [Header("Telas do Tutorial")]
    public GameObject[] telasPortugues;
    public GameObject[] telasMatematica;

    [Header("Botão Pular")]
    public Button botaoPular;

    [Header("Configurações de Transição de Tela")]
    [Tooltip("Duração do Fade de transição entre as telas")]
    public float tempoTransicao = 0.35f;

    [Header("Animação Especial do 'Personagem'")]
    [Tooltip("Tempo do impacto elástico na troca de pose")]
    public float duracaoTrocaPose = 0.45f;
    [Tooltip("Força do impacto/deformação ao trocar de pose (X: alarga, Y: achata)")]
    public Vector3 forcaImpactoPose = new Vector3(0.25f, -0.2f, 0f);
    [Tooltip("Leve inclinação/rotação ao reagir com a nova pose")]
    public float inclinacaoPose = 8f;
    [Tooltip("Intensidade do pulinho/respiração contínua (Idle)")]
    public float forcaFlutuacaoPersonagem = 12f;
    [Tooltip("Ativar animação contínua (Idle) no Personagem")]
    public bool animacaoContinuaPersonagem = true;

    [Header("Animação dos Textos e Balões")]
    [Tooltip("Tempo de surgimento dos textos/balões")]
    public float tempoEntradaTexto = 0.3f;

    [Header("Áudio do Tutorial")]
    public AudioSource musicaSource;
    public AudioClip trilhaSonora;

    private GameObject[] tutorialAtual;
    private int cenaAtual = 0;
    private bool transicionando = false;
    private Action actionScript;

    private Dictionary<Transform, Vector3> posicoesOriginais = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Vector3> escalasOriginais = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Quaternion> rotacoesOriginais = new Dictionary<Transform, Quaternion>();

    void Start()
    {
        actionScript = FindFirstObjectByType<Action>();
        AutoVincularTelasTutorial();
        EsconderTelas(telasPortugues);
        EsconderTelas(telasMatematica);
    }

    private void AutoVincularTelasTutorial()
    {
        if (telasPortugues == null || telasPortugues.Length == 0)
        {
            List<GameObject> lista = new List<GameObject>();
            for (int i = 0; i <= 6; i++)
            {
                GameObject g = GameObject.Find($"Canvas_Tutorial_Portugues_{i}");
                if (g != null) lista.Add(g);
            }
            if (lista.Count > 0) telasPortugues = lista.ToArray();
        }

        if (telasMatematica == null || telasMatematica.Length == 0)
        {
            List<GameObject> lista = new List<GameObject>();
            for (int i = 0; i <= 6; i++)
            {
                GameObject g = GameObject.Find($"Canvas_Tutorial_Matematica_{i}") ?? GameObject.Find($"Canvas_Tutorial_Matematica_{i} ");
                if (g != null) lista.Add(g);
            }
            if (lista.Count > 0) telasMatematica = lista.ToArray();
        }

        AjustarTelasTutorial();
    }

    public void AjustarTelasTutorial()
    {
        List<GameObject> todas = new List<GameObject>();
        if (telasPortugues != null) todas.AddRange(telasPortugues);
        if (telasMatematica != null) todas.AddRange(telasMatematica);

        foreach (var tela in todas)
        {
            if (tela == null) continue;

            // Pivot mantido original (0,0)

            CanvasScaler cs = tela.GetComponent<CanvasScaler>();
            if (cs == null)
            {
                cs = tela.AddComponent<CanvasScaler>();
            }
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 0.5f;

            CanvasScaler[] subScalers = tela.GetComponentsInChildren<CanvasScaler>(true);
            foreach (var subScaler in subScalers)
            {
                if (subScaler.gameObject != tela)
                {
                    subScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    subScaler.scaleFactor = 1f;
                }
            }
        }
    }


    public void EsconderTelas(GameObject[] telas)
    {
        if (telas == null) return;
        foreach (var tela in telas)
        {
            if (tela != null)
            {
                CanvasGroup cg = ObterOuCriarCanvasGroup(tela);
                cg.alpha = 0;
                tela.SetActive(false);
            }
        }
    }

    private CanvasGroup ObterOuCriarCanvasGroup(GameObject obj)
    {
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null) cg = obj.AddComponent<CanvasGroup>();
        return cg;
    }

    public void IniciarTutorialPortugues()
    {
        tutorialAtual = telasPortugues;
        IniciarTutorial();
    }

    public void IniciarTutorialMatematica()
    {
        tutorialAtual = telasMatematica;
        IniciarTutorial();
    }

    private void IniciarTutorial()
    {
        if (tutorialAtual == null || tutorialAtual.Length == 0)
        {
            FinalizarTutorial();
            return;
        }

       
        if (musicaSource != null && trilhaSonora != null)
        {
            musicaSource.clip = trilhaSonora;
            musicaSource.loop = true;
            musicaSource.volume = PlayerPrefs.GetFloat("VolumeMusica", 0.5f);
            musicaSource.Play();
        }

        if (actionScript != null && actionScript.groupMenu != null)
        {
            actionScript.groupMenu.alpha = 0;
            actionScript.groupMenu.gameObject.SetActive(false);
        }

        cenaAtual = 0;
        transicionando = false;
        MostrarCenaComAnimacao(tutorialAtual[cenaAtual], false);
    }

    public void PularTutorial()
    {
        if (actionScript != null) actionScript.TocarSomClique();

        if (tutorialAtual != null && cenaAtual < tutorialAtual.Length && tutorialAtual[cenaAtual] != null)
        {
            PararAnimacoes(tutorialAtual[cenaAtual]);
            tutorialAtual[cenaAtual].SetActive(false);
        }

        EsconderTelas(telasPortugues);
        EsconderTelas(telasMatematica);

        transicionando = false;
        FinalizarTutorial();
    }

    public void AvancarCena()
    {
        if (tutorialAtual == null || transicionando) return;

      
        if (actionScript != null) actionScript.TocarSomClique();

        if (cenaAtual < tutorialAtual.Length - 1)
        {
            GameObject telaSaindo = tutorialAtual[cenaAtual];
            cenaAtual++;
            GameObject telaEntrando = tutorialAtual[cenaAtual];
            TrocarCenaComAnimacao(telaSaindo, telaEntrando);
        }
        else
        {
            GameObject telaSaindo = tutorialAtual[cenaAtual];
            FecharTutorialComAnimacao(telaSaindo);
        }
    }

    private void MostrarCenaComAnimacao(GameObject tela, bool isTrocaDePose)
    {
        if (tela == null) return;

        PararAnimacoes(tela);

        tela.SetActive(true);
        GarantirBotaoPular(tela);
        CanvasGroup cg = ObterOuCriarCanvasGroup(tela);
        cg.alpha = 0f;

        cg.DOFade(1f, tempoTransicao).SetEase(Ease.OutQuad);

        AnimarElementosDaTela(tela, isTrocaDePose);
    }

    private void TrocarCenaComAnimacao(GameObject telaSaindo, GameObject telaEntrando)
    {
        transicionando = true;
        CanvasGroup cgSaindo = ObterOuCriarCanvasGroup(telaSaindo);
        CanvasGroup cgEntrando = ObterOuCriarCanvasGroup(telaEntrando);

        PararAnimacoes(telaSaindo);

        cgSaindo.DOFade(0f, tempoTransicao * 0.6f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            telaSaindo.SetActive(false);

            telaEntrando.SetActive(true);
            GarantirBotaoPular(telaEntrando);
            cgEntrando.alpha = 0f;

            cgEntrando.DOFade(1f, tempoTransicao * 0.8f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                transicionando = false;
            });

            AnimarElementosDaTela(telaEntrando, true);
        });
    }

    private void FecharTutorialComAnimacao(GameObject telaSaindo)
    {
        transicionando = true;
        CanvasGroup cgSaindo = ObterOuCriarCanvasGroup(telaSaindo);
        PararAnimacoes(telaSaindo);

        cgSaindo.DOFade(0f, tempoTransicao * 0.6f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            telaSaindo.SetActive(false);
            transicionando = false;
            FinalizarTutorial();
        });
    }

    private void AnimarElementosDaTela(GameObject tela, bool isTrocaDePose)
    {
        Transform transformPersonagem = null;

        Transform[] todosFilhos = tela.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in todosFilhos)
        {
            if (t.name.ToLower().Contains("personagem"))
            {
                transformPersonagem = t;
                break;
            }
        }

        if (transformPersonagem != null)
        {
            RegistrarOriginais(transformPersonagem);
            transformPersonagem.DOKill();

            Vector3 escalaBase = escalasOriginais[transformPersonagem];
            Vector3 posBase = posicoesOriginais[transformPersonagem];
            Quaternion rotBase = rotacoesOriginais[transformPersonagem];

            transformPersonagem.localPosition = posBase;
            transformPersonagem.localRotation = rotBase;

            if (isTrocaDePose)
            {
                transformPersonagem.localScale = escalaBase;
                transformPersonagem.DOPunchScale(forcaImpactoPose, duracaoTrocaPose, 7, 0.4f);

                float anguloReacao = (Random.value > 0.5f ? 1 : -1) * inclinacaoPose;
                transformPersonagem.DOPunchRotation(new Vector3(0, 0, anguloReacao), duracaoTrocaPose, 6, 0.4f).OnComplete(() =>
                {
                    IniciarIdlePersonagem(transformPersonagem, posBase);
                });
            }
            else
            {
                transformPersonagem.localScale = Vector3.zero;
                transformPersonagem.localPosition = posBase - new Vector3(0, 40f, 0);

                transformPersonagem.DOScale(escalaBase, duracaoTrocaPose).SetEase(Ease.OutBack);
                transformPersonagem.DOLocalMoveY(posBase.y, duracaoTrocaPose).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    IniciarIdlePersonagem(transformPersonagem, posBase);
                });
            }
        }

        Image[] imagens = tela.GetComponentsInChildren<Image>(true);
        foreach (Image img in imagens)
        {
            if (img.gameObject == tela || (transformPersonagem != null && img.transform == transformPersonagem) || img.gameObject.name.ToLower().Contains("pular")) continue;

            Transform t = img.transform;
            RegistrarOriginais(t);

            t.DOKill();
            t.localScale = Vector3.zero;
            t.DOScale(escalasOriginais[t], tempoEntradaTexto).SetEase(Ease.OutBack).SetDelay(0.12f);
        }

        TextMeshProUGUI[] tmps = tela.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in tmps)
        {
            if (txt.gameObject.name.ToLower().Contains("pular") || (txt.transform.parent != null && txt.transform.parent.name.ToLower().Contains("pular"))) continue;
            Transform t = txt.transform;
            RegistrarOriginais(t);

            t.DOKill();
            t.localScale = Vector3.zero;
            t.DOScale(escalasOriginais[t], tempoEntradaTexto).SetEase(Ease.OutBack).SetDelay(0.2f);
        }

        Text[] textosLegados = tela.GetComponentsInChildren<Text>(true);
        foreach (var txt in textosLegados)
        {
            Transform t = txt.transform;
            RegistrarOriginais(t);

            t.DOKill();
            t.localScale = Vector3.zero;
            t.DOScale(escalasOriginais[t], tempoEntradaTexto).SetEase(Ease.OutBack).SetDelay(0.2f);
        }
    }

    private void IniciarIdlePersonagem(Transform t, Vector3 posBase)
    {
        if (animacaoContinuaPersonagem && t != null && t.gameObject.activeInHierarchy)
        {
            t.DOLocalMoveY(posBase.y + forcaFlutuacaoPersonagem, 1.3f)
             .SetLoops(-1, LoopType.Yoyo)
             .SetEase(Ease.InOutSine);
        }
    }

    private void RegistrarOriginais(Transform t)
    {
        if (!posicoesOriginais.ContainsKey(t)) posicoesOriginais[t] = t.localPosition;
        if (!escalasOriginais.ContainsKey(t)) escalasOriginais[t] = t.localScale == Vector3.zero ? Vector3.one : t.localScale;
        if (!rotacoesOriginais.ContainsKey(t)) rotacoesOriginais[t] = t.localRotation;
    }

    private void PararAnimacoes(GameObject tela)
    {
        if (tela == null) return;

        Transform[] todosFilhos = tela.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in todosFilhos)
        {
            t.DOKill();
            if (posicoesOriginais.ContainsKey(t)) t.localPosition = posicoesOriginais[t];
            if (escalasOriginais.ContainsKey(t)) t.localScale = escalasOriginais[t];
            if (rotacoesOriginais.ContainsKey(t)) t.localRotation = rotacoesOriginais[t];
        }
    }

    private void FinalizarTutorial()
    {
        if (musicaSource != null)
        {
            musicaSource.Stop();
        }

        if (actionScript != null)
        {
            CanvasGroup telaDestino;

            if (tutorialAtual == telasMatematica)
            {
                telaDestino = actionScript.groupDificuldadeMatematica;
                actionScript.telaDificuldadeAtual = actionScript.groupDificuldadeMatematica;
            }
            else
            {
                telaDestino = actionScript.groupDificuldadePortugues;
                actionScript.telaDificuldadeAtual = actionScript.groupDificuldadePortugues;
            }

            actionScript.groupMenu.gameObject.SetActive(true);
            actionScript.groupMenu.alpha = 0;
            actionScript.TrocarTela(actionScript.groupMenu, telaDestino);
            actionScript.VerificarDesbloqueios();
            actionScript.TocarMusicaPersonagens();
        }
    }

    private void GarantirBotaoPular(GameObject tela)
    {
        if (tela == null) return;

        Button[] botoes = tela.GetComponentsInChildren<Button>(true);
        foreach (var b in botoes)
        {
            if (b.name.ToLower().Contains("pular") || b.name.ToLower().Contains("skip"))
            {
                b.onClick.RemoveListener(PularTutorial);
                b.onClick.AddListener(PularTutorial);
                b.gameObject.SetActive(true);
                return;
            }
        }

        if (botaoPular != null)
        {
            botaoPular.onClick.RemoveListener(PularTutorial);
            botaoPular.onClick.AddListener(PularTutorial);
            botaoPular.gameObject.SetActive(true);
            return;
        }

        GameObject btnObj = new GameObject("BtnPular");
        btnObj.transform.SetParent(tela.transform, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-40f, -40f);
        rt.sizeDelta = new Vector2(160f, 55f);

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.12f, 0.45f, 0.85f, 0.95f);

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.2f, 0.6f, 1f, 1f);
        cb.pressedColor = new Color(0.08f, 0.35f, 0.7f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(PularTutorial);

        GameObject txtObj = new GameObject("TextoPular");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtTxt = txtObj.AddComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        TextMeshProUGUI tmpFonte = tela.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpFonte != null) tmp.font = tmpFonte.font;
        tmp.text = "PULAR >>";
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }
}
