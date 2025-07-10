using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace AvadaKedavrav2
{
    [BurstCompile]
    internal partial struct UpdateEmittersOneShootWithStripsJob : IJob
    {
        public NativeQueue<AvadaKedavraRequest> requests;
        public NativeList<RuntimeEmitter> plannedEmitters;
        public NativeList<RuntimeEmitter> dropppedEmitters;
        public NativeArray<UnsafeQueue<int>> stripsPool;
        public AvadaKedavraRoot root;
        public double elapsed;
       public NativeList<AvadaAliveOneShootStrips> aliveEffects;
        [ReadOnly] public NativeArray<AvadaKedavraEmitter> emitters;

        [BurstCompile]
        public void Execute()
        {
            #region Create emitters from requests

            while (requests.TryDequeue(out var request))
            {
                if(aliveEffects.Length>=root.hardCapacityLimit) continue;
                var alive = new AvadaAliveOneShootStrips()
                {
                    invalidateAt = elapsed + request.lifetime,
                    reservedStrips = new UnsafeList<AvadaKedavraStripId>(1, Allocator.Persistent),
                };
                for (var i = 0; i < emitters.Length; i++)
                {
                    var emitter = emitters[i];
                    var planned = new RuntimeEmitter()
                    {
                        particles = emitter.particles,
                        at = elapsed + emitter.delay,
                        eventId = emitter.eventId,
                        request = request,
                        stripIndex = -1,
                    };
                    if (emitter.stripData.stripped)
                    {
                        if (Hint.Unlikely(!stripsPool[i].TryDequeue(out var stripIndex)))
                        {
#if AVADA_ENABLE_LOG_ERRORS
                            Debug.LogError($"[Avada] Strip pool  is less than zero, its not good, u must increase pool for this strip");
#endif
                        }
                        else
                        {
                            alive.reservedStrips.Add(new AvadaKedavraStripId() { id = new int2(i, stripIndex) });
                            planned.stripIndex = stripIndex;
                        }
                    }

                    plannedEmitters.Add(planned);
                }


                aliveEffects.Add(alive);
            }

            #endregion

            #region Drop emitters

            for (int i = plannedEmitters.Length - 1; i >= 0; i--)
            {
                var emitter = plannedEmitters[i];

                if (emitter.at <= elapsed)
                {
                    plannedEmitters.RemoveAt(i);
                    // if (emitter.bufferIndex != 0 && emitter.version != data[emitter.bufferIndex].version)
                    // {
                    //If particle was die before all emitters complete we must break off this emitter
// #if AVADA_KEDAVRA_DEBUG
//                         Debug.Log("[Avada] Remove outdated emitter");
// #endif
                    // continue;
                    // }

                    dropppedEmitters.Add(emitter);
                }
            }
#if AVADA_KEDAVRA_DEBUG
            Debug.Log($"[Avada] Drop emitters length: {dropppedEmitters.Length}");
#endif

            #endregion
        }
    }
}