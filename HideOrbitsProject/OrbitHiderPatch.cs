using HarmonyLib;
using KSP.Game;
using KSP.Map;
using KSP.Sim.impl;
using UnityEngine;

namespace HideOrbits
{
    internal class OrbitHiderPatch
    {
        public static Color hiddenOrbit = new(0, 0, 0, 0);

        [HarmonyPatch(typeof(OrbitRenderer), nameof(OrbitRenderer.UpdateOrbitStyling))]
        [HarmonyPostfix]
        public static void OrbitRenderer_UpdateOrbitStyling(Dictionary<IGGuid, OrbitRenderer.OrbitRenderData> ____orbitRenderData)
        {
            CelestialBodyComponent vesselParentBody = null;
            VesselComponent activeVessel = GameManager.Instance.Game.ViewController.GetActiveSimVessel(true);
            if (activeVessel != null)
            {
                //HideOrbitsPlugin.Instance.logger.LogInfo($"Vessel Guid {activeVessel.Guid}");
                vesselParentBody = activeVessel.mainBody;
                //HideOrbitsPlugin.Instance.logger.LogInfo($"Vessel parent Guid {vesselParentBody.Guid}");
            }

            //HideOrbitsPlugin.Instance.logger.LogInfo("OrbitRenderer Called");
            if (HideOrbitsPlugin.Instance != null && HideOrbitsPlugin.Instance.AutoHideOrbits) {
                //HideOrbitsPlugin.Instance.logger.LogInfo("Updating orbits");
                foreach (OrbitRenderer.OrbitRenderData orbitRenderData in ____orbitRenderData.Values)
                {
                    if (orbitRenderData.Segments != null)
                    {
                        if (orbitRenderData.IsCelestialBody)
                        {
                            //HideOrbitsPlugin.Instance.logger.LogInfo($"orbitRenderData parent Guid {orbitRenderData.ParentGuid}");
                            if (orbitRenderData.ParentGuid.ToString() == vesselParentBody?.Guid)
                            {
                                continue;
                            }
                            foreach (OrbitRenderSegment segment in orbitRenderData.Segments)
                            {
                                segment.SetColors(hiddenOrbit, hiddenOrbit);
                            }
                        }
                    }
                }
            }
        }
    }
}
