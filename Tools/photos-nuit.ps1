param([switch]$Setup)
$u = "C:\Program Files\Unity\Hub\Editor\6000.6.2f1\Editor\Unity.exe"
$p = "C:\Users\marti\Documents\GitHub\federaris"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
if ($Setup) {
    $a = Start-Process -FilePath $u -ArgumentList "-batchmode", "-quit", "-projectPath", "`"$p`"", "-executeMethod", "Fief.KitFiefSetup.Run", "-logFile", "`"$here\fed-setup.log`"" -Wait -PassThru
    "setup exit=" + $a.ExitCode
    Select-String -Path "$here\fed-setup.log" -Pattern "\[FIEF\] Kit|error CS|compiler errors|Exception:" | Select-Object -First 25 | ForEach-Object { $_.Line }
}
$a = Start-Process -FilePath $u -ArgumentList "-batchmode", "-projectPath", "`"$p`"", "-executeMethod", "Fief.NightCapture.Run", "-logFile", "`"$here\fed-run.log`"" -PassThru
if (-not $a.WaitForExit(420000)) { $a.Kill(); "run TIMEOUT" } else { "run exit=" + $a.ExitCode }
Select-String -Path "$here\fed-run.log" -Pattern "\[FIEF\]|error CS|compiler errors|Exception:|NullReference" | Select-Object -First 40 | ForEach-Object { $_.Line }
Get-ChildItem "$p\Logs\Captures" -ErrorAction SilentlyContinue | Select-Object Name, Length, LastWriteTime
