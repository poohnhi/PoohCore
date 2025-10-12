using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Characters;
using System;

namespace PoohCore
{
    internal class Patches
    {
        private static IMonitor Monitor;
        internal static void behaviorOnFarmerLocationEntry_Postfix(NPC __instance, GameLocation location, Farmer who)
        {
            try
            {
                CharacterData data = __instance.GetData();
                if (data is not null && data.CustomFields.TryGetValue("poohnhi.PoohCore/HighCharacter", out string HighCharacterValue))
                {
                    if (HighCharacterValue is not null && HighCharacterValue != "false")
                    {
                        if (__instance.Sprite is not null && __instance.Sprite.CurrentAnimation == null && __instance.Sprite.SourceRect.Height > 32)
                        {
                            __instance.Sprite.SpriteWidth = __instance.Sprite.SourceRect.Width;
                            __instance.Sprite.SpriteHeight = __instance.Sprite.SourceRect.Height;
                            __instance.Sprite.currentFrame = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //Monitor.Log($"Failed in PoohCore {nameof(behaviorOnFarmerLocationEntry_Postfix)}:\n{ex}", LogLevel.Error);
            }
        }
        internal static void addActor_Postfix(Event __instance, string name, int x, int y, int facingDirection, GameLocation location)
        {
            try
            {
                bool isOptionalNpc;
                if (ModEntry.ListOfModifiedNPC.Contains(name))
                {
                    NPC actorByName = __instance.getActorByName(name, out isOptionalNpc);
                    if (actorByName is not null)
                    {
                        __instance.actors.Remove(actorByName);
                        NPC myNPC = Game1.getCharacterFromName(name);
                        string textureNameForCharacter = NPC.getTextureNameForCharacter(name);
                        NPC nPC;
                        nPC = new NPC(new AnimatedSprite("Characters\\" + textureNameForCharacter, 0, myNPC.Sprite.SourceRect.Width, myNPC.Sprite.SourceRect.Height), actorByName.Position, actorByName.currentLocation.Name, actorByName.FacingDirection, actorByName.Name, actorByName.Portrait, eventActor: true);
                        if (__instance.isFestival)
                        {
                            try
                            {
                                if (__instance.TryGetFestivalDialogueForYear(nPC, nPC.Name, out var dialogue))
                                {
                                    nPC.setNewDialogue(dialogue);
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                        __instance.actors.Add(nPC);
                    }
                }
            }
            catch (Exception ex)
            {
                //Monitor.Log($"Failed in PoohCore {nameof(behaviorOnFarmerLocationEntry_Postfix)}:\n{ex}", LogLevel.Trace);
            }
        }
        internal static void resetForNewDay_Prefix(NPC __instance, int dayOfMonth)
        {
            try
            {
                if (__instance is not null)
                {
                    CharacterData data = __instance.GetData();
                    if (data is not null)
                    {
                        try
                        {
                            var test = data.CustomFields.TryGetValue("poohnhi.PoohCore/WideCharacter", out string WideCharacterValue);
                            if (WideCharacterValue is not null && WideCharacterValue != "false")
                            {
                                __instance.forceOneTileWide.Set(true);
                            }
                        }
                        catch
                        {

                        }
                        
                    }
                }
            }
            catch (Exception ex)
            {
                //Monitor.Log($"Failed in PoohCore {nameof(resetForNewDay_Prefix)}:\n{ex}", LogLevel.Trace);
            }
        }
    }
}
