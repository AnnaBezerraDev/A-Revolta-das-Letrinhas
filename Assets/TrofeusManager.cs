using UnityEngine;
using UnityEngine.UI;

public class TrofeusManager : MonoBehaviour
{
    [System.Serializable]
    public class SlotTrofeu
    {
        public string nomeSlot;
        public Image imagemTrofeu;
    }

    [Header("Sprites dos Troféus")]
    public Sprite spriteBloqueado; 
    public Sprite spriteBronze;
    public Sprite spritePrata;
    public Sprite spriteOuro;

    [Header("Troféus de Português (Esquerda)")]
    public SlotTrofeu portFacil;
    public SlotTrofeu portMedio;
    public SlotTrofeu portDificil;

    [Header("Troféus de Matemática (Direita)")]
    public SlotTrofeu mathFacil;
    public SlotTrofeu mathMedio;
    public SlotTrofeu mathDificil;

    private void Start()
    {
        AtualizarTelaDeTrofeus();
    }

    private void OnEnable()
    {
        GerenciadorPerfis.OnPerfilAlterado -= AtualizarTelaDeTrofeus;
        GerenciadorPerfis.OnPerfilAlterado += AtualizarTelaDeTrofeus;
        AtualizarTelaDeTrofeus();
    }

    private void OnDisable()
    {
        GerenciadorPerfis.OnPerfilAlterado -= AtualizarTelaDeTrofeus;
    }

    public void AtualizarTelaDeTrofeus()
    {
        AtualizarSlot(portFacil, "Port", 0);
        AtualizarSlot(portMedio, "Port", 1);
        AtualizarSlot(portDificil, "Port", 2);

        AtualizarSlot(mathFacil, "Math", 0);
        AtualizarSlot(mathMedio, "Math", 1);
        AtualizarSlot(mathDificil, "Math", 2);
    }

    private void AtualizarSlot(SlotTrofeu slot, string materia, int nivel)
    {
        if (slot == null || slot.imagemTrofeu == null) return;

        
        int rank = GerenciadorPerfis.GetInt($"TrofeuRank_{materia}_{nivel}", 0);

        Debug.Log($"[TrofeusManager] {materia} Nível {nivel} -> Troféu Rank: {rank}");

        switch (rank)
        {
            case 3: 
                if (spriteOuro != null) slot.imagemTrofeu.sprite = spriteOuro;
                break;

            case 2: 
                if (spritePrata != null) slot.imagemTrofeu.sprite = spritePrata;
                break;

            case 1: 
                if (spriteBronze != null) slot.imagemTrofeu.sprite = spriteBronze;
                break;

            default:
                if (spriteBloqueado != null) slot.imagemTrofeu.sprite = spriteBloqueado;
                break;
        }
    }
}