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


namespace BossItemDrop
{
    [BepInDependency("com.bepis.r2api", "2.5.14")]
    [BepInPlugin("com.sheen.BossItemDrop", "Boss Item Drop", "1.2.2")]
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]

    public class BossItemDrop : BaseUnityPlugin
    {
        public const string ModVer = "1.2.2";
        public const string ModName = "BossItemDrop";
        public const string ModGuid = "com.sheen.BossItemDrop";

        // configurable stuff
        public static ConfigEntry<float> baseDropChance { get; set; }
        public static ConfigEntry<bool> eliteMultChance { get; set; }
        public static ConfigEntry<float> eliteMultiplier { get; set; }
        public static ConfigEntry<bool> dropFromTeleBoss { get; set; }
        public static ConfigEntry<bool> allowNonStandardDrops { get; set; }
        public static ConfigEntry<bool> dropForEachPlayer { get; set; }
        public static ConfigEntry<int> scaleDropPerStage { get; set; }
        public static ConfigEntry<float> perStageIncrement { get; set; }
        public static ConfigEntry<bool> allowExtraAspectDrops { get; set; }
        public static ConfigEntry<bool> onlyDropWithSacrifice { get; set; }
        public static ConfigEntry<bool> useSacrificeDropChance { get; set; }
        public static ConfigEntry<bool> useLuck { get; set; }

        public void Awake()
        {
            // config binds
            baseDropChance = Config.Bind<float>(
                "Drop Chances",
                "baseDropChance",
                4,
                "The base drop chance for boss items, as a percent from 0 to 100."
                );
            eliteMultChance = Config.Bind<bool>(
                "Drop Chances",
                "eliteMultiplyChance",
                true,
                "Determines if an elite boss will have a multiplier on their drop chance."
                );
            eliteMultiplier = Config.Bind<float>(
                "Drop Chances",
                "eliteMultiplier",
                2,
                "The multiplier applied to the drop chance if the boss is elite. Has no effect if eliteMultiplyChance is false."
                );
            scaleDropPerStage = Config.Bind<int>(
                "Drop Chances",
                "scaleDropRatePerStageMode",
                0,
                "Determines if and how the drop rate changes with each stage. 0 = no change, 1 = increase, 2 = decrease"
                );
            perStageIncrement = Config.Bind<float>(
                "Drop Chances",
                "perStageIncrement",
                0.1f,
                "The increment applied to the base drop rate after each stage. Its effect is dependent on scaleDropRatePerStageMode's value"
                );
            onlyDropWithSacrifice = Config.Bind<bool>(
                "Drop Chances",
                "onlyDropWithSacrificeEnabled",
                false,
                "Determines if extra boss item drops should be restricted to runs in which the Sacrifice artifact is enabled."
                );
            useSacrificeDropChance = Config.Bind<bool>(
                "Drop Chances",
                "useSacrificeDropChance",
                false,
                "If enabled, all drop chance calculation will be ignored, and the drop rates for enemies while using sacrifice will be used instead."
                );
            useLuck = Config.Bind<bool>(
                "Drop Chances",
                "useLuck",
                false,
                "Determines if luck is used to reroll drops."
                );
            dropFromTeleBoss = Config.Bind<bool>(
                "Drop Logistics",
                "dropFromTeleBoss",
                true,
                "Determines if extra drops can come from the teleporter bosses."
                );
            allowNonStandardDrops = Config.Bind<bool>(
                "Drop Logistics",
                "allowNonStandardDrops",
                true,
                "Allows for items that are not the standard teleporter boss items to drop from bosses, i.e. Pearls, Irradiant Pearls, and Aspect Items."
                );
            dropForEachPlayer = Config.Bind<bool>(
                "Drop Logistics",
                "dropForEachPlayer",
                true,
                "Determines if an item should drop for each player, or if only one item should drop per kill."
                );
            allowExtraAspectDrops = Config.Bind<bool>(
                "Drop Logistics",
                "allowExtraAspectDrops",
                false,
                "Determines if Aspect Items (i.e. Ifrit's Distinction) have an increased drop rate on boss enemies of that elite type."
                );

            On.RoR2.GlobalEventManager.OnCharacterDeath += (orig, self, damageReport) =>
            {
                orig(self, damageReport);
                HookOnBodyDeath(damageReport);
            };
        }

        // method that drops the item
        public void HookOnBodyDeath(DamageReport damageReport)
        {
            // grabbing some values from the damage report
            CharacterBody enemy = damageReport.victimBody;
            int enemyBodyIndex = enemy.bodyIndex;

            CharacterMaster killerMaster = damageReport.attackerMaster;

            // exiting the method early if the enemy is a teleporter boss and the config value disallows that
            if (enemy.isBoss && !dropFromTeleBoss.Value)
            {
                return;
            }

            // exiting early if non-sacrifice drops are disabled, and the artifact is not on
            if (onlyDropWithSacrifice.Value && !RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.sacrificeArtifactDef))
            {
                return;
            }

            ItemIndex itemToDrop = ItemIndex.Count;
            EquipmentIndex equipToDrop = EquipmentIndex.Count;

            int players = Run.instance.participatingPlayerCount;

            // checking if extra elite aspects should drop
            if (enemy.isElite && allowExtraAspectDrops.Value)
            {
                equipToDrop = enemy.equipmentSlot.equipmentIndex;
            }

            // determining the item to drop
            switch (enemyBodyIndex)
            {
                case 93:    // Wandering Vagrant
                    itemToDrop = ItemIndex.NovaOnLowHealth;         // Genesis Loop
                    break;
                case 87:    // Stone Titan
                    itemToDrop = ItemIndex.Knurl;                   // Titanic Knurl
                    break;
                case 13:    // Beetle Queen
                    itemToDrop = ItemIndex.BeetleGland;             // Queen's Gland
                    break;
                case 45:    // Grovetender
                    itemToDrop = ItemIndex.SprintWisp;              // The Little Disciple
                    break;
                case 60:    // Magma Worm
                    itemToDrop = ItemIndex.FireballsOnHit;          // Molten Perforator
                    if (allowNonStandardDrops.Value)
                        equipToDrop = EquipmentIndex.AffixRed;      // Ifrit's Distinction
                    break;
                case 31:    // Overloading Worm
                    itemToDrop = ItemIndex.FireballsOnHit;          // Molten Perforator
                    if (allowNonStandardDrops.Value)
                        equipToDrop = EquipmentIndex.AffixBlue;     // Silence Between Two Strikes
                    break;
                case 52:    // Imp Overlord
                    itemToDrop = ItemIndex.BleedOnHitAndExplode;    // Shatterspleen
                    break;
                case 24:    // Clay Dunestrider
                    itemToDrop = ItemIndex.SiphonOnLowHealth;       // Mired Urn
                    break;
                case 72:    // Solus Control Unit
                    if (allowNonStandardDrops.Value)
                        itemToDrop = ItemIndex.Pearl;               // Pearl
                    break;
                case 74:    // Scavenger
                case 85:    // Alloy Worship Unit
                    if (allowNonStandardDrops.Value)
                        itemToDrop = ItemIndex.ShinyPearl;          // Irradiant Pearl
                    break;
                default:    // Catching any unincluded enemies
                    itemToDrop = ItemIndex.Count;
                    equipToDrop = EquipmentIndex.Count;
                    break;
            }

            // determining the drop chance
            float dropChance = 0;
            if (useSacrificeDropChance.Value)
            {
                dropChance = Util.GetExpAdjustedDropChancePercent(5f, damageReport.victim.gameObject);
            }
            else
            {
                dropChance = baseDropChance.Value;

                if (scaleDropPerStage.Value == 1)
                {
                    dropChance += perStageIncrement.Value * Run.instance.stageClearCount;
                }

                else if (scaleDropPerStage.Value == 2)
                {
                    dropChance -= perStageIncrement.Value * Run.instance.stageClearCount;
                    if (dropChance <= 0) dropChance = 0;
                }

                if (enemy.isElite && eliteMultChance.Value)
                {
                    dropChance *= eliteMultiplier.Value;
                }
            }

            // checking if luck is used
            bool doDrop = false;
            if (useLuck.Value)
            {
                doDrop = Util.CheckRoll(dropChance, killerMaster);
            }
            else
            {
                doDrop = Util.CheckRoll(dropChance);
            }

            if (doDrop)
            {
                // dropping multiple items if there are 2 or more players and the config setting allows it
                if (dropForEachPlayer.Value && players >= 2)
                {
                    // determining the drop angles for the items
                    float radiansOffset = 2f * Mathf.PI / players;

                    for (int i = 0; i < players; i++)
                    {
                        if (itemToDrop != ItemIndex.Count)
                        {
                            PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(itemToDrop), enemy.transform.position, (Vector3.up * 20f) +
                               (5 * Vector3.right * Mathf.Cos(radiansOffset * i)) + (5 * Vector3.forward * Mathf.Sin(radiansOffset * i)));
                        }
                        if (equipToDrop != EquipmentIndex.Count)
                        {
                            PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(equipToDrop), enemy.transform.position, (Vector3.up * 20f) +
                               (5 * Vector3.right * Mathf.Cos(radiansOffset * (i + 0.5f)) + (5 * Vector3.forward * Mathf.Sin(radiansOffset * (i + 0.5f)))));
                        }
                    }
                }
                else
                {
                    if (itemToDrop != ItemIndex.Count)
                    {
                        PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(itemToDrop), enemy.transform.position, Vector3.up * 20f);
                    }
                    if (equipToDrop != EquipmentIndex.Count)
                    {
                        PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(equipToDrop), enemy.transform.position, Vector3.up * 20f);
                    }
                }
            }
            return;
        }
    }
}
