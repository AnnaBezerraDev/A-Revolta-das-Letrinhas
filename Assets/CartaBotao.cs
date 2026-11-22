using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CartaBotao : MonoBehaviour
{
    [Header("Arraste os componentes aqui:")]
    public TextMeshProUGUI textoDaCarta;
    public Button botaoComponente;
    public Image imagemDoBotao;

    private GameManager gameManager;

    void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        if (textoDaCarta == null) textoDaCarta = GetComponentInChildren<TextMeshProUGUI>();
        if (botaoComponente == null) botaoComponente = GetComponent<Button>();
        if (imagemDoBotao == null) imagemDoBotao = GetComponent<Image>();

        if (botaoComponente != null)
        {
            botaoComponente.onClick.RemoveAllListeners();
           
            botaoComponente.onClick.AddListener(ClicarNaCarta);
        }
    }

    public void DefinirLetra(string letra)
    {
        if (textoDaCarta != null)
        {
            textoDaCarta.text = letra;
            textoDaCarta.enableAutoSizing = true;
            textoDaCarta.fontSizeMin = 14f;

            if (letra != null && letra.Length > 6)
            {
                textoDaCarta.fontSizeMax = 28f;
            }
            else if (letra != null && letra.Length > 3)
            {
                textoDaCarta.fontSizeMax = 34f;
            }
            else
            {
                textoDaCarta.fontSizeMax = 42f;
            }
        }
    }

    public void AlterarVisual(Sprite novoSprite)
    {
        if (imagemDoBotao != null && novoSprite != null)
        {
            imagemDoBotao.sprite = novoSprite;
        }
    }

    
    public void ClicarNaCarta()
    {
        if (gameManager == null) gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();

        if (gameManager != null && textoDaCarta != null)
        {
            if (!gameManager.PodeInteragir)
            {
                Debug.Log($"[CartaBotao] Clique ignorado pois a interação ainda está bloqueada.");
                return;
            }

            if (Time.time - gameManager.TempoUltimoClique < 0.35f)
            {
                Debug.Log($"[CartaBotao] Clique ignorado por debounce de tempo.");
                return;
            }

            Debug.Log($"[INVESTIGAÇÃO] Você clicou no botão: '{gameObject.name}' | Do Canvas pai: '{GetComponentInParent<Canvas>().gameObject.name}' | E o texto dele é: '{textoDaCarta.text}'");

            gameManager.VerificarResposta(textoDaCarta.text);
        }
    }
}