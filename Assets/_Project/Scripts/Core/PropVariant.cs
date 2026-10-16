using UnityEngine;

namespace Horror
{
    /// <summary>
    /// Variacion de color de un mueble (2026-10-08): multiplica el color base de sus materiales por 'tint' con un
    /// MaterialPropertyBlock (no crea materiales nuevos). Asi dos sillas o mesas del mismo modelo no salen identicas.
    /// 2026-10-16: un bloque por submalla (los modelos AA llevan varios materiales; con un solo bloque todos tomaban el color del primero y el
    /// cristal perdia su transparencia).
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
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null || !mats[i].HasProperty(BaseColorId)) continue;
                    var c = mats[i].GetColor(BaseColorId);
                    r.GetPropertyBlock(mpb, i);
                    mpb.SetColor(BaseColorId, new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a));    // el alfa del material se conserva
                    r.SetPropertyBlock(mpb, i);
                }
            }
        }
    }
}
