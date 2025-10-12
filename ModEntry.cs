using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using HarmonyLib;
using ContentPatcher;
using StardewValley.Delegates;
using StardewValley.GameData.Characters;
using StardewValley.GameData.Crops;
using StardewValley.GameData.FruitTrees;

namespace PoohCore
{
    public partial class ModEntry : Mod
    {
        public static HashSet<string> ListOfModifiedNPC = [];
        internal static List<string> ListOfMailFlag = [];
        static string[] universalLoves;
        static string[] universalHates;
        static string[] universalLikes;
        static string[] universalDislikes;
        static string[] universalNeutrals;
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.DayEnding += this.OnDayEnding;
            var harmony = new Harmony(this.ModManifest.UniqueID);

            try
            {
                harmony.Patch(
                    original: AccessTools.Method(typeof(NPC), "behaviorOnFarmerLocationEntry"),
                    postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.behaviorOnFarmerLocationEntry_Postfix))
                );
            }
            catch (Exception ex)
            {
                Monitor.Log(ex.ToString(), LogLevel.Error);
            }
            try
            {
                harmony.Patch(
                original: AccessTools.Method(typeof(Event), "addActor"),
                postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.addActor_Postfix))
                );
            }
            catch (Exception ex)
            {
                Monitor.Log(ex.ToString(), LogLevel.Error);
            }
            try
            {
                harmony.Patch(
                original: AccessTools.Method(typeof(NPC), "resetForNewDay"),
                prefix: new HarmonyMethod(typeof(Patches), nameof(Patches.resetForNewDay_Prefix))
            );
            }
            catch (Exception ex)
            {
                Monitor.Log(ex.ToString(), LogLevel.Error);
            }

        }
        public void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            var api = this.Helper.ModRegistry.GetApi<IContentPatcherAPI>("Pathoschild.ContentPatcher");
            api.RegisterToken(this.ModManifest, "GetMailFlagProgressNumber", new GetMailFlagProgressNumberToken());
            api.RegisterToken(this.ModManifest, "GetGiftTasteCPTokenFromSomeOne", new GetGiftTasteCPTokenFromSomeOneToken());
            GameStateQuery.Register("poohnhi.PoohCore_HARVEST_ITEM_PRICE", delegate (string[] query, GameStateQueryContext context)
            {
                try
                {
                    if (!GameStateQuery.Helpers.TryGetItemArg(query, 1, context.TargetItem, context.InputItem, out var item, out var error) || !ArgUtility.TryGetInt(query, 2, out var minPrice, out error) || !ArgUtility.TryGetOptionalInt(query, 3, out var maxPrice, out error, int.MaxValue))
                    {
                        return GameStateQuery.Helpers.ErrorResult(query, error);
                    }
                    if (item.Category == -74)
                    {
                        // special case: fruit trees will be their fruit*15 for the final price
                        StardewValley.Object obj = new StardewValley.Object(item.ItemId, 1);
                        if (obj.IsFruitTreeSapling())
                        {
                            int? totalPrice = 0;
                            FruitTreeData fruitTreeData;
                            FruitTree.TryGetData(obj.ItemId, out fruitTreeData);
                            if (fruitTreeData?.Fruit is not null)
                            {
                                foreach (FruitTreeFruitData entry in fruitTreeData.Fruit)
                                {
                                    Item cropItem = ItemRegistry.Create(entry.ItemId);
                                    if (cropItem is not null)
                                    {
                                        totalPrice += cropItem?.sellToStorePrice();
                                    }
                                }
                                int? price = totalPrice*15;
                                if (price >= minPrice)
                                {
                                    /*if (price <= maxPrice)
                                    {
                                        Monitor.Log($"Range is {minPrice} - {maxPrice}. Item {item.DisplayName} have crop price: {price}.", LogLevel.Error);
                                    }*/
                                    return price <= maxPrice;
                                }
                            }
                        }
                        else
                        {
                            // Get crop data from item (assume they're seeds)
                            CropData cropData;
                            Crop.TryGetData(item.ItemId, out cropData);

                            // Get price from that crop and compare it to max and min price
                            if (cropData is not null)
                            {
                                Item cropItem = ItemRegistry.Create(cropData.HarvestItemId);
                                int? price = cropItem?.sellToStorePrice();
                                if (price >= minPrice)
                                {
                                    /*if (price <= maxPrice)
                                    {
                                        Monitor.Log($"Range is {minPrice} - {maxPrice}. Item {item.DisplayName} have crop price: {price}.", LogLevel.Error);
                                    }*/
                                    return price <= maxPrice;
                                }
                            }
                        }
                    }
                    else
                    {
                        return false;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }
        public void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            universalLoves = ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Love"]);
            universalHates = ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Hate"]);
            universalLikes = ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Like"]);
            universalDislikes = ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Dislike"]);
            universalNeutrals = ArgUtility.SplitBySpace(Game1.NPCGiftTastes["Universal_Neutral"]);
            foreach (NPC thisChar in Utility.getAllVillagers())
            {
                try
                {
                    if (thisChar is not null)
                    {
                        CharacterData data = thisChar.GetData();
                        if (data is not null)
                        {
                            try
                            {
                                var test = data.CustomFields.TryGetValue("poohnhi.PoohCore/WideCharacter", out string WideCharacterValue);
                                if (WideCharacterValue is not null && WideCharacterValue != "false")
                                ListOfModifiedNPC.Add(thisChar.Name);
                            }
                            catch { }
                            try
                            {
                                var test = data.CustomFields.TryGetValue("poohnhi.PoohCore/HighCharacter", out string HighCharacterValue);
                                if (HighCharacterValue is not null && HighCharacterValue != "false")
                                ListOfModifiedNPC.Add(thisChar.Name);
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }
        }

        public void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            foreach (var todayMailFlag in ListOfMailFlag)
            {
                if (Game1.player.mailReceived.Contains(todayMailFlag))
                {
                    Game1.player.mailReceived.Remove(todayMailFlag);
                    string todayMailFlagNumber = GetMailFlagProgressNumberFromMailFlag(todayMailFlag);
                    string seenFlag = todayMailFlag + todayMailFlagNumber;
                    string nextFlag = todayMailFlag + GetNextNumberFromString(todayMailFlagNumber);
                    Game1.player.mailReceived.Remove(seenFlag);
                    Game1.player.mailReceived.Add(nextFlag);
                }
            }
            ListOfMailFlag = [];
        }
    }
}
