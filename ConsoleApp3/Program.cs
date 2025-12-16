using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace TextRogueLike
{
    // «Чёрный кот» — специальный артефакт
    class BlackCat
    {
        public override string ToString() => "Чёрный кот";
    }

    // «Святая книга» — специальный артефакт
    class HolyBook
    {
        public override string ToString() => "Святая книга";
    }

    // Зелье исцеления
    class Potion
    {
        public string Name { get; }
        public int HealAmount { get; }
        public Potion(string name, int heal)
        {
            Name = name;
            HealAmount = heal;
        }
        public override string ToString() => $"{Name} (+{HealAmount} HP)";
    }

    // Базовый персонаж
    abstract class Character
    {
        public string Name { get; protected set; }
        public int HP { get; protected set; }
        public int MaxHP { get; protected set; }
        public int AttackPower { get; protected set; }
        protected double ParryChance { get; set; }
        protected static Random rand = new Random();

        protected Character(string name, int hp, int atk, double parryChance)
        {
            Name = name;
            MaxHP = hp;
            HP = hp;
            AttackPower = atk;
            ParryChance = parryChance;
        }

        public bool IsAlive => HP > 0;

        protected void TakeDamage(int dmg)
        {
            HP -= dmg;
            if (HP < 0) HP = 0;
        }

        public abstract void ReceiveHit(int dmg);
        public abstract void Attack(Character target);
    }

    // Оружие: даёт бонус к атаке
    class Weapon
    {
        public string Name { get; }
        public int AttackBonus { get; }
        public Weapon(string name, int atkBonus)
        {
            Name = name;
            AttackBonus = atkBonus;
        }
        public override string ToString() => $"{Name} (+{AttackBonus} к атаке)";
    }

    // Броня: даёт бонус к максимуму HP
    class Armor
    {
        public string Name { get; }
        public int HPBonus { get; }
        public Armor(string name, int hpBonus)
        {
            Name = name;
            HPBonus = hpBonus;
        }
        public override string ToString() => $"{Name} (+{HPBonus} к Max HP)";
    }

    // Игрок
    class Player : Character
    {
        public Weapon Weapon { get; private set; }
        public Armor Armor { get; private set; }

        public bool IsFrozen { get; set; }
        public bool IsBleeding { get; set; }
        public bool IsPoisoned { get; set; }
        public bool IsBurning { get; set; }

        private List<object> inventory = new List<object>();
        private readonly double CritChance = 0.20;

        public int PotionCount => inventory.FindAll(x => x is Potion).Count;
        public bool HasBlackCat => inventory.Exists(x => x is BlackCat);
        public int HolyBookCount => inventory.FindAll(x => x is HolyBook).Count;
        public bool HasHolyBook => HolyBookCount > 0;

        public Player(string name)
            : base(name, hp: 100, atk: 10, parryChance: 0.15)
        {
            Weapon = new Weapon("Ручные кулаки", 0);
            Armor = new Armor("Рваная одежда", 0);
        }

        public override void ReceiveHit(int dmg)
        {
            if (IsFrozen)
            {
                TakeDamage(dmg);
                Console.WriteLine($"{Name} получает {dmg} урона (HP: {HP}/{MaxHP}).");
                return;
            }
            if (rand.NextDouble() < ParryChance)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{Name} парировал(а) удар!");
                Console.ResetColor();
                return;
            }
            TakeDamage(dmg);
            Console.WriteLine($"{Name} получает {dmg} урона (HP: {HP}/{MaxHP}).");
        }

        public override void Attack(Character target)
        {
            int totalAtk = AttackPower + Weapon.AttackBonus;
            int baseDmg = Math.Max(1, totalAtk - (target is Character c ? c.DefensePower() : 0));
            bool isCrit = rand.NextDouble() < CritChance;
            int dmg = isCrit ? baseDmg * 2 : baseDmg;

            if (isCrit)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{Name} наносит КРИТИЧЕСКИЙ удар за {dmg} урона!");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine($"{Name} атакует {target.Name} за {dmg} урона.");
            }
            target.ReceiveHit(dmg);
        }

        // Вот здесь добавлен недостающий метод:
        public void DefendAndTakeHit(int incomingDamage)
        {
            // уменьшаем урон на бонус брони
            int def = Armor.HPBonus;
            int reduced = Math.Max(1, incomingDamage - def);
            Console.WriteLine($"{Name} защищается и снижает урон с {incomingDamage} до {reduced}.");
            // напрямую снимаем здоровье (без парирования)
            TakeDamage(reduced);
            Console.WriteLine($"{Name} получает {reduced} урона (HP: {HP}/{MaxHP}).");
        }

        private static BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

        public void Equip(Armor newArmor)
        {
            MaxHP -= Armor.HPBonus;
            HP = Math.Min(HP, MaxHP);

            Armor = newArmor;
            MaxHP += newArmor.HPBonus;
            HP += newArmor.HPBonus;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Экипировано {newArmor}. HP: {HP}/{MaxHP}");
            Console.ResetColor();
        }

        public void Equip(Weapon newWeapon)
        {
            Weapon = newWeapon;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Экипировано {newWeapon}.");
            Console.ResetColor();
        }

        public void AddPotion(Potion p)
        {
            inventory.Add(p);
            Console.WriteLine($"Вы нашли зелье: {p}. Всего зелий: {PotionCount}");
        }

        public void AddBlackCat(BlackCat c)
        {
            inventory.Add(c);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Вы нашли Чёрного кота!");
            Console.ResetColor();
        }

        public void AddHolyBook(HolyBook b)
        {
            inventory.Add(b);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Вы нашли Святую книгу!");
            Console.ResetColor();
        }

        public void UsePotion()
        {
            int idx = inventory.FindIndex(x => x is Potion);
            if (idx < 0)
            {
                Console.WriteLine("У вас нет зелий!");
                return;
            }
            var p = (Potion)inventory[idx];
            inventory.RemoveAt(idx);
            HP = Math.Min(MaxHP, HP + p.HealAmount);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Вы использовали {p}. HP теперь {HP}/{MaxHP}. Осталось зелий: {PotionCount}");
            Console.ResetColor();
        }

        public void RemovePotion()
        {
            int idx = inventory.FindIndex(x => x is Potion);
            if (idx >= 0) inventory.RemoveAt(idx);
        }

        public bool UseBlackCat()
        {
            int idx = inventory.FindIndex(x => x is BlackCat);
            if (idx < 0) return false;
            inventory.RemoveAt(idx);
            Console.WriteLine("Чёрный кот прогнал врага!");
            return true;
        }

        public bool UseHolyBook(Enemy enemy)
        {
            int idx = inventory.FindIndex(x => x is HolyBook);
            if (idx < 0)
            {
                Console.WriteLine("У вас нет Святой книги!");
                return false;
            }
            inventory.RemoveAt(idx);
            typeof(Character)
              .GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.NonPublic)!
              .Invoke(enemy, new object[] { 30 });
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Святая книга наносит 30 ед. урона (игнорирует парирование).");
            Console.WriteLine($"{enemy.Name} HP: {enemy.HP}/{enemy.MaxHP}");
            Console.ResetColor();
            return true;
        }

        public void ApplyEffectDamage(int dmg, string effectName)
        {
            HP = Math.Max(0, HP - dmg);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Вы получаете {dmg} урона от {effectName}. HP: {HP}/{MaxHP}");
            Console.ResetColor();
        }

        public void RestoreHP(int amount)
        {
            HP = Math.Min(MaxHP, HP + amount);
        }
    }

    // Базовый враг
    abstract class Enemy : Character
    {
        protected readonly double CritChance = 0.15;

        protected Enemy(string name, int hp, int atk, double parryChance = 0.10)
            : base(name, hp, atk, parryChance) { }

        public override void ReceiveHit(int dmg)
        {
            if (rand.NextDouble() < ParryChance)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{Name} парировал(а) удар!");
                Console.ResetColor();
                return;
            }
            TakeDamage(dmg);
            Console.WriteLine($"{Name} получает {dmg} урона (HP: {HP}/{MaxHP}).");
        }
    }

    class Goblin : Enemy
    {
        private readonly double StealChance = 0.12;
        public Goblin() : base("Гоблин", hp: 30, atk: 8) { }

        public override void Attack(Character target)
        {
            if (target is Player p && p.PotionCount > 0 && rand.NextDouble() < StealChance)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Гоблин использует «Кража» и ворует у вас зелье!");
                Console.ResetColor();
                p.RemovePotion();
                Console.WriteLine($"У вас осталось {p.PotionCount} зелий.");
                return;
            }
            bool isCrit = rand.NextDouble() < CritChance;
            int dmg = isCrit ? AttackPower * 2 : AttackPower;
            if (isCrit)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{Name} наносит КРИТИЧЕСКИЙ удар за {dmg} урона!");
                Console.ResetColor();
            }
            else
                Console.WriteLine($"{Name} атакует за {dmg} урона.");
            target.ReceiveHit(dmg);
        }
    }

    class Skeleton : Enemy
    {
        public Skeleton() : base("Скелет", hp: 40, atk: 6) { }

        public override void Attack(Character target)
        {
            bool isCrit = rand.NextDouble() < CritChance;
            int dmg = isCrit ? AttackPower * 2 : AttackPower;
            if (isCrit)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{Name} наносит КРИТИЧЕСКИЙ удар за {dmg} урона!");
                Console.ResetColor();
            }
            else
                Console.WriteLine($"{Name} атакует за {dmg} урона.");
            target.ReceiveHit(dmg);
        }
    }

    class Mage : Enemy
    {
        private bool shielded = false;
        protected new readonly double CritChance = 0.15;
        protected readonly double AbilityChance = 0.40;

        public Mage() : base("Маг", hp: 25, atk: 5) { }

        public override void Attack(Character target)
        {
            if (!shielded && rand.NextDouble() < AbilityChance)
            {
                shielded = true;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{Name} поднимает магический щит (следующий урон уменьшится на 5)!");
                Console.ResetColor();
            }

            bool isCrit = rand.NextDouble() < CritChance;
            int dmg = isCrit ? AttackPower * 2 : AttackPower;
            if (isCrit)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{Name} наносит КРИТИЧЕСКИЙ удар за {dmg} урона!");
                Console.ResetColor();
            }
            else
                Console.WriteLine($"{Name} атакует за {dmg} урона.");
            target.ReceiveHit(dmg);

            if (rand.NextDouble() < AbilityChance && target is Player p)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{Name} использует «Заморозка»! Вы пропустите следующий ход.");
                Console.ResetColor();
                p.IsFrozen = true;
            }
        }

        public override void ReceiveHit(int dmg)
        {
            if (shielded)
            {
                int reduced = Math.Max(0, dmg - 5);
                Console.WriteLine($"{Name} щит снижает урон с {dmg} до {reduced}.");
                shielded = false;
                typeof(Character)
                  .GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.NonPublic)!
                  .Invoke(this, new object[] { reduced });
                Console.WriteLine($"{Name} HP: {HP}/{MaxHP}");
            }
            else
                base.ReceiveHit(dmg);
        }
    }

    // Боссы
    class BossVVG : Goblin
    {
        public BossVVG()
        {
            Name = "Гоблин-Король";
            MaxHP = HP = 80;
            AttackPower = (int)(AttackPower * 1.5);
            ParryChance = 0.15;
        }
        public override void Attack(Character target)
        {
            base.Attack(target);
            if (target is Player p && !p.IsBleeding && rand.NextDouble() < 0.10)
            {
                p.IsBleeding = true;
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"{Name} накладывает «Кровотечение»: вы теряете 2 ед. каждый ход!");
                Console.ResetColor();
            }
        }
    }

    class BossKovalskiy : Skeleton
    {
        public BossKovalskiy()
        {
            Name = "Король-Лич";
            MaxHP = HP = 100;
            AttackPower = (int)(AttackPower * 1.3);
            ParryChance = 0.15;
        }
        public override void Attack(Character target)
        {
            base.Attack(target);
            if (target is Player p && !p.IsPoisoned && rand.NextDouble() < 0.10)
            {
                p.IsPoisoned = true;
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"{Name} применяет «Яд»: вы теряете 3 ед. каждый ход!");
                Console.ResetColor();
            }
        }
    }

    class BossArchMage : Mage
    {
        public BossArchMage()
        {
            Name = "Призрак героя";
            MaxHP = HP = 70;
            AttackPower = (int)(AttackPower * 1.6);
            ParryChance = 0.15;
        }
    }

    class BossPestov : Skeleton
    {
        public BossPestov()
        {
            Name = "Высший демон маг";
            MaxHP = HP = 110;
            AttackPower = (int)(AttackPower * 1.8);
            ParryChance = 0.15;
        }
        public override void Attack(Character target)
        {
            base.Attack(target);
            if (target is Player p && !p.IsBurning && rand.NextDouble() < 0.10)
            {
                p.IsBurning = true;
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"{Name} создаёт «Горящая земля»: вы теряете 3 ед. огн. урона каждый ход!");
                Console.ResetColor();
            }
        }
    }

    // Супербосс с двумя фазами: обычная и "Безумие"
    class FinalBossTokisaki : Enemy
    {
        private readonly double regenChance = 0.20;       // шанс Alef
        private readonly double doubleHitChance = 0.20;   // шанс двойного/крит удара
        private readonly double passiveHitChance = 0.20;  // шанс пасс. урона

        // Лимит на полное восстановление
        private int regenUsesLeft = 2;
        // Флаг второй фазы
        private bool isMadness = false;

        public FinalBossTokisaki()
            : base("Tokisaki – Супербосс", hp: 200, atk: 10)
        {
            ParryChance = 0.15;
        }

        public override void Attack(Character target)
        {
            // Переход во вторую фазу "Безумие"
            if (!isMadness && HP <= 100)
            {
                isMadness = true;
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"{Name} впадает в БЕЗУМИЕ! Урон увеличивается до 25/50.");
                Console.ResetColor();
            }

            // Полное восстановление HP (Alef) — не более 2 раз
            if (regenUsesLeft > 0 && rand.NextDouble() < regenChance)
            {
                regenUsesLeft--;
                HP = MaxHP;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{Name} использует «Alef» и полностью восстанавливает HP! (Осталось: {regenUsesLeft})");
                Console.ResetColor();
                return;
            }

            // Рассчитываем урон
            bool doubleHit = rand.NextDouble() < doubleHitChance;
            int dmg;
            if (isMadness)
            {
                // фаза Безумие: обычный урон 25, крит 50
                dmg = doubleHit ? 50 : 25;
            }
            else
            {
                // первая фаза: классический Zafkiel
                dmg = doubleHit ? AttackPower * 2 : AttackPower;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(doubleHit
                ? $"{Name} активирует «Zafkiel» и наносит {dmg} урона!"
                : $"{Name} наносит мощный удар за {dmg} урона.");
            Console.ResetColor();
            target.ReceiveHit(dmg);

            // Пассивный урон Drugie ia
            if (rand.NextDouble() < passiveHitChance)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{Name} использует «Drugie ia»: дополнительный 6 HP урона.");
                Console.ResetColor();
                target.ReceiveHit(6);
            }
        }

        public override void ReceiveHit(int dmg)
        {
            if (rand.NextDouble() < ParryChance)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{Name} парировал(а) удар!");
                Console.ResetColor();
                return;
            }
            typeof(Character)
              .GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.NonPublic)!
              .Invoke(this, new object[] { dmg });
            Console.WriteLine($"{Name} получает {dmg} урона (HP: {HP}/{MaxHP}).");
        }
    }

    // Логика игры
    class Game
    {
        private readonly Player player;
        private int turnCount = 0;
        private int bossesDefeated = 0;
        private bool finalBossSpawned = false;
        private int fightsCount = 0;
        private static readonly Random rand = new Random();

        private readonly List<Type> remainingBosses = new List<Type>
    {
        typeof(BossVVG),
        typeof(BossKovalskiy),
        typeof(BossArchMage),
        typeof(BossPestov)
    };

        private readonly int[] armorBonuses = new[] { 5, 10, 15, 20 };
        private readonly int[] weaponBonuses = new[] { 1, 3, 7, 10 };

        public Game()
        {
            Console.Write("Введите имя героя: ");
            string name = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(name)) name = "Герой";
            player = new Player(name);
        }

        public void Run()
        {
            Console.Title = "Text Rogue-like";
            Console.WriteLine($"\nДобро пожаловать, {player.Name}!\n");

            while (player.IsAlive)
            {
                turnCount++;
                Console.WriteLine($"\n--- Ход #{turnCount} ---");
                Thread.Sleep(200);

                if (rand.NextDouble() < 0.50)
                    OpenChest();
                else
                    DoBattleOrBoss();

                if (!player.IsAlive)
                    Console.WriteLine("\nВы пали. Игра окончена.");
            }

            Console.WriteLine("\nСпасибо за игру!");
        }

        private void DoBattleOrBoss()
        {
            if (bossesDefeated >= 3 && !finalBossSpawned)
            {
                finalBossSpawned = true;
                var fb = new FinalBossTokisaki();
                Console.WriteLine($"\n!!! ФИНАЛЬНЫЙ БОСС: {fb.Name} !!!");
                Fight(fb);
            }
            else if (turnCount % 10 == 0 && remainingBosses.Count > 0)
            {
                int idx = rand.Next(remainingBosses.Count);
                var boss = (Enemy)Activator.CreateInstance(remainingBosses[idx])!;
                remainingBosses.RemoveAt(idx);
                Console.WriteLine($"\n!! БОСС: {boss.Name} !!");
                Fight(boss);
            }
            else
            {
                var enemy = SpawnRandomEnemy();
                Console.WriteLine($"\nПоявился враг: {enemy.Name}");
                Fight(enemy);
            }
        }

        private Enemy SpawnRandomEnemy()
        {
            double p = rand.NextDouble();
            if (p < 0.5) return new Goblin();
            if (p < 0.8) return new Skeleton();
            return new Mage();
        }

        private void OpenChest()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\nВы нашли сундук!");
            Console.ResetColor();
            Thread.Sleep(200);

            double c = rand.NextDouble();
            if (c < 0.13)
            {
                Console.WriteLine("В сундуке два предмета!");
                GiveSingleItemFromChest();
                GiveSingleItemFromChest();
            }
            else if (c < 0.23)
            {
                if (!player.HasBlackCat) player.AddBlackCat(new BlackCat());
                else GiveSingleItemFromChest();
            }
            else if (c < 0.43)
            {
                if (player.HolyBookCount < 2) player.AddHolyBook(new HolyBook());
                else GiveSingleItemFromChest();
            }
            else
            {
                GiveSingleItemFromChest();
            }
        }

        private void GiveSingleItemFromChest()
        {
            double p = rand.NextDouble();
            if (p < 0.10)
            {
                if (!player.HasBlackCat) player.AddBlackCat(new BlackCat());
                else player.AddPotion(new Potion("Зелье исцеления", player.MaxHP));
            }
            else if (p < 0.30)
            {
                if (player.HolyBookCount < 2) player.AddHolyBook(new HolyBook());
                else player.AddPotion(new Potion("Зелье исцеления", player.MaxHP));
            }
            else if (p < 0.50)
                player.AddPotion(new Potion("Зелье исцеления", player.MaxHP));
            else if (p < 0.75)
            {
                int bonus = armorBonuses[rand.Next(armorBonuses.Length)];
                var armor = new Armor($"Броня {bonus}", bonus);
                Console.WriteLine($"Доп. предмет: {armor}");
                Console.Write("Экипировать? (y/n): ");
                if (Console.ReadLine()?.Trim().ToLower() == "y")
                    player.Equip(armor);
            }
            else
            {
                int bonus = weaponBonuses[rand.Next(weaponBonuses.Length)];
                string[] types = { "Лук", "Посох", "Копьё", "Кинжал", "Дубина" };
                var weapon = new Weapon($"{types[rand.Next(types.Length)]} {bonus}", bonus);
                Console.WriteLine($"Доп. предмет: {weapon}");
                Console.Write("Экипировать? (y/n): ");
                if (Console.ReadLine()?.Trim().ToLower() == "y")
                    player.Equip(weapon);
            }
        }

        private void Fight(Enemy enemy)
        {
            player.IsBleeding = player.IsPoisoned = player.IsBurning = false;
            bool defending = false;

            while (player.IsAlive && enemy.IsAlive)
            {
                if (player.IsBleeding) player.ApplyEffectDamage(2, "кровотечение");
                if (player.IsPoisoned) player.ApplyEffectDamage(3, "яда");
                if (player.IsBurning) player.ApplyEffectDamage(3, "горящей земли");
                if (!player.IsAlive) break;

                Console.WriteLine($"\n{player.Name}: {player.HP}/{player.MaxHP} HP  vs  {enemy.Name}: {enemy.HP}/{enemy.MaxHP} HP");
                Console.WriteLine($"Зелий: {player.PotionCount}  |  Кот: {(player.HasBlackCat ? 1 : 0)}  |  Книг: {player.HolyBookCount}");
                bool skipPlayer = player.IsFrozen;
                if (skipPlayer)
                    Console.WriteLine("Вы заморожены и пропускаете ход!");

                if (!skipPlayer)
                {
                    string opts = "1–Атаковать, 2–Защищаться"
                                  + (player.PotionCount > 0 ? ", 3–Зелье" : "")
                                  + (player.HasBlackCat ? ", 4–Кот" : "")
                                  + (player.HasHolyBook ? ", 5–Книга" : "");
                    Console.Write($"Ваш ход ({opts}): ");
                    var cmd = Console.ReadLine()?.Trim();

                    if (cmd == "2")
                    {
                        defending = true;
                        Console.WriteLine("Вы готовитесь защищаться...");
                    }
                    else if (cmd == "3" && player.PotionCount > 0)
                    {
                        defending = false;
                        player.UsePotion();
                    }
                    else if (cmd == "4" && player.HasBlackCat)
                    {
                        defending = false;
                        if (player.UseBlackCat()) return;
                    }
                    else if (cmd == "5" && player.HasHolyBook)
                    {
                        defending = false;
                        player.UseHolyBook(enemy);
                    }
                    else
                    {
                        defending = false;
                        player.Attack(enemy);
                    }
                }

                if (player.IsFrozen)
                    player.IsFrozen = false;

                if (!enemy.IsAlive)
                {
                    Console.WriteLine($"\n{enemy.Name} побеждён!");
                    if (enemy is BossVVG || enemy is BossKovalskiy || enemy is BossArchMage || enemy is BossPestov)
                        bossesDefeated++;
                    break;
                }

                Console.WriteLine($"\nХод врага {enemy.Name}:");
                if (defending)
                    player.DefendAndTakeHit(enemy.AttackPower);
                else
                    enemy.Attack(player);

                if (!player.IsAlive)
                {
                    Console.WriteLine("\nВы пали в бою.");
                    break;
                }

                defending = false;
            }

            fightsCount++;
            if (player.IsAlive && fightsCount % 3 == 0 && rand.NextDouble() < 0.6)
            {
                player.RestoreHP(60);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\nПосле боя вы восстановили 60 HP (текущее {player.HP}/{player.MaxHP}).");
                Console.ResetColor();
            }

            player.IsBleeding = player.IsPoisoned = player.IsBurning = false;
        }
    }

    // Точка входа
    class Program
    {
        static void Main()
        {
            var game = new Game();
            game.Run();
            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }

    // Расширение для защиты при рассчёте урона (если нужно)
    static class CharacterExtensions
    {
        public static int DefensePower(this Character c)
        {
            var armorField = c.GetType().GetProperty("Armor",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (armorField != null)
            {
                var armor = armorField.GetValue(c) as Armor;
                return armor?.HPBonus ?? 0;
            }
            return 0;
        }
    }
}