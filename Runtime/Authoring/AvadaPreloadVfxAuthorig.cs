#if UNITY_EDITOR
using System;
using System.Linq;
using AvadaKedavrav2;
using AvadaKedavrav2.So;
using Unity.Entities;
using UnityEngine;

namespace AvadaKedavra2.Runtime.Authoring
{
    [DisallowMultipleComponent]
    public class AvadaPreloadVfxAuthorig : MonoBehaviour
    {
        [SerializeField] private AvadaKedavraV2EffectSo[] _effectSo = Array.Empty<AvadaKedavraV2EffectSo>();

        class _ : Baker<AvadaPreloadVfxAuthorig>
        {
            public override void Bake(AvadaPreloadVfxAuthorig authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                foreach (var effect in authoring._effectSo)
                {
                    var ef = CreateAdditionalEntity(TransformUsageFlags.None);
                    AddComponent(ef, new AvadaPreloadVfx() { vfx = effect });
                }
            }
        }
    }
}

#endif