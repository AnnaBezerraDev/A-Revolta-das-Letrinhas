using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Lê textos em voz alta com a voz em português do próprio Windows (ex.: "Microsoft Maria").
/// Não usa arquivos de áudio: por isso chega às escolas pela atualização automática.
///
/// Funciona com um PowerShell escondido que fica aberto enquanto o jogo roda e recebe os textos
/// pela entrada padrão (em Base64, para acentos chegarem certos). Se o Windows não tiver voz em
/// português, ou o PowerShell estiver bloqueado, a leitura fica indisponível e o botão "Ouvir" some.
/// </summary>
public static class LeitorDeVoz
{
    private static Process processo;
    private static StreamWriter entrada;
    private static bool indisponivel;
    private static bool registrouSaida;

    /// <summary>false se o Windows deste computador não consegue ler em português.</summary>
    public static bool Disponivel
    {
        get
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (processo != null && processo.HasExited) MarcarIndisponivel("o leitor de voz fechou (sem voz em português?)");
            return !indisponivel;
#else
            return false;
#endif
        }
    }

    /// <summary>Começa a preparar a voz (a primeira leitura sai mais rápido).</summary>
    public static void Preparar()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        GarantirProcesso();
#endif
    }

    /// <summary>Lê o texto (interrompe o que estiver sendo lido). Respeita o som desligado no jogo.</summary>
    public static void Falar(string texto)
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (string.IsNullOrWhiteSpace(texto)) return;
        if (PlayerPrefs.GetInt("SomMutado", 1) == 0) return; // som desligado no botão do Lobby
        if (!GarantirProcesso()) return;

        int volume = Mathf.Clamp(Mathf.RoundToInt(PlayerPrefs.GetFloat("VolumeSFX", 0.5f) * 140f + 30f), 30, 100);
        string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(LimparTexto(texto)));
        Enviar($"$s.SpeakAsyncCancelAll(); $s.Volume = {volume}; $null = $s.SpeakAsync([Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('{b64}')))");
#endif
    }

    public static void Parar()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (processo != null && !processo.HasExited) Enviar("$s.SpeakAsyncCancelAll()");
#endif
    }

    /// <summary>Tira marcações de cor/negrito e troca símbolos pelo que se fala.</summary>
    public static string LimparTexto(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";
        string t = System.Text.RegularExpressions.Regex.Replace(texto, "<[^>]+>", " ");
        t = t.Replace("•", " ").Replace("\n", ". ");
        return t.Trim();
    }

    /// <summary>"2 + 6 = ?" → "Quanto é 2 mais 6?"; "O dobro de 7 é ?" → "O dobro de 7 é quanto?"; problemas ficam como estão.</summary>
    public static string ContaFalada(string conta)
    {
        if (string.IsNullOrEmpty(conta)) return "";
        string t = conta.Trim();
        if (t.EndsWith(" é ?")) return t.Substring(0, t.Length - 4) + " é quanto?";
        if (!t.EndsWith("= ?")) return t; // texto de problema: lê como está escrito
        t = t.Substring(0, t.Length - 3).Trim();

        t = System.Text.RegularExpressions.Regex.Replace(t, @"R\$ ?(\d+),(\d{2})", m =>
            m.Groups[2].Value == "00" ? $"{m.Groups[1].Value} reais" : $"{m.Groups[1].Value} reais e {m.Groups[2].Value} centavos");
        t = System.Text.RegularExpressions.Regex.Replace(t, @"(\d+)/(\d+)", m => $"{m.Groups[1].Value} {NomeFracao(m.Groups[2].Value, m.Groups[1].Value != "1")}");
        t = t.Replace(" + ", " mais ").Replace(" - ", " menos ").Replace(" x ", " vezes ").Replace(" ÷ ", " dividido por ")
             .Replace("%", " por cento").Replace("(", " ").Replace(")", " ");
        return "Quanto é " + System.Text.RegularExpressions.Regex.Replace(t, @"\s+", " ").Trim() + "?";
    }

    private static string NomeFracao(string denominador, bool plural)
    {
        string nome;
        switch (denominador)
        {
            case "2": nome = "meio"; break;
            case "3": nome = "terço"; break;
            case "4": nome = "quarto"; break;
            case "5": nome = "quinto"; break;
            case "10": nome = "décimo"; break;
            default: return "sobre " + denominador;
        }
        return plural ? nome + "s" : nome;
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private static bool GarantirProcesso()
    {
        if (indisponivel) return false;
        if (processo != null && !processo.HasExited) return true;
        if (processo != null) { MarcarIndisponivel("o leitor de voz fechou"); return false; }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"),
                Arguments = "-NoLogo -NoProfile -NonInteractive -Command -",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            if (!File.Exists(psi.FileName)) psi.FileName = "powershell.exe";
            processo = Process.Start(psi);
            processo.OutputDataReceived += (s, e) => { };
            processo.ErrorDataReceived += (s, e) => { };
            processo.BeginOutputReadLine();
            processo.BeginErrorReadLine();
            entrada = processo.StandardInput;
            entrada.AutoFlush = true;

            // Escolhe a voz em português; sem ela, encerra (o jogo então esconde o botão "Ouvir")
            Enviar("Add-Type -AssemblyName System.Speech; $s = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
                   "$v = $s.GetInstalledVoices() | Where-Object { $_.Enabled -and $_.VoiceInfo.Culture.Name -like 'pt*' } | Select-Object -First 1; " +
                   "if (-not $v) { exit 3 }; $s.SelectVoice($v.VoiceInfo.Name); $s.Rate = -2; $s.SetOutputToDefaultAudioDevice()");

            if (!registrouSaida)
            {
                Application.quitting += Encerrar;
                registrouSaida = true;
            }
            return true;
        }
        catch (Exception ex)
        {
            MarcarIndisponivel(ex.Message);
            return false;
        }
    }

    private static void Enviar(string comando)
    {
        try
        {
            entrada.WriteLine(comando);
        }
        catch (Exception ex)
        {
            MarcarIndisponivel(ex.Message);
        }
    }

    private static void MarcarIndisponivel(string motivo)
    {
        if (!indisponivel) Debug.LogWarning("[Voz] Leitura em voz alta indisponível: " + motivo);
        indisponivel = true;
    }

    public static void Encerrar()
    {
        try
        {
            if (processo != null && !processo.HasExited)
            {
                entrada?.WriteLine("exit");
                if (!processo.WaitForExit(500)) processo.Kill();
            }
        }
        catch { }
        processo = null;
    }
#else
    public static void Encerrar() { }
#endif
}
