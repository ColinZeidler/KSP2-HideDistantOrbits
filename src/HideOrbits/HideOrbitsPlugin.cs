using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;
using KSP.UI.Binding;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Mods;
using SpaceWarp.API.Game;
using SpaceWarp.API.Game.Extensions;
using SpaceWarp.API.UI;
using SpaceWarp.API.UI.Appbar;
using UnityEngine;
using BepInEx.Logging;

namespace HideOrbits;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
public class HideOrbitsPlugin : BaseSpaceWarpPlugin
{
    // Useful in case some other mod wants to use this mod a dependency
    [PublicAPI] public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    [PublicAPI] public const string ModName = MyPluginInfo.PLUGIN_NAME;
    [PublicAPI] public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    // Singleton instance of the plugin class
    [PublicAPI] public static HideOrbitsPlugin Instance { get; set; }

    // UI window state
    private bool _isWindowOpen;
    private Rect _windowRect;

    public bool AutoHideOrbits { get; private set; }
    public bool HideVesselOrbits { get; private set; }
    public ManualLogSource MyLogger { get; private set; }

    public string ShipID { get; set; }

    // AppBar button IDs
    private const string ToolbarFlightButtonID = "BTN-HideOrbitsFlight";
    private const string ToolbarOabButtonID = "BTN-HideOrbitsOAB";
    private const string ToolbarKscButtonID = "BTN-HideOrbitsKSC";

    /// <summary>
    /// Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        base.OnInitialized();

        Instance = this;

        // Register Flight AppBar button
        Appbar.RegisterAppButton(
            ModName,
            ToolbarFlightButtonID,
            AssetManager.GetAsset<Texture2D>($"{ModGuid}/images/icon.png"),
            isOpen =>
            {
                _isWindowOpen = isOpen;
                GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(isOpen);
            }
        );

        // Register OAB AppBar Button
        Appbar.RegisterOABAppButton(
            ModName,
            ToolbarOabButtonID,
            AssetManager.GetAsset<Texture2D>($"{ModGuid}/images/icon.png"),
            isOpen =>
            {
                _isWindowOpen = isOpen;
                GameObject.Find(ToolbarOabButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(isOpen);
            }
        );

        // Register KSC AppBar Button
        Appbar.RegisterKSCAppButton(
            ModName,
            ToolbarKscButtonID,
            AssetManager.GetAsset<Texture2D>($"{ModGuid}/images/icon.png"),
            () =>
            {
                _isWindowOpen = !_isWindowOpen;
            }
        );

        // Register all Harmony patches in the project
        var harmony = new Harmony(ModGuid);
        harmony.PatchAll(typeof(OrbitHiderPatch));
        //Harmony.CreateAndPatchAll(typeof(OrbitHiderPatch).Assembly);

        // Try to get the currently active vessel, set its throttle to 100% and toggle on the landing gear
        try
        {
            var currentVessel = Vehicle.ActiveVesselVehicle;
            if (currentVessel != null)
            {
                currentVessel.SetMainThrottle(1.0f);
                currentVessel.SetGearState(true);
            }
        }
        catch (Exception){}

        // Fetch a configuration value or create a default one if it does not exist
        var defaultValue = true;
        var configValue = Config.Bind<bool>("Orbits", "Enable Orbit Hiding", defaultValue, "Enables automatic hiding of distant orbits");
        AutoHideOrbits = configValue.Value;

        var hideVessels = Config.Bind<bool>("Orbits", "Enable Vessel Orbit Hiding", defaultValue, "Hides non active or target vessel orbits by default");
        HideVesselOrbits = hideVessels.Value;

        ShipID = "No ship yet";

        // Log the config value into <KSP2 Root>/BepInEx/LogOutput.log
        Logger.LogInfo($"OrbitHiding: {configValue.Value}");
        MyLogger = Logger;
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
                "HideOrbits",
                GUILayout.Height(350),
                GUILayout.Width(350)
            );
        }
    }
    void ToggleGuiButton(bool toggle)
    {
        _isWindowOpen = toggle;
        GameObject.Find(ToolbarFlightButtonID)?.GetComponent<UIValue_WriteBool_Toggle>()?.SetValue(toggle);
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
        GUILayout.Label($"Ship ID: {ShipID}");
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
