using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace cbhk.Distributor
{
    public partial class GeneratorFunction: ObservableObject
    {
        private MainWindow cbhk;

        public GeneratorFunction(MainWindow win)
        {
            cbhk = win;
        }

        /// <summary>
        /// 设置主窗体状态
        /// </summary>
        private void SetCBHKState()
        {
            cbhk.WindowState = MainWindow.cbhkVisibility switch
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
            Generators.OnlyOneCommandGenerator.OnlyOneCommand ooc = new();
            Generators.OnlyOneCommandGenerator.OnlyOneCommandDataContext context = ooc.DataContext as Generators.OnlyOneCommandGenerator.OnlyOneCommandDataContext;
            SetCBHKState();
            context.home = cbhk;
            ooc.Show();
            ooc.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动刷怪笼生成器
        /// </summary>
        private void StartSpawnerGenerator()
        {
            Generators.SpawnerGenerator.Spawner spawner = new();
            SetCBHKState();
            Generators.SpawnerGenerator.SpawnerDataContext context = spawner.DataContext as Generators.SpawnerGenerator.SpawnerDataContext;
            context.home = cbhk;
            spawner.Show();
            spawner.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动as生成器
        /// </summary>
        private void StartArmorStandsGenerator()
        {
            Generators.ArmorStandGenerator.ArmorStand armorStand = new();
            Generators.ArmorStandGenerator.ArmorStandDataContext context = armorStand.DataContext as Generators.ArmorStandGenerator.ArmorStandDataContext;
            context.home = cbhk;
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
            Generators.TagGenerator.Tag tag = new();
            Generators.TagGenerator.TagDataContext context = tag.DataContext as Generators.TagGenerator.TagDataContext;
            context.home = cbhk;
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
            Generators.ItemGenerator.Item item = new();
            Generators.ItemGenerator.ItemDataContext context = item.DataContext as Generators.ItemGenerator.ItemDataContext;
            context.home = cbhk;
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
            Generators.EntityGenerator.Entity entity = new();
            Generators.EntityGenerator.EntityDataContext context = entity.DataContext as Generators.EntityGenerator.EntityDataContext;
            context.home = cbhk;
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
            Generators.FireworkRocketGenerator.FireworkRocket fireworkRocket = new();
            Generators.FireworkRocketGenerator.FireworkRocketDataContext context = fireworkRocket.DataContext as Generators.FireworkRocketGenerator.FireworkRocketDataContext;
            context.home = cbhk;
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
            Generators.RecipeGenerator.Recipe recipe = new();
            Generators.RecipeGenerator.RecipeDataContext context = recipe.DataContext as Generators.RecipeGenerator.RecipeDataContext;
            context.home = cbhk;
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
            Generators.VillagerGenerator.Villager villager = new();
            Generators.VillagerGenerator.VillagerDataContext context = villager.DataContext as Generators.VillagerGenerator.VillagerDataContext;
            context.home = cbhk;
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
            Generators.WrittenBookGenerator.WrittenBook writtenBook = new();
            Generators.WrittenBookGenerator.WrittenBookDataContext context = writtenBook.DataContext as Generators.WrittenBookGenerator.WrittenBookDataContext;
            context.home = cbhk;
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
            Generators.DataPackGenerator.Datapack dataPack = new();
            Generators.DataPackGenerator.DatapackDataContext context = dataPack.DataContext as Generators.DataPackGenerator.DatapackDataContext;
            context.home = cbhk;
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
            Generators.SignGenerator.Sign sign = new();
            Generators.SignGenerator.SignDataContext context = sign.DataContext as Generators.SignGenerator.SignDataContext;
            context.home = cbhk;
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
            Generators.CustomWorldGenerators.CustomWorldBaseWindow customWorldBaseWindow = new();
            Generators.CustomWorldGenerators.CustomWorldBaseWindowDataContext context = customWorldBaseWindow.DataContext as Generators.CustomWorldGenerators.CustomWorldBaseWindowDataContext;
            context.home = cbhk;
            SetCBHKState();
            customWorldBaseWindow.Show();
            customWorldBaseWindow.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动维度生成器
        /// </summary>
        private void StartDimensionGenerator()
        {
            Generators.DimensionGenerator.Dimension dimension = new();
            Generators.DimensionGenerator.DimensionDataContext context = dimension.DataContext as Generators.DimensionGenerator.DimensionDataContext;
            context.home = cbhk;
            SetCBHKState();
            dimension.Show();
            dimension.Focus();
        }

        [RelayCommand]
        /// <summary>
        /// 启动维度类型生成器
        /// </summary>
        private void StartDimensionTypeGenerator()
        {
            Generators.DimensionTypeGenerator.DimensionType dimensionType = new();
            Generators.DimensionTypeGenerator.DimensionTypeDataContext context = dimensionType.DataContext as Generators.DimensionTypeGenerator.DimensionTypeDataContext;
            context.home = cbhk;
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
        public static IRelayCommand Set(string id, GeneratorFunction function)
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
                "CustomWorld"=>function.StartCustomWorldGeneratorCommand,
                "Dimensions"=>function.StartDimensionGeneratorCommand,
                "DimensionType"=>function.StartDimensionTypeGeneratorCommand,
                _ => null
            };
            return result;
        }
    }
}