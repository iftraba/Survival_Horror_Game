# Art pipeline

Cómo entran personajes, animaciones y objetos en el juego.

## Personajes (propiedad del usuario, no generados por mí)
- Protagonista: **Soldier** de Mixamo (viene dentro de los FBX de animación del jugador).
- Zombis: Civil, Yaku (carpeta Katana), Zombie Girl y Zombie Poli (cop). Jefe: PumpkinHulk.
- Mixamo: personaje en **T-pose, FBX for Unity**; animaciones **Without Skin, 30 fps, sin reducción de claves** (y **In Place** si existe la casilla). Detalle en `Tools/mixamo/LEEME.md`.
- Descargas locales en `Tools/mixamo/download/` (ignorado por git); lo importado va a `Assets/_Project/Art/Mixamo/`.

## Personajes de Meshy para Mixamo (2026-10-07)
Las exportaciones de Meshy (zip con `.fbx` + 4 PNG) tienen ~600.000 caras y Mixamo quiere un único archivo. Flujo: extraer a `Tools/raw_generated/`, `Tools/blender/prepare_for_mixamo.py` (una malla, 1,8 m, solo la textura de color incrustada) y después `Tools/blender/decimate_for_mixamo.py` (Decimate colapso a ~50.000 caras, conserva UV y textura) → FBX único de ~5 MB en `Tools/mixamo/upload/`. Se usan rutas absolutas con Blender. **No uses la opción `tpose`** de `prepare_for_mixamo.py` con estos modelos: estira la camisa y los brazos. Si un personaje no viene en T/A-pose, regenerarlo en Meshy. Tras Mixamo, el personaje descargado va a `Assets/_Project/Art/Mixamo/Characters/` y se monta con un kit (ejemplo: `OficialZombieKit`). Hechos: `Meshy_Oficial.fbx` (T-pose perfecta) y `Meshy_Nuevo1.fbx` (brazos casi pegados al cuerpo; si Mixamo no coloca bien los marcadores, regenerar en pose T).

## Texturas de los objetos recogibles (2026-10-08)
`Tools/blender/texture_items.py` hornea en Blender (Cycles, CPU) texturas para los objetos y `ItemTextureKit` (menú *Horror/Aplicar texturas de objetos*) las monta en Unity. Salen en `Assets/_Project/Art/Textures/Items/` como `Nombre_BaseColor`, `Nombre_MetallicSmoothness` (R metal, A suavidad) y `Nombre_Normal`.
- **Modo `blend`** (riñonera, tarjeta, llaves; antes solo colores planos): desenvuelve UV, crea un material procedural por cada material según su nombre (metal: arañazos, mugre en los huecos por AO, bordes pulidos por *pointiness*, latón/oro más saturados; tela: trama tejida, polvo y roces; plástico: roces y manchas; la tarjeta blanca lleva una trama de seguridad) y hornea color (por emisión: el horneado difuso deja negro lo metálico), rugosidad, metal y normales. Reexporta el FBX en su sitio (mismos ejes y escala; comprobado que las medidas no cambian) con un único material `Nombre_baked`. **Para repetirlo hay que partir de las copias de `Tools/raw_generated/backup_items/`**: el FBX reexportado ya no tiene los materiales originales.
- **Muebles** (2026-10-08; escritorio, silla, taquilla, estantería, archivador, catre, caja y bidón; antes colores planos): mismo modo `blend` a 2048 (silla 1024), con materiales nuevos según el nombre del material: **madera** (`wood`, `board`: veta a lo largo de la tabla según la orientación de cada cara, vetas alargadas de tono distinto, barniz gastado en los bordes, mugre en las juntas), **chapa pintada** (`body`: desconchones en los bordes que dejan ver el metal, óxido en huecos y chorretones verticales), **óxido** (`rust`, `iron`), **cartón** y **tela gruesa** (`blanket`, `mattress`, `pillow`). Texturas en `Art/Textures/Props/`; copias de los FBX originales en `Tools/raw_generated/backup_props/` (para repetirlo hay que partir de ellas). ~40 s por mueble.
- **Modo `maps`** (modelos de Meshy): con sus UV hornea normales (de la luminancia de la textura + ruido fino) y metal/suavidad según el tipo (arma: zonas grises metálicas; bote: tapa y fondo plateados; cartón y plástico mates). A la escopeta además le recolorea las zonas naranjas como madera con veta.

## Texturas del escenario (2026-10-08)
`Tools/blender/build_env_textures.py` (numpy dentro de Blender, sin interfaz: `blender --background --python build_env_textures.py -- <Assets/_Project/Art/Textures> [tex_floor tex_wall ...]`) genera las texturas tileables del escenario y `EnvTextureKit` (menú *Horror/Aplicar texturas del escenario*) las monta en los materiales `Env_*`. Cada una saca color, `_n` (normales), `_ms` (metal en RGB, suavidad en el alfa; `_MetallicGlossMap` de URP Lit) y `_ao` (oclusión). Se mantiene la escala de las anteriores, así que no cambian las UV:
- **Suelo** (`tex_floor`, 2048, 4 m, 8×8 baldosas de 0,5 m): baldosa vinílica en damero apagado con motas, canto redondeado, juntas sucias, zonas gastadas mates con arañazos, baldosas rajadas, esquinas desconchadas que dejan ver el mortero y charcos brillantes.
- **Pared** (`tex_wall`, 2048, 3 m): yeso pintado con textura de rodillo, pintura desconchada con el borde levantado, humedades con cerco marrón y moho, chorretones, grietas con ramas, golpes y rozaduras. Sin bandas por altura: la planta alta (y = 4 m) no cuadra con la baja.
- **Columnas** (`tex_concrete`, 2048, 3 m; material nuevo `Env_Column`): hormigón visto con árido, coqueras, juntas del encofrado cada metro, escorrentías, eflorescencias, desconchones y grietas. El kit se lo pone a los `Column`/`Pillar` de la escena que llevaban `Env_Wall`; si se vuelve a construir una zona con su kit, hay que repetir el menú.
- **Techo** (`tex_ceiling`, 1024, 2,4 m, 4×4 placas): placas acústicas con fisuras, una placa cambiada, manchas de agua con cerco y perfil en T pintado con óxido.
- **Madera** (`tex_wood`, 1024, 8 tablas; puertas, marcos, zócalos): veta con arcos distintos en cada tabla, poros, nudos, barniz gastado mate, arañazos y suciedad.
- **Metal** (`tex_metal`, 1024): chapa pintada con desconchones y arañazos que dejan ver el acero (metálico), óxido y chorretones.
Las texturas anteriores están en `Tools/raw_generated/backup_env/`; `build_textures.py` se conserva para las manchas de sangre.

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
