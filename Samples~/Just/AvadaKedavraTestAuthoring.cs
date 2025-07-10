#if UNITY_EDITOR
using System;
using AvadaKedavrav2;
using AvadaKedavrav2.Samples;
using AvadaKedavrav2.So;
using DotsCore.Keke;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Src.PackageCandidate.AvadaKedavra
{
    public class AvadaKedavraTestAuthoring : MonoBehaviour
    {
        [SerializeField] private AvadaKedavraTest data;
        [SerializeField] private AvadaKedavraV2EffectSo effect;

        class _ : Baker<AvadaKedavraTestAuthoring>
        {
            public override void Bake(AvadaKedavraTestAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.None);
                var d = authoring.data;
                d.request = authoring.effect.AsColdRequest();
                AddComponent(e, d);
            }
        }
    }

    [Serializable]
    public partial struct AvadaKedavraTest : IComponentData
    {
        public enum Type
        {
            OneShot,
            EntityBind,
            AvadaCustom,
        }

        public AvadaKedavraRequest request;
        public Type t;
        public float delay;
        public float _delay;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    partial struct AvadaKedavraStressTestSystem : ISystem
    {
        private Lookups _lookups;
        private Random _rnd;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AvadaKedavraRequest>();
            _lookups = new Lookups(ref state);
            _rnd = Random.CreateFromIndex(3290482394);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _lookups.Update(ref state);
            var avadaEntity = SystemAPI.GetSingletonEntity<AvadaKedavraRequest>();
            var ecb = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var aRw in SystemAPI.Query<RefRW<AvadaKedavraTest>>())
            {
                ref var a = ref aRw.ValueRW;
                a._delay -= SystemAPI.Time.DeltaTime;
                if (a._delay > 0) continue;
                a._delay = a.delay;
                switch (a.t)
                {
                    case AvadaKedavraTest.Type.OneShot:
                        var r = a.request;
                        r.From(_rnd.NextFloat3(-100, 100));
                        r.Direction(math.up());
                        AvadaKedavraRequest.Send(ecb, avadaEntity, r);
                        break;
                    case AvadaKedavraTest.Type.EntityBind:
                    {
                        var e = ecb.CreateEntity();
                        ecb.AddComponent(e, new LocalTransform { Position = _rnd.NextFloat3(-100, 100), Rotation = quaternion.identity, Scale = 1f });
                        ecb.AddComponent<LocalToWorld>(e);
                        ecb.AddComponent(e, new Lifetime() { lifetime = _rnd.NextFloat(1,5) });
                        ecb.AddComponent(e, new PhysicsVelocity { Linear = _rnd.NextFloat3(-10, 10) });
                        ecb.AddComponent(e, new PhysicsDamping() { Angular = 0, Linear = 0 });
                        // ecb.AddComponent(e, PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 10f));
                        ecb.AddSharedComponent(e, new PhysicsWorldIndex { Value = 0 });

                        var r2 = a.request;
                        r2.Bind(e);
                        AvadaKedavraRequest.Send(ecb, avadaEntity, r2);
                        break;
                    }
                    case AvadaKedavraTest.Type.AvadaCustom:
                    {
                        var e = ecb.CreateEntity();
                        ecb.AddComponent(e,
                            new AvadaKedavraData { value = new AvadaKedavdaElement { from = _rnd.NextFloat3(-100, 100), to = _rnd.NextFloat3(-100, 100) } });
                        ecb.AddComponent(e, new Lifetime { lifetime = 5f });
                        var r3 = a.request;
                        r3.Bind(e);
                        AvadaKedavraRequest.Send(ecb, avadaEntity, r3);
                        break;
                    }
                }
            }
        }

        [BurstCompile]
        internal struct Lookups
        {
            public Lookups(ref SystemState state) : this()
            {
            }

            [BurstCompile]
            public void Update(ref SystemState state)
            {
            }
        }
    }
}

#endif