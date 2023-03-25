using NewHorizons.External.Modules.Volumes;
using NewHorizons.External.Modules.Volumes.VolumeInfos;
using UnityEngine;

namespace NewHorizons.Builder.Volumes.VisorEffects
{
    public static class VisorRainEffectVolumeBuilder
    {
        public static VisorRainEffectVolume Make(GameObject planetGO, Sector sector, VisorEffectModule.RainEffectVolumeInfo info)
        {
            var volume = PriorityVolumeBuilder.Make<VisorRainEffectVolume>(planetGO, sector, info);

            volume._rainDirection = VisorRainEffectVolume.RainDirection.Radial;
            volume._dropletRate = info.dropletRate;
            volume._streakRate = info.streakRate;

            return volume;
        }
    }
}
