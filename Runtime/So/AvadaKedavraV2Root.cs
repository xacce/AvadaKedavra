using System;
using UnityEngine;
using UnityEngine.VFX;

namespace AvadaKedavrav2.So
{
    [CreateAssetMenu(menuName = "AvadaKedavra/New effect")]
    public class AvadaKedavraV2EffectSo : ScriptableObject
    {
        [SerializeField] private VisualEffectAsset vfx_s;
        [SerializeField] private int initialBufferCapacity_s;
        [SerializeField] private int bufferGrowRate_s;
        [SerializeField] private int hardCapPerFrameCapacityGrowLimit_s;
        [SerializeField] private int hardCapacityLimit_s;
        [SerializeField] private AvadaKedavraManagedEmitter[] emitters_s = Array.Empty<AvadaKedavraManagedEmitter>();
        [SerializeField] private AvadaEffectType avadaEffectType_s;
        [SerializeField] private AvadaSyncType avadaSyncType_s;
        [SerializeField] private int id_s;
        [SerializeField] private float baseLifetime_s;

        public int hardCapPerFrameCapacityGrowLimit => hardCapPerFrameCapacityGrowLimit_s;

        public float baseLifetime => baseLifetime_s;

        public AvadaKedavraVfxId id => new AvadaKedavraVfxId(id_s);

        public AvadaKedavraRequest AsColdRequest()
        {
            return new AvadaKedavraRequest()
            {
                id = id,
                vfx = this,
                lifetime = baseLifetime,
            };
        }

        public AvadaKedavraRequest AsHotRequest()
        {
            return new AvadaKedavraRequest()
            {
                id = id,
                lifetime = baseLifetime,
            };
        }

        public AvadaKedavraRequest AsHotBlobRequest()
        {
            return new AvadaKedavraRequest()
            {
                id = id,
                lifetime = baseLifetime,
            };
        }

        public AvadaKedavraRoot AsUnmanaged() => new AvadaKedavraRoot()
        {
            avadaEffectType = avadaEffectType,
            avadaSyncType = avadaSyncType,
            initialBufferCapacity = initialBufferCapacity,
            bufferGrowRate = bufferGrowRate,
            hardCapacityLimit = hardCapacityLimit_s,
            hardCapPerFrameCapacityGrowLimit = hardCapPerFrameCapacityGrowLimit_s,
        };

        public AvadaKedavraManagedEmitter[] emitters => emitters_s;

        public AvadaEffectType avadaEffectType => avadaEffectType_s;

        public AvadaSyncType avadaSyncType => avadaSyncType_s;

        public int initialBufferCapacity => initialBufferCapacity_s;

        public int bufferGrowRate => bufferGrowRate_s;


        public VisualEffectAsset vfx => vfx_s;

    }
}