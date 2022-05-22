﻿using NewHorizons.Components;
using NewHorizons.External.Modules;
using NewHorizons.Handlers;
using NewHorizons.Utility;
using UnityEngine;
using Logger = NewHorizons.Utility.Logger;
using Random = UnityEngine.Random;
namespace NewHorizons.Builder.Props
{
    public static class TornadoBuilder
    {
        private static GameObject _upPrefab;
        private static GameObject _downPrefab;
        private static GameObject _hurricanePrefab;
        private static GameObject _soundPrefab;

        private static Texture2D _mainTexture;
        private static Texture2D _detailTexture;
        private static readonly int DetailColor = Shader.PropertyToID("_DetailColor");
        private static readonly int TintColor = Shader.PropertyToID("_TintColor");

        public static void Make(GameObject planetGO, Sector sector, PropModule.TornadoInfo info, bool hasClouds)
        {
            if (_upPrefab == null)
            {
                _upPrefab = GameObject.Find("BrittleHollow_Body/Sector_BH/Sector_SouthHemisphere/Sector_SouthPole/Sector_Observatory/Interactables_Observatory/MockUpTornado").InstantiateInactive();
                _upPrefab.name = "Tornado_Up_Prefab";
            }
            if (_downPrefab == null)
            {
                _downPrefab = GameObject.Find("BrittleHollow_Body/Sector_BH/Sector_SouthHemisphere/Sector_SouthPole/Sector_Observatory/Interactables_Observatory/MockDownTornado").InstantiateInactive();
                _downPrefab.name = "Tornado_Down_Prefab";
            }
            if (_hurricanePrefab == null)
            {
                _hurricanePrefab = GameObject.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Tornadoes_GDInterior/Hurricane/").InstantiateInactive();
                // For some reason they put the hurricane at the origin and offset all its children (450)
                // Increasing by 40 will keep the bottom above the ground
                foreach (Transform child in _hurricanePrefab.transform)
                {
                    child.localPosition += new Vector3(0, 40 - 450, 0);
                }
                foreach (Transform child in _hurricanePrefab.transform.Find("Effects_GD_Hurricane"))
                {
                    if (child.name.Contains("HurricaneCloudBlend"))
                    {
                        child.localPosition = new Vector3(0, 60, 0);
                        child.localScale = Vector3.one * 1.1f;
                    }
                    if (child.name.Equals("Effects_GD_HurricaneCycloneExterior"))
                    {
                        child.localScale = new Vector3(0.88f, 1f, 0.88f);
                    }
                }
                foreach(var renderer in _hurricanePrefab.GetComponentsInChildren<Renderer>())
                {
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            if (_soundPrefab == null)
            {
                _soundPrefab = GameObject.Find("GiantsDeep_Body/Sector_GD/Sector_GDInterior/Tornadoes_GDInterior/SouthernTornadoes/DownTornado_Pivot/DownTornado/AudioRail").InstantiateInactive();
                _soundPrefab.name = "AudioRail_Prefab";
            }
            if (_mainTexture == null)
            {
                _mainTexture = ImageUtilities.GetTexture(Main.Instance, "AssetBundle/Tornado_BH_Cyclone_02_d.png");
            }
            if (_detailTexture == null)
            {
                _detailTexture = ImageUtilities.GetTexture(Main.Instance, "AssetBundle/Tornado_BH_CycloneDetail_d.png");
            }

            Vector3 position;
            if (info.position != null)
            {
                position = info.position ?? Random.onUnitSphere * info.elevation;
            }
            else if (info.elevation != 0)
            {
                position = Random.onUnitSphere * info.elevation;
            }
            else
            {
                Logger.LogError($"Need either a position or an elevation for tornados");
                return;
            }

            if (info.type.ToLower() == "hurricane") MakeHurricane(planetGO, sector, info, position);
            else MakeTornado(planetGO, sector, info, position, info.type.ToLower() == "downwards");
        }

        private static void MakeTornado(GameObject planetGO, Sector sector, PropModule.TornadoInfo info, Vector3 position, bool downwards)
        {
            var tornadoGO = downwards ? _downPrefab.InstantiateInactive() : _upPrefab.InstantiateInactive();
            tornadoGO.transform.parent = sector.transform;
            tornadoGO.transform.position = planetGO.transform.TransformPoint(position);
            tornadoGO.transform.rotation = Quaternion.FromToRotation(Vector3.up, sector.transform.TransformDirection(position.normalized));

            // Add the sound thing before changing the scale
            var soundGO = _soundPrefab.InstantiateInactive();
            soundGO.name = "AudioRail";
            soundGO.transform.parent = tornadoGO.transform;
            soundGO.transform.localPosition = Vector3.zero;
            soundGO.transform.localRotation = Quaternion.identity;

            // Height of the tornado is 10 by default
            var audioRail = soundGO.GetComponent<AudioRail>();
            audioRail.SetSector(sector);
            audioRail._railPointsRoot.GetChild(0).transform.localPosition = Vector3.zero;
            audioRail._railPointsRoot.GetChild(1).transform.localPosition = Vector3.up * 10;
            audioRail._railPoints = new Vector3[]
            {
                Vector3.zero,
                Vector3.up * 10
            };

            var audioSpreadController = soundGO.GetComponentInChildren<AudioSpreadController>();
            audioSpreadController.SetSector(sector);

            var audioSource = audioRail._audioTransform.GetComponent<AudioSource>();
            audioSource.playOnAwake = true;

            var scale = info.height == 0 ? 1 : info.height / 10f;
            tornadoGO.transform.localScale = Vector3.one * scale;

            // Resize the distance it can be heard from to match roughly with the size
            audioSource.maxDistance = 100 * scale;

            var controller = tornadoGO.GetComponent<TornadoController>();
            controller.SetSector(sector);

            // Found these values by messing around in unity explorer until it looked right
            controller._bottomStartPos = Vector3.up * -20;
            controller._midStartPos = Vector3.up * 150;
            controller._topStartPos = Vector3.up * 300;

            controller._bottomBone.localPosition = controller._bottomStartPos;
            controller._midBone.localPosition = controller._midStartPos;
            controller._topBone.localPosition = controller._topStartPos;

            OWAssetHandler.LoadObject(tornadoGO);
            sector.OnOccupantEnterSector += (sd) => OWAssetHandler.LoadObject(tornadoGO);

            tornadoGO.GetComponentInChildren<CapsuleShape>().enabled = true;

            // Resize it so the force volume goes all the way up
            switch (downwards)
            {
                case true:
                    tornadoGO.transform.Find("MockDownTornado_FluidCenter").localScale = new Vector3(1, 2f, 1);
                    break;
                default:
                    tornadoGO.transform.Find("MockUpTornado_FluidCenter").localScale = new Vector3(1, 2f, 1);
                    break;
            }

            if (info.tint != null)
            {
                ApplyTint(tornadoGO, info.tint.ToColor(), false, downwards);
            }

            if (info.wanderRate != 0)
            {
                ApplyWanderer(tornadoGO, sector, info);
            }

            soundGO.SetActive(true);
            tornadoGO.SetActive(true);
        }

        private static void MakeHurricane(GameObject planetGO, Sector sector, PropModule.TornadoInfo info, Vector3 position)
        {
            var hurricaneGO = _hurricanePrefab.InstantiateInactive();
            hurricaneGO.transform.parent = sector.transform;
            hurricaneGO.transform.position = planetGO.transform.TransformPoint(position);
            hurricaneGO.transform.rotation = Quaternion.FromToRotation(Vector3.up, sector.transform.TransformDirection(position.normalized));

            hurricaneGO.GetComponentInChildren<HurricaneFluidVolume>()._density = 8;

            // Height of the hurricane is 405 by default
            if (info.height != 0) hurricaneGO.transform.localScale = Vector3.one * info.height / 405f;

            if (info.tint != null)
            {
                ApplyTint(hurricaneGO, info.tint.ToColor(), true, false);
            }

            if (info.wanderRate != 0)
            {
                ApplyWanderer(hurricaneGO, sector, info);
            }

            hurricaneGO.SetActive(true);
        }

        private static void ApplyTint(GameObject go, Color colour, bool hurricane, bool downwards)
        {
            colour.a = 1f;

            var detailTexture = ImageUtilities.TintImage(_detailTexture, colour);
            var mainTexture = ImageUtilities.TintImage(_mainTexture, colour);

            string materialName;
            if (hurricane) materialName = "Hurricane_GD_Cyclone_mat";
            else materialName = $"Tornado_BH_Cyclone_{(downwards ? "Down" : "Up")}_mat";

            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            {
                renderer.material.SetColor("_DetailColor", colour);
                renderer.material.SetColor("_TintColor", colour);

                if (renderer.material.name.Contains(materialName))
                {
                    renderer.material.SetTexture("_DetailTex", detailTexture);
                    renderer.material.SetTexture("_MainTex", mainTexture);
                    renderer.material.SetColor("_FresnelColor", colour);
                }
                else
                {
                    // If we set the colour on the ones with the material from before, it makes the gradient look bad
                    renderer.material.color = colour;
                }
            }
        }

        private static void ApplyWanderer(GameObject go, Sector sector, PropModule.TornadoInfo info)
        {
            var wanderer = go.AddComponent<NHTornadoWanderController>();
            wanderer.wanderRate = info.wanderRate;
            wanderer.wanderDegreesX = info.wanderDegreesX;
            wanderer.wanderDegreesZ = info.wanderDegreesZ;
            wanderer.sector = sector;
        }
    }
}
