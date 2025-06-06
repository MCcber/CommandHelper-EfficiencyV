using CBHK.View.Generator;
using CBHK.Model;
using CBHK.View;
using CBHK.ViewModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using System.Windows;

namespace CBHK.GeneralTool
{
    public partial class DistributorGenerator : ObservableObject
    {
        #region Field
        private MainViewModel CBHK;
        private readonly IContainerProvider _container;
        #endregion

        #region Method
        public DistributorGenerator(IContainerProvider container)
        {
            _container = container;
            MainView mainWindow = _container.Resolve<MainView>();
            if (mainWindow is not null && mainWindow.DataContext is MainViewModel viewModel)
                CBHK = viewModel;
        }

        /// <summary>
        /// 设置主窗体状态
        /// </summary>
        private void SetCBHKState()
        {
            CBHK.WindowState = CBHK.MainViewVisibility switch
            {
                MainWindowProperties.Visibility.MinState => WindowState.Minimized,
                MainWindowProperties.Visibility.KeepState => WindowState.Normal,
                MainWindowProperties.Visibility.Close => WindowState.Minimized,
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
            advancementView.Show();
            advancementView.Focus();
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动as生成器
        /// </summary>
        private void StartArmorStandsGenerator()
        {
            ArmorStandView armorStand = _container.Resolve<ArmorStandView>();
            armorStand.Show();
            armorStand.Focus();
            SetCBHKState();
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
        /// 启动指定的自定义世界生成器
        /// </summary>
        private void StartCustomWorldGenerator()
        {
            SetCBHKState();
        }

        [RelayCommand]
        /// <summary>
        /// 启动维度生成器
        /// </summary>
        private void StartDimensionGenerator()
        {
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
                "Datapack" => function.StartDatapacksGeneratorCommand,
                "ChatType" => function.StartChatTpyeGeneratorCommand,
                "Armorstand" => function.StartArmorStandsGeneratorCommand,
                "WrittenBook" => function.StartWrittenBooksGeneratorCommand,
                "Spawners" => function.StartSpawnerGeneratorCommand,
                "Recipes" => function.StartRecipesGeneratorCommand,
                "Villagers" => function.StartVillagersGeneratorCommand,
                "Tags" => function.StartTagsGeneratorCommand,
                "Items" => function.StartItemsGeneratorCommand,
                "Fireworks" => function.StartFireworksGeneratorCommand,
                "Entities" => function.StartEntitiesGeneratorCommand,
                "Signs" => function.StartSignCommand,
                "CustomWorld" => function.StartCustomWorldGeneratorCommand,
                "Dimensions" => function.StartDimensionGeneratorCommand,
                "DimensionType" => function.StartDimensionTypeGeneratorCommand,
                _ => null
            };
            return result;
        }
    }
}