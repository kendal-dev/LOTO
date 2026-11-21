# Builds the Quest APK from the command line, with Unity CLOSED (same as the menu Loto > Build APK).
# Usage (from anywhere):
#   powershell -NoProfile -ExecutionPolicy Bypass -File <repo>\unity\Tools\build.ps1 [-Install] [-Unity <path>]
# -Install: after a good build, installs Builds\loto.apk on the Quest connected by USB (adb install -r).
# Output: Builds\loto.apk | log: Logs\build.log | exit code 0 = built, 1 = build failed, 2 = not started.
# With Unity open use the menu instead: a second Unity cannot open the same project, and the laptop
# has no memory for two.
param(
  [string]$Unity = 'C:\Program Files\Unity\Hub\Editor\6000.4.6f1\Editor\Unity.exe',
  [switch]$Install
)

$proj = Split-Path -Parent $PSScriptRoot
$log  = Join-Path $proj 'Logs\build.log'
$apk  = Join-Path $proj 'Builds\loto.apk'

if (-not (Test-Path $Unity)) { "STOP: Unity 6000.4.6f1 not found at $Unity (pass -Unity <path>)"; exit 2 }
if (Get-Process Unity -ErrorAction SilentlyContinue) { 'STOP: Unity is open. Close it, or build from its menu: Loto > Build APK'; exit 2 }
if (Test-Path (Join-Path $proj 'Temp\UnityLockfile')) { 'NOTE: Temp\UnityLockfile exists (Unity closed badly last time?). Trying anyway.' }

New-Item -ItemType Directory -Force (Split-Path $log) | Out-Null
$before = if (Test-Path $apk) { (Get-Item $apk).LastWriteTime } else { [datetime]::MinValue }
$t0 = Get-Date
"Building... (about 3 to 12 minutes; the first time, with no Library folder, much longer). Log: $log"

# -force-d3d11: on this laptop Unity crashed with "d3d12: device removed" during Walkback.
$p = Start-Process -FilePath $Unity -Wait -PassThru -ArgumentList @(
  '-batchmode', '-force-d3d11',
  '-projectPath', "`"$proj`"",
  '-buildTarget', 'Android',
  '-executeMethod', 'Loto.EditorTools.BuildApk.Run',
  '-logFile', "`"$log`"",
  '-quit')

$minutes = ((Get-Date) - $t0).TotalMinutes.ToString('F1')
if (Test-Path $log) {
  $errors = Select-String -Path $log -Pattern 'error CS\d+' | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique
  if ($errors) { 'COMPILE ERRORS:'; $errors | Select-Object -First 30 }
  Select-String -Path $log -Pattern '^\[Loto\] APK:|Build completed with a result' | ForEach-Object { $_.Line.Trim() } | Select-Object -Last 3
}

$built = (Test-Path $apk) -and ((Get-Item $apk).LastWriteTime -gt $before)
if ($p.ExitCode -ne 0 -or -not $built) {
  "FAILED after $minutes min (Unity exit code $($p.ExitCode)). The APK in Builds\ is the OLD one, if any. Read $log"
  exit 1
}
$item = Get-Item $apk
"OK in $minutes min: $apk, {0:N1} MB, written {1}" -f ($item.Length / 1MB), $item.LastWriteTime.ToString('dd/MM HH:mm:ss')

if ($Install) {
  $adb = (Get-Command adb -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source)
  if (-not $adb) {
    $candidates = @(
      (Join-Path (Split-Path $Unity) 'Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'),
      (Join-Path $env:LOCALAPPDATA 'Microsoft\WinGet\Packages\Genymobile.scrcpy_Microsoft.Winget.Source_8wekyb3d8bbwe\*\adb.exe'))
    foreach ($c in $candidates) {
      $found = Get-Item $c -ErrorAction SilentlyContinue | Select-Object -First 1
      if ($found) { $adb = $found.FullName; break }
    }
  }
  if (-not $adb) { 'adb not found: install by hand with  adb install -r Builds\loto.apk'; exit 0 }
  "adb: $adb"
  & $adb devices
  & $adb install -r $apk
  'If it says "unauthorized": accept USB debugging inside the headset and run again with -Install.'
}
exit 0
