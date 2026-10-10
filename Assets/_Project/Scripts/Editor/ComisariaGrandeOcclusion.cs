#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Comisaria grande (menu Horror/Comisaria grande/8 Hornear oclusion): occlusion culling horneado. Sin el, cada vista dibuja tambien lo que queda
    /// tras las paredes dentro del cono de la camara (el vestibulo llegaba a ~7.500 llamadas de dibujo). Paredes, suelos y techos son occluders
    /// (estaticos); las estanterias, taquillas y archivadores NO lo son (son porosas y tapaban las cajas de dentro; ver ComisariaGrandeProps.Place).
    /// Hay que repetirlo despues de rehacer el nivel (menus 1 a 6). El horneado va en segundo plano (~1 min).
    /// Medido en Play el 2026-10-10 (docs/rendimiento.md): llamadas de dibujo del vestibulo 7.544 -> 3.014 y CPU 21,1 -> 13,1 ms.
    /// </summary>
    public static class ComisariaGrandeOcclusion
    {
        [MenuItem("Horror/Comisaria grande/8 Hornear oclusion")]
        public static void Menu() { Debug.Log("[Horror] " + Build()); }

        public static string Build()
        {
            if (EditorApplication.isPlaying) return "no con el editor en Play";
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ComisariaGrande.ScenePath) scene = EditorSceneManager.OpenScene(ComisariaGrande.ScenePath, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene);
            if (StaticOcclusionCulling.isRunning) return "ya se esta horneando";
            return StaticOcclusionCulling.GenerateInBackground() ? "oclusion: horneando en segundo plano (consulta StaticOcclusionCulling.isRunning; al acabar, guarda la escena)" : "no se pudo iniciar el horneado";
        }
    }
}
#endif
