using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NewHorizons.Utility;
using Newtonsoft.Json;

namespace NewHorizons.External.Modules
{
    [JsonObject]
    public class AmbientLightModule
    {
        /// <summary>
        /// The range of the light. Defaults to surfaceSize * 2.
        /// </summary>
        [Range(0, double.MaxValue)] public float outerRadius;

        /// <summary>
        /// The lower radius where the light is brightest, fading in from outerRadius. Defaults to half of outerRadius.
        /// </summary>
        [Range(0, double.MaxValue)] public float innerRadius;

        /// <summary>
        /// The brightness of the light. For reference, Timber Hearth is `1.4`, and Giant's Deep is `0.8`.
        /// </summary>
        [Range(0, double.MaxValue)][DefaultValue(1f)] public float intensity = 1f;

        /// <summary>
        /// The tint of the light
        /// </summary>
        public MColor tint;

        /// <summary>
        /// If true, the light will start fading away below innerRadius, instead of staying constant. This means that innerRadius will be the midpoint between the outerRadius and an inner cutoff.
        /// </summary>
        [DefaultValue(false)] public bool isShell = false;

        /// <summary>
        /// The position of the light
        /// </summary>
        public MVector3 position;
    }
}