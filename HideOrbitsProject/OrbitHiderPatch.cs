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
                //HideOrbitsPlugin.Instance.logger.LogInfo($"Vessel parent Guid {vesselParentBody.SimulationObject.GlobalId}");
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
                            // orbitRenderData.ParentGuid is the Guid of the planet the orbit is for.
                            if (orbitRenderData.ParentGuid == vesselParentBody?.SimulationObject.GlobalId)
                            {
                                continue;
                            }

                            // check if current orbitRenderData body is a child of vesselParentBody
                            CelestialBodyComponent orbitBody = GameManager.Instance.Game.SpaceSimulation.GetSimulationObjectComponent<CelestialBodyComponent>(orbitRenderData.ParentGuid);
                            if ((bool)orbitBody?.HasParent(vesselParentBody))
                            {
                                continue;
                            }

                            // TODO what happens if we orbit Mun, will we see Minmus Orbit? Kerbin Orbit?
                            if ((bool)orbitBody.HasChild(vesselParentBody))
                            {
                                continue;
                            }

                            bool targetOrbit = activeVessel != null && activeVessel.TargetObjectId == orbitRenderData.Orbiter.SimulationObject.GlobalId;
                            if (targetOrbit)
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
