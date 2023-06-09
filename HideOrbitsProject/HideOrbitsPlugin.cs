﻿using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using KSP.UI.Binding;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Mods;
using SpaceWarp.API.UI;
using SpaceWarp.API.UI.Appbar;
using UnityEngine;

namespace HideOrbits;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
public class HideOrbitsPlugin : BaseSpaceWarpPlugin
{
    // These are useful in case some other mod wants to add a dependency to this one
    public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    public const string ModName = MyPluginInfo.PLUGIN_NAME;
    public const string ModVer = MyPluginInfo.PLUGIN_VERSION;
    
    private bool _isWindowOpen;
    private Rect _windowRect;

    public bool AutoHideOrbits { get; private set; }
    public bool HideVesselOrbits { get; private set; }
    public ManualLogSource logger { get; private set; }

    private const string ToolbarFlightButtonID = "BTN-HideOrbitsFlight";

    public static HideOrbitsPlugin Instance { get; set; }

    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        base.OnInitialized();

        Instance = this;

        // Register Flight AppBar button
        Appbar.RegisterAppButton(
            "Hide Orbits",
            ToolbarFlightButtonID,
            AssetManager.GetAsset<Texture2D>($"{SpaceWarpMetadata.ModID}/images/icon.png"),
            ToggleGuiButton
        );

        // Register all Harmony patches in the project
        var harmony = new Harmony(ModGuid);
        harmony.PatchAll(typeof(OrbitHiderPatch));
        //Harmony.CreateAndPatchAll(typeof(OrbitHiderPatch).Assembly);
        
        // Fetch a configuration value or create a default one if it does not exist
        var defaultValue = true;
        var configValue = Config.Bind<bool>("Orbits", "Enable Orbit Hiding", defaultValue, "Enables automatic hiding of distant orbits");
        AutoHideOrbits = configValue.Value;

        var hideVessels = Config.Bind<bool>("Orbits", "Enable Vessel Orbit Hiding", defaultValue, "Hides non active or target vessel orbits by default");
        HideVesselOrbits = hideVessels.Value;

        
        // Log the config value into <KSP2 Root>/BepInEx/LogOutput.log
        Logger.LogInfo($"OrbitHiding: {configValue.Value}");
        logger = Logger;
    }

    void ToggleGuiButton(bool toggle)
    {
        _isWindowOpen = toggle;
        GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(toggle);
    }

    /// <summary>
    /// Draws a simple UI window when <code>this._isWindowOpen</code> is set to <code>true</code>.
    /// </summary>
    private void OnGUI()
    {
        // Set the UI
        GUI.skin = Skins.ConsoleSkin;

        if (_isWindowOpen)
        {
            _windowRect = GUILayout.Window(
                GUIUtility.GetControlID(FocusType.Passive),
                _windowRect,
                FillWindow,
                "Hide Orbits",
                GUILayout.Height(350),
                GUILayout.Width(350)
            );
        }
    }

    /// <summary>
    /// Defines the content of the UI window drawn in the <code>OnGui</code> method.
    /// </summary>
    /// <param name="windowID"></param>
    private void FillWindow(int windowID)
    {
        GUILayout.BeginVertical();
        var exitRect = new Rect(_windowRect.width - 18, 2, 16, 16);
        string exitFont = "<size=8>x</size>";
        if (exitRect.Contains(Event.current.mousePosition))
        {
            exitFont = "<color=red><size=8>x</size></color>";
        }

        if (GUI.Button(exitRect, exitFont))
        {
            if (_isWindowOpen)
            {
                ToggleGuiButton(false);
            }
        }
        GUILayout.EndVertical();


        GUILayout.BeginHorizontal();
        GUILayout.Label("Hide Orbits - Automatically hide distant planet orbits while zoomed in");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Auto hiding orbits: {AutoHideOrbits}");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Toggle Planet Orbits"))
        {
            AutoHideOrbits = !AutoHideOrbits;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Hiding vessel orbits: {HideVesselOrbits}");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Toggle Vessel Orbits"))
        {
            HideVesselOrbits = !HideVesselOrbits;
        }
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0, 0, 10000, 500));
    }
}
