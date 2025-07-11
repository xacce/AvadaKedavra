using System;
using System.Runtime.CompilerServices;
using AvadaKedavrav2.So;
using Core.Hybrid;
using DotsCore.Keke;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;


namespace AvadaKedavrav2
{
    [Serializable]
    public struct AvadaKedavraVfxId : IEquatable<AvadaKedavraVfxId>
    {
#if UNITY_EDITOR
        [IdSelector(typeof(AvadaKedavraV2EffectSo))]
#endif
        public int id;

        public AvadaKedavraVfxId(int id)
        {
            this.id = id;
        }

        public bool Equals(AvadaKedavraVfxId other)
        {
            return id == other.id;
        }

        public override bool Equals(object obj)
        {
            return obj is AvadaKedavraVfxId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return id;
        }

        public override string ToString()
        {
            return id.ToString();
        }
    }

    public struct AvadaKedavraRoot
    {
        public AvadaEffectType avadaEffectType;
        public AvadaSyncType avadaSyncType;
        public int initialBufferCapacity;
        public int bufferGrowRate;
        public int hardCapacityLimit;
        public int hardCapPerFrameCapacityGrowLimit;
    }

    [Serializable]
    public struct AvadaKedavraEmitter
    {
        public enum StripCleanupMode
        {
            Lifetime,
            Parent,
        }

        [Serializable]
        public struct StripData
        {
            // public int stripId;
            public bool stripped;

            public int stripsMaxCount;
            // public StripCleanupMode cleanupMode;
        }

        public float delay;
        public uint particles;
        public int eventId;
        public StripData stripData;
    }

    [Serializable]
    public struct AvadaKedavraManagedEmitter
    {
        public float delay;
        public uint particles;
        public string eventId;
        public AvadaKedavraEmitter.StripData stripData;
    }

    public struct AvadaKedavraStripId
    {
        public int2 id;
        public int emitterIndex => id.x;
        public int stripIndex => id.y;
    }

    internal struct AvadaAliveBufferedStrips
    {
        public int bufferId;
        public Entity bind;
        public UnsafeList<AvadaKedavraStripId> reservedStrips;
    }

    internal struct AvadaAliveOneShootStrips
    {
        public double invalidateAt;
        public UnsafeList<AvadaKedavraStripId> reservedStrips;
    }

    internal struct AvadaAliveBuffered
    {
        public int bufferId;
        public Entity bind;
    }


    public partial struct AvadaKedavraData : IComponentData
    {
        public AvadaKedavdaElement value;
    }

    [Serializable]
    public partial struct AvadaKedavra2BakedRequest
    {
        public AvadaKedavraVfxId id;

        public float lifetime;

        public bool isValid => id.id != 0;

        // void kek()=>vfx.Value.GetInstanceID()
        public AvadaKedavraRequest AsRequest() => new AvadaKedavraRequest()
        {
            id = id,
            lifetime = lifetime,
            hot = 1
        };
    }

    public partial struct AvadaPreloadVfx : IComponentData
    {
        public UnityObjectRef<AvadaKedavraV2EffectSo> vfx;

        public AvadaKedavraRequest AsRequest() => new AvadaKedavraRequest()
        {
            vfx = vfx,
        };
    }

    [InternalBufferCapacity(0)]
    public partial struct AvadaKedavraRequest : IBufferElementData
    {
        public byte hot;
        public Vector3 from;
        public Vector3 to;
        public Vector3 direction;
        public Vector3 scale;
        public Vector3 extra;
        public UnityObjectRef<AvadaKedavraV2EffectSo> vfx;
        public AvadaKedavraVfxId id;
        public Entity bind;
        public float lifetime;
        public bool isValid => id.id != 0;

        public void SetLifetime(float lifetime)
        {
            this.lifetime = lifetime;
        }

        public void Bind(Entity bind)
        {
            this.bind = bind;
        }

        public void Extra(Vector3 extra)
        {
            this.extra = extra;
        }

        public void Scale(Vector3 scale)
        {
            this.scale = scale;
        }

        public void Scale(float scale)
        {
            this.scale = new Vector3(scale, scale, scale);
        }

        public void Direction(Vector3 direction)
        {
            this.direction = direction;
        }

        public void To(Vector3 to)
        {
            this.to = to;
        }

        public void From(Vector3 from)
        {
            this.from = from;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Send(EntityCommandBuffer ecb, Entity avadaEntity, AvadaKedavraRequest request)
        {
            ecb.AppendToBuffer(avadaEntity, request);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Send(int index, EntityCommandBuffer.ParallelWriter ecb, Entity avadaEntity, AvadaKedavraRequest request)
        {
            ecb.AppendToBuffer(index, avadaEntity, request);
        }
    }

    public partial struct RuntimeEmitter
    {
        public uint particles;
        public int eventId;
        public int stripIndex;

        public int bufferIndex;

        // public int version;
        public double at;
        public AvadaKedavraRequest request;
    }


    public partial struct AvadaKedavraRequestLog : IBufferElementData
    {
        public AvadaKedavraRequest request;
    }

    public enum AvadaEffectType
    {
        OneHoot,
        Buffered,
        BufferedWithStrips,
        OneShootWithStrips,
    }

    [Flags]
    public enum AvadaSyncType
    {
        LocalToWorld = 1 << 0,
        AvadaKedavraAll = 1 << 1,
        LocalTransform = 1 << 2,
        // AvadaKedavraOnlyExtra = 1 << 1,
    }
}