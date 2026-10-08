using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Zombi que se agolpa tras la verja (exterior de la comisaria): no persigue ni detecta, mira a la verja y la golpea y
    /// sacude cada poco (la misma animacion de ataque que contra las puertas). Si le disparan, se vuelve un zombi normal (pero
    /// la verja le cierra el paso).
    /// </summary>
    [RequireComponent(typeof(ZombieAI))]
    public class FenceRattler : MonoBehaviour
    {
        [Tooltip("Hacia donde mira (la verja)")] public Vector3 faceDir = Vector3.forward;
        public Vector2 every = new Vector2(1.2f, 2.6f);
        ZombieAI ai; float next;

        void Start()
        {
            ai = GetComponent<ZombieAI>();
            ai.dormant = true; ai.wakeOnlyWhenShot = true;
            transform.rotation = Quaternion.LookRotation(faceDir);
            next = Time.time + Random.Range(0f, every.y);
        }

        void Update()
        {
            if (ai == null || !ai.IsDormant || ai.Hp.IsDead) { enabled = false; return; }
            if (Time.time < next) return;
            next = Time.time + Random.Range(every.x, every.y);
            ai.BashDoor();
            if (Random.value < 0.3f) GameAudio.Play(Sfx.DoorLocked, transform.position + faceDir * 0.5f, 0.35f, Random.Range(1.4f, 1.8f));   // traqueteo de la verja
        }
    }
}
