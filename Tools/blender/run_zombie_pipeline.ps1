# Convierte un zombi generado con IA (FBX crudo de Meshy en Tools/raw_generated) en un personaje listo para Unity:
# extraer textura -> limpiar y hornear -> enderezar pose -> rigear con el esqueleto del proyecto -> copiar a Assets.
# Uso: powershell -File run_zombie_pipeline.ps1 -Raw MeshyZombieCop -Out ZombieGenCop
param([string]$Raw, [string]$Out)

$p = "C:\Users\iftra\My project"
$rawDir = "$p\Tools\raw_generated"; $stage = "$rawDir\stage\$Out"
$exe = "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe"
$bl = "$p\Tools\blender"
New-Item -ItemType Directory -Force $stage | Out-Null

function RunBlender($argLine, $log) {
    if (Test-Path $log) { Remove-Item $log }
    Start-Process $exe -ArgumentList $argLine -WindowStyle Hidden -RedirectStandardOutput $log
    for ($i = 0; $i -lt 100; $i++) {
        Start-Sleep 3
        if ((Test-Path $log) -and (Select-String $log -Pattern "PROCESO_TERMINADO|Traceback" -Quiet)) { break }
    }
    $t = Get-Content $log
    if (-not ($t -match "PROCESO_TERMINADO")) { throw "Blender fallo en $log" }
    $t | Where-Object { $_ -match '^\[|ALTURA|vertices' }
}

# 1) textura embebida: bloque 'R' (datos crudos) cuyo contenido empieza por FFD8FF y termina en FFD9
$b = [IO.File]::ReadAllBytes("$rawDir\$Raw.fbx"); $jpg = $null
for ($i = 5; $i -lt $b.Length - 4; $i++) {
    if ($b[$i] -eq 0xFF -and $b[$i+1] -eq 0xD8 -and $b[$i+2] -eq 0xFF -and $b[$i-5] -eq 0x52) {
        $len = [BitConverter]::ToUInt32($b, $i - 4)
        if ($len -gt 100000 -and ($i + $len) -le $b.Length -and $b[$i+$len-2] -eq 0xFF -and $b[$i+$len-1] -eq 0xD9) {
            $jpg = $b[$i..($i+$len-1)]; break
        }
    }
}
if (-not $jpg) { throw "No se encontro la textura en $Raw.fbx" }
[IO.File]::WriteAllBytes("$rawDir\${Raw}_basecolor.jpg", $jpg)
"textura: $($jpg.Length) bytes"

# 2) limpieza + horneado (origen en los pies, 1.8 m, 14000 triangulos, textura 2048)
RunBlender "-b --python `"$bl\process_generated_weapon.py`" -- `"$rawDir\$Raw.fbx`" `"$rawDir\${Raw}_basecolor.jpg`" `"$stage`" ${Out}Low 14000 2048 1.8 0 0.5 0" "$stage\1.log"
# 3) pose recta
RunBlender "-b --python `"$bl\straighten_character.py`" -- `"$stage\${Out}Low.fbx`" `"$stage\${Out}Straight.fbx`" 1.8" "$stage\2.log"
# 4) esqueleto del proyecto + pesos
RunBlender "-b --python `"$bl\rig_generated_character.py`" -- `"$stage\${Out}Straight.fbx`" `"$stage\${Out}Low_basecolor.png`" `"$stage\${Out}Rigged.fbx`"" "$stage\3.log"

# 5) a Unity
Copy-Item "$stage\${Out}Rigged.fbx" "$p\Assets\_Project\Art\Characters\$Out.fbx" -Force
Copy-Item "$stage\${Out}Low_basecolor.png" "$p\Assets\_Project\Art\Generated\${Out}_basecolor.png" -Force
Copy-Item "$stage\${Out}Low_normal.png" "$p\Assets\_Project\Art\Generated\${Out}_normal.png" -Force
"LISTO $Out"
