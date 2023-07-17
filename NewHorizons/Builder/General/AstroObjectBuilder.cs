using NewHorizons.Components;
using NewHorizons.Components.Orbital;
using NewHorizons.External.Configs;
using NewHorizons.Utility.OWML;
using UnityEngine;

namespace NewHorizons.Builder.General
{
    public static class AstroObjectBuilder
    {
        public static NHAstroObject Make(GameObject body, AstroObject primaryBody, PlanetConfig config, bool isVanilla)
        {
            NHAstroObject astroObject = body.AddComponent<NHAstroObject>();
            astroObject.isVanilla = isVanilla;
            astroObject.HideDisplayName = !config.Base.hasMapMarker;
            astroObject.invulnerableToSun = config.Base.invulnerableToSun;

            if (config.Orbit != null) astroObject.SetOrbitalParametersFromConfig(config.Orbit);

            var type = AstroObject.Type.Planet;
            if (config.Orbit.isMoon) type = AstroObject.Type.Moon;
            // else if (config.Base.IsSatellite) type = AstroObject.Type.Satellite;
            else if (config.CometTail != null) type = AstroObject.Type.Comet;
            else if (config.Star != null) type = AstroObject.Type.Star;
            else if (config.FocalPoint != null) type = AstroObject.Type.None;
            astroObject._type = type;
            astroObject._name = AstroObject.Name.CustomString;
            astroObject._customName = config.name;
            astroObject._primaryBody = primaryBody;

            // Expand gravitational sphere of influence of the primary to encompass this body if needed
            if (primaryBody?.gameObject?.GetComponent<SphereCollider>() != null && !config.Orbit.isStatic)
            {
                var primarySphereOfInfluence = primaryBody.GetGravityVolume().gameObject.GetComponent<SphereCollider>();
                if (primarySphereOfInfluence.radius < config.Orbit.semiMajorAxis)
                    primarySphereOfInfluence.radius = config.Orbit.semiMajorAxis * 1.5f;
            }

            if (config.Orbit.isTidallyLocked)
            {
                var alignmentAxis = config.Orbit.alignmentAxis ?? new Vector3(0, -1, 0);

                // Start it off facing the right way
                var facing = body.transform.TransformDirection(alignmentAxis);
                body.transform.rotation = Quaternion.FromToRotation(facing, alignmentAxis) * body.transform.rotation;

                var alignment = body.AddComponent<AlignWithTargetBody>();
                alignment.SetTargetBody(primaryBody?.GetAttachedOWRigidbody());
                alignment._usePhysicsToRotate = false;
                alignment._localAlignmentAxis = alignmentAxis;

                // Static bodies won't update rotation with physics for some reason
                // Have to set it in 2 ticks else it flings the player into deep space on spawn (#171)
                // Pushed to 3 frames after system is ready, bc spawning takes 2 frames, this is hurting my brain too much to try to improve the numbers idc
                if (!config.Orbit.isStatic) Delay.FireOnNextUpdate(() => alignment._usePhysicsToRotate = true);
                //if (!config.Orbit.isStatic) Delay.RunWhen(() => Main.IsSystemReady, () => Delay.FireInNUpdates(() => alignment._usePhysicsToRotate = true, 3));
            }

            if (config.Base.centerOfSolarSystem)
            {
                NHLogger.Log($"Setting center of universe to {config.name}");

                Delay.RunWhen(
                    () => Locator._centerOfTheUniverse != null,
                    () => Locator._centerOfTheUniverse._staticReferenceFrame = astroObject.GetComponent<OWRigidbody>()
                );

                PreserveActiveCenterOfTheUniverse.Apply(astroObject.gameObject);
            }

            return astroObject;
        }
    }
}
