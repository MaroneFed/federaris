# Lance La Nuit dans Unity, sans ouvrir de fenetre, et prend des photos du jeu (Logs/Captures).
#   powershell -ExecutionPolicy Bypass -File Tools\photos-nuit.ps1          les photos
#   powershell -ExecutionPolicy Bypass -File Tools\photos-nuit.ps1 -Kit     refait d'abord les matieres du kit
# Unity doit etre FERME sur ce projet (deux Unity ne peuvent pas ouvrir le meme projet).
param([switch]$Kit)
$p = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$v = (Select-String -Path "$p\ProjectSettings\ProjectVersion.txt" -Pattern "m_EditorVersion: (.+)").Matches[0].Groups[1].Value.Trim()
$u = "C:\Program Files\Unity\Hub\Editor\$v\Editor\Unity.exe"
if (-not (Test-Path $u)) { "Unity $v introuvable : $u"; exit 1 }
New-Item -ItemType Directory -Force "$p\Logs" | Out-Null
$filtre = "\[FIEF\]|error CS|compiler errors|Exception:|NullReference|Shader error"
if ($Kit) {
    $a = Start-Process -FilePath $u -ArgumentList "-batchmode", "-quit", "-projectPath", "`"$p`"", "-executeMethod", "Fief.KitFiefSetup.Run", "-logFile", "`"$p\Logs\kit-unity.log`"" -Wait -PassThru
    "kit : code " + $a.ExitCode
    Select-String -Path "$p\Logs\kit-unity.log" -Pattern $filtre | Select-Object -First 25 | ForEach-Object { $_.Line }
}
$a = Start-Process -FilePath $u -ArgumentList "-batchmode", "-projectPath", "`"$p`"", "-executeMethod", "Fief.NightCapture.Run", "-logFile", "`"$p\Logs\photos-unity.log`"" -PassThru
if (-not $a.WaitForExit(420000)) { $a.Kill(); "photos : trop long, arrete" } else { "photos : code " + $a.ExitCode }
Select-String -Path "$p\Logs\photos-unity.log" -Pattern $filtre | Select-Object -First 40 | ForEach-Object { $_.Line }
Get-ChildItem "$p\Logs\Captures" -ErrorAction SilentlyContinue | Select-Object Name, Length, LastWriteTime
