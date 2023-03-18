using NewHorizons.Builder.General;
using NewHorizons.Utility;
using NewHorizons.Utility.OWMLUtilities;
using UnityEngine;
using Logger = NewHorizons.Utility.Logger;
namespace NewHorizons.Components
{
    public class ShipWarpController : MonoBehaviour
    {
        private SingularityController _blackhole;
        private SingularityController _whitehole;
        private OWAudioSource _oneShotSource;

        private bool _isWarpingIn;
        private bool _wearingSuit;
        private bool _waitingToBeSeated;
        private bool _eyesOpen = false;

        private float _impactDeathSpeed;

        private const float size = 14f;

        private readonly string _blackHolePath = "TowerTwin_Body/Sector_TowerTwin/Sector_Tower_HGT/Interactables_Tower_HGT/Interactables_Tower_TT/Prefab_NOM_WarpTransmitter (1)/BlackHole/BlackHoleSingularity";
        private readonly string _whiteHolePath = "TowerTwin_Body/Sector_TowerTwin/Sector_Tower_HGT/Interactables_Tower_HGT/Interactables_Tower_CT/Prefab_NOM_WarpTransmitter/WhiteHole/WhiteHoleSingularity";

        private GameObject _blackHolePrefab;
        private GameObject _whiteHolePrefab;

        public void Init()
        {
            _blackHolePrefab = SearchUtilities.Find(_blackHolePath);
            _whiteHolePrefab = SearchUtilities.Find(_whiteHolePath);

            MakeBlackHole();
            MakeWhiteHole();

            if (_oneShotSource == null)
            {
                var audioObject = new GameObject("WarpOneShot");
                audioObject.transform.parent = transform;
                audioObject.SetActive(false);
                _oneShotSource = audioObject.AddComponent<OWAudioSource>();
                _oneShotSource._track = OWAudioMixer.TrackName.Ship;
                audioObject.SetActive(true);
            }
        }

        public void Start()
        {
            _isWarpingIn = false;
            GlobalMessenger.AddListener("FinishOpenEyes", new Callback(OnFinishOpenEyes));
        }

        public void OnDestroy()
        {
            GlobalMessenger.RemoveListener("FinishOpenEyes", new Callback(OnFinishOpenEyes));
        }

        private void MakeBlackHole()
        {
            if (_blackhole != null) return;
            if (_blackHolePrefab == null) return;

            var blackHoleShader = _blackHolePrefab.GetComponent<MeshRenderer>().material.shader;
            if (blackHoleShader == null) blackHoleShader = _blackHolePrefab.GetComponent<MeshRenderer>().sharedMaterial.shader;

            var blackHoleRender = new GameObject("BlackHoleRender");
            blackHoleRender.transform.parent = base.transform;
            blackHoleRender.transform.localPosition = new Vector3(0, 1, 0);
            blackHoleRender.transform.localScale = Vector3.one * size;

            var meshFilter = blackHoleRender.AddComponent<MeshFilter>();
            meshFilter.mesh = _blackHolePrefab.GetComponent<MeshFilter>().mesh;

            var meshRenderer = blackHoleRender.AddComponent<MeshRenderer>();
            meshRenderer.material = new Material(blackHoleShader);
            meshRenderer.material.SetFloat("_Radius", size * 0.4f);
            meshRenderer.material.SetFloat("_MaxDistortRadius", size * 0.95f);
            meshRenderer.material.SetFloat("_MassScale", 1);
            meshRenderer.material.SetFloat("_DistortFadeDist", size * 0.55f);

            _blackhole = blackHoleRender.AddComponent<SingularityController>();
            blackHoleRender.SetActive(true);
        }

        private void MakeWhiteHole()
        {
            if (_whitehole != null) return;
            if (_whiteHolePrefab == null) return;

            var whiteHoleShader = _whiteHolePrefab.GetComponent<MeshRenderer>().material.shader;
            if (whiteHoleShader == null) whiteHoleShader = _whiteHolePrefab.GetComponent<MeshRenderer>().sharedMaterial.shader;

            var whiteHoleRenderer = new GameObject("WhiteHoleRenderer");
            whiteHoleRenderer.transform.parent = base.transform;
            whiteHoleRenderer.transform.localPosition = new Vector3(0, 1, 0);
            whiteHoleRenderer.transform.localScale = Vector3.one * size * 2.8f;

            var meshFilter = whiteHoleRenderer.AddComponent<MeshFilter>();
            meshFilter.mesh = _whiteHolePrefab.GetComponent<MeshFilter>().mesh;

            var meshRenderer = whiteHoleRenderer.AddComponent<MeshRenderer>();
            meshRenderer.material = new Material(whiteHoleShader);
            meshRenderer.sharedMaterial.SetFloat("_Radius", size * 0.4f);
            meshRenderer.sharedMaterial.SetFloat("_DistortFadeDist", size);
            meshRenderer.sharedMaterial.SetFloat("_MaxDistortRadius", size * 2.8f);
            meshRenderer.sharedMaterial.SetColor("_Color", new Color(1.88f, 1.88f, 1.88f, 1f));

            _whitehole = whiteHoleRenderer.AddComponent<SingularityController>();
            whiteHoleRenderer.SetActive(true);
        }

        public void WarpIn(bool wearingSuit)
        {
            Logger.LogVerbose("Starting warp-in");
            // Trying really hard to stop the player from dying while warping in
            _impactDeathSpeed = Locator.GetDeathManager()._impactDeathSpeed;
            Locator.GetDeathManager()._impactDeathSpeed = Mathf.Infinity;
            Locator.GetDeathManager()._invincible = true;

            _isWarpingIn = true;
            _wearingSuit = wearingSuit;
            _whitehole.Create();
        }

        public void WarpOut()
        {
            Logger.LogVerbose("Starting warp-out");
            _oneShotSource.PlayOneShot(global::AudioType.VesselSingularityCreate, 1f);
            _blackhole.Create();
        }

        public void Update()
        {
            if (_isWarpingIn && LateInitializerManager.isDoneInitializing)
            {
                Delay.FireInNUpdates(() => StartWarpInEffect(), 1);
                _isWarpingIn = false;
            }

            if (_waitingToBeSeated)
            {
                if (Locator.GetPlayerTransform().TryGetComponent<PlayerResources>(out var resources) && resources._currentHealth < 100f)
                {
                    Logger.LogVerbose("Player died in a warp drive accident, reviving them");
                    // Means the player was killed meaning they weren't teleported in
                    resources._currentHealth = 100f;
                    if (!PlayerState.AtFlightConsole()) TeleportToShip();
                }
            }

            // Idk whats making this work but now it works and idc
            if (_waitingToBeSeated && PlayerState.IsInsideShip() && _eyesOpen)
            {
                Delay.FireInNUpdates(() => FinishWarpIn(), 1);
                _waitingToBeSeated = false;
            }
        }

        private void OnFinishOpenEyes()
        {
            _eyesOpen = true;
        }

        private void StartWarpInEffect()
        {
            Logger.LogVerbose("Starting warp-in effect");
            _oneShotSource.PlayOneShot(global::AudioType.VesselSingularityCollapse, 1f);
            Locator.GetDeathManager()._invincible = true;
            if (Main.Instance.CurrentStarSystem.Equals("SolarSystem")) TeleportToShip();
            _whitehole.Create();
            _waitingToBeSeated = true;
            if (_wearingSuit && !Locator.GetPlayerController()._isWearingSuit)
            {
                SpawnPointBuilder.SuitUp();
            }
        }

        private void TeleportToShip()
        {
            var playerSpawner = GameObject.FindObjectOfType<PlayerSpawner>();
            playerSpawner.DebugWarp(playerSpawner.GetSpawnPoint(SpawnLocation.Ship));
        }

        public void FinishWarpIn()
        {
            Logger.LogVerbose("Finishing warp");
            Locator.GetShipBody().GetComponentInChildren<ShipCockpitController>().OnPressInteract();
            _waitingToBeSeated = false;
            Delay.FireInNUpdates(() => _whitehole.Collapse(), 30);

            var resources = Locator.GetPlayerTransform().GetComponent<PlayerResources>();

            Locator.GetDeathManager()._impactDeathSpeed = _impactDeathSpeed;
            resources._currentHealth = 100f;
            Locator.GetDeathManager()._invincible = false;

            // For some reason warping into the ship makes you suffocate while in the ship
            if (_wearingSuit) resources.OnSuitUp();
            var o2Volume = Locator.GetShipBody().GetComponent<OxygenVolume>();
            var atmoVolume = SearchUtilities.Find("Ship_Body/Volumes/ShipAtmosphereVolume").GetComponent<SimpleFluidVolume>();

            resources._cameraFluidDetector.AddVolume(atmoVolume);
            resources._cameraFluidDetector.OnVolumeAdded(atmoVolume);
            resources._cameraFluidDetector.OnVolumeActivated(atmoVolume);

            GlobalMessenger.FireEvent("EnterShip");
            PlayerState.OnEnterShip();
        }
    }
}
