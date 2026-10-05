using Unity.AI.Navigation;
using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Hornea el NavMesh al arrancar. Evita depender de datos de NavMesh guardados en la escena
    /// y permite excluir del horneado objetos que bloquean el paso (la puerta).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class RuntimeNavMesh : MonoBehaviour
    {
        public NavMeshSurface surface;
        [Tooltip("Objetos que se desactivan mientras se hornea (p. ej. la hoja de la puerta)")]
        public GameObject[] disableDuringBake;

        void Awake()
        {
            if (surface == null) surface = GetComponent<NavMeshSurface>();
            if (surface == null) return;

            foreach (var g in disableDuringBake) if (g != null) g.SetActive(false);
            surface.BuildNavMesh();
            foreach (var g in disableDuringBake) if (g != null) g.SetActive(true);
        }
    }
}
