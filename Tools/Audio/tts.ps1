# Genera clips de voz con System.Speech a partir de un JSON: [{ "voice": "...", "rate": 0, "text": "...", "out": "ruta.wav" }]
# uso: powershell -File tts.ps1 jobs.json
param([string]$jobs)
Add-Type -AssemblyName System.Speech
$list = Get-Content -Raw -Encoding UTF8 $jobs | ConvertFrom-Json
$fmt = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(44100, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$available = $synth.GetInstalledVoices() | ForEach-Object { $_.VoiceInfo.Name }
foreach ($j in $list) {
    $voice = $j.voice
    if (-not ($available -contains $voice)) { $voice = $available[0] }
    $synth.SelectVoice($voice)
    $synth.Rate = [int]$j.rate
    $synth.SetOutputToWaveFile($j.out, $fmt)
    $synth.Speak([string]$j.text)
    $synth.SetOutputToNull()
}
$synth.Dispose()
Write-Output ("tts ok: " + $list.Count)
