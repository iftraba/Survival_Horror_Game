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

## Assets de la Asset Store
- **Zombie** de **Pxltiger** (Asset Store, 3 zombis humanoides + 10 animaciones, `Assets/Zombie/`). Se descarga desde Package Manager → My Assets; queda en `%APPDATA%/Unity/Asset Store-5.x/<editor>/<categoría>/*.unitypackage` y se importa con `AssetDatabase.ImportPackage(ruta, false)` (también por el MCP). Hay que mirar su licencia de la Asset Store (uso en el juego permitido; no se redistribuye el pack suelto) y citar al autor en los créditos.
- Pasos tras importar (los materiales vienen del render antiguo): 1) comprobar que cada FBX tiene un **avatar humanoide válido** (`Zombie2/3` copiaban el de `Zombie1`; se pusieron en *Create From This Model*); 2) crear un material URP/Lit (`Zombie_URP`) con las texturas (base, normal, oclusión, metálico-suavidad, emisión) y **redirigir el material de cada FBX** (`ModelImporter.AddRemap`; ojo: el nombre interno del material puede variar, `Zombie3` usaba `04 - Default`); 3) construir los prefabs con el kit del proyecto (`PxlZombieKit`).
- Comprobar siempre en el prefab final que el shader no es `Standard` (se vería rosa en URP).

## Repositorio
- GitHub `iftraba/Survival_Horror_Game`, rama `main`; la etiqueta `estable` marca la última versión estable.
- **Git LFS** para binarios (FBX, imágenes, audio). Cuota gratuita 1 GB: vigilar los FBX con malla de las animaciones.

## Exportar el juego
Procedimiento: `manage_build` (acción `build`, `output_path` = `Builds/StandaloneWindows64/Sector7_Grimheim.exe`; hay que fijarlo porque el nombre del producto lleva dos puntos y no vale como nombre de archivo) genera la build en el proyecto (ignorada por git, ~232 MB, ~30 s); después se copia a `Desktop/Juego` con `robocopy /MIR` excluyendo la carpeta `*_BackUpThisFolder_ButDontShipItWithYourGame` (símbolos de depuración: no se envía). Windows 64 bits, `Desktop/Juego/Sector7_Grimheim.exe` (producto **"Sector 7: Grimheim"**, empresa "iftraba", 1920×1080). El nombre del producto da la carpeta de datos (`AppData/LocalLow/iftraba/Sector 7_ Grimheim`: Unity cambia los dos puntos por `_`) y `SaveSystem` copia los guardados de la carpeta antigua `Comisaria` la primera vez. Las preferencias (volumen, sensibilidad) se reinician al cambiar de nombre. Requiere el módulo Windows Mono sano del editor; con Smart App Control activo, Unity no puede compilar (bloquea `Unity.AspNetCore.NamedPipeSupport.dll`).
