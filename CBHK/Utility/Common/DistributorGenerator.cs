using CBHK.Domain;
using CBHK.Domain.Model.Database;
using CBHK.View;
using CBHK.View.Generator;
using CBHK.ViewModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CBHK.Utility
{
    public partial class DistributorGenerator : ObservableObject
    {
        #region Field
        private MainViewModel CBHK;
        private readonly IContainerProvider container;
        private readonly EnvironmentConfig config;
        private Dictionary<string, IRelayCommand> commandDictionary = [];
        #endregion

        #region Method
        public DistributorGenerator(IContainerProvider Container,MainView cbhk,CBHKDataContext context)
        {
            container = Container;
            config = context.EnvironmentConfigSet.First();
            MainView mainWindow = container.Resolve<MainView>();
            if(mainWindow.DataContext is MainViewModel mainViewModel)
            {
                CBHK = mainViewModel;
            }

            commandDictionary = new()
            {
                { "Advancement",StartAdvancementGeneratorCommand },
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
                { "DimensionType" , StartDimensionTypeGeneratorCommand}
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
        #endregion
    }
}