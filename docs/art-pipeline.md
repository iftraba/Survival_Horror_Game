# Art pipeline

Cómo entran personajes, animaciones y objetos en el juego.

## Personajes (propiedad del usuario, no generados por mí)
- Protagonista: **Soldier** de Mixamo (viene dentro de los FBX de animación del jugador).
- Zombis: Civil, Yaku (carpeta Katana), Zombie Girl y Zombie Poli (cop). Jefe: PumpkinHulk.
- Mixamo: personaje en **T-pose, FBX for Unity**; animaciones **Without Skin, 30 fps, sin reducción de claves** (y **In Place** si existe la casilla). Detalle en `Tools/mixamo/LEEME.md`.
- Descargas locales en `Tools/mixamo/download/` (ignorado por git); lo importado va a `Assets/_Project/Art/Mixamo/`.

## Objetos y armas
- Opción A: IA generativa 3D (Meshy). Skill del proyecto `pedir-modelo-ia`: redacta el prompt (≤780 caracteres, inglés).
- Opción B: Blender por script (`Tools/blender/`, Blender 5.2). Skill `modelo-blender`.
- Ejemplo hecho con Blender: `Tools/blender/build_rinonera.py` (riñonera; reutiliza el `Mesher` de `build_items.py`, genera vistas previas con `-- <fbx> <carpeta>` y exporta con origen en el centro de la base). Meshy en **modo texto** devolvió una persona de 25 cm dos veces para este objeto (ver `estado-actual.md`): para objetos simples, Blender o imagen → 3D.
- Ejemplo hecho con Blender: `Tools/blender/build_phone.py` (teléfono de disco). Se exporta con `bake_space_transform=True` y el origen en el suelo para que Unity lo oriente y apoye bien.
- Los personajes humanoides **no** se hacen con scripts: los modelos decimados o reposados por código salieron amorfos.

## Repositorio
- GitHub `iftraba/Survival_Horror_Game`, rama `main`; la etiqueta `estable` marca la última versión estable.
- **Git LFS** para binarios (FBX, imágenes, audio). Cuota gratuita 1 GB: vigilar los FBX con malla de las animaciones.

## Exportar el juego
Procedimiento: `manage_build` (acción `build`) genera `Builds/StandaloneWindows64/Comisaria.exe` en el proyecto (ignorado por git, ~232 MB, ~30 s); después se copia a `Desktop/Juego` con `robocopy /MIR` excluyendo `Comisaria_BackUpThisFolder_ButDontShipItWithYourGame` (símbolos de depuración: no se envía). Windows 64 bits, `Desktop/Juego/Comisaria.exe` (producto "Comisaria", empresa "iftraba", 1920×1080). Requiere el módulo Windows Mono sano del editor; con Smart App Control activo, Unity no puede compilar (bloquea `Unity.AspNetCore.NamedPipeSupport.dll`).
