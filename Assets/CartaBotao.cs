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
            if (letra != null && letra.Length > 5)
            {
                textoDaCarta.enableAutoSizing = true;
                textoDaCarta.fontSizeMin = 14f;
                textoDaCarta.fontSizeMax = 34f;
            }
            else if (textoDaCarta.enableAutoSizing && letra != null && letra.Length <= 5)
            {
                textoDaCarta.enableAutoSizing = false;
                textoDaCarta.fontSize = 42f;
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
        if (gameManager != null && textoDaCarta != null)
        {
            Debug.Log($"[INVESTIGAÇÃO] Você clicou no botão: '{gameObject.name}' | Do Canvas pai: '{GetComponentInParent<Canvas>().gameObject.name}' | E o texto dele é: '{textoDaCarta.text}'");

            gameManager.VerificarResposta(textoDaCarta.text);
        }
    }
}