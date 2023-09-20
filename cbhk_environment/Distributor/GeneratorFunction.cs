using cbhk_environment.Generators.SignGenerator;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;

namespace cbhk_environment.Distributor
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
            cbhk.WindowState = MainWindow.cbhk_visibility switch
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
            Generators.OnlyOneCommandGenerator.OnlyOneCommand ooc_window = new(cbhk);
            SetCBHKState();
            ooc_window.Topmost = true;
            ooc_window.Show();
            ooc_window.Topmost = false;
        }

        /// <summary>
        /// 启动刷怪笼生成器
        /// </summary>
        private void StartSpawnerGeneratorCommand()
        {
            Generators.SpawnerGenerator.Spawner s_window = new Generators.SpawnerGenerator.Spawner(cbhk);
            SetCBHKState();
            s_window.Topmost = true;
            s_window.Show();
            s_window.Topmost = false;
        }

        /// <summary>
        /// 启动as生成器
        /// </summary>
        private void StartArmorStandsGeneratorCommand()
        {
            Generators.ArmorStandGenerator.ArmorStand as_window = new Generators.ArmorStandGenerator.ArmorStand(cbhk);
            SetCBHKState();
            as_window.Topmost = true;
            as_window.Show();
            as_window.Topmost = false;
        }

        /// <summary>
        /// 启动tag生成器
        /// </summary>
        private void StartTagsGeneratorCommand()
        {
            Generators.TagGenerator.Tag tag_window = new Generators.TagGenerator.Tag(cbhk);
            SetCBHKState();
            tag_window.Topmost = true;
            tag_window.Show();
            tag_window.Topmost = false;
        }

        /// <summary>
        /// 启动物品生成器
        /// </summary>
        private void StartItemsGeneratorCommand()
        {
            Generators.ItemGenerator.Item item_window = new Generators.ItemGenerator.Item(cbhk);
            SetCBHKState();
            item_window.Topmost = true;
            item_window.Show();
            item_window.Topmost = false;
        }

        /// <summary>
        /// 启动实体生成器
        /// </summary>
        private void StartEntitiesGeneratorCommand()
        {
            Generators.EntityGenerator.Entity entity_window = new(cbhk);
            SetCBHKState();
            entity_window.Topmost = true;
            entity_window.Show();
            entity_window.Topmost = false;
        }

        /// <summary>
        /// 启动烟花生成器
        /// </summary>
        private void StartFireworksGeneratorCommand()
        {
            Generators.FireworkRocketGenerator.FireworkRocket fireworkRocket = new Generators.FireworkRocketGenerator.FireworkRocket(cbhk);
            SetCBHKState();
            fireworkRocket.Topmost = true;
            fireworkRocket.Show();
            fireworkRocket.Topmost = false;
        }

        /// <summary>
        /// 启动配方生成器
        /// </summary>
        private void StartRecipesGeneratorCommand()
        {
            Generators.RecipeGenerator.Recipe recipe = new Generators.RecipeGenerator.Recipe(cbhk);
            SetCBHKState();
            recipe.Topmost = true;
            recipe.Show();
            recipe.Topmost = false;
        }

        /// <summary>
        /// 启动村民生成器
        /// </summary>
        private void StartVillagersGeneratorCommand()
        {
            Generators.VillagerGenerator.Villager villager = new Generators.VillagerGenerator.Villager(cbhk);
            SetCBHKState();
            villager.Topmost = true;
            villager.Show();
            villager.Topmost = false;
        }

        /// <summary>
        /// 启动成书生成器
        /// </summary>
        private void StartWrittenBooksGeneratorCommand()
        {
            Generators.WrittenBookGenerator.WrittenBook writtenBook = new Generators.WrittenBookGenerator.WrittenBook(cbhk);
            SetCBHKState();
            writtenBook.Topmost = true;
            writtenBook.Show();
            writtenBook.Topmost = false;
        }

        /// <summary>
        /// 启动数据包生成器
        /// </summary>
        private void StartDatapacksGeneratorCommand()
        {
            Generators.DataPackGenerator.Datapack dataPack = new(cbhk);
            SetCBHKState();
            dataPack.Topmost = true;
            dataPack.Show();
            dataPack.Topmost = false;
        }

        private void StartSignCommand()
        {
            Generators.SignGenerator.Sign sign = new();
            SignDataContext context = sign.DataContext as SignDataContext;
            context.home = cbhk;
            SetCBHKState();
            sign.Show();
            sign.Focus();
        }
    }
}
