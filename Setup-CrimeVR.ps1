$unityPath = "C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe"
$projectPath = "C:\JuegoPistasVR\JuegoPistasVR"

if (-not (Test-Path $unityPath)) {
    Write-Error "No se encontro Unity en: $unityPath"
    exit 1
}

& $unityPath `
    -batchmode `
    -nographics `
    -projectPath $projectPath `
    -executeMethod CrimeVR.Editor.CrimeVRProjectSetup.SetupProjectFoundationBatch `
    -quit `
    -logFile -
