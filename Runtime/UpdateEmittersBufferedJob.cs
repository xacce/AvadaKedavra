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
    internal partial struct UpdateEmittersBufferedJob : IJob
    {
        public NativeQueue<AvadaKedavraRequest> requests;
        public NativeList<RuntimeEmitter> plannedEmitters;
        public NativeList<RuntimeEmitter> dropppedEmitters;
        public NativeQueue<int> idPool;
        public NativeQueue<AvadaKedavraRequest> cuttedRequests;
        public AvadaKedavraRoot root;
        public int bufferLength;
        public double elapsed;
        [WriteOnly] public NativeList<AvadaAliveBuffered> aliveEffects;
        [ReadOnly] public NativeArray<AvadaKedavraEmitter> emitters;

        [BurstCompile]
        public void Execute()
        {
            #region Create emitters from requests

            while (requests.TryDequeue(out var request))
            {
                if (request.bind.Equals(Entity.Null))
                {
#if AVADA_ENABLE_LOG_ERRORS
                    Debug.LogError($"[Avada] Cant use {request.id} vfx, because it requires bind");
#endif
                    continue;
                }

                var bufferIndex = -1;
                
                    if (Hint.Unlikely(!idPool.TryDequeue(out bufferIndex)))
                    {
                        var grow = (cuttedRequests.Count + 1) * root.bufferGrowRate;

                        if (grow > 0 && grow <= root.hardCapPerFrameCapacityGrowLimit && grow + bufferLength <= root.hardCapacityLimit)
                        {
#if AVADA_ENABLE_GROW_LOG
                            Debug.Log($"[Avada] Growing buffer by {grow} for {request.id}");
#endif
                            cuttedRequests.Enqueue(request);
                        }
#if AVADA_ENABLE_LOG_ERRORS
                        else
                        {
                            Debug.LogError($"[Avada] Cant reserve particle for {request.id}, pool is full");
                        }
#endif

                        continue;
                    }

                var alive = new AvadaAliveBuffered()
                {
                    bufferId = bufferIndex,
                    bind = request.bind,
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
                        planned.bufferIndex = bufferIndex;

                    

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