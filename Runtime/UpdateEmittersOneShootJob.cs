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
    internal partial struct UpdateEmittersJob : IJob
    {
        public NativeQueue<AvadaKedavraRequest> requests;
        public NativeList<RuntimeEmitter> plannedEmitters;
        public NativeList<RuntimeEmitter> dropppedEmitters;
        public AvadaKedavraRoot root;
        public double elapsed;
        [ReadOnly] public NativeArray<AvadaKedavraEmitter> emitters;

        [BurstCompile]
        public void Execute()
        {
            #region Create emitters from requests

            while (requests.TryDequeue(out var request))
            {
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

                    plannedEmitters.Add(planned);
                }
            }

            #endregion

            #region Drop emitters

            for (int i = plannedEmitters.Length - 1; i >= 0; i--)
            {
                var emitter = plannedEmitters[i];

                if (emitter.at <= elapsed)
                {
                    plannedEmitters.RemoveAt(i);
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