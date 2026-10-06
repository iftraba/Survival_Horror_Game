---
name: pedir-modelo-ia
description: Redacta el encargo de un modelo 3D (personaje, arma, objeto, prop de escenario) como prompt listo para pegar en una IA generativa de 3D como Meshy, siguiendo el estilo y las restricciones del juego de terror (Unity). Úsala cuando el usuario diga que necesita un modelo, que se lo pedirá a la IA, o pida "el prompt del modelo".
---

# Pedir un modelo a la IA generativa

El usuario genera el modelo con una IA 3D (normalmente Meshy) y se lo pasa a Claude para importarlo. Esta skill
produce **el prompt**, no el modelo. No gastes créditos de la API (`generate_model`) sin que el usuario lo pida
expresamente: por defecto solo entregas el texto.

## Qué preguntar (solo lo que falte; si es obvio, decídelo y dilo)
- Qué es y para qué se usa en el juego (zombi, jefe, arma, mueble…).
- Si es un **personaje que irá a Mixamo** (humanoide) o un **objeto/arma/prop** (sin rig).
- Estilo: realista medio-horror tipo RE2 salvo que el usuario diga otra cosa.

## Reglas del prompt
- **En inglés**, una sola descripción, **máximo ~780 caracteres** (Meshy rechaza más de 800).
- Un solo objeto, centrado, **sin fondo, sin suelo, sin peana, sin texto**.
- **Personajes humanoides** (para Mixamo):
  - pose **A-pose o T-pose**, brazos separados del cuerpo, **manos abiertas con los dedos separados**, piernas
    ligeramente abiertas, mirando al frente;
  - **sin armas ni objetos en las manos**, sin capa ni ropa suelta que cubra los brazos;
  - cuerpo de proporciones humanas normales, ropa y heridas modeladas, no solo pintadas;
  - zombis: piel pálida o gris, heridas, ropa rota, ojos apagados; **no** encorvados ni con los brazos adelantados
    (eso lo da la animación).
- **Armas y objetos**: orientación recta (cañón o eje largo hacia delante), sin mano ni soporte, materiales
  separables (metal, madera, plástico), detalle de bordes y desgaste.
- Estética: PBR realista, texturas sucias y gastadas, nada de cartoon salvo petición.
- Evita nombres de marcas o franquicias; descríbelo.

## Formato de la respuesta
1. **Prompt** en un bloque de código (listo para copiar, con el recuento de caracteres).
2. **Prompt negativo** si la herramienta lo admite (p. ej. `extra limbs, bent arms, weapon in hands, base, background, text`).
3. **Ajustes recomendados**: modelo/versión más reciente, topología triangular o cuádrica, polígonos objetivo
   (personaje 15-30k, arma 5-12k, prop 2-8k), textura PBR activada, pose A/T si aplica.
4. Si el encargo es un personaje: pide **2 variantes** para elegir.

## Qué hacer cuando el usuario devuelva el modelo
- Pide que lo deje en `Tools/raw_generated/` (FBX o GLB). Los personajes humanoides siguen el flujo de
  `Tools/mixamo/LEEME.md` (subir a Mixamo, descargar con **T-pose, FBX for Unity**) y se importan con
  `MixamoImport`; los objetos y armas se importan directos a `Assets/_Project/Art/`.
- Comprueba tamaño real (un humano ≈ 1,8 m; pistola ≈ 0,2 m; escopeta ≈ 1,0 m), orientación y escala antes de
  colocarlo.
- Si el modelo sale deforme (brazos torcidos, dedos fundidos), no lo "arregles" con scripts pesados: regenera con un
  prompt más estricto o usa la skill `modelo-blender` para hacerlo a mano. Los modelos decimados o reposados por
  script salieron amorfos en este proyecto.
- La clave de API de Meshy nunca se pega en el chat; si hace falta, va en variables de entorno.
