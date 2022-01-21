using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using StardewValley;
using StardewValley.GameData.FishPond;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.Tools;
using SObject = StardewValley.Object;

namespace Pathoschild.Stardew.Common.Items.ItemData
{
    /// <summary>Provides methods for searching and constructing items.</summary>
    /// <remarks>This is copied from the SMAPI source code and should be kept in sync with it.</remarks>
    internal class ItemRepository
    {
        /*********
        ** Fields
        *********/
        /// <summary>The custom ID offset for items don't have a unique ID in the game.</summary>
        private readonly int CustomIDOffset = 1000;


        /*********
        ** Public methods
        *********/
        /// <summary>Get all spawnable items.</summary>
        /// <param name="itemTypes">The item types to fetch (or null for any type).</param>
        /// <param name="includeVariants">Whether to include flavored variants like "Sunflower Honey".</param>
        [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "TryCreate invokes the lambda immediately.")]
        public IEnumerable<SearchableItem> GetAll(ItemType[] itemTypes = null, bool includeVariants = true)
        {
            //
            //
            // Be careful about closure variable capture here!
            //
            // SearchableItem stores the Func<Item> to create new instances later. Loop variables passed into the
            // function will be captured, so every func in the loop will use the value from the last iteration. Use the
            // TryCreate(type, id, entity => item) form to avoid the issue, or create a local variable to pass in.
            //
            //

            IEnumerable<SearchableItem> GetAllRaw()
            {
                HashSet<ItemType> types = itemTypes?.Any() == true ? new HashSet<ItemType>(itemTypes) : null;
                bool ShouldGet(ItemType type) => types == null || types.Contains(type);

                // get tools
                if (ShouldGet(ItemType.Tool))
                {
                    for (int q = Tool.stone; q <= Tool.iridium; q++)
                    {
                        int quality = q;

                        yield return this.TryCreate(ItemType.Tool, $"{ToolFactory.axe}", _ => ToolFactory.getToolFromDescription(ToolFactory.axe, quality));
                        yield return this.TryCreate(ItemType.Tool, $"{ToolFactory.hoe}", _ => ToolFactory.getToolFromDescription(ToolFactory.hoe, quality));
                        yield return this.TryCreate(ItemType.Tool, $"{ToolFactory.pickAxe}", _ => ToolFactory.getToolFromDescription(ToolFactory.pickAxe, quality));
                        yield return this.TryCreate(ItemType.Tool, $"{ToolFactory.wateringCan}", _ => ToolFactory.getToolFromDescription(ToolFactory.wateringCan, quality));
                        if (quality != Tool.iridium)
                            yield return this.TryCreate(ItemType.Tool, $"{ToolFactory.fishingRod}", _ => ToolFactory.getToolFromDescription(ToolFactory.fishingRod, quality));
                    }
                    yield return this.TryCreate(ItemType.Tool, $"{this.CustomIDOffset}", _ => new MilkPail()); // these don't have any sort of ID, so we'll just assign some arbitrary ones
                    yield return this.TryCreate(ItemType.Tool, $"{this.CustomIDOffset + 1}", _ => new Shears());
                    yield return this.TryCreate(ItemType.Tool, $"{this.CustomIDOffset + 2}", _ => new Pan());
                    yield return this.TryCreate(ItemType.Tool, $"{this.CustomIDOffset + 3}", _ => new Wand());
                }

                // clothing
                if (ShouldGet(ItemType.Clothing))
                {
                    foreach (SearchableItem item in this.GetForItemType(ItemType.Clothing, "P")) // pants
                        yield return item;
                    foreach (SearchableItem item in this.GetForItemType(ItemType.Clothing, "S")) // shirts
                        yield return item;
                }

                // wallpapers
                if (ShouldGet(ItemType.Wallpaper))
                {
                    for (int id = 0; id < 112; id++)
                        yield return this.TryCreate(ItemType.Wallpaper, id.ToString(), p => new Wallpaper(int.Parse(p.ID)) { Category = SObject.furnitureCategory });
                }

                // flooring
                if (ShouldGet(ItemType.Flooring))
                {
                    for (int id = 0; id < 56; id++)
                        yield return this.TryCreate(ItemType.Flooring, id.ToString(), p => new Wallpaper(int.Parse(p.ID), isFloor: true) { Category = SObject.furnitureCategory });
                }

                // equipment
                if (ShouldGet(ItemType.Boots))
                {
                    foreach (SearchableItem item in this.GetForItemType(ItemType.Boots, "B"))
                        yield return item;
                }
                if (ShouldGet(ItemType.Hat))
                {
                    foreach (SearchableItem item in this.GetForItemType(ItemType.Hat, "H"))
                        yield return item;
                }

                // weapons
                if (ShouldGet(ItemType.Weapon))
                {
                    foreach (SearchableItem item in this.GetForItemType(ItemType.Weapon, "W"))
                        yield return item;
                }

                // furniture
                if (ShouldGet(ItemType.Furniture))
                {
                    foreach (SearchableItem item in this.GetForItemType(ItemType.Furniture, "F"))
                        yield return item;
                }

                // craftables
                if (ShouldGet(ItemType.BigCraftable))
                {
                    foreach (SearchableItem item in this.GetForItemType(ItemType.BigCraftable, "BC"))
                        yield return item;
                }

                // objects
                if (ShouldGet(ItemType.Object) || ShouldGet(ItemType.Ring))
                {
                    foreach (SearchableItem result in this.GetForItemType(ItemType.Object, "O"))
                    {
                        // ring
                        if (result.Item is Ring)
                        {
                            if (ShouldGet(ItemType.Ring))
                                yield return new SearchableItem(ItemType.Ring, result.ID, _ => result.CreateItem());
                        }

                        // secret notes
                        else if (result.ID == "79")
                        {
                            if (ShouldGet(ItemType.Object))
                            {
                                foreach (int secretNoteId in this.TryLoad<int, string>("Data\\SecretNotes").Keys)
                                {
                                    yield return this.TryCreate(ItemType.Object, $"SecretNote::{secretNoteId}", _ =>
                                    {
                                        Item note = Utility.CreateItemByID("(O)79", 1);
                                        note.Name = $"{note.Name} #{secretNoteId}";
                                        return note;
                                    });
                                }
                            }
                        }

                        // item
                        else if (ShouldGet(ItemType.Object))
                        {
                            // spawn main item
                            yield return result;

                            // flavored items
                            if (includeVariants)
                            {
                                var item = (SObject)result.Item;

                                switch (result.Item.Category)
                                {
                                    // fruit products
                                    case SObject.FruitsCategory:
                                        // wine
                                        yield return this.TryCreate(ItemType.Object, $"{result.ID}/wine", _ => new SObject("348", 1)
                                        {
                                            Name = $"{item.Name} Wine",
                                            Price = item.Price * 3,
                                            preserve = { SObject.PreserveType.Wine },
                                            preservedParentSheetIndex = { item.ParentSheetIndex.ToString() }
                                        });

                                        // jelly
                                        yield return this.TryCreate(ItemType.Object, $"{result.ID}/jelly", _ => new SObject("344", 1)
                                        {
                                            Name = $"{item.Name} Jelly",
                                            Price = 50 + item.Price * 2,
                                            preserve = { SObject.PreserveType.Jelly },
                                            preservedParentSheetIndex = { item.ParentSheetIndex.ToString() }
                                        });
                                        break;

                                    // vegetable products
                                    case SObject.VegetableCategory:
                                        // juice
                                        yield return this.TryCreate(ItemType.Object, $"{result.ID}/juice", _ => new SObject("350", 1)
                                        {
                                            Name = $"{item.Name} Juice",
                                            Price = (int)(item.Price * 2.25d),
                                            preserve = { SObject.PreserveType.Juice },
                                            preservedParentSheetIndex = { item.ParentSheetIndex.ToString() }
                                        });

                                        // pickled
                                        yield return this.TryCreate(ItemType.Object, $"{result.ID}/pickled", _ => new SObject("342", 1)
                                        {
                                            Name = $"Pickled {item.Name}",
                                            Price = 50 + item.Price * 2,
                                            preserve = { SObject.PreserveType.Pickle },
                                            preservedParentSheetIndex = { item.ParentSheetIndex.ToString() }
                                        });
                                        break;

                                    // flower honey
                                    case SObject.flowersCategory:
                                        yield return this.TryCreate(ItemType.Object, $"{result.ID}/honey", _ =>
                                        {
                                            SObject honey = new SObject(Vector2.Zero, "340", $"{item.Name} Honey", false, true, false, false)
                                            {
                                                Name = $"{item.Name} Honey",
                                                preservedParentSheetIndex = { item.ParentSheetIndex.ToString() }
                                            };
                                            honey.Price += item.Price * 2;
                                            return honey;
                                        });
                                        break;

                                    // roe and aged roe (derived from FishPond.GetFishProduce)
                                    case SObject.sellAtFishShopCategory when item.ParentSheetIndex == 812:
                                        {
                                            this.GetRoeContextTagLookups(out HashSet<string> simpleTags, out List<List<string>> complexTags);

                                            foreach (var pair in Game1.objectInformation)
                                            {
                                                // get input
                                                SObject input = this.TryCreate(ItemType.Object, pair.Key, p => new SObject(p.ID, 1))?.Item as SObject;
                                                var inputTags = input?.GetContextTags();
                                                if (inputTags?.Any() != true)
                                                    continue;

                                                // check if roe-producing fish
                                                if (!inputTags.Any(tag => simpleTags.Contains(tag)) && !complexTags.Any(set => set.All(tag => input.HasContextTag(tag))))
                                                    continue;

                                                // yield roe
                                                SObject roe = null;
                                                Color color = this.GetRoeColor(input);
                                                yield return this.TryCreate(ItemType.Object, $"{result.ID}/roe", _ =>
                                                {
                                                    roe = new ColoredObject("812", 1, color)
                                                    {
                                                        name = $"{input.Name} Roe",
                                                        preserve = { Value = SObject.PreserveType.Roe },
                                                        preservedParentSheetIndex = { Value = input.ParentSheetIndex.ToString() }
                                                    };
                                                    roe.Price += input.Price / 2;
                                                    return roe;
                                                });

                                                // aged roe
                                                if (roe != null && pair.Key != "698") // aged sturgeon roe is caviar, which is a separate item
                                                {
                                                    yield return this.TryCreate(ItemType.Object, $"{result.ID}/aged-roe", _ => new ColoredObject("447", 1, color)
                                                    {
                                                        name = $"Aged {input.Name} Roe",
                                                        Category = -27,
                                                        preserve = { Value = SObject.PreserveType.AgedRoe },
                                                        preservedParentSheetIndex = { Value = input.ParentSheetIndex.ToString() },
                                                        Price = roe.Price * 2
                                                    });
                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            return GetAllRaw().Where(p => p != null);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Get items provided through an item type definition.</summary>
        /// <param name="type">The item type.</param>
        /// <param name="identifier">The game's type definition identifier.</param>
        private IEnumerable<SearchableItem> GetForItemType(ItemType type, string identifier)
        {
            ItemDataDefinition typeDef = ItemDataDefinition.ItemTypes.FirstOrDefault(p => p.Identifier == $"({identifier})");
            if (typeDef == null)
                yield break;

            foreach (string id in typeDef.GetAllItemIDs())
                yield return this.TryCreate(type, id, p => typeDef.CreateItem(p.ID, 1, SObject.lowQuality));
        }

        /// <summary>Get optimized lookups to match items which produce roe in a fish pond.</summary>
        /// <param name="simpleTags">A lookup of simple singular tags which match a roe-producing fish.</param>
        /// <param name="complexTags">A list of tag sets which match roe-producing fish.</param>
        private void GetRoeContextTagLookups(out HashSet<string> simpleTags, out List<List<string>> complexTags)
        {
            simpleTags = new HashSet<string>();
            complexTags = new List<List<string>>();

            foreach (FishPondData data in Game1.content.Load<List<FishPondData>>("Data\\FishPondData"))
            {
                if (data.ProducedItems.All(p => p.ItemID != "812"))
                    continue; // doesn't produce roe

                if (data.RequiredTags.Count == 1 && !data.RequiredTags[0].StartsWith("!"))
                    simpleTags.Add(data.RequiredTags[0]);
                else
                    complexTags.Add(data.RequiredTags);
            }
        }

        /// <summary>Try to load a data file, and return empty data if it's invalid.</summary>
        /// <typeparam name="TKey">The asset key type.</typeparam>
        /// <typeparam name="TValue">The asset value type.</typeparam>
        /// <param name="assetName">The data asset name.</param>
        private Dictionary<TKey, TValue> TryLoad<TKey, TValue>(string assetName)
        {
            try
            {
                return Game1.content.Load<Dictionary<TKey, TValue>>(assetName);
            }
            catch (ContentLoadException)
            {
                // generally due to a player incorrectly replacing a data file with an XNB mod
                return new Dictionary<TKey, TValue>();
            }
        }

        /// <summary>Create a searchable item if valid.</summary>
        /// <param name="type">The item type.</param>
        /// <param name="key">The locally unique item key.</param>
        /// <param name="createItem">Create an item instance.</param>
        private SearchableItem TryCreate(ItemType type, string key, Func<SearchableItem, Item> createItem)
        {
            try
            {
                var item = new SearchableItem(type, key, createItem);
                item.Item.getDescription(); // force-load item data, so it crashes here if it's invalid
                return item;
            }
            catch
            {
                return null; // if some item data is invalid, just don't include it
            }
        }

        /// <summary>Get the color to use a given fish's roe.</summary>
        /// <param name="fish">The fish whose roe to color.</param>
        /// <remarks>Derived from <see cref="StardewValley.Buildings.FishPond.GetFishProduce"/>.</remarks>
        private Color GetRoeColor(SObject fish)
        {
            return fish.ParentSheetIndex == 698 // sturgeon
                ? new Color(61, 55, 42)
                : (TailoringMenu.GetDyeColor(fish) ?? Color.Orange);
        }
    }
}
