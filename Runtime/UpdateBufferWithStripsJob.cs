using DotsCore.Keke;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace AvadaKedavrav2
{
    [BurstCompile]
    internal partial struct UpdateBufferWithStripsJob : IJob
    {
        public NativeList<AvadaAliveBufferedStrips> aliveEffects;
        public NativeQueue<int> idPool;
        public NativeArray<UnsafeQueue<int>> stripsPool;
        public AvadaKedavraRoot root;
        public double elapsed;

        [WriteOnly] public NativeArray<AvadaKedavdaElement> rwData;
        [ReadOnly] public ComponentLookup<LocalToWorld> ltwRo;
        [ReadOnly] public ComponentLookup<LocalTransform> transformRo;
        [ReadOnly] public ComponentLookup<AvadaKedavraData> avadaRo;
        [ReadOnly] public EntityStorageInfoLookup entityStorageInfoLookup;

        [BurstCompile]
        public void Execute()
        {
            for (int i = aliveEffects.Length - 1; i >= 0; i--)
            {
                var element = aliveEffects[i];
                bool invalidate = false;
                // if (root.avadaEffectType == AvadaEffectType.Stripped && element.invalidateAt <= elapsed)
                // {
                //     invalidate = true;
                // }

                if (!entityStorageInfoLookup.Exists(element.bind))
                {
                    invalidate = true;
                }

                if (invalidate)
                {
                    aliveEffects.RemoveAt(i);
                    idPool.Enqueue(element.bufferId);
                    var elementBufferData = new AvadaKedavdaElement
                    {
                        dead = 1
                    };
                    rwData[element.bufferId] = elementBufferData;

                    if (element.reservedStrips.IsCreated)
                    {
                        for (int j = 0; j < element.reservedStrips.Length; j++)
                        {
                            var reserved = element.reservedStrips[j];
                            stripsPool[reserved.emitterIndex].Enqueue(reserved.stripIndex);
                        }

                        element.reservedStrips.Dispose();
                    }

                    continue;
                }

                if (root.avadaSyncType == 0) continue;
                var current = new AvadaKedavdaElement();
                bool update = false;
                if ((root.avadaSyncType & AvadaSyncType.LocalToWorld) != 0 && ltwRo.TryGetComponent(element.bind, out var ltw))
                {
                    // elementBufferData.from = ltw.Position + math.rotate(ltw.Rotation, element.origin.bindOffset);
                    current.from = ltw.Position;
                    current.direction = ltw.Forward;
                    update = true;
                }

                if ((root.avadaSyncType & AvadaSyncType.LocalTransform) != 0 && transformRo.TryGetComponent(element.bind, out var t))
                {
                    current.from = t.Position;
                    current.direction = t.Forward();
                    update = true;
                }

                // if ((element.syncType & SyncType.AvadaKedavraOnlyExtra) != 0 && avadaRo.TryGetComponent(element.bind, out var avada))
                // {
                //     current.direction = avada.value.direction;
                //     current.from = avada.value.from;
                //     current.to = avada.value.to;
                //     current.scale = avada.value.scale;
                //     current.extra = avada.value.extra;
                // }
                if ((root.avadaSyncType & AvadaSyncType.AvadaKedavraAll) != 0 && avadaRo.TryGetComponent(element.bind, out var avada))
                {
                    current.direction = avada.value.direction;
                    current.from = avada.value.from;
                    current.to = avada.value.to;
                    current.scale = avada.value.scale;
                    current.extra = avada.value.extra;
                    update = true;
                }

                if (update)
                    rwData[element.bufferId] = current;
               
            }
        }
    }
}