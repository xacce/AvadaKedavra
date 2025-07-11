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
    internal partial struct UpdateBufferJob : IJob
    {
        public NativeList<AvadaAliveBuffered> aliveEffects;
        public NativeQueue<int> idPool;
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
                bool invalidate = !entityStorageInfoLookup.Exists(element.bind);

                if (invalidate)
                {
                    aliveEffects.RemoveAt(i);
                    idPool.Enqueue(element.bufferId);
                    var elementBufferData = new AvadaKedavdaElement
                    {
                        dead = 1
                    };
                    rwData[element.bufferId] = elementBufferData;


                    continue;
                }

                if (root.avadaSyncType == 0) continue;
                bool update = false;
                var current = new AvadaKedavdaElement();
                if ((root.avadaSyncType & AvadaSyncType.LocalToWorld) != 0 && ltwRo.TryGetComponent(element.bind, out var ltw))
                {
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

                if ((root.avadaSyncType & AvadaSyncType.AvadaKedavraAll) != 0 && avadaRo.TryGetComponent(element.bind, out var avada))
                {
                    current.direction = avada.value.direction;
                    current.from = avada.value.from;
                    current.to = avada.value.to;
                    current.scale = avada.value.scale;
                    current.extra = avada.value.extra;
                    update = true;
                }

                if (update) rwData[element.bufferId] = current;
                
            }
        }
    }
}