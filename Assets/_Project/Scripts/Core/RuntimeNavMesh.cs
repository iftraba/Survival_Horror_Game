using Unity.AI.Navigation;
using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Hornea el NavMesh al arrancar. Evita depender de datos de NavMesh guardados en la escena
    /// y permite excluir del horneado objetos que bloquean el paso (las hojas de las puertas).
    /// Se hace en Start, no en Awake: los NavMeshModifier (p. ej. "los muebles no son suelo") se registran en su
    /// OnEnable, y si se hornea antes no se aplican y los zombis acaban caminando por encima de las mesas.
    /// El orden de ejecucion adelantado hace que este Start vaya antes que el de los zombis.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class RuntimeNavMesh : MonoBehaviour
    {
        public NavMeshSurface surface;
        [Tooltip("Objetos que se desactivan mientras se hornea (p. ej. la hoja de la puerta)")]
        public GameObject[] disableDuringBake;

        void Start()
        {
            if (surface == null) surface = GetComponent<NavMeshSurface>();
            if (surface == null) return;

            foreach (var g in disableDuringBake) if (g != null) g.SetActive(false);
            surface.BuildNavMesh();
            foreach (var g in disableDuringBake) if (g != null) g.SetActive(true);
        }

        /// <summary>Rehace el NavMesh en segundo plano (al romper muebles el jefe): las fuentes se recogen ahora, con las puertas fuera.</summary>
        public void Refresh()
        {
            if (surface == null || surface.navMeshData == null) return;
            foreach (var g in disableDuringBake) if (g != null) g.SetActive(false);
            surface.UpdateNavMesh(surface.navMeshData);
            foreach (var g in disableDuringBake) if (g != null) g.SetActive(true);
        }
    }
}
