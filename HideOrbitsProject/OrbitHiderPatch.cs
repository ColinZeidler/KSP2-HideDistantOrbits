using System;
using System.Collections.Generic;
using System.Text;
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
            VesselComponent activeVessel = GameManager.Instance.Game.ViewController.GetActiveSimVessel(true);
            if (activeVessel != null)
            {
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
                            foreach (OrbitRenderSegment segment in orbitRenderData.Segments)
                            {
                                Color startColor = hiddenOrbit;
                                segment.SetColors(hiddenOrbit, hiddenOrbit);
                            }
                        }
                    }
                }
            }
        }

    }
}
