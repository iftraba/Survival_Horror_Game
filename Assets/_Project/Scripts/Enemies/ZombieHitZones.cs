using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Zonas de impacto que siguen a los huesos animados: cabeza, torso, brazos y piernas como capsulas.
    /// La capsula del collider solo sirve para detectar que la bala ha llegado al zombi; aqui se decide si
    /// de verdad toca el cuerpo y donde (la cabeza hace mucho mas dano, las extremidades menos).
    /// </summary>
    public class ZombieHitZones : MonoBehaviour
    {
        public float headMultiplier = 3f;
        public float torsoMultiplier = 1f;
        public float limbMultiplier = 0.6f;

        [Header("Tamano (m, a escala 1). Se multiplican por la escala del modelo")]
        public float headRadius = 0.15f;
        [Tooltip("Cuanto por encima del hueso de la cabeza esta el centro del craneo")] public float headLift = 0.13f;
        public float torsoRadius = 0.2f;
        [Tooltip("Multiplica el radio de los brazos y las piernas")] public float limbRadiusScale = 1f;

        struct Zone
        {
            public Transform a, b;      // extremos del segmento (b null = esfera en a)
            public float radius;
            public float multiplier;
            public bool isHead;
            public Vector3 offset;      // desplazamiento local del centro de la esfera respecto al hueso
        }

        Zone[] zones;

        void Awake()
        {
            // Humanoid (Mixamo): huesos por su funcion; esqueleto propio: por nombre
            var anim = GetComponentInChildren<Animator>();
            bool human = anim != null && anim.isHuman;
            Transform Bone(string n)
            {
                if (human)
                {
                    var hb = n switch
                    {
                        "Hips" => HumanBodyBones.Hips, "Neck" => HumanBodyBones.Neck, "Head" => HumanBodyBones.Head,
                        "UpperArm.L" => HumanBodyBones.LeftUpperArm, "UpperArm.R" => HumanBodyBones.RightUpperArm,
                        "LowerArm.L" => HumanBodyBones.LeftLowerArm, "LowerArm.R" => HumanBodyBones.RightLowerArm,
                        "Hand.L" => HumanBodyBones.LeftHand, "Hand.R" => HumanBodyBones.RightHand,
                        "UpperLeg.L" => HumanBodyBones.LeftUpperLeg, "UpperLeg.R" => HumanBodyBones.RightUpperLeg,
                        "LowerLeg.L" => HumanBodyBones.LeftLowerLeg, "LowerLeg.R" => HumanBodyBones.RightLowerLeg,
                        "Foot.L" => HumanBodyBones.LeftFoot, "Foot.R" => HumanBodyBones.RightFoot,
                        _ => HumanBodyBones.LastBone,
                    };
                    if (hb != HumanBodyBones.LastBone) return anim.GetBoneTransform(hb);
                }
                return FindDeep(transform, n);
            }
            var hips = Bone("Hips"); var neck = Bone("Neck"); var head = Bone("Head");
            // un modelo escalado (jefe x1.35, Yaku x0.9...) tiene la cabeza y el torso escalados: las zonas tambien
            float sc = anim != null ? anim.transform.lossyScale.y : 1f;
            var list = new System.Collections.Generic.List<Zone>();
            // la cabeza: esfera algo por encima del hueso (el hueso esta en la base del craneo)
            if (head != null) list.Add(new Zone { a = head, b = neck, radius = headRadius * sc, multiplier = headMultiplier, isHead = true, offset = new Vector3(0f, headLift * sc, 0f) });
            if (hips != null && neck != null) list.Add(new Zone { a = hips, b = neck, radius = torsoRadius * sc, multiplier = torsoMultiplier });
            foreach (var s in new[] { "L", "R" })
            {
                float k = sc * limbRadiusScale;
                AddLimb(list, Bone("UpperArm." + s), Bone("LowerArm." + s), 0.075f * k);
                AddLimb(list, Bone("LowerArm." + s), Bone("Hand." + s), 0.065f * k);
                AddLimb(list, Bone("UpperLeg." + s), Bone("LowerLeg." + s), 0.1f * k);
                AddLimb(list, Bone("LowerLeg." + s), Bone("Foot." + s), 0.085f * k);
            }
            zones = list.ToArray();
        }

        void AddLimb(System.Collections.Generic.List<Zone> list, Transform a, Transform b, float r)
        {
            if (a != null && b != null) list.Add(new Zone { a = a, b = b, radius = r, multiplier = limbMultiplier });
        }

        /// <summary>Prueba un rayo contra las zonas. Devuelve false si pasa sin tocar el cuerpo.</summary>
        public bool Test(Ray ray, float maxDistance, out float multiplier, out Vector3 point, out bool headshot)
        {
            multiplier = 1f; point = default; headshot = false;
            if (zones == null || zones.Length == 0) return false;
            float best = float.MaxValue;
            foreach (var z in zones)
            {
                Vector3 p0, p1;
                if (z.isHead)
                {
                    // el hueso de la cabeza nace en la base del craneo: el centro va 13 cm mas alla, en la direccion cuello -> cabeza
                    Vector3 up = z.b != null ? (z.a.position - z.b.position).normalized : Vector3.up;
                    p0 = p1 = z.a.position + up * z.offset.y;
                }
                else
                {
                    p0 = z.a.position;
                    p1 = z.b != null ? z.b.position : p0;
                }
                if (!RayCapsule(ray, p0, p1, z.radius, out float t) || t > maxDistance || t >= best) continue;
                best = t;
                multiplier = z.multiplier;
                headshot = z.isHead;
            }
            if (best == float.MaxValue) return false;
            point = ray.GetPoint(best);
            return true;
        }

        /// <summary>Distancia a lo largo del rayo hasta donde entra en la capsula (aproximada por la menor distancia).</summary>
        static bool RayCapsule(Ray ray, Vector3 p0, Vector3 p1, float r, out float t)
        {
            // punto mas cercano entre la recta del rayo y el segmento p0-p1
            Vector3 d1 = ray.direction, d2 = p1 - p0, w = ray.origin - p0;
            float a = 1f, b = Vector3.Dot(d1, d2), c = Vector3.Dot(d2, d2), d = Vector3.Dot(d1, w), e = Vector3.Dot(d2, w);
            float denom = a * c - b * b;
            float s = c < 1e-6f ? 0f : Mathf.Clamp01(denom > 1e-6f ? (a * e - b * d) / denom : 0f);
            Vector3 q = p0 + d2 * s;
            t = Mathf.Max(0f, Vector3.Dot(q - ray.origin, d1));
            float dist = Vector3.Distance(ray.origin + d1 * t, q);
            if (dist > r) return false;
            t = Mathf.Max(0f, t - Mathf.Sqrt(r * r - dist * dist));   // entrada en la superficie
            return true;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root)
            {
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
