﻿using cbhk.Common;
using cbhk.Generators.DataPackGenerator;
using cbhk.Generators.DimensionTypeGenerator;
using cbhk.Generators.EntityGenerator;
using cbhk.Generators.FireworkRocketGenerator;
using cbhk.Generators.ItemGenerator;
using cbhk.Generators.RecipeGenerator;
using cbhk.Generators.SignGenerator;
using cbhk.Generators.VillagerGenerator;
using cbhk.Model;
using cbhk.View;
using cbhk.View.Generators;
using cbhk.ViewModel;
using cbhk.ViewModel.Generators.Datapack;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Prism.Ioc;
using System.Windows;

namespace cbhk.GeneralTools
{
    public partial class DistributorGenerator : ObservableObject
    {
        private MainViewModel cbhk;
        private readonly IContainerProvider _container;

        public DistributorGenerator(IContainerProvider container)
        {
            _container = container;
            MainView mainWindow = _container.Resolve<MainView>();
            if (mainWindow is not null)
                cbhk = mainWindow.DataContext as MainViewModel;
        }

        /// <summary>
        /// 设置主窗体状态
        /// </summary>
        private void SetCBHKState()
        {
            cbhk.WindowState = cbhk.MainViewVisibility switch
            {
                MainWindowProperties.Visibility.MinState => WindowState.Minimized,
                MainWindowProperties.Visibility.KeepState => WindowState.Normal,
                MainWindowProperties.Visibility.Close => WindowState.Minimized,
                _ => throw new System.NotImplementedException()
            };
        }

        [RelayCommand]
        /// <summary>
        /// 启动ooc生成器
        /// </summary>
        public void StartOnlyOneCommandGenerator()
        {
            OnlyOneCommandView onlyOneCommandView = _container.Resolve<OnlyOneCommandView>();
            SetCBHKState();
            onlyOneCommandView.Show();
            onlyOneCommandView.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动刷怪笼生成器
        /// </summary>
        private void StartSpawnerGenerator()
        {
            SpawnerView spawner = _container.Resolve<SpawnerView>();
            SetCBHKState();
            spawner.Show();
            spawner.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动as生成器
        /// </summary>
        private void StartArmorStandsGenerator()
        {
            ArmorStandView armorStand = _container.Resolve<ArmorStandView>(Name.ArmorStandView);
            SetCBHKState();
            armorStand.Show();
            armorStand.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动tag生成器
        /// </summary>
        private void StartTagsGenerator()
        {
            TagView tag = _container.Resolve<TagView>();
            SetCBHKState();
            tag.Show();
            tag.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动物品生成器
        /// </summary>
        private void StartItemsGenerator()
        {
            ItemView item = _container.Resolve<ItemView>();
            SetCBHKState();
            item.Show();
            item.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动实体生成器
        /// </summary>
        private void StartEntitiesGenerator()
        {
            EntityView entity = _container.Resolve<EntityView>();
            SetCBHKState();
            entity.Show();
            entity.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动烟花生成器
        /// </summary>
        private void StartFireworksGenerator()
        {
            FireworkRocketView fireworkRocket = _container.Resolve<FireworkRocketView>();
            SetCBHKState();
            fireworkRocket.Show();
            fireworkRocket.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动配方生成器
        /// </summary>
        private void StartRecipesGenerator()
        {
            RecipeView recipe = _container.Resolve<RecipeView>();
            SetCBHKState();
            recipe.Show();
            recipe.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动村民生成器
        /// </summary>
        private void StartVillagersGenerator()
        {
            VillagerView villager = _container.Resolve<VillagerView>();
            SetCBHKState();
            villager.Show();
            villager.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动成书生成器
        /// </summary>
        private void StartWrittenBooksGenerator()
        {
            WrittenBookView writtenBook = _container.Resolve<WrittenBookView>();
            SetCBHKState();
            writtenBook.Show();
            writtenBook.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动数据包生成器
        /// </summary>
        private void StartDatapacksGenerator()
        {
            DatapackView dataPack = _container.Resolve<DatapackView>();
            SetCBHKState();
            dataPack.Show();
            dataPack.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动告示牌生成器
        /// </summary>
        private void StartSign()
        {
            SignView sign = _container.Resolve<SignView>();
            SetCBHKState();
            sign.Show();
            sign.Focus();
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
            SetCBHKState();
            dimensionType.Show();
            dimensionType.Focus();
        }
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
                "Ooc" => function.StartOnlyOneCommandGeneratorCommand,
                "Datapack" => function.StartDatapacksGeneratorCommand,
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