using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Mantiene atrancada una puerta hasta que se cumple una marca de progreso (comisaria grande: la puerta del pasillo de las
    /// calderas no tiene corriente hasta poner los tres fusibles). Con la marca, la puerta se libera sola (tambien al cargar).
    /// </summary>
    public class ProgressSeal : MonoBehaviour
    {
        public Door door;
        public string flag = "corriente";
        [Tooltip("Luz o piloto que se enciende cuando hay corriente")] public GameObject poweredVisual;
        [Tooltip("Luz o piloto que se ve sin corriente")] public GameObject unpoweredVisual;

        void OnEnable() { Progress.Changed += Check; }
        void OnDisable() => Progress.Changed -= Check;
        void Start() => Check();

        bool waiting, applied;

        void Check()
        {
            if (door == null) return;
            bool on = Progress.Has(flag);
            if (on && !applied && !waiting)
            {
                float delay = ProgressCutscene.DelayFor(flag);             // con secuencia de camara, la corriente llega cuando la camara llega a la puerta
                if (delay > 0f) { StartCoroutine(ApplyAfter(delay)); return; }
            }
            if (waiting) return;
            Apply(on);
        }

        System.Collections.IEnumerator ApplyAfter(float delay)
        {
            waiting = true;
            yield return new WaitForSeconds(0.2f);
            for (float w = 0f; ProgressCutscene.Travelling(flag) && w < 40f; w += Time.deltaTime) yield return null;      // espera a que la camara llegue
            yield return new WaitForSeconds(0.6f);
            waiting = false;
            Apply(Progress.Has(flag));
        }

        void Apply(bool on)
        {
            applied = on;
            if (on) door.Unseal(); else door.Seal();
            if (poweredVisual != null) poweredVisual.SetActive(on);
            if (unpoweredVisual != null) unpoweredVisual.SetActive(!on);
            if (on) GameAudio.Play(Sfx.DoorUnlock, door.transform.position);
        }
    }
}
