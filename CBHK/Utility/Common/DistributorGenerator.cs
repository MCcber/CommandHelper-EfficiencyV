using CBHK.Domain;
using CBHK.Domain.Model;
using CBHK.View;
using CBHK.View.Generator;
using CBHK.ViewModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using System.Collections.Generic;
using System.Windows;

namespace CBHK.Utility
{
    public partial class DistributorGenerator : ObservableObject
    {
        #region Field
        private MainViewModel CBHK;
        private readonly IContainerProvider container;
        private readonly EnvironmentSettings config;
        private Dictionary<string, IRelayCommand> commandDictionary = [];
        #endregion

        #region Method
        public DistributorGenerator(IContainerProvider Container,MainView cbhk,EnvironmentSettingsService settingsService)
        {
            container = Container;
            config = settingsService.Current;
            if(cbhk.DataContext is MainViewModel mainViewModel)
            {
                CBHK = mainViewModel;
            }

            commandDictionary = new()
            {
                { "Advancement",StartAdvancementGeneratorCommand },
                { "BannerPattern", StartBannerPatternGeneratorCommand },
                { "BlockSoundSet", StartBlockSoundSetGeneratorCommand },
                { "BlockTransformer", StartBlockTransformerGeneratorCommand },
                { "VariantResource", StartVariantResourceGeneratorCommand },
                { "Ooc" , StartOnlyOneCommandGeneratorCommand},
                { "DamageType" , StartDamageTypeGeneratorCommand},
                { "Datapack" , StartDatapacksGeneratorCommand},
                { "ChatType" , StartChatTpyeGeneratorCommand},
                { "CustomWorld",StartCustomWorldGeneratorGeneratorCommand},
                { "Armorstand" , StartArmorStandGeneratorCommand},
                { "WrittenBook" , StartWrittenBooksGeneratorCommand},
                { "Spawner" , StartSpawnerGeneratorCommand},
                { "Recipe" , StartRecipesGeneratorCommand},
                { "Villager" , StartVillagersGeneratorCommand},
                { "Tag" , StartTagsGeneratorCommand},
                { "Item" , StartItemsGeneratorCommand},
                { "LootTable" , StartLootTableGeneratorCommand},
                { "Predicate",StartPredicateGeneratorCommand},
                { "ItemModifier" , StartItemModifierGeneratorCommand},
                { "Firework" , StartFireworksGeneratorCommand},
                { "Entity" , StartEntitiesGeneratorCommand},
                { "Sign" , StartSignCommand},
                { "Dimension" , StartDimensionGeneratorCommand},
                { "DimensionType" , StartDimensionTypeGeneratorCommand },
                { "BlockStateProvider", StartBlockStateProviderGeneratorCommand },
                { "ContextFloatProvider", StartContextFloatProviderGeneratorCommand },
                { "ContextIntProvider", StartContextIntProviderGeneratorCommand },
                { "DecoratedPotPattern", StartDecoratedPotPatternGeneratorCommand },
                { "Dialog", StartDialogGeneratorCommand },
                { "Enchantment", StartEnchantmentGeneratorCommand },
                { "EnchantmentProvider", StartEnchantmentProviderGeneratorCommand },
                { "Instrument", StartInstrumentGeneratorCommand },
                { "JukeboxSong", StartJukeboxSongGeneratorCommand },
                { "SlotSource", StartSlotSourceGeneratorCommand },
                { "SulfurCubeArchetype", StartSulfurCubeArchetypeGeneratorCommand },
                { "TestEnvironment", StartTestEnvironmentGeneratorCommand },
                { "TestInstance", StartTestInstanceGeneratorCommand },
                { "Timeline", StartTimelineGeneratorCommand },
                { "TradeSet", StartTradeSetGeneratorCommand },
                { "TrialSpawner", StartTrialSpawnerGeneratorCommand },
                { "TrimMaterial", StartTrimMaterialGeneratorCommand },
                { "TrimPattern", StartTrimPatternGeneratorCommand },
                { "VillagerTrade", StartVillagerTradeGeneratorCommand },
                { "WorldClock", StartWorldClockGeneratorCommand },
            };
        }

        public IRelayCommand GetGeneratorClickCommand(string target)
        {
            if(commandDictionary.TryGetValue(target,out IRelayCommand result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 设置主窗体状态
        /// </summary>
        private void SetCBHKState()
        {
            if (config.Visibility == "Collapsed")
            {
                CBHK.ShowInTaskBar = false;
            }
            CBHK.WindowState = config.Visibility switch
            {
                "Hidden" or "Collapsed" => WindowState.Minimized,
                _ => WindowState.Normal
            };
        }

        [RelayCommand]
        /// <summary>
        /// 启动进度生成器
        /// </summary>
        private void StartAdvancementGenerator()
        {
            AdvancementView advancementView = container.Resolve<AdvancementView>();
            SetCBHKState();
            advancementView.Show();
            advancementView.Focus();
        }

        [RelayCommand]
        private void StartBannerPatternGenerator()
        {
            BannerPatternView bannerPatternView = container.Resolve<BannerPatternView>();
            SetCBHKState();
            bannerPatternView.Show();
            bannerPatternView.Focus();
        }

        [RelayCommand]
        public void StartBlockSoundSetGenerator()
        {
            BlockSoundSetView blockSoundSetView = container.Resolve<BlockSoundSetView>();
            SetCBHKState();
            blockSoundSetView.Show();
            blockSoundSetView.Focus();
        }

        [RelayCommand]
        public void StartBlockTransformerGenerator()
        {
            BlockTransformerView blockTransformerView = container.Resolve<BlockTransformerView>();
            SetCBHKState();
            blockTransformerView.Show();
            blockTransformerView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动as生成器
        /// </summary>
        private void StartArmorStandGenerator()
        {
            ArmorStandView armorStand = container.Resolve<ArmorStandView>();
            armorStand.Show();
            armorStand.Focus();
            SetCBHKState();
        }

        /// <summary>
        /// 启动伤害类型生成器
        /// </summary>
        [RelayCommand]
        private void StartDamageTypeGenerator()
        {
            DamageTypeView damageTypeView = container.Resolve<DamageTypeView>();
            SetCBHKState();
            damageTypeView.Show();
            damageTypeView.Focus();
        }

        /// <summary>
        /// 启动维度生成器
        /// </summary>
        [RelayCommand]
        private void StartDimensionGenerator()
        {
            DimensionView dimensionView = container.Resolve<DimensionView>();
            SetCBHKState();
            dimensionView.Show();
            dimensionView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动聊天类型编辑器
        /// </summary>
        private void StartChatTpyeGenerator()
        {
            ChatTypeView chatTypeView = container.Resolve<ChatTypeView>();
            chatTypeView.Show();
            chatTypeView.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动自定义世界生成生成器
        /// </summary>
        private void StartCustomWorldGeneratorGenerator()
        {
            CustomWorldGeneratorView customWorldGeneratorView = container.Resolve<CustomWorldGeneratorView>();
            customWorldGeneratorView.Show();
            customWorldGeneratorView.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动ooc生成器
        /// </summary>
        public void StartOnlyOneCommandGenerator()
        {
            OnlyOneCommandView onlyOneCommandView = container.Resolve<OnlyOneCommandView>();
            onlyOneCommandView.Show();
            onlyOneCommandView.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动刷怪笼生成器
        /// </summary>
        private void StartSpawnerGenerator()
        {
            SpawnerView spawner = container.Resolve<SpawnerView>();
            spawner.Show();
            spawner.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动tag生成器
        /// </summary>
        private void StartTagsGenerator()
        {
            TagView tag = container.Resolve<TagView>();
            tag.Show();
            tag.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动物品生成器
        /// </summary>
        private void StartItemsGenerator()
        {
            ItemView item = container.Resolve<ItemView>();
            item.Show();
            item.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动物品修饰器编辑器
        /// </summary>
        private void StartItemModifierGenerator()
        {
            ItemModifierView itemModifierView = container.Resolve<ItemModifierView>();
            itemModifierView.Show();
            itemModifierView.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动战利品表生成器
        /// </summary>
        private void StartLootTableGenerator()
        {
            LootTableView lootTableView = container.Resolve<LootTableView>();
            lootTableView.Show();
            lootTableView.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动战利品表谓词生成器
        /// </summary>
        private void StartPredicateGenerator()
        {
            PredicateView predicate = container.Resolve<PredicateView>();
            predicate.Show();
            predicate.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动实体生成器
        /// </summary>
        private void StartEntitiesGenerator()
        {
            EntityView entity = container.Resolve<EntityView>();
            entity.Show();
            entity.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动烟花生成器
        /// </summary>
        private void StartFireworksGenerator()
        {
            FireworkRocketView fireworkRocket = container.Resolve<FireworkRocketView>();
            fireworkRocket.Show();
            fireworkRocket.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动配方生成器
        /// </summary>
        private void StartRecipesGenerator()
        {
            RecipeView recipe = container.Resolve<RecipeView>();
            recipe.Show();
            recipe.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动村民生成器
        /// </summary>
        private void StartVillagersGenerator()
        {
            VillagerView villager = container.Resolve<VillagerView>();
            villager.Show();
            villager.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动成书生成器
        /// </summary>
        private void StartWrittenBooksGenerator()
        {
            WrittenBookView writtenBook = container.Resolve<WrittenBookView>();
            writtenBook.Show();
            writtenBook.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动数据包生成器
        /// </summary>
        private void StartDatapacksGenerator()
        {
            DatapackView dataPack = container.Resolve<DatapackView>();
            dataPack.Show();
            dataPack.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动告示牌生成器
        /// </summary>
        private void StartSign()
        {
            SignView sign = container.Resolve<SignView>();
            sign.Show();
            sign.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动维度类型生成器
        /// </summary>
        private void StartDimensionTypeGenerator()
        {
            DimensionTypeView dimensionType = container.Resolve<DimensionTypeView>();
            dimensionType.Show();
            dimensionType.Focus();
            SetCBHKState();
        }
        [RelayCommand]
        /// <summary>
        /// 启动方块状态提供商编辑器
        /// </summary>
        private void StartBlockStateProviderGenerator()
        {
            BlockStateProviderView blockStateProviderView = container.Resolve<BlockStateProviderView>();
            SetCBHKState();
            blockStateProviderView.Show();
            blockStateProviderView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动数据包变种资源编辑器
        /// </summary>
        private void StartVariantResourceGenerator()
        {
            VariantResourceView variantResourceView = container.Resolve<VariantResourceView>();
            SetCBHKState();
            variantResourceView.Show();
            variantResourceView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动浮点数数值提供器编辑器
        /// </summary>
        private void StartContextFloatProviderGenerator()
        {
            ContextFloatProviderView contextFloatProviderView = container.Resolve<ContextFloatProviderView>();
            SetCBHKState();
            contextFloatProviderView.Show();
            contextFloatProviderView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动整数数值提供器编辑器
        /// </summary>
        private void StartContextIntProviderGenerator()
        {
            ContextIntProviderView contextIntProviderView = container.Resolve<ContextIntProviderView>();
            SetCBHKState();
            contextIntProviderView.Show();
            contextIntProviderView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动饰纹陶罐图案编辑器
        /// </summary>
        private void StartDecoratedPotPatternGenerator()
        {
            DecoratedPotPatternView decoratedPotPatternView = container.Resolve<DecoratedPotPatternView>();
            SetCBHKState();
            decoratedPotPatternView.Show();
            decoratedPotPatternView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动对话框编辑器
        /// </summary>
        private void StartDialogGenerator()
        {
            DialogView dialogView = container.Resolve<DialogView>();
            SetCBHKState();
            dialogView.Show();
            dialogView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动魔咒编辑器
        /// </summary>
        private void StartEnchantmentGenerator()
        {
            EnchantmentView enchantmentView = container.Resolve<EnchantmentView>();
            SetCBHKState();
            enchantmentView.Show();
            enchantmentView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动魔咒提供器编辑器
        /// </summary>
        private void StartEnchantmentProviderGenerator()
        {
            EnchantmentProviderView enchantmentProviderView = container.Resolve<EnchantmentProviderView>();
            SetCBHKState();
            enchantmentProviderView.Show();
            enchantmentProviderView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动山羊角乐器编辑器
        /// </summary>
        private void StartInstrumentGenerator()
        {
            InstrumentView instrumentView = container.Resolve<InstrumentView>();
            SetCBHKState();
            instrumentView.Show();
            instrumentView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动唱片机曲目编辑器
        /// </summary>
        private void StartJukeboxSongGenerator()
        {
            JukeboxSongView jukeboxSongView = container.Resolve<JukeboxSongView>();
            SetCBHKState();
            jukeboxSongView.Show();
            jukeboxSongView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动槽位源编辑器
        /// </summary>
        private void StartSlotSourceGenerator()
        {
            SlotSourceView slotSourceView = container.Resolve<SlotSourceView>();
            SetCBHKState();
            slotSourceView.Show();
            slotSourceView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动硫方怪原型编辑器
        /// </summary>
        private void StartSulfurCubeArchetypeGenerator()
        {
            SulfurCubeArchetypeView sulfurCubeArchetypeView = container.Resolve<SulfurCubeArchetypeView>();
            SetCBHKState();
            sulfurCubeArchetypeView.Show();
            sulfurCubeArchetypeView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动测试环境编辑器
        /// </summary>
        private void StartTestEnvironmentGenerator()
        {
            TestEnvironmentView testEnvironmentView = container.Resolve<TestEnvironmentView>();
            SetCBHKState();
            testEnvironmentView.Show();
            testEnvironmentView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动测试实例编辑器
        /// </summary>
        private void StartTestInstanceGenerator()
        {
            TestInstanceView testInstanceView = container.Resolve<TestInstanceView>();
            SetCBHKState();
            testInstanceView.Show();
            testInstanceView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动时间线编辑器
        /// </summary>
        private void StartTimelineGenerator()
        {
            TimelineView timelineView = container.Resolve<TimelineView>();
            SetCBHKState();
            timelineView.Show();
            timelineView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动交易集编辑器
        /// </summary>
        private void StartTradeSetGenerator()
        {
            TradeSetView tradeSetView = container.Resolve<TradeSetView>();
            SetCBHKState();
            tradeSetView.Show();
            tradeSetView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动试炼刷怪笼配置编辑器
        /// </summary>
        private void StartTrialSpawnerGenerator()
        {
            TrialSpawnerView trialSpawnerView = container.Resolve<TrialSpawnerView>();
            SetCBHKState();
            trialSpawnerView.Show();
            trialSpawnerView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动纹饰材料编辑器
        /// </summary>
        private void StartTrimMaterialGenerator()
        {
            TrimMaterialView trimMaterialView = container.Resolve<TrimMaterialView>();
            SetCBHKState();
            trimMaterialView.Show();
            trimMaterialView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动纹饰图案编辑器
        /// </summary>
        private void StartTrimPatternGenerator()
        {
            TrimPatternView trimPatternView = container.Resolve<TrimPatternView>();
            SetCBHKState();
            trimPatternView.Show();
            trimPatternView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动村民交易编辑器
        /// </summary>
        private void StartVillagerTradeGenerator()
        {
            VillagerTradeView villagerTradeView = container.Resolve<VillagerTradeView>();
            SetCBHKState();
            villagerTradeView.Show();
            villagerTradeView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动世界时钟编辑器
        /// </summary>
        private void StartWorldClockGenerator()
        {
            WorldClockView worldClockView = container.Resolve<WorldClockView>();
            SetCBHKState();
            worldClockView.Show();
            worldClockView.Focus();
        }

        #endregion

    }
}