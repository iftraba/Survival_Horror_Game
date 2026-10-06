---
name: modelo-blender
description: Genera un modelo 3D (arma, objeto, mueble, prop, pieza de escenario) desde cero en Blender con un script de Python, lo exporta a FBX y lo importa en el proyecto Unity. Úsala cuando el usuario pida que Claude "genere/haga/modele" algo en Blender en lugar de usar IA generativa.
---

# Modelar en Blender para el juego

Blender 5.2 está en `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe`. Se trabaja **sin interfaz** con
scripts de Python guardados en `Tools/blender/` (ejemplos del proyecto: `build_weapons.py`, `build_items.py`,
`build_textures.py`, `preview_model.py`). Para cada modelo nuevo, crea `Tools/blender/build_<nombre>.py`.

## Límites realistas
- Hecho a mano con primitivas esto sirve para **armas, objetos, mobiliario, props y arquitectura**. **No** lo uses
  para personajes humanoides ni zombis: salen amorfos. Para eso, usa la skill `pedir-modelo-ia` (+ Mixamo).
- Si el usuario pide un personaje, dilo y propón la otra skill.

## Convenciones del proyecto
- Unidades: **metros**, escala 1. Mide el modelo y comprueba que el tamaño real es creíble.
- Origen en el punto de uso: **arma = en el agarre**, objeto de suelo = centro de la base, mueble = centro de la base.
- Ejes: en Blender el frente es **-Y**; al exportar FBX con *Forward -Z, Up Y* queda como **+Z en Unity**
  (el cañón de un arma apunta a +Z). Aplica transformaciones (`transform_apply`) antes de exportar.
- Presupuesto: arma 2-6k triángulos, prop 0.3-3k, mueble 0.5-4k. Bisel pequeño (0.002-0.005 m) en bordes duros para
  que cojan luz.
- Materiales con nombres claros (`metal`, `wood`, `plastic`…), PBR sencillo (color base, rugosidad, metálico).
  Sin texturas externas salvo que existan en el proyecto.
- Un script debe poder repetirse: limpia la escena al empezar (`bpy.ops.wm.read_factory_settings(use_empty=True)`)
  y escribe el resultado en una ruta que se le pasa.

## Flujo
1. Aclara dimensiones, uso y estilo; si falta algo obvio, decídelo y dilo.
2. Escribe `Tools/blender/build_<nombre>.py` (geometría con `bmesh` o primitivas + modificadores bisel/array;
   termina imprimiendo `PROCESO_TERMINADO` y las dimensiones).
3. Ejecuta en segundo plano con Start-Process y espera al marcador, como en `run_zombie_pipeline.ps1`:
   `blender.exe --background --python <script> -- <salida.fbx>`; revisa el log si falla (`Traceback`).
4. **Renderiza una vista previa** (`preview_model.py` o un render EEVEE rápido, 3 ángulos) y míralo antes de dar el
   modelo por bueno. Corrige proporciones, orientación y escala.
5. Exporta FBX a `Assets/_Project/Art/<Carpeta>/` (`bpy.ops.export_scene.fbx`, `apply_scale_options='FBX_SCALE_ALL'`,
   `axis_forward='-Z'`, `axis_up='Y'`).
6. En Unity (MCP): refresca, comprueba que importa sin avisos, crea el prefab o colócalo en la escena, y verifica
   en pantalla (captura de la cámara). Si es un arma, ajusta `heldScale` en su `WeaponData` y el agarre.
7. No recompiles Unity mientras corra un trabajo largo (ver memoria del proyecto) y no pruebes en Play si el
   usuario está jugando (`EditorApplication.isPlaying`).
8. Confirma con el usuario antes de commit/push; los FBX grandes van por Git LFS.

## Plantilla mínima del script
```python
import bpy, sys
OUT = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)

def mat(name, rgb, rough=0.6, metal=0.0):
    m = bpy.data.materials.new(name); m.use_nodes = True
    b = m.node_tree.nodes["Principled BSDF"]
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = rough
    b.inputs["Metallic"].default_value = metal
    return m

def box(name, centro, tam, material):
    bpy.ops.mesh.primitive_cube_add(location=centro)
    o = bpy.context.object; o.name = name
    o.scale = (tam[0] / 2, tam[1] / 2, tam[2] / 2)
    bpy.ops.object.transform_apply(scale=True)
    o.data.materials.append(material)
    return o

# ... construir piezas, unirlas con bpy.ops.object.join() ...
bpy.ops.export_scene.fbx(filepath=OUT, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y')
print("PROCESO_TERMINADO")
```
