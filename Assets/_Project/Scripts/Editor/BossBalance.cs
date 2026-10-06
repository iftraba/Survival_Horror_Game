#if UNITY_EDITOR
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Horror.EditorTools
{
    /// <summary>
    /// Calcula la vida del jefe a partir de "muere con N escopetazos a la cabeza". Se simulan miles de disparos con la
    /// dispersion REAL de la escopeta (perdigones, angulo, dano) contra las zonas de impacto del jefe en reposo, apuntando
    /// al centro de la cabeza, y se mide el dano medio por disparo a varias distancias.
    /// </summary>
    public static class BossBalance
    {
        public struct Result { public float avgDamage, avgPelletsOnHead, avgPelletsHit, hitFraction; }

        public static Result Simulate(GameObject bossPrefab, WeaponData gun, float distance, int shots = 6000, bool aimAtTorso = false)
        {
            var inst = Object.Instantiate(bossPrefab);
            try
            {
                var an = inst.GetComponentInChildren<Animator>();
                var clip = ZombieKit.Clip("B_Idle");
                AnimationMode.StartAnimationMode();
                AnimationMode.SampleAnimationClip(an.gameObject, clip, 0.4f);
                AnimationMode.StopAnimationMode();
                var zones = inst.GetComponent<ZombieHitZones>();
                typeof(ZombieHitZones).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(zones, null);
                // las zonas leen las posiciones de los huesos en el momento del disparo: se dejan en la pose muestreada
                AnimationMode.StartAnimationMode();
                AnimationMode.SampleAnimationClip(an.gameObject, clip, 0.4f);

                var head = an.GetBoneTransform(HumanBodyBones.Head);
                var neck = an.GetBoneTransform(HumanBodyBones.Neck);
                var hips = an.GetBoneTransform(HumanBodyBones.Hips);
                float sc = an.transform.lossyScale.y;
                Vector3 target = aimAtTorso ? Vector3.Lerp(hips.position, neck.position, 0.7f)
                                            : head.position + (head.position - neck.position).normalized * zones.headLift * sc;
                // el jugador esta delante del jefe, a 'distance' m y a la altura de la camara, mirando al objetivo
                Vector3 fwd = inst.transform.forward;
                Vector3 origin = new Vector3(inst.transform.position.x, 0f, inst.transform.position.z) + fwd * distance + Vector3.up * 1.7f;
                Quaternion look = Quaternion.LookRotation((target - origin).normalized);
                var rnd = new System.Random(12345);
                double total = 0, onHead = 0, hit = 0; int anyHit = 0;
                for (int s = 0; s < shots; s++)
                {
                    bool shotHit = false;
                    for (int p = 0; p < gun.pellets; p++)
                    {
                        float pitch = (float)(rnd.NextDouble() * 2 - 1) * gun.spread, yaw = (float)(rnd.NextDouble() * 2 - 1) * gun.spread;
                        var ray = new Ray(origin, look * Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward);
                        if (!zones.Test(ray, gun.range, out float mult, out _, out bool headshot)) continue;
                        total += gun.damage * mult; hit++; shotHit = true;
                        if (headshot) onHead++;
                    }
                    if (shotHit) anyHit++;
                }
                AnimationMode.StopAnimationMode();
                return new Result { avgDamage = (float)(total / shots), avgPelletsOnHead = (float)(onHead / shots), avgPelletsHit = (float)(hit / shots), hitFraction = anyHit / (float)shots };
            }
            finally { Object.DestroyImmediate(inst); }
        }

        /// <summary>Fija la vida del jefe para que muera con 'headShots' escopetazos a la cabeza a 'distance' m. Devuelve el informe.</summary>
        public static string Apply(int headShots = 15, float distance = 5f)
        {
            var prefabPath = "Assets/_Project/Prefabs/Characters/Boss.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var gun = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/Data/W_Shotgun.asset");
            var sb = new StringBuilder();
            sb.AppendLine($"Escopeta: {gun.pellets} perdigones x {gun.damage} de dano, dispersion +-{gun.spread} grados, cabeza x{prefab.GetComponent<ZombieHitZones>().headMultiplier}");
            Result atRef = default;
            foreach (float d in new[] { 3f, 5f, 8f, 12f })
            {
                var r = Simulate(prefab, gun, d);
                if (Mathf.Approximately(d, distance)) atRef = r;
                sb.AppendLine($"  a {d,4:F0} m apuntando a la cabeza: {r.avgDamage,6:F0} de dano medio ({r.avgPelletsOnHead:F1} perdigones en la cabeza, {r.avgPelletsHit:F1} en total)");
            }
            var torso = Simulate(prefab, gun, distance, 4000, true);
            float hp = Mathf.Round(atRef.avgDamage * headShots / 10f) * 10f;
            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            root.GetComponent<Health>().maxHealth = hp;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            float pistolHead = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Project/Data/W_Pistol.asset").damage * 3f;
            sb.AppendLine($"Vida del jefe = {headShots} x {atRef.avgDamage:F0} (a {distance:F0} m) = {hp:F0}");
            sb.AppendLine($"  a la cabeza: {hp / atRef.avgDamage:F1} disparos | al torso: {hp / Mathf.Max(1f, torso.avgDamage):F0} disparos ({torso.avgDamage:F0} de dano medio) | pistola a la cabeza: {hp / pistolHead:F0} balas");
            return sb.ToString();
        }
    }
}
#endif
