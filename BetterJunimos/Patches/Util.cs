﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;

namespace BetterJunimos.Patches {
    public class Util {
        public static bool WereJunimosPaidToday = false;
        public static Dictionary<string, List<int>> JunimoPaymentsToday = new Dictionary<string, List<int>>();

        public const int DefaultRadius = 8;
        public static int MaxRadius;
        internal static ModConfig Config;
        internal static IReflectionHelper Reflection;
        internal static JunimoAbilities Abilities;

        public static JunimoHut GetHutFromJunimo(JunimoHarvester junimo) {
            NetGuid netHome = Reflection.GetField<NetGuid>(junimo, "netHome").GetValue();
            return Game1.getFarm().buildings[netHome.Value] as JunimoHut;
        }

        public static bool JunimoPaymentReceiveItems(JunimoHut hut) {
            Farm farm = Game1.getFarm();
            NetObjectList<Item> chest = hut.output.Value.items;
            bool paidForage = ReceiveItems(chest, Config.JunimoPayment.DailyWage.ForagedItems, "Forage");
            bool paidFlowers = ReceiveItems(chest, Config.JunimoPayment.DailyWage.Flowers, "Flower");
            bool paidFruit = ReceiveItems(chest, Config.JunimoPayment.DailyWage.Fruit, "Fruit");
            bool paidWine = ReceiveItems(chest, Config.JunimoPayment.DailyWage.Wine, "Artisan Goods");

            return paidForage && paidFlowers && paidFruit && paidWine;
        }

        public static bool ReceiveItems(NetObjectList<Item> chest, int needed, string type) {
            if (needed == 0) return true;
            List<int> items;
            if (!JunimoPaymentsToday.TryGetValue(type, out items)) {
                items = new List<int>();
                JunimoPaymentsToday[type] = items;
            }
            int paidSoFar = items.Count();
            if (paidSoFar == needed) return true;

            foreach (int i in Enumerable.Range(paidSoFar, needed)) {
                Item foundItem = chest.FirstOrDefault(item => item.getCategoryName() == type);
                if (foundItem != null) {
                    items.Add(foundItem.ParentSheetIndex);
                    ReduceItemCount(chest, foundItem);
                }
            }
            return items.Count() == needed;
        }

        public static void ReduceItemCount(NetObjectList<Item> chest, Item item) {
            if (Config.FunChanges.InfiniteJunimoInventory) { return; }
            item.Stack--;
            if (item.Stack == 0) {
                chest.Remove(item);
            }
        }

        public static void AnimateJunimo(int type, JunimoHarvester junimo) {
            var netAnimationEvent = Reflection.GetField<NetEvent1Field<int, NetInt>>(junimo, "netAnimationEvent");
            netAnimationEvent.GetValue().Fire(type);
        }

        public static void spawnJunimoAtHut(JunimoHut hut) {
            if (hut == null) return;
            Farm farm = Game1.getFarm();
            JunimoHarvester junimoHarvester = new JunimoHarvester(new Vector2((float)hut.tileX.Value + 1, (float)hut.tileY.Value + 1) * 64f + new Vector2(0.0f, 32f), hut, hut.getUnusedJunimoNumber());
            farm.characters.Add((NPC)junimoHarvester);
            hut.myJunimos.Add(junimoHarvester);

            if (Game1.isRaining) {
                var alpha = Reflection.GetField<float>(junimoHarvester, "alpha");
                alpha.SetValue(Config.FunChanges.RainyJunimoSpiritFactor);
            }
            if (!Utility.isOnScreen(Utility.Vector2ToPoint(new Vector2((float)hut.tileX.Value + 1, (float)hut.tileY.Value + 1)), 64, farm))
                return;
            farm.playSound("junimoMeep1");
        }

        public static void spawnJunimo() {
            Farm farm = Game1.getFarm();
            JunimoHut hut = farm.buildings.FirstOrDefault(building => building is JunimoHut) as JunimoHut;
            spawnJunimoAtHut(hut);
        }

        //Big thanks to Routine for this workaround for mac users.
        //https://github.com/Platonymous/Stardew-Valley-Mods/blob/master/PyTK/PyUtils.cs#L117
        /// <summary>Gets the correct type of the object, handling different assembly names for mac/linux users.</summary>
        public static Type GetSDVType(string type) {
            const string prefix = "StardewValley.";
            Type defaultSDV = Type.GetType(prefix + type + ", Stardew Valley");

            return defaultSDV ?? Type.GetType(prefix + type + ", StardewValley");
        }
    }
}
