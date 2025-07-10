using AvadaKedavrav2;
using UnityEngine;
using UnityEngine.VFX;

namespace DotsCore.Keke
{
    [VFXType(VFXTypeAttribute.Usage.GraphicsBuffer)]
    public struct AvadaKedavdaElement
    {
        public int dead;
        public Vector3 from;
        public Vector3 to;
        public Vector3 direction;
        public Vector3 scale;
        public Vector3 extra;
    }
}