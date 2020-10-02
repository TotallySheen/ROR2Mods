using System;
using RoR2;
using BepInEx;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using BepInEx.Configuration;
using RoR2.Artifacts;
using R2API;
using R2API.Utils;


namespace BorderlandsItemPack
{
    [BepInDependency("com.bepis.r2api", "2.5.14")]
    [BepInPlugin("com.sheen.BorderlandsItemPack", "Borderlands Item Pack", "0.1.0")]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(ItemAPI),nameof(BuffAPI),nameof(ResourcesAPI),nameof(ItemDropAPI))]

    public class BorderlandsItemPack : BaseUnityPlugin
    {
        public const string ModVer = "0.1.0";
        public const string ModName = "BorderlandsItemPack";
        public const string ModGuid = "com.sheen.BorderlandsItemPack";

        //private CustomItem 

        public void Awake()
        {
            
        }
    }
}
