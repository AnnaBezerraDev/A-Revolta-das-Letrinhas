using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Imagens que ficam no servidor (wwwroot/imagens) — desenhos das palavras e personagens de perfil.
/// O jogo baixa cada imagem uma vez e guarda no computador (pasta de dados do jogo), então funciona
/// sem internet depois. Imagens: Noto Emoji v2.042 (Google), licença Apache 2.0.
/// Assim as novas imagens chegam às escolas sem reinstalar o jogo.
/// </summary>
public class ImagensRemotas : MonoBehaviour
{
    private static ImagensRemotas _instancia;
    private static readonly Dictionary<string, Sprite> memoria = new Dictionary<string, Sprite>();
    private static readonly Dictionary<string, List<Action<Sprite>>> esperando = new Dictionary<string, List<Action<Sprite>>>();
    private static readonly HashSet<string> semImagem = new HashSet<string>();
    private const int DOWNLOADS_SIMULTANEOS = 6;
    private static int baixandoAgora;
    private static readonly Queue<string> filaDownload = new Queue<string>();

    /// <summary>Palavras que têm desenho no servidor (wwwroot/imagens/palavras/&lt;PALAVRA&gt;.png).</summary>
    private static readonly HashSet<string> PalavrasComDesenho = new HashSet<string>
    {
        "UVA", "OVO", "LUA", "FIO", "BOLA", "CASA", "GATO", "PATO", "SAPO", "DADO", "MALA", "VACA", "FACA", "LOBO", "SUCO",
        "BOCA", "PIPA", "BOLO", "FOGO", "DEDO", "MULA", "RATO", "CAMA", "LATA", "PENA", "VELA", "MOTO", "DOCE", "FOCA",
        "SINO", "MAPA", "LAGO", "FADA", "GALO", "BULE", "COCO", "BOTA", "SOPA", "FLOR", "URSO", "NUVEM", "LIVRO", "PRATO",
        "CARRO", "MANGA", "PORTA", "FESTA", "LEITE", "PERNA", "PEDRA", "PIANO", "VENTO", "GARFO", "BARCO", "CORDA", "PRAIA",
        "FRUTA", "TIGRE", "ZEBRA", "MOSCA", "PORCO", "PENTE", "DENTE", "MUNDO", "BOLSA", "TERRA", "NARIZ", "SAPATO", "JANELA",
        "PANELA", "BONECA", "MACACO", "CAVALO", "GIRAFA", "TOMATE", "BANANA", "ESCOLA", "PIPOCA", "CANETA", "BALEIA", "CAMELO",
        "CEBOLA", "GALINHA", "MOCHILA", "CENOURA", "BATATA", "CADEIRA", "COELHO", "ABELHA", "ANIMAL", "CORUJA", "MALETA",
        "MENINO", "MENINA", "SACOLA", "FORMIGA", "MORANGO", "PEPINO", "LUVA", "PETECA", "COMIDA", "SALADA", "PATA", "TURMA",
        "ELEFANTE", "HIPOPOTAMO", "ILHA", "JACARE", "KIWI", "LEAO", "NAVIO", "OVELHA", "QUEIJO", "TARTARUGA", "WAFFLE",
        "XICARA", "YAKISOBA"
    };

    /// <summary>"Hipopótamo" → "HIPOPOTAMO" (sem acento, maiúsculas), como no nome do arquivo.</summary>
    public static string ChavePalavra(string palavra)
    {
        if (string.IsNullOrEmpty(palavra)) return "";
        string d = palavra.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();
        foreach (char c in d)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    public static bool TemDesenho(string palavra) => PalavrasComDesenho.Contains(ChavePalavra(palavra));

    /// <summary>Desenho da palavra (callback recebe null se não houver).</summary>
    public static void DesenhoDaPalavra(string palavra, Action<Sprite> pronto)
    {
        string chave = ChavePalavra(palavra);
        if (!PalavrasComDesenho.Contains(chave)) { pronto?.Invoke(null); return; }
        Obter("palavras/" + chave + ".png", pronto);
    }

    public static void ImagemDoPersonagem(string codigo, Action<Sprite> pronto)
    {
        if (string.IsNullOrEmpty(codigo)) { pronto?.Invoke(null); return; }
        Obter("personagens/" + codigo + ".png", pronto);
    }

    /// <summary>Imagem por caminho relativo a /imagens/ no servidor (memória → disco → download).</summary>
    public static void Obter(string caminho, Action<Sprite> pronto)
    {
        if (memoria.TryGetValue(caminho, out Sprite s)) { pronto?.Invoke(s); return; }
        if (semImagem.Contains(caminho)) { pronto?.Invoke(null); return; }

        string arquivo = CaminhoNoDisco(caminho);
        if (File.Exists(arquivo))
        {
            Sprite doDisco = CriarSprite(File.ReadAllBytes(arquivo));
            if (doDisco != null)
            {
                memoria[caminho] = doDisco;
                pronto?.Invoke(doDisco);
                return;
            }
        }

        if (esperando.TryGetValue(caminho, out var lista)) { lista.Add(pronto); return; }
        esperando[caminho] = new List<Action<Sprite>> { pronto };
        filaDownload.Enqueue(caminho);
        Garantir().ProximosDownloads();
    }

    private static ImagensRemotas Garantir()
    {
        if (_instancia == null)
        {
            GameObject go = new GameObject("[ImagensRemotas]");
            DontDestroyOnLoad(go);
            _instancia = go.AddComponent<ImagensRemotas>();
        }
        return _instancia;
    }

    private void ProximosDownloads()
    {
        while (baixandoAgora < DOWNLOADS_SIMULTANEOS && filaDownload.Count > 0)
        {
            baixandoAgora++;
            StartCoroutine(Baixar(filaDownload.Dequeue()));
        }
    }

    private IEnumerator Baixar(string caminho)
    {
        string servidor = ServicoNuvem.Instancia != null ? ServicoNuvem.Instancia.servidorUrl : "";
        Sprite sprite = null;
        if (!string.IsNullOrEmpty(servidor))
        {
            using (UnityWebRequest req = UnityWebRequest.Get($"{servidor}/imagens/{caminho}"))
            {
                req.timeout = 20;
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success)
                {
                    byte[] bytes = req.downloadHandler.data;
                    sprite = CriarSprite(bytes);
                    if (sprite != null)
                    {
                        try
                        {
                            string arquivo = CaminhoNoDisco(caminho);
                            Directory.CreateDirectory(Path.GetDirectoryName(arquivo));
                            File.WriteAllBytes(arquivo, bytes);
                        }
                        catch (Exception ex) { Debug.LogWarning("[Imagens] Não foi possível guardar no disco: " + ex.Message); }
                    }
                }
                else if (req.responseCode == 404)
                {
                    semImagem.Add(caminho);
                }
            }
        }

        if (sprite != null) memoria[caminho] = sprite;
        if (esperando.TryGetValue(caminho, out var lista))
        {
            esperando.Remove(caminho);
            foreach (var cb in lista)
            {
                try { cb?.Invoke(sprite); } catch (Exception ex) { Debug.LogException(ex); }
            }
        }
        baixandoAgora--;
        ProximosDownloads();
    }

    private static string CaminhoNoDisco(string caminho) =>
        Path.Combine(Application.persistentDataPath, "imagens", caminho.Replace('/', Path.DirectorySeparatorChar));

    private static Sprite CriarSprite(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 8) return null;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes)) { Destroy(tex); return null; }
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
    }
}
