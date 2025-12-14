using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Utils;
using State.Data;
using State.Network;

namespace State
{
    /// <summary>
    /// State - Server-Authoritative Player Data Storage for Valheim Mod Ecosystem.
    /// Provides extensible, synced player data storage with JSON persistence.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        /// <summary>Plugin GUID for BepInEx.</summary>
        public const string PluginGUID = "com.slatyo.state";
        /// <summary>Plugin display name.</summary>
        public const string PluginName = "State";
        /// <summary>Plugin version.</summary>
        public const string PluginVersion = "1.0.0";

        /// <summary>Logger instance for State.</summary>
        public static ManualLogSource Log { get; private set; }

        /// <summary>Plugin instance.</summary>
        public static Plugin Instance { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo($"{PluginName} v{PluginVersion} is loading...");

            // Initialize the player data store
            Store.Initialize();

            // Initialize network RPCs
            StateNetwork.Initialize();

            // Initialize Harmony patches
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded successfully");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            StateNetwork.Cleanup();
        }

        /// <summary>
        /// Helper method to check if running on server.
        /// </summary>
        public static bool IsServer() => ZNet.instance != null && ZNet.instance.IsServer();

        /// <summary>
        /// Helper method to check if running on client (not server).
        /// </summary>
        public static bool IsClient() => ZNet.instance != null && !ZNet.instance.IsServer();

        /// <summary>
        /// Helper method to check if in single player mode.
        /// </summary>
        public static bool IsSinglePlayer() => ZNet.instance != null && !ZNet.instance.IsDedicated() && ZNet.instance.IsServer();
    }
}
