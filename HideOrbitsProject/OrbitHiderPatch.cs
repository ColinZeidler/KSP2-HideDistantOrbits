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

            if (HideOrbitsPlugin.Instance != null && HideOrbitsPlugin.Instance.AutoHideOrbits) {
                //HideOrbitsPlugin.Instance.logger.LogInfo("Updating orbits");

                //Collect bodies related to local planet (and its moons)
                Dictionary<IGGuid, CelestialBodyComponent> vesselParentChildren = new();
                foreach (OrbitRenderer.OrbitRenderData orbitRenderData in ____orbitRenderData.Values)
                {
                    if (orbitRenderData.Segments != null)
                    {
                        if (orbitRenderData.IsCelestialBody)
                        {
                            CelestialBodyComponent orbitBody = GameManager.Instance.Game.SpaceSimulation.GetSimulationObjectComponent<CelestialBodyComponent>(orbitRenderData.ParentGuid);
                            if (orbitBody.HasChild(vesselParentBody) || orbitBody.SimulationObject.GlobalId == vesselParentBody.SimulationObject.GlobalId)
                            {
                                if (!orbitBody.IsStar)
                                {
                                    foreach (CelestialBodyComponent child in orbitBody.orbitingBodies)
                                    {
                                        if (child != null)
                                        {
                                            vesselParentChildren.Add(child.SimulationObject.GlobalId, child);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Hide unimportant orbits
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
                            if (vesselParentChildren.ContainsKey(orbitRenderData.ParentGuid))
                            {
                                continue;
                            }

                            if ((bool)orbitBody?.HasParent(vesselParentBody))
                            {
                                continue;
                            }

                            // TODO what happens if we orbit Mun, will we see Minmus Orbit? Kerbin Orbit?
                            if (orbitBody.HasChild(vesselParentBody))
                            {
                                continue;
                            }

                            bool targetOrbit = activeVessel != null && activeVessel.TargetObjectId == orbitRenderData.Orbiter.SimulationObject.GlobalId;
                            if (targetOrbit)
                            {
                                continue;
                                // TODO change color of target orbits?
                            }

                            foreach (OrbitRenderSegment segment in orbitRenderData.Segments)
                            {
                                segment.SetColors(hiddenOrbit, hiddenOrbit);
                            }
                        }
                        else
                        {
                            if (!HideOrbitsPlugin.Instance.HideVesselOrbits)
                            {
                                continue;
                            }
                            if (orbitRenderData.Vessel.GlobalId != activeVessel.GlobalId && orbitRenderData.Vessel.GlobalId != activeVessel.TargetObjectId)
                            {
                                foreach (OrbitRenderSegment segment in orbitRenderData.Segments)
                                {
                                    segment.SetColors(hiddenOrbit, hiddenOrbit);
                                }
                            }
                        }
                    }
                } // end Hide orbits

            }
        }
    }
}
