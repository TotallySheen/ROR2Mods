using System;
using RoR2;
using BepInEx;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using BepInEx.Configuration;

namespace BossItemDrop
{
    [BepInPlugin("com.sheen.BossItemDrop", "Boss Item Drop", "1.0.0")]
    public class BossItemDrop : BaseUnityPlugin
    {
        // configurable stuff
        public static ConfigEntry<float> baseDropChance { get; set; }
        public static ConfigEntry<bool> eliteMultChance { get; set; }
        public static ConfigEntry<float> eliteMultiplier { get; set; }
        public static ConfigEntry<bool> dropFromTeleBoss { get; set; }
        public static ConfigEntry<bool> allowNonStandardDrops { get; set; }
        public static ConfigEntry<bool> dropForEachPlayer { get; set; }

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

            ItemIndex itemToDrop = ItemIndex.Count;
            EquipmentIndex equipToDrop = EquipmentIndex.Count;

            int players = Run.instance.participatingPlayerCount;

            // determining the item to drop
            switch (enemyBodyIndex)
            {
                case 79:    // Wandering Vagrant
                    itemToDrop = ItemIndex.NovaOnLowHealth;     // Genesis Loop
                    break;
                case 73:    // Stone Titan
                    itemToDrop = ItemIndex.Knurl;               // Titanic Knurl
                    break;
                case 10:    // Beetle Queen
                    itemToDrop = ItemIndex.BeetleGland;         // Queen's Gland
                    break;
                case 36:    // Grovetender
                    itemToDrop = ItemIndex.SprintWisp;          // The Little Disciple
                    break;
                case 49:    // Magma Worm
                    if (allowNonStandardDrops.Value)
                        equipToDrop = EquipmentIndex.AffixRed;  // Ifrit's Distinction
                    break;
                case 23:    // Overloading Worm
                    if (allowNonStandardDrops.Value)
                        equipToDrop = EquipmentIndex.AffixBlue; // Silence Between Two Strikes
                    break;
                case 43:    // Imp Overlord
                case 16:    // Clay Dunestrider
                case 58:    // Solus Control Unit
                    if (allowNonStandardDrops.Value)
                        itemToDrop = ItemIndex.Pearl;           // Pearl
                    break;
                case 60:    // Scavenger
                case 71:    // Alloy Worship Unit
                    if (allowNonStandardDrops.Value)
                        itemToDrop = ItemIndex.ShinyPearl;      // Irradiant Pearl
                    break;
            }

            // determining the drop chance
            float dropChance = baseDropChance.Value;

            if (enemy.isElite && eliteMultChance.Value)
            {
                dropChance *= eliteMultiplier.Value;
            }

            bool doDrop = Util.CheckRoll(dropChance, killerMaster);

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
