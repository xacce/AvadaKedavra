using AvadaKedavrav2;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;

namespace AvadaKedavra2.Runtime
{
#if AVADA_DISABLE_AUTO_START || AVADA_CUSTOM_PRELOAD_SYSTEM
 [DisableAutoCreation]
#else
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SceneSystemGroup))]
#endif
    [BurstCompile]
    partial struct AvadKedavraDeliveryPreloadedVfxSystem : ISystem
    {
        private EntityQuery _q;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _q = state.GetEntityQuery(ComponentType.ReadWrite<AvadaPreloadVfx>());
            state.RequireForUpdate(_q);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //AVADA_CUSTOM_PRELOAD_SYSTEM set for create custom preload system 
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            var preload = _q.ToComponentDataArray<AvadaPreloadVfx>(Allocator.Temp);
            var avadaRw = SystemAPI.GetSingletonBuffer<AvadaKedavraRequest>(false);
            for (int i = 0; i < preload.Length; i++)
            {
                avadaRw.Add(preload[i].AsRequest());
            }

            ecb.DestroyEntity(_q, EntityQueryCaptureMode.AtPlayback);
        }
    }
}