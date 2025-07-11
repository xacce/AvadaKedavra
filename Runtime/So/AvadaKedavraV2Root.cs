using System;
using Core.Hybrid;
using UnityEngine;
using UnityEngine.VFX;

namespace AvadaKedavrav2.So
{
    [CreateAssetMenu(menuName = "AvadaKedavra/New effect")]
    public class AvadaKedavraV2EffectSo : ScriptableObject, IUniqueIdProvider
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

        public AvadaKedavraVfxId avadaId => new AvadaKedavraVfxId(id_s);
        public int id => id_s;

        public AvadaKedavraRequest AsColdRequest()
        {
            return new AvadaKedavraRequest()
            {
                id = avadaId,
                vfx = this,
                lifetime = baseLifetime,
            };
        }

        public AvadaKedavraRequest AsHotRequest()
        {
            return new AvadaKedavraRequest()
            {
                id = avadaId,
                lifetime = baseLifetime,
                hot = 1,
            };
        }

        public AvadaKedavra2BakedRequest AsHotBlobRequest()
        {
            return new AvadaKedavra2BakedRequest()
            {
                id = avadaId,
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