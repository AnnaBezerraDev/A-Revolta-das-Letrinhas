using System;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

/// <summary>
/// Garante que qualquer VideoPlayer configurado com arquivo de vídeo local
/// resolva o caminho correto dentro de Application.streamingAssetsPath
/// tanto no executável Windows quanto no WebGL.
/// </summary>
public class StreamingVideoAdapter : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        SceneManager.sceneLoaded += (scene, mode) =>
        {
            FixAllVideoPlayers();
        };
    }

    public static void FixAllVideoPlayers()
    {
        VideoPlayer[] players = UnityEngine.Object.FindObjectsByType<VideoPlayer>(FindObjectsSortMode.None);
        foreach (var vp in players)
        {
            if (vp == null) continue;

            if (vp.source == VideoSource.Url && !string.IsNullOrEmpty(vp.url))
            {
                if (!vp.url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !vp.url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                    !vp.url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    string filename = Path.GetFileName(vp.url);
#if UNITY_WEBGL && !UNITY_EDITOR
                    vp.url = Application.streamingAssetsPath + "/" + filename;
#else
                    vp.url = Path.Combine(Application.streamingAssetsPath, filename);
#endif
                    Debug.Log($"[StreamingVideoAdapter] VideoPlayer '{vp.gameObject.name}' configurado para: {vp.url}");
                }
            }
        }
    }
}
