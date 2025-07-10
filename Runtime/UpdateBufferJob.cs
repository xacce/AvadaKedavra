using DotsCore.Keke;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

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
        [ReadOnly] public ComponentLookup<AvadaKedavraData> avadaRo;
        [ReadOnly] public EntityStorageInfoLookup entityStorageInfoLookup;

        [BurstCompile]
        public void Execute()
        {
            #region Update

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
                var current = new AvadaKedavdaElement();
                if ((root.avadaSyncType & AvadaSyncType.LocalToWorld) != 0 && ltwRo.TryGetComponent(element.bind, out var ltw))
                {
                    current.from = ltw.Position;
                    current.direction = ltw.Forward;
                }

                if ((root.avadaSyncType & AvadaSyncType.AvadaKedavraAll) != 0 && avadaRo.TryGetComponent(element.bind, out var avada))
                {
                    current.direction = avada.value.direction;
                    current.from = avada.value.from;
                    current.to = avada.value.to;
                    current.scale = avada.value.scale;
                    current.extra = avada.value.extra;
                }

                rwData[element.bufferId] = current;
            }

            #endregion
        }
    }
}