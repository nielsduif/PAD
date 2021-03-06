using HarvestValley.GameObjects;
using HarvestValley.GameObjects.Shop;
using HarvestValley.GameObjects.Tools;
using HarvestValley.GameObjects.Tutorial;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace HarvestValley.GameStates
{
    class PlayingState : GameObjectList
    {
        Map map;
        Options options;
        GameObjectList cells;
        Player player;
        GameObjectList plants;
        GameObjectList trees;
        GameObjectList stones;
        GameObjectList sprinklers;
        GameObjectList cliff;
        GameObjectList borderGrass;
        MouseGameObject MouseGO;
        TutorialStepList tutorialStepList;
        EnergyBar energyBar;
        Sleeping sleeping;
        Hotbar hotbar;
        ItemList itemList;
        ShopMenuUIList shopUI;
        GameObjectList shopPC;
        Wallet wallet;
        GameObjectList tent;
        Target target;
        Sounds sounds;
        Vector2 prevPos;

        /// <summary>
        /// In the constructor we initialize instances and add them to the PlayingState GameObjectList
        /// </summary>
        public PlayingState()
        {
            sounds = new Sounds();
            MouseGO = new MouseGameObject();

            map = new Map();
            cells = new GameObjectList();

            // Adding cells to the map GameObjectList
            for (int i = 0; i < map.rows; i++)
            {
                for (int x = 0; x < map.cols; x++)
                {
                    Cell c = new Cell(new Vector2(Cell.CELL_SIZE * x - GameEnvironment.Screen.X, Cell.CELL_SIZE * i - GameEnvironment.Screen.Y), .5f, x + (map.cols * i));
                    cells.Add(c);
                }
            }
            Add(cells);

            cliff = new GameObjectList();
            Add(cliff);

            borderGrass = new GameObjectList();
            Add(borderGrass);
            BuildBorder();

            plants = new GameObjectList();
            Add(plants);

            player = new Player("Player/jorrit", new Vector2(GameEnvironment.Screen.X / 2, GameEnvironment.Screen.Y / 2));
            Add(player);

            tent = new GameObjectList();
            tent.Add(new Tent());
            Add(tent);

            shopPC = new GameObjectList();
            shopPC.Add(new ShopBoard(tent.Children[0] as Tent));
            Add(shopPC);

            stones = new GameObjectList();
            Add(stones);

            trees = new GameObjectList();
            Add(trees);

            sprinklers = new GameObjectList();
            Add(sprinklers);

            energyBar = new EnergyBar(GameEnvironment.Screen.X - 60, GameEnvironment.Screen.Y - 220, 40, 200);
            Add(energyBar);

            sleeping = new Sleeping();
            Add(sleeping);

            itemList = new ItemList();

            hotbar = new Hotbar(itemList);
            Add(hotbar);

            wallet = new Wallet();

            Add(shopUI = new ShopMenuUIList(itemList, (tent.Children[0] as Tent), MouseGO, wallet));

            Add(wallet);

            options = new Options(MouseGO);

            Add(target = new Target(itemList, wallet, player, sounds));

            Add(options);

            tutorialStepList = new TutorialStepList(MouseGO);
            Add(tutorialStepList);

            Add(MouseGO);

            SpawnTent();
            SpawnShopSign();
            PlaceStonesAndTrees();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            // Checks if the UI is active and if it is, the game is frozen and only the UI works
            if (!UIIsActive)
            {
                SleepActions(gameTime);
                CheckSleepHitbox();
                CheckPlantsWater();
                PlayerEnergy();
            }
        }

        public override void HandleInput(InputHelper inputHelper)
        {
            base.HandleInput(inputHelper);
            // Checks if the UI is active and if it is, the game is frozen and only the UI works
            if (!UIIsActive)
            {
                CameraSystem(inputHelper);
                CheckActionInput(inputHelper);
                PickupSprinkler(inputHelper);
                CheckHotbarSelection(inputHelper);
                ToggleShopMenu(inputHelper);
            }
        }

        /// <summary>
        /// Deducts energy in de player class when called
        /// </summary>
        void PlayerEnergy()
        {
            if (player.DeductEnergy)
            {
                energyBar.energyLost += energyBar.oneUse;
            }
        }

        /// <summary>
        /// Checks for all the UI booleans if the UI is active
        /// </summary>
        bool UIIsActive
        {
            get { return target.panel_bg.Visible || options.IsActive || shopUI.IsActive; }
        }

        /// <summary>
        /// Opens the shop page when the shopsign next to the tent is pressed and within player reach
        /// </summary>
        /// <param name="inputHelper"></param>
        void ToggleShopMenu(InputHelper inputHelper)
        {
            //Activate UI bools
            if (inputHelper.MouseLeftButtonPressed() && MouseGO.CollidesWith((shopPC.Children[0] as ShopBoard).Sprite) && player.playerReach.CollidesWith((shopPC.Children[0] as ShopBoard).Sprite))
            {
                shopUI.InitShopWelcomePage();
            }
        }

        void ConvertFromHotbarToMoney(Item item, int amount)
        {
            if (item == target.targetItem && item.isStackable && !target.collected)
            {
                target.AddToTarget(amount);
            }
        }

        /// <summary>
        /// Checks if the player walks into the grey part of the tent, when he does the sleeping actions are called via a boolean
        /// </summary>
        void CheckSleepHitbox()
        {
            if ((tent.Children[0] as Tent).CollidesWithSleep(player))
            {
                sleeping.sleepHitboxHit = true;
                if (tutorialStepList.step == 5)
                {
                    tutorialStepList.step += 1;
                }
            }
            else
            {
                sleeping.sleepHitboxHit = false;
            }
        }

        /// <summary>
        /// Checks where the tent is placed, with which cells it collides and then sets the cellHasTent boolean in those cells to true
        /// </summary>
        void SpawnTent()
        {
            foreach (Cell c in cells.Children)
            {
                foreach (Tent t in tent.Children)
                {
                    if (c.CellCollidesWith(t.Children[0] as SpriteGameObject))
                    {
                        c.cellHasTent = true;
                    }
                }
            }
        }

        /// <summary>
        /// Checks where the shop sign is placed, with which cells it collides and then sets the cellHasShop boolean in those cells to true
        /// </summary>
        void SpawnShopSign()
        {
            foreach (Cell c in cells.Children)
            {
                if (c.CellCollidesWith((shopPC.Children[0] as ShopBoard).Sprite))
                {
                    c.cellHasShop = true;
                }
            }
        }

        /// <summary>
        /// Creates a border around the map by placing cliffs around the map
        /// </summary>
        void BuildBorder()
        {
            for (int x = 0; x < map.cols; x++)
            {
                cliff.Add(new Cliff(new Vector2(-map.mapSizeX + map.cellSize * x, -map.mapSizeY - map.cellSize), 0));
                cliff.Add(new Cliff(new Vector2(-map.mapSizeX + map.cellSize * x, map.rows * map.cellSize - map.mapSizeY), 180));
            }
            cliff.Add(new Cliff(new Vector2(-map.mapSizeX - map.cellSize, -map.mapSizeY - map.cellSize), 0, 2));
            cliff.Add(new Cliff(new Vector2(GameEnvironment.Screen.X + map.mapSizeX, -map.mapSizeY - map.cellSize), 90, 2));
            for (int y = 0; y < map.rows; y++)
            {
                cliff.Add(new Cliff(new Vector2(-map.mapSizeX - map.cellSize, -map.mapSizeY + map.cellSize * y), 270));
                cliff.Add(new Cliff(new Vector2(GameEnvironment.Screen.X + map.mapSizeX, -map.mapSizeY + map.cellSize * y), 90));
            }
            cliff.Add(new Cliff(new Vector2(-map.mapSizeX - map.cellSize, map.rows * map.cellSize - map.mapSizeY), 270, 2));
            cliff.Add(new Cliff(new Vector2(GameEnvironment.Screen.X + map.mapSizeX, map.rows * map.cellSize - map.mapSizeY), 180, 2));

            for (int i = 0; i < map.cols + 2; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    borderGrass.Add(new SpriteGameObject("tiles/spr_grass") { Position = new Vector2(-map.mapSizeX - map.cellSize + map.cellSize * i, -map.mapSizeY - map.cellSize * 6 + map.cellSize * j), scale = .5f });
                    borderGrass.Add(new SpriteGameObject("tiles/spr_grass") { Position = new Vector2(-map.mapSizeX - map.cellSize + map.cellSize * i, map.rows * map.cellSize - map.mapSizeY + map.cellSize * 5 - map.cellSize * j), scale = .5f });
                }
            }
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < map.rows + 12; j++)
                {
                    borderGrass.Add(new SpriteGameObject("tiles/spr_grass") { Position = new Vector2(-map.mapSizeX - map.cellSize * 10 + map.cellSize * i, -map.mapSizeY - map.cellSize * 6 + map.cellSize * j), scale = .5f });
                    borderGrass.Add(new SpriteGameObject("tiles/spr_grass") { Position = new Vector2(GameEnvironment.Screen.X + map.mapSizeX + map.cellSize + map.cellSize * i, -map.mapSizeY - map.cellSize * 6 + map.cellSize * j), scale = .5f });
                }
            }
        }

        /// <summary> 
        /// Places the initial stones and trees on the map
        /// </summary>
        void PlaceStonesAndTrees()
        {
            foreach (Cell c in cells.Children)
            {
                if (!c.cellHasTent && !c.cellHasShop)
                {
                    #region outer ring
                    if (c.Position.X > -map.mapSizeX - 10 && c.Position.X < -map.mapSizeX + 5 * map.cellSize
                    && c.Position.Y > -map.mapSizeY + 4 * map.cellSize && c.Position.Y < GameEnvironment.Screen.Y + map.mapSizeY - 5 * map.cellSize)
                    {
                        int r = GameEnvironment.Random.Next(map.outerringRandomTree);
                        if (r > 0 && !c.CellCollidesWith(player.playerReach) && !c.HasCollision)
                        {
                            c.cellHasTree = true;
                            trees.Add(new Tree(c.Position, .5f, 3));
                        }
                        if (!c.HasCollision)
                        {
                            int s = GameEnvironment.Random.Next(map.outerringRandomStone);
                            if (s == 0 && !c.CellCollidesWith(player.playerReach))
                            {
                                c.cellHasStone = true;
                                stones.Add(new Stone(c.Position, .5f));
                            }
                        }
                    }
                    if (c.Position.X > GameEnvironment.Screen.X + map.mapSizeX - 5 * map.cellSize - 10 && c.Position.X < GameEnvironment.Screen.X + map.mapSizeX
                    && c.Position.Y > -map.mapSizeY + 4 * map.cellSize && c.Position.Y < GameEnvironment.Screen.Y + map.mapSizeY - 5 * map.cellSize)
                    {
                        int r = GameEnvironment.Random.Next(map.outerringRandomTree);
                        if (r > 0 && !c.CellCollidesWith(player.playerReach) && !c.HasCollision)
                        {
                            c.cellHasTree = true;
                            trees.Add(new Tree(c.Position, .5f, 3));
                        }
                        if (!c.HasCollision)
                        {
                            int s = GameEnvironment.Random.Next(map.outerringRandomStone);
                            if (s == 0 && !c.CellCollidesWith(player.playerReach))
                            {
                                c.cellHasStone = true;
                                stones.Add(new Stone(c.Position, .5f));
                            }
                        }
                    }
                    if (c.Position.Y > -map.mapSizeY - 10 && c.Position.Y < -map.mapSizeY + 5 * map.cellSize)
                    {
                        int r = GameEnvironment.Random.Next(map.outerringRandomTree);
                        if (r > 0 && !c.CellCollidesWith(player.playerReach) && !c.HasCollision)
                        {
                            c.cellHasTree = true;
                            trees.Add(new Tree(c.Position, .5f, 3));
                        }
                        if (!c.HasCollision)
                        {
                            int s = GameEnvironment.Random.Next(map.outerringRandomStone);
                            if (s == 0 && !c.CellCollidesWith(player.playerReach))
                            {
                                c.cellHasStone = true;
                                stones.Add(new Stone(c.Position, .5f));
                            }
                        }
                    }
                    if (c.Position.Y > GameEnvironment.Screen.Y + map.mapSizeY - 5 * map.cellSize - 60 && c.Position.Y < GameEnvironment.Screen.Y + map.mapSizeY)
                    {
                        int r = GameEnvironment.Random.Next(map.outerringRandomTree);
                        if (r > 0 && !c.CellCollidesWith(player.playerReach) && !c.HasCollision)
                        {
                            c.cellHasTree = true;
                            trees.Add(new Tree(c.Position, .5f, 3));
                        }
                        if (!c.HasCollision)
                        {
                            int s = GameEnvironment.Random.Next(map.outerringRandomStone);
                            if (s == 0 && !c.CellCollidesWith(player.playerReach))
                            {
                                c.cellHasStone = true;
                                stones.Add(new Stone(c.Position, .5f));
                            }
                        }
                    }
                    #endregion

                    #region middle ring
                    if (c.Position.X > -map.mapSizeX - 10 + 5 * map.cellSize && c.Position.X < -map.mapSizeX + 10 * map.cellSize
                    && c.Position.Y > -map.mapSizeY + 4 * map.cellSize && c.Position.Y < GameEnvironment.Screen.Y + map.mapSizeY - 5 * map.cellSize)
                    {
                        int r = GameEnvironment.Random.Next(map.middleringRandomTree);
                        if (r == 0 && !c.CellCollidesWith(player.playerReach) && !c.HasCollision)
                        {
                            c.cellHasTree = true;
                            trees.Add(new Tree(c.Position, .5f, 3));
                        }
                        if (!c.HasCollision)
                        {
                            int s = GameEnvironment.Random.Next(map.middleringRandomStone);
                            if (s == 0 && !c.CellCollidesWith(player.playerReach))
                            {
                                c.cellHasStone = true;
                                stones.Add(new Stone(c.Position, .5f));
                            }
                        }
                    }
                    if (c.Position.X > GameEnvironment.Screen.X + map.mapSizeX - 10 * map.cellSize - 10 && c.Position.X < GameEnvironment.Screen.X + map.mapSizeX - 5 * map.cellSize
                    && c.Position.Y > -map.mapSizeY + 4 * map.cellSize && c.Position.Y < GameEnvironment.Screen.Y + map.mapSizeY - 5 * map.cellSize)
                    {
                        int r = GameEnvironment.Random.Next(map.middleringRandomTree);
                        if (r == 0 && !c.CellCollidesWith(player.playerReach) && !c.HasCollision)
                        {
                            c.cellHasTree = true;
                            trees.Add(new Tree(c.Position, .5f, 3));
                        }
                        if (!c.HasCollision)
                        {
                            int s = GameEnvironment.Random.Next(map.middleringRandomStone);
                            if (s == 0 && !c.CellCollidesWith(player.playerReach))
                            {
                                c.cellHasStone = true;
                                stones.Add(new Stone(c.Position, .5f));
                            }
                        }
                    }
                    if (c.Position.Y > -map.mapSizeY - 10 + 5 * map.cellSize && c.Position.Y < -map.mapSizeY + 10 * map.cellSize
                    && c.Position.X > -map.mapSizeX + 9 * map.cellSize && c.Position.X < GameEnvironment.Screen.X + map.mapSizeX - 10 * map.cellSize)
                    {
                        int r = GameEnvironment.Random.Next(map.middleringRandomTree);
                        if (r == 0 && !c.CellCollidesWith(player.playerReach) && !c.HasCollision)
                        {
                            c.cellHasTree = true;
                            trees.Add(new Tree(c.Position, .5f, 3));
                        }
                        if (!c.HasCollision)
                        {
                            int s = GameEnvironment.Random.Next(map.middleringRandomStone);
                            if (s == 0 && !c.CellCollidesWith(player.playerReach))
                            {
                                c.cellHasStone = true;
                                stones.Add(new Stone(c.Position, .5f));
                            }
                        }
                    }
                    if (c.Position.Y > GameEnvironment.Screen.Y + map.mapSizeY - 11 * map.cellSize && c.Position.Y - 60 < GameEnvironment.Screen.Y + map.mapSizeY - 6 * map.cellSize
                        && c.Position.X > -map.mapSizeX + 9 * map.cellSize && c.Position.X < GameEnvironment.Screen.X + map.mapSizeX - 10 * map.cellSize)
                    {
                        int r = GameEnvironment.Random.Next(map.middleringRandomTree);
                        if (r == 0 && !c.CellCollidesWith(player.playerReach) && !c.HasCollision)
                        {
                            c.cellHasTree = true;
                            trees.Add(new Tree(c.Position, .5f, 3));
                        }
                        if (!c.HasCollision)
                        {
                            int s = GameEnvironment.Random.Next(map.middleringRandomStone);
                            if (s == 0 && !c.CellCollidesWith(player.playerReach))
                            {
                                c.cellHasStone = true;
                                stones.Add(new Stone(c.Position, .5f));
                            }
                        }
                    }
                    #endregion

                    #region inner ring
                    if (c.Position.X > -map.mapSizeX + 10 * map.cellSize && c.Position.X < GameEnvironment.Screen.X + map.mapSizeX - 10 * map.cellSize
                    && c.Position.Y > -map.mapSizeY + 10 * map.cellSize && c.Position.Y < GameEnvironment.Screen.Y + map.mapSizeY - 10 * map.cellSize)
                    {
                        int r = GameEnvironment.Random.Next(map.innerringRandomTree);
                        if (r == 0 && !c.CellCollidesWith(player.playerReach) && !c.HasCollision)
                        {
                            c.cellHasTree = true;
                            trees.Add(new Tree(c.Position, .5f, 3));
                        }
                        if (!c.HasCollision)
                        {
                            int s = GameEnvironment.Random.Next(map.innerringRandomStone);
                            if (s == 0 && !c.CellCollidesWith(player.playerReach))
                            {
                                c.cellHasStone = true;
                                stones.Add(new Stone(c.Position, .5f));
                            }
                        }
                    }
                    #endregion
                }
            }
        }

        /// <summary>
        /// Contains all the actions surrounding sleeping
        /// </summary>
        void SleepActions(GameTime gameTime)
        {
            if (sleeping.fadeIn)
            {
                player.sleeping = true;
                player.Visible = false;
            }
            else if (sleeping.fadeOut)
            {
                player.sleeping = false;
                player.Visible = true;
            }
            if (sleeping.fadeAmount >= 1)
            {
                player.sleepingPosition = true;

                //Play RoosterCrowing
                GameEnvironment.AssetManager.PlaySound(sounds.SEIs[5]);

                if (sleeping.fadeOut)
                {
                    for (int i = 0; i < cells.Children.Count; i++)
                    {
                        if ((cells.Children[i] as Cell).cellHasSprinkler) //planten naast een sprinkler
                        {
                            foreach (Cell c in cells.Children)
                            {
                                if ((cells.Children[i] as Cell).Position + new Vector2(64, 0) == c.Position && c.cellIsTilled) // rechts
                                {
                                    c.cellHasWater = true;
                                    c.nextToSprinkler = true;
                                    c.ChangeSpriteTo(2);
                                    if (c.cellHasPlant)
                                    {
                                        for (int x = plants.Children.Count - 1; x >= 0; x--)
                                        {
                                            if (plants.Children[x].Position == c.Position)
                                            {
                                                (plants.Children[x] as Plant).soilHasWater = true;
                                            }
                                        }
                                    }
                                }
                                if ((cells.Children[i] as Cell).Position + new Vector2(0, 64) == c.Position && c.cellIsTilled)
                                {
                                    c.cellHasWater = true;
                                    c.nextToSprinkler = true;
                                    c.ChangeSpriteTo(2);
                                    if (c.cellHasPlant)
                                    {
                                        for (int x = plants.Children.Count - 1; x >= 0; x--)
                                        {
                                            if (plants.Children[x].Position == c.Position)
                                            {
                                                (plants.Children[x] as Plant).soilHasWater = true;
                                            }
                                        }
                                    }
                                }
                                if ((cells.Children[i] as Cell).Position + new Vector2(0, -64) == c.Position && c.cellIsTilled)
                                {
                                    c.cellHasWater = true;
                                    c.nextToSprinkler = true;
                                    c.ChangeSpriteTo(2);
                                    if (c.cellHasPlant)
                                    {
                                        for (int x = plants.Children.Count - 1; x >= 0; x--)
                                        {
                                            if (plants.Children[x].Position == c.Position)
                                            {
                                                (plants.Children[x] as Plant).soilHasWater = true;
                                            }
                                        }
                                    }
                                }
                                if ((cells.Children[i] as Cell).Position + new Vector2(-64, 0) == c.Position && c.cellIsTilled)
                                {
                                    c.cellHasWater = true;
                                    c.nextToSprinkler = true;
                                    c.ChangeSpriteTo(2);
                                    if (c.cellHasPlant)
                                    {
                                        for (int x = plants.Children.Count - 1; x >= 0; x--)
                                        {
                                            if (plants.Children[x].Position == c.Position)
                                            {
                                                (plants.Children[x] as Plant).soilHasWater = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    foreach (Cell c in cells.Children)
                    {
                        if (!c.cellHasWater && c.cellIsTilled)
                        {
                            if (c.randomGrass == 1) //remove grass randomly
                            {
                                c.ChangeSpriteTo(0);

                                if (c.cellHasPlant)
                                {
                                    for (int x = plants.Children.Count - 1; x >= 0; x--)
                                    {
                                        if (plants.Children[x].Position == c.Position)
                                        {
                                            plants.Remove(plants.Children[x]);
                                        }
                                    }
                                }

                                c.cellHasPlant = false;
                                c.cellIsTilled = false;
                            }
                            c.nextRandom = true;
                        }
                        foreach (Plant p in plants.Children)
                        {
                            if (p.soilHasWater)
                            {
                                p.growthStage++;
                                p.soilHasWater = false;
                            }
                        }
                        if (c.cellHasWater && !c.nextToSprinkler)
                        {
                            c.cellHasWater = false;
                            foreach (Plant p in plants.Children)
                            {
                                p.soilHasWater = false;
                            }
                            c.ChangeSpriteTo(1);
                        }
                    }

                    energyBar.Reset();
                    for (int i = trees.Children.Count - 1; i >= 0; i--)
                    {
                        (trees.Children[i] as Tree).growthStage++;
                    }
                }
                if (energyBar.passOut)
                {
                    sleeping.Sleep();
                    sleeping.useOnce = false;
                }
                sleeping.Update(gameTime);
            }
            if (energyBar.passOut)
            {
                sleeping.Sleep();
                sleeping.useOnce = false;
            }
        }

        /// <summary>
        /// Continually checks for the cell boolean cellHasWater to also set the soilHasWater boolean in plant when they overlap
        /// </summary>
        void CheckPlantsWater()
        {
            foreach (Cell c in cells.Children)
            {
                for (int i = plants.Children.Count - 1; i >= 0; i--)
                {
                    if (c.Position == plants.Children[i].Position)
                    {
                        if (c.cellHasWater)
                        {
                            (plants.Children[i] as Plant).soilHasWater = true;
                        }
                        if (!c.cellHasWater)
                        {
                            (plants.Children[i] as Plant).soilHasWater = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks for WASD walking inputs
        /// When input is detected moves all the map objects 
        /// Also checks for all collisions
        /// </summary>
        /// <param name="inputHelper"></param>
        void CameraSystem(InputHelper inputHelper)
        {
            Vector2 moveVector = Vector2.Zero;

            if (!inputHelper.IsKeyDown(Keys.A) && !inputHelper.IsKeyDown(Keys.S) && !inputHelper.IsKeyDown(Keys.D) && !inputHelper.IsKeyDown(Keys.W))
            {
                GameEnvironment.AssetManager.StopSound(sounds.SEIs[0]);
            }
            else
            {
                GameEnvironment.AssetManager.PlayOnce(sounds.SEIs[0]);
            }
            if (inputHelper.IsKeyDown(Keys.A))
            {
                moveVector = new Vector2(player.speed, moveVector.Y);
            }
            if (inputHelper.IsKeyDown(Keys.D))
            {
                moveVector = new Vector2(-player.speed, moveVector.Y);
            }
            if (inputHelper.IsKeyDown(Keys.D) && inputHelper.IsKeyDown(Keys.A))
            {
                moveVector = new Vector2(0, moveVector.Y);
            }
            if (inputHelper.IsKeyDown(Keys.S))
            {
                moveVector = new Vector2(moveVector.X, -player.speed);
            }
            if (inputHelper.IsKeyDown(Keys.W))
            {
                moveVector = new Vector2(moveVector.X, player.speed);
            }
            if (inputHelper.IsKeyDown(Keys.W) && inputHelper.IsKeyDown(Keys.S))
            {
                moveVector = new Vector2(moveVector.X, 0);
            }

            for (int i = trees.Children.Count - 1; i >= 0; i--)
            {
                if ((trees.Children[i] as Tree).CollidesWith(player))
                {
                    SetPreviousPosition();
                }
            }

            foreach (Cliff c in cliff.Children)
            {
                foreach (RotatingSpriteGameObject r in c.Children)
                {
                    if (r.CollidesWith(player))
                    {
                        SetPreviousPosition();
                    }
                }
            }

            if (player.sleepingPosition)
            {
                cells.Position = prevPos - player.newSleepingPosition;
                trees.Position = prevPos - player.newSleepingPosition;
                stones.Position = prevPos - player.newSleepingPosition;
                sprinklers.Position = prevPos - player.newSleepingPosition;
                plants.Position = prevPos - player.newSleepingPosition;
                tent.Position = prevPos - player.newSleepingPosition;
                cliff.Position = prevPos - player.newSleepingPosition;
                borderGrass.Position = prevPos - player.newSleepingPosition;
                shopPC.Position = prevPos - player.newSleepingPosition;
                player.sleepingPosition = false;
            }

            if (player.sleeping)
            {
                SetPreviousPosition();
            }

            if ((tent.Children[0] as Tent).CollidesWith(player))
            {
                SetPreviousPosition();
            }

            for (int i = stones.Children.Count - 1; i >= 0; i--)
            {
                if ((stones.Children[i] as Stone).CollidesWith(player))
                {
                    SetPreviousPosition();
                }
            }

            for (int i = sprinklers.Children.Count - 1; i >= 0; i--)
            {
                if ((sprinklers.Children[i] as SprinklerObject).CollidesWith(player))
                {
                    SetPreviousPosition();
                }
            }

            if ((shopPC.Children[0] as ShopBoard).CollidesWith(player))
            {
                SetPreviousPosition();
            }

            prevPos = cells.Position;
            borderGrass.Position += moveVector;
            cliff.Position += moveVector;
            tent.Position += moveVector;
            cells.Position += moveVector;
            trees.Position += moveVector;
            stones.Position += moveVector;
            sprinklers.Position += moveVector;
            plants.Position += moveVector;
            shopPC.Position += moveVector;
        }

        /// <summary>
        /// when collision is detected in CameraSystem all the map objects are set to their last position, which prevents the player from walking through them
        /// </summary>
        void SetPreviousPosition()
        {
            cells.Position = prevPos;
            trees.Position = prevPos;
            stones.Position = prevPos;
            sprinklers.Position = prevPos;
            plants.Position = prevPos;
            tent.Position = prevPos;
            cliff.Position = prevPos;
            borderGrass.Position = prevPos;
            shopPC.Position = prevPos;
        }

        /// <summary>
        /// Checks for all the actions for each cell, mouse collision and playerreach to reduce the amount of for each loops and if statements in all the included functions
        /// </summary>
        /// <param name="inputHelper"></param>
        void CheckActionInput(InputHelper inputHelper)
        {
            foreach (Cell c in cells.Children)
            {
                if (c.CellCollidesWith(MouseGO.HitBox) && c.CellCollidesWith(player.playerReach))
                {
                    if (inputHelper.MouseLeftButtonDown())
                    {
                        CheckHoeInput(c);
                        CheckSeedInput(c);
                        CheckTreeSeedInput(c);

                    }
                    if (inputHelper.MouseLeftButtonPressed())
                    {
                        CheckWateringCanInput(c);
                        CheckPickaxeInput(c);
                        CheckSprinklerInput(c);
                        CheckAxeInput(c);
                    }
                    if (inputHelper.MouseRightButtonDown())
                    {
                        CheckPlantPickup(c);
                    }
                }
            }
        }

        /// <summary>
        /// Checks for input from the hoe, if detected it tills the ground and reduces energy
        /// </summary>
        /// <param name="c"></param>
        void CheckHoeInput(Cell c)
        {
            if (itemList.itemSelected == "HOE" && !c.cellIsTilled && !c.HasCollision)
            {
                if (tutorialStepList.step == 1)
                {
                    tutorialStepList.step += 1;
                }
                //Play HittingGround
                GameEnvironment.AssetManager.PlaySound(sounds.SEIs[7]);

                c.ChangeSpriteTo(1);
                c.cellIsTilled = true;
                energyBar.energyLost += energyBar.oneUse;
            }
        }

        /// <summary>
        /// Checks for input from the seed, if detected reduces the amount of seeds, places a plant and reduces energy
        /// </summary>
        /// <param name="c"></param>
        void CheckSeedInput(Cell c)
        {
            foreach (Item item in itemList.Children)
            {
                if (item is Seed)
                {
                    if (itemList.itemSelected == "SEED" && c.cellIsTilled && !c.cellHasPlant && item.itemAmount > 0 && !c.cellHasSprinkler)
                    {
                        if (tutorialStepList.step == 2)
                        {
                            tutorialStepList.step += 1;
                        }

                        //Play Shakking1
                        GameEnvironment.AssetManager.PlaySound(sounds.SEIs[8]);

                        item.itemAmount -= 1;
                        c.cellHasPlant = true;
                        energyBar.energyLost += energyBar.oneUse;
                        plants.Add(new Plant(c.Position, 2));
                    }
                }
            }
        }

        /// <summary>
        /// Checks for input form the sprinkler, if detected reduces the amount of sprinklers, places a sprinkler and reduces energy
        /// </summary>
        /// <param name="c"></param>
        void CheckSprinklerInput(Cell c)
        {
            foreach (Item item in itemList.Children)
            {
                if (item is Sprinkler)
                {
                    if (itemList.itemSelected == "SPRINKLER" && !c.cellHasPlant && !c.HasCollision && item.itemAmount > 0)
                    {
                        item.itemAmount -= 1;
                        c.cellHasSprinkler = true;
                        energyBar.energyLost += energyBar.oneUse;
                        sprinklers.Add(new SprinklerObject(c.Position, 1));
                        //Play MetalRattling
                        GameEnvironment.AssetManager.PlaySound(sounds.SEIs[6]);
                    }
                }
            }
        }

        /// <summary>
        /// Checks for input from the watering can, if detected gives the cell water and reduces energy
        /// </summary>
        /// <param name="c"></param>
        void CheckWateringCanInput(Cell c)
        {
            if (itemList.itemSelected == "WATERINGCAN" && c.cellIsTilled && !c.cellHasWater)
            {
                if (tutorialStepList.step == 3)
                {
                    tutorialStepList.step += 1;
                }
                //Play WaterSplash
                GameEnvironment.AssetManager.PlaySound(sounds.SEIs[4]);

                energyBar.energyLost += energyBar.oneUse;
                c.cellHasWater = true;
                c.ChangeSpriteTo(2);
            }
        }

        /// <summary>
        /// Checks for input from the tree seed, if detected reduces a tree seed, places a tree and reduces energy
        /// </summary>
        /// <param name="c"></param>
        void CheckTreeSeedInput(Cell c)
        {
            if (!c.CellCollidesWith(player))
            {
                for (int i = trees.Children.Count - 1; i >= 0; i--)
                {
                    foreach (Item item in itemList.Children)
                    {
                        if (item is TreeSeed)
                        {
                            if (itemList.itemSelected == "TREESEED" && !c.cellIsTilled && !c.cellHasPlant && !c.HasCollision && item.itemAmount > 0)
                            {
                                GameEnvironment.AssetManager.PlayOnce(sounds.SEIs[12]);
                                item.itemAmount -= 1;
                                c.cellHasTree = true;
                                trees.Add(new Tree(c.Position, .5f, 1));
                                energyBar.energyLost += energyBar.oneUse;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks for input from the pickaxe, if detected shows the stone being hit by changing its sprite, if the stone has no more health left it gets removed, the player receives rocks and energy is reduced
        /// </summary>
        /// <param name="c"></param>
        void CheckPickaxeInput(Cell c)
        {
            for (int i = stones.Children.Count - 1; i >= 0; i--)
            {
                Stone s = stones.Children[i] as Stone;
                if (s.CollidesWith(MouseGO.HitBox) && s.CollidesWith(player.playerReach))
                {
                    if (itemList.itemSelected == "PICKAXE" && !(stones.Children[i] as Stone).stoneHit && (stones.Children[i] as Stone)._sprite == 1)
                    {
                        //play PickaxeSwing
                        GameEnvironment.AssetManager.PlaySound(sounds.SEIs[2]);

                        (stones.Children[i] as Stone).stoneHit = true;
                        (stones.Children[i] as Stone).hitTimer = (stones.Children[i] as Stone).hitTimerReset;
                        (stones.Children[i] as Stone).health -= 1;
                        energyBar.energyLost += energyBar.oneUse;
                        if ((stones.Children[i] as Stone).health <= 0)
                        {
                            if (tutorialStepList.step == 4)
                            {
                                tutorialStepList.step += 1;
                            }
                            if (c.Position == s.Position)
                            {
                                c.cellHasStone = false;
                            }
                            foreach (Item item in itemList.Children)
                            {
                                if (item is Rock)
                                {
                                    int randomAddition = GameEnvironment.Random.Next(2, 5);
                                    item.itemAmount += randomAddition;
                                    ConvertFromHotbarToMoney(item, randomAddition);
                                }
                            }
                            stones.Remove(stones.Children[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks for input from the axe, if detected shows the tree being hit by changing its sprite, if the tree has no more health left it gets removed, the player receives wood and energy is reduced
        /// </summary>
        /// <param name="c"></param>
        void CheckAxeInput(Cell c)
        {
            for (int i = trees.Children.Count - 1; i >= 0; i--)
            {
                if ((trees.Children[i] as Tree).CollidesWith(MouseGO.HitBox) && (trees.Children[i] as Tree).CollidesWith(player.playerReach))
                {
                    if (itemList.itemSelected == "AXE" && !(trees.Children[i] as Tree).treeHit && (trees.Children[i] as Tree).growthStage == 3)
                    {
                        GameEnvironment.AssetManager.PlaySound(sounds.SEIs[1]);
                        (trees.Children[i] as Tree).treeHit = true;
                        (trees.Children[i] as Tree).hitTimer = (trees.Children[i] as Tree).hitTimerReset;
                        (trees.Children[i] as Tree).health -= 1;
                        energyBar.energyLost += energyBar.oneUse;
                        if ((trees.Children[i] as Tree).health <= 0)
                        {
                            if (tutorialStepList.step == 4)
                            {
                                tutorialStepList.step += 1;
                            }
                            (trees.Children[i] as Tree).treeHit = false;
                            if (c.Position == (trees.Children[i] as Tree).Position)
                            {
                                c.cellHasTree = false;
                            }
                            trees.Remove(trees.Children[i]);

                            //play TreeFalling
                            GameEnvironment.AssetManager.PlaySound(sounds.SEIs[3]);
                            foreach (Item item in itemList.Children)
                            {
                                if (item is Wood)
                                {
                                    int randomAddition = GameEnvironment.Random.Next(3, 7);
                                    item.itemAmount += randomAddition;
                                    ConvertFromHotbarToMoney(item, randomAddition);
                                }
                                if (item is TreeSeed)
                                {
                                    int randomAddition = GameEnvironment.Random.Next(2);
                                    item.itemAmount += randomAddition;
                                    ConvertFromHotbarToMoney(item, randomAddition);
                                }
                            }
                        }
                    }
                    else if (itemList.itemSelected == "AXE" && !(trees.Children[i] as Tree).treeHit)
                    {
                        if (c.Position == (trees.Children[i] as Tree).Position)
                        {
                            c.cellHasTree = false;
                        }
                        trees.Remove(trees.Children[i]);

                        //play TreeFalling
                        GameEnvironment.AssetManager.PlaySound(sounds.SEIs[3]);
                    }
                }
            }
        }

        /// <summary>
        /// Checks for plant pickup, if detected adds wheat and new seeds to the players inventory, removes the plant and reduces energy
        /// </summary>
        /// <param name="c"></param>
        void CheckPlantPickup(Cell c)
        {
            if (c.cellHasPlant)
            {
                for (int i = plants.Children.Count - 1; i >= 0; i--)
                {
                    if (plants.Children[i].Position == c.Position)
                    {
                        if ((plants.Children[i] as Plant).growthStage >= 4)
                        {
                            if (tutorialStepList.step == 6)
                            {
                                tutorialStepList.step += 1;
                            }
                            foreach (Item item in itemList.Children)
                            {
                                if (item is Wheat)
                                {
                                    int randomAddition = GameEnvironment.Random.Next(1, 3);
                                    item.itemAmount += randomAddition;
                                    ConvertFromHotbarToMoney(item, randomAddition);
                                }
                                if (item is Seed)
                                {
                                    int randomAddition = GameEnvironment.Random.Next(1, 3);
                                    item.itemAmount += randomAddition;
                                    ConvertFromHotbarToMoney(item, randomAddition);
                                }
                            }
                            c.cellHasPlant = false;
                            plants.Remove(plants.Children[i]);
                            //Play WheatPickup
                            GameEnvironment.AssetManager.PlaySound(sounds.SEIs[10]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks for sprinkler pickup, if detected removes the sprinkler from the map, adds it to your inventory and reduces energy
        /// </summary>
        /// <param name="inputHelper"></param>
        void PickupSprinkler(InputHelper inputHelper)
        {
            for (int i = sprinklers.Children.Count - 1; i >= 0; i--)
            {
                SprinklerObject s = sprinklers.Children[i] as SprinklerObject;
                if (inputHelper.MouseRightButtonPressed() && s.CollidesWith(player.playerReach) && s.CollidesWith(MouseGO.HitBox))
                {
                    energyBar.energyLost += energyBar.oneUse;
                    //Play WaterSplash
                    GameEnvironment.AssetManager.PlaySound(sounds.SEIs[7]);
                    foreach (Cell c in cells.Children)
                    {
                        if (c.Position == s.Position)
                        {
                            c.cellHasSprinkler = false;
                        }
                        c.nextToSprinkler = false;
                    }
                    foreach (Item item in itemList.Children)
                    {
                        if (item is Sprinkler)
                        {
                            item.itemAmount += 1;
                        }
                    }
                    sprinklers.Remove(sprinklers.Children[i]);
                }
            }
        }

        /// <summary>
        /// Checks for hotbar selection via 1-0 or mouse selection
        /// </summary>
        /// <param name="inputHelper"></param>
        void CheckHotbarSelection(InputHelper inputHelper)
        {
            //keyboard input
            #region keyboard input
            if (inputHelper.KeyPressed(Keys.D1))
            {
                itemList.itemSelected = "HOE";
                hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[0].Position;
            }
            else if (inputHelper.KeyPressed(Keys.D2))
            {
                itemList.itemSelected = "AXE";
                hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[1].Position;
            }
            else if (inputHelper.KeyPressed(Keys.D3))
            {
                itemList.itemSelected = "PICKAXE";
                hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[2].Position;
            }
            else if (inputHelper.KeyPressed(Keys.D4))
            {
                itemList.itemSelected = "WATERINGCAN";
                hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[3].Position;
            }
            else if (inputHelper.KeyPressed(Keys.D5))
            {
                itemList.itemSelected = "SEED";
                hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[4].Position;
            }
            else if (inputHelper.KeyPressed(Keys.D6))
            {
                itemList.itemSelected = "TREESEED";
                hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[5].Position;
            }
            else if (inputHelper.KeyPressed(Keys.D7))
            {
                itemList.itemSelected = "SPRINKLER";
                hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[6].Position;
            }
            else if (inputHelper.KeyPressed(Keys.D8))
            {
                itemList.itemSelected = "WOOD";
                hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[7].Position;
            }
            else if (inputHelper.KeyPressed(Keys.D9))
            {
                itemList.itemSelected = "ROCK";
                hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[8].Position;
            }
            else if (inputHelper.KeyPressed(Keys.D0))
            {
                itemList.itemSelected = "WHEAT";
                hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[9].Position;
            }
            #endregion


            //mouse input
            #region mouse input
            if (inputHelper.MouseLeftButtonPressed())
            {
                if (MouseGO.CollidesWith(hotbar.hotbarSquares.Children[0] as SpriteGameObject))
                {
                    itemList.itemSelected = "HOE";
                    hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[0].Position;
                }
                else if (MouseGO.CollidesWith(hotbar.hotbarSquares.Children[1] as SpriteGameObject))
                {
                    itemList.itemSelected = "AXE";
                    hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[1].Position;
                }
                else if (MouseGO.CollidesWith(hotbar.hotbarSquares.Children[2] as SpriteGameObject))
                {
                    itemList.itemSelected = "PICKAXE";
                    hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[2].Position;
                }
                else if (MouseGO.CollidesWith(hotbar.hotbarSquares.Children[3] as SpriteGameObject))
                {
                    itemList.itemSelected = "WATERINGCAN";
                    hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[3].Position;
                }
                else if (MouseGO.CollidesWith(hotbar.hotbarSquares.Children[4] as SpriteGameObject))
                {
                    itemList.itemSelected = "SEED";
                    hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[4].Position;
                }
                else if (MouseGO.CollidesWith(hotbar.hotbarSquares.Children[5] as SpriteGameObject))
                {
                    itemList.itemSelected = "TREESEED";
                    hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[5].Position;
                }
                else if (MouseGO.CollidesWith(hotbar.hotbarSquares.Children[6] as SpriteGameObject))
                {
                    itemList.itemSelected = "SPRINKLER";
                    hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[6].Position;
                }
                else if (MouseGO.CollidesWith(hotbar.hotbarSquares.Children[7] as SpriteGameObject))
                {
                    itemList.itemSelected = "WOOD";
                    hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[7].Position;
                }
                else if (MouseGO.CollidesWith(hotbar.hotbarSquares.Children[8] as SpriteGameObject))
                {
                    itemList.itemSelected = "ROCK";
                    hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[8].Position;
                }
                else if (MouseGO.CollidesWith(hotbar.hotbarSquares.Children[9] as SpriteGameObject))
                {
                    itemList.itemSelected = "WHEAT";
                    hotbar.selectedSquare.Position = hotbar.hotbarSquares.Children[9].Position;
                }
            }
            #endregion
        }
    }
}
