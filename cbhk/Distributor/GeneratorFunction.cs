using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace cbhk.Distributor
{
    public class GeneratorFunction: ObservableObject
    {
        private MainWindow cbhk;

        #region 启动命令
        public RelayCommand StartOoc { get; set; }
        public RelayCommand StartSpawner { get; set; }
        public RelayCommand StartArmorStand { get; set; }
        public RelayCommand StartTags { get; set; }
        public RelayCommand StartItems { get; set; }
        public RelayCommand StartEntities { get; set; }
        public RelayCommand StartFireworks { get; set; }
        public RelayCommand StartRecipes { get; set; }
        public RelayCommand StartVillagers { get; set; }
        public RelayCommand StartWrittenBook { get; set; }
        public RelayCommand StartDatapack { get; set; }
        public RelayCommand StartSign { get; set; }
        #endregion

        public GeneratorFunction(MainWindow win)
        {
            cbhk = win;
            #region 链接命令
            StartOoc = new(StartOnlyOneCommandGeneratorCommand);
            StartSpawner = new(StartSpawnerGeneratorCommand);
            StartArmorStand = new(StartArmorStandsGeneratorCommand);
            StartTags = new(StartTagsGeneratorCommand);
            StartItems = new(StartItemsGeneratorCommand);
            StartEntities = new(StartEntitiesGeneratorCommand);
            StartFireworks = new(StartFireworksGeneratorCommand);
            StartRecipes = new(StartRecipesGeneratorCommand);
            StartVillagers = new(StartVillagersGeneratorCommand);
            StartWrittenBook = new(StartWrittenBooksGeneratorCommand);
            StartDatapack = new(StartDatapacksGeneratorCommand);
            StartSign = new(StartSignCommand);
            #endregion
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

        /// <summary>
        /// 启动ooc生成器
        /// </summary>
        public void StartOnlyOneCommandGeneratorCommand()
        {
            Generators.OnlyOneCommandGenerator.OnlyOneCommand ooc = new();
            Generators.OnlyOneCommandGenerator.OnlyOneCommandDataContext context = ooc.DataContext as Generators.OnlyOneCommandGenerator.OnlyOneCommandDataContext;
            SetCBHKState();
            context.home = cbhk;
            ooc.Show();
            ooc.Focus();
        }

        /// <summary>
        /// 启动刷怪笼生成器
        /// </summary>
        private void StartSpawnerGeneratorCommand()
        {
            Generators.SpawnerGenerator.Spawner spawner = new();
            SetCBHKState();
            Generators.SpawnerGenerator.SpawnerDataContext context = spawner.DataContext as Generators.SpawnerGenerator.SpawnerDataContext;
            context.home = cbhk;
            spawner.Show();
            spawner.Focus();
        }

        /// <summary>
        /// 启动as生成器
        /// </summary>
        private void StartArmorStandsGeneratorCommand()
        {
            Generators.ArmorStandGenerator.ArmorStand armorStand = new();
            Generators.ArmorStandGenerator.ArmorStandDataContext context = armorStand.DataContext as Generators.ArmorStandGenerator.ArmorStandDataContext;
            context.home = cbhk;
            SetCBHKState();
            armorStand.Show();
            armorStand.Focus();
        }

        /// <summary>
        /// 启动tag生成器
        /// </summary>
        private void StartTagsGeneratorCommand()
        {
            Generators.TagGenerator.Tag tag = new();
            Generators.TagGenerator.TagDataContext context = tag.DataContext as Generators.TagGenerator.TagDataContext;
            context.home = cbhk;
            SetCBHKState();
            tag.Show();
            tag.Focus();
        }

        /// <summary>
        /// 启动物品生成器
        /// </summary>
        private void StartItemsGeneratorCommand()
        {
            Generators.ItemGenerator.Item item = new();
            Generators.ItemGenerator.ItemDataContext context = item.DataContext as Generators.ItemGenerator.ItemDataContext;
            context.home = cbhk;
            SetCBHKState();
            item.Show();
            item.Focus();
        }

        /// <summary>
        /// 启动实体生成器
        /// </summary>
        private void StartEntitiesGeneratorCommand()
        {
            Generators.EntityGenerator.Entity entity = new();
            Generators.EntityGenerator.EntityDataContext context = entity.DataContext as Generators.EntityGenerator.EntityDataContext;
            context.home = cbhk;
            SetCBHKState();
            entity.Show();
            entity.Focus();
        }

        /// <summary>
        /// 启动烟花生成器
        /// </summary>
        private void StartFireworksGeneratorCommand()
        {
            Generators.FireworkRocketGenerator.FireworkRocket fireworkRocket = new();
            Generators.FireworkRocketGenerator.FireworkRocketDataContext context = fireworkRocket.DataContext as Generators.FireworkRocketGenerator.FireworkRocketDataContext;
            context.home = cbhk;
            SetCBHKState();
            fireworkRocket.Show();
            fireworkRocket.Focus();
        }

        /// <summary>
        /// 启动配方生成器
        /// </summary>
        private void StartRecipesGeneratorCommand()
        {
            Generators.RecipeGenerator.Recipe recipe = new();
            Generators.RecipeGenerator.RecipeDataContext context = recipe.DataContext as Generators.RecipeGenerator.RecipeDataContext;
            context.home = cbhk;
            SetCBHKState();
            recipe.Show();
            recipe.Focus();
        }

        /// <summary>
        /// 启动村民生成器
        /// </summary>
        private void StartVillagersGeneratorCommand()
        {
            Generators.VillagerGenerator.Villager villager = new();
            Generators.VillagerGenerator.VillagerDataContext context = villager.DataContext as Generators.VillagerGenerator.VillagerDataContext;
            context.home = cbhk;
            SetCBHKState();
            villager.Show();
            villager.Focus();
        }

        /// <summary>
        /// 启动成书生成器
        /// </summary>
        private void StartWrittenBooksGeneratorCommand()
        {
            Generators.WrittenBookGenerator.WrittenBook writtenBook = new();
            Generators.WrittenBookGenerator.WrittenBookDataContext context = writtenBook.DataContext as Generators.WrittenBookGenerator.WrittenBookDataContext;
            context.home = cbhk;
            SetCBHKState();
            writtenBook.Show();
            writtenBook.Focus();
        }

        /// <summary>
        /// 启动数据包生成器
        /// </summary>
        private void StartDatapacksGeneratorCommand()
        {
            Generators.DataPackGenerator.Datapack dataPack = new();
            Generators.DataPackGenerator.DatapackDataContext context = dataPack.DataContext as Generators.DataPackGenerator.DatapackDataContext;
            context.home = cbhk;
            SetCBHKState();
            dataPack.Show();
            dataPack.Focus();
        }

        private void StartSignCommand()
        {
            Generators.SignGenerator.Sign sign = new();
            Generators.SignGenerator.SignDataContext context = sign.DataContext as Generators.SignGenerator.SignDataContext;
            context.home = cbhk;
            SetCBHKState();
            sign.Show();
            sign.Focus();
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
        public static RelayCommand Set(string id, GeneratorFunction function)
        {
            RelayCommand result = id switch
            {
                "Ooc" => function.StartOoc,
                "Datapack" => function.StartDatapack,
                "Armorstand" => function.StartArmorStand,
                "Writtenbook" => function.StartWrittenBook,
                "Spawners" => function.StartSpawner,
                "Recipes" => function.StartRecipes,
                "Villagers" => function.StartVillagers,
                "Tags" => function.StartTags,
                "Items" => function.StartItems,
                "Fireworks" => function.StartFireworks,
                "Entities" => function.StartEntities,
                "Signs" => function.StartSign,
                _ => null
            };
            return result;
        }
    }
}