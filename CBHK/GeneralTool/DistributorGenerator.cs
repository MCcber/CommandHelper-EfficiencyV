using CBHK.Domain;
using CBHK.Domain.Model;
using CBHK.Model;
using CBHK.View;
using CBHK.View.Generator;
using CBHK.ViewModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using System.Linq;
using System.Windows;

namespace CBHK.GeneralTool
{
    public partial class DistributorGenerator : ObservableObject
    {
        #region Field
        private MainViewModel CBHK;
        private readonly IContainerProvider _container;
        private readonly CBHKDataContext _context;
        private readonly EnvironmentConfig _config;
        #endregion

        #region Method
        public DistributorGenerator(IContainerProvider container,CBHKDataContext context)
        {
            _container = container;
            _context = context;
            _config = _context.EnvironmentConfigSet.First();
            MainView mainWindow = _container.Resolve<MainView>();
            if (mainWindow is not null && mainWindow.DataContext is MainViewModel viewModel)
            {
                CBHK = viewModel;
            }
        }

        /// <summary>
        /// 设置主窗体状态
        /// </summary>
        private void SetCBHKState()
        {
            if (_config.Visibility == "关闭")
            {
                CBHK.ShowInTaskBar = false;
            }
            CBHK.WindowState = _config.Visibility switch
            {
                "最小化" or "关闭" => WindowState.Minimized,
                "保持不变" => WindowState.Normal,
                _ => WindowState.Normal
            };
        }

        [RelayCommand]
        /// <summary>
        /// 启动进度生成器
        /// </summary>
        private void StartAdvancementGenerator()
        {
            AdvancementView advancementView = _container.Resolve<AdvancementView>();
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
            ArmorStandView armorStand = _container.Resolve<ArmorStandView>();
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
            DamageTypeView damageTypeView = _container.Resolve<DamageTypeView>();
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
            DimensionView dimensionView = _container.Resolve<DimensionView>();
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
            ChatTypeView chatTypeView = _container.Resolve<ChatTypeView>();
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
            CustomWorldGeneratorView customWorldGeneratorView = _container.Resolve<CustomWorldGeneratorView>();
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
            OnlyOneCommandView onlyOneCommandView = _container.Resolve<OnlyOneCommandView>();
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
            SpawnerView spawner = _container.Resolve<SpawnerView>();
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
            TagView tag = _container.Resolve<TagView>();
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
            ItemView item = _container.Resolve<ItemView>();
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
            ItemModifierView itemModifierView = _container.Resolve<ItemModifierView>();
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
            LootTableView lootTableView = _container.Resolve<LootTableView>();
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
            PredicateView predicate = _container.Resolve<PredicateView>();
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
            EntityView entity = _container.Resolve<EntityView>();
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
            FireworkRocketView fireworkRocket = _container.Resolve<FireworkRocketView>();
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
            RecipeView recipe = _container.Resolve<RecipeView>();
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
            VillagerView villager = _container.Resolve<VillagerView>();
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
            WrittenBookView writtenBook = _container.Resolve<WrittenBookView>();
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
            DatapackView dataPack = _container.Resolve<DatapackView>();
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
            SignView sign = _container.Resolve<SignView>();
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
            DimensionTypeView dimensionType = _container.Resolve<DimensionTypeView>();
            dimensionType.Show();
            dimensionType.Focus();
            SetCBHKState();
        }
        #endregion
    }

    public static class GeneratorClickEvent
    {
        /// <summary>
        /// 为生成器按钮分配方法
        /// </summary>
        /// <param name="id"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        public static IRelayCommand Set(string id, DistributorGenerator function)
        {
            IRelayCommand result = id switch
            {
                "Advancement" => function.StartAdvancementGeneratorCommand,
                "Ooc" => function.StartOnlyOneCommandGeneratorCommand,
                "DamageType" => function.StartDamageTypeGeneratorCommand,
                "Datapack" => function.StartDatapacksGeneratorCommand,
                "ChatType" => function.StartChatTpyeGeneratorCommand,
                "CustomWorld"=>function.StartCustomWorldGeneratorGeneratorCommand,
                "Armorstand" => function.StartArmorStandGeneratorCommand,
                "WrittenBook" => function.StartWrittenBooksGeneratorCommand,
                "Spawners" => function.StartSpawnerGeneratorCommand,
                "Recipes" => function.StartRecipesGeneratorCommand,
                "Villagers" => function.StartVillagersGeneratorCommand,
                "Tags" => function.StartTagsGeneratorCommand,
                "Items" => function.StartItemsGeneratorCommand,
                "LootTable" => function.StartLootTableGeneratorCommand,
                "Predicate"=>function.StartPredicateGeneratorCommand,
                "ItemModifier" => function.StartItemModifierGeneratorCommand,
                "Fireworks" => function.StartFireworksGeneratorCommand,
                "Entities" => function.StartEntitiesGeneratorCommand,
                "Sign" => function.StartSignCommand,
                "Dimension" => function.StartDimensionGeneratorCommand,
                "DimensionType" => function.StartDimensionTypeGeneratorCommand,
                _ => null
            };
            return result;
        }
    }
}