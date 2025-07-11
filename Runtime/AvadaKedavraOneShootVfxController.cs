using System.Text;
using AvadaKedavra2.Runtime;
using AvadaKedavrav2.So;
using DotsCore.Keke;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Core;
using Unity.Jobs;
using UnityEngine;

namespace AvadaKedavrav2
{
    public class AvadaKedavraOneShootVfxController : AvadaKedavraBaseVfxController
    {
        public override void DoLoad(AvadaKedavraV2EffectSo request)
        {
            if (isLoaded || isLoading) return;
            Load(request);
        }


        public override JobHandle JobUpdateEmitters(TimeData time)
        {
            return new UpdateEmittersJob()
            {
                requests = _requests,
                plannedEmitters = _plannedEmitters,
                dropppedEmitters = _dropppedEmitters,
                emitters = _emitters,
                elapsed = time.ElapsedTime,
                root = _rootUnmanaged,
            }.Schedule();
        }

        public override JobHandle PreUpdate(JobHandle dependency, TimeData time, AvadaKedavraDispatchSystem.Lookups lookups)
        {
            return default;
        }


        public override void PostUpdate()
        {
        }


        public override bool HasState()
        {
            return false;
        }

        public override void DrawDebug(StringBuilder str)
        {
            str.AppendLine($"[{_rootManaged.avadaId.id}] Queued emitters: {_plannedEmitters.Length}");
            str.AppendLine($"[{_rootManaged.avadaId.id}] Alive GPU: {_effect.aliveParticleCount}");
        }

        public override bool CanDeactivated()
        {
            return false;
        }

    }
}