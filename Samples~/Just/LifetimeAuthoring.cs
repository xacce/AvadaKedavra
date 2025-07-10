using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace AvadaKedavrav2.Samples
{
    [DisallowMultipleComponent]
    public class LifetimeAuthoring : MonoBehaviour
    {
        [SerializeField] private float maxLifetime_s = 1f;

        private class LifetimeBaker : Baker<LifetimeAuthoring>
        {
            public override void Bake(LifetimeAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent<Lifetime>(e);
                SetComponent(
                    e,
                    new Lifetime()
                    {
                        lifetime = authoring.maxLifetime_s,
                    });
            }
        }
    }

    public partial struct Lifetime : IComponentData
    {
        public float lifetime;
    }
    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public partial struct LifetimeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndInitializationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var delatime = SystemAPI.Time.DeltaTime;
            foreach (var (lifetime, entity) in SystemAPI.Query<RefRW<Lifetime>>().WithEntityAccess())
            {
                lifetime.ValueRW.lifetime -= delatime;
                if (lifetime.ValueRO.lifetime < 0)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}