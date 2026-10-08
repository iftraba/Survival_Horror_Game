using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Variacion de color de un mueble (2026-10-08): multiplica el color base de sus materiales por 'tint' con un
    /// MaterialPropertyBlock (no crea materiales nuevos). Asi dos sillas o mesas del mismo modelo no salen identicas.
    /// </summary>
    public class PropVariant : MonoBehaviour
    {
        public Color tint = Color.white;
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        void Awake() => Apply();

        public void Apply()
        {
            var mpb = new MaterialPropertyBlock();
            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                if (r.sharedMaterial == null || !r.sharedMaterial.HasProperty(BaseColorId)) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColorId, r.sharedMaterial.GetColor(BaseColorId) * tint);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
