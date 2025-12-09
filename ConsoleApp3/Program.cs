using System;
using System.Threading;

namespace TextRogueLike
{
    // Базовый класс персонажа (игрок или враг)
    abstract class Character
    {
        public string Name { get; protected set; }
        public int HP { get; protected set; }
        public int MaxHP { get; protected set; }
        public int AttackPower { get; protected set; }
        public int DefensePower { get; protected set; }

        protected static Random rand = new Random();

        protected Character(string name, int hp, int attack, int defense)
        {
            Name = name;
            MaxHP = hp;
            HP = hp;
            AttackPower = attack;
            DefensePower = defense;
        }

        public bool IsAlive => HP > 0;

        // Урон "через метод", чтобы не лезть напрямую в HP
        public void TakeDamage(int dmg)
        {
            HP -= dmg;
            if (HP < 0) HP = 0;
        }

        public abstract void Attack(Character target);

        public override string ToString()
        {
            return $"{Name} [HP: {HP}/{MaxHP}, Atk: {AttackPower}, Def: {DefensePower}]";
        }
    }

    // Игрок
    class Player : Character
    {
        public Weapon Weapon { get; private set; }
        public Armor Armor { get; private set; }
        public bool IsFrozen { get; set; }

        public Player(string name)
            : base(name, hp: 100, attack: 10, defense: 5)
        {
            Weapon = new Weapon("Ру́чные кулаки", 0);
            Armor = new Armor("Рваная одежда", 0);
            IsFrozen = false;
        }

        public override void Attack(Character target)
        {
            int atk = AttackPower + Weapon.Attack;
            int dmg = atk - target.DefensePower;
            if (dmg < 1) dmg = 1;
            Console.WriteLine($"{Name} атакует {target.Name} за {dmg} урона.");
            target.TakeDamage(dmg);
        }

        public void DrinkPotion()
        {
            HP = MaxHP;
            Console.WriteLine("Вы выпили лечебное зелье и полностью восстановили здоровье!");
        }

        public void Equip(Weapon w) => Weapon = w;
        public void Equip(Armor a) => Armor = a;

        // Метод защиты: шанс увернуться + блок
        public void DefendAndTakeHit(int incomingAttack)
        {
            if (rand.NextDouble() < 0.4)
            {
                Console.WriteLine("Вы полностью уклонились от удара!");
                return;
            }

            double factor = 0.7 + rand.NextDouble() * 0.3;
            int blockValue = (int)Math.Round((DefensePower + Armor.Defense) * factor);
            int dmg = incomingAttack - blockValue;
            if (dmg < 1) dmg = 1;

            TakeDamage(dmg);
            Console.WriteLine($"Вы не уклонились. Блок {blockValue} уменьшил урон до {dmg}. Ваше HP теперь {HP}/{MaxHP}.");
        }
    }

    // Враги
    abstract class Enemy : Character
    {
        protected Enemy(string name, int hp, int atk, int def)
            : base(name, hp, atk, def)
        { }
    }

    class Goblin : Enemy
    {
        protected double CritChance;

        public Goblin() : base("Гоблин", hp: 30, atk: 8, def: 2)
        {
            CritChance = 0.2;
        }

        public override void Attack(Character target)
        {
            int baseDmg = AttackPower;
            bool isCrit = rand.NextDouble() < CritChance;
            int dmg = isCrit ? baseDmg * 2 : baseDmg;

            Console.WriteLine(isCrit
                ? $"{Name} наносит КРИТИЧЕСКИЙ удар за {dmg}!"
                : $"{Name} атакует за {dmg}.");

            target.TakeDamage(dmg);
        }
    }

    class Skeleton : Enemy
    {
        public Skeleton() : base("Скелет", hp: 40, atk: 6, def: 3) { }

        public override void Attack(Character target)
        {
            int dmg = AttackPower;
            Console.WriteLine($"{Name} атакует и игнорирует вашу броню, урон {dmg}.");
            target.TakeDamage(dmg);
        }
    }

    class Mage : Enemy
    {
        protected double FreezeChance;

        public Mage() : base("Маг", hp: 25, atk: 5, def: 1)
        {
            FreezeChance = 0.2;
        }

        public override void Attack(Character target)
        {
            int dmg = AttackPower;
            Console.WriteLine($"{Name} атакует за {dmg}.");
            target.TakeDamage(dmg);

            if (rand.NextDouble() < FreezeChance && target is Player p)
            {
                p.IsFrozen = true;
                Console.WriteLine($"{Name} наложил заморозку! Вы пропустите следующий ход.");
            }
        }
    }

    // Боссы
    class BossVVG : Goblin
    {
        public BossVVG()
        {
            Name = "ВВГ (босс-гоблин)";
            MaxHP = (int)(MaxHP * 2.0);
            HP = MaxHP;
            AttackPower = (int)(AttackPower * 1.5);
            DefensePower = (int)(DefensePower * 1.2);
            CritChance += 0.10;
        }
    }

    class BossKovalskiy : Skeleton
    {
        public BossKovalskiy()
        {
            Name = "Ковальский (босс-скелет)";
            MaxHP = (int)(MaxHP * 2.5);
            HP = MaxHP;
            AttackPower = (int)(AttackPower * 1.3);
            DefensePower = (int)(DefensePower * 1.4);
        }
    }

    class BossArchMage : Mage
    {
        public BossArchMage()
        {
            Name = "Архимаг C++ (босс-маг)";
            MaxHP = (int)(MaxHP * 1.8);
            HP = MaxHP;
            AttackPower = (int)(AttackPower * 1.6);
            DefensePower = (int)(DefensePower * 1.1);
            FreezeChance += 0.10;
        }
    }

    class BossPestov : Skeleton
    {
        protected double FreezeChance;

        public BossPestov()
        {
            Name = "Пестов С-- (босс-скелет)";
            MaxHP = (int)(MaxHP * 1.3);
            HP = MaxHP;
            AttackPower = (int)(AttackPower * 1.8);
            DefensePower = (int)(DefensePower * 0.6);
            FreezeChance = 0.15;
        }

        public override void Attack(Character target)
        {
            int dmg = AttackPower;
            Console.WriteLine($"{Name} атакует, игнорирует броню за {dmg} урона.");
            target.TakeDamage(dmg);

            if (rand.NextDouble() < FreezeChance && target is Player p)
            {
                p.IsFrozen = true;
                Console.WriteLine($"{Name} наложил заморозку!");
            }
        }
    }

    // Предметы
    class Weapon
    {
        public string Name { get; }
        public int Attack { get; }
        public Weapon(string name, int atk)
        {
            Name = name;
            Attack = atk;
        }
        public override string ToString() => $"{Name} (+{Attack} к атаке)";
    }

    class Armor
    {
        public string Name { get; }
        public int Defense { get; }
        public Armor(string name, int def)
        {
            Name = name;
            Defense = def;
        }
        public override string ToString() => $"{Name} (+{Defense} к брони)";
    }

    // Основной класс-игра
    class Game
    {
        private Player player;
        private int turnCount = 0;
        private static Random rand = new Random();

        public Game()
        {
            Console.Write("Введите имя героя: ");
            string name = Console.ReadLine()!;   // ! чтобы подавить nullable‐предупреждение
            player = new Player(name);
        }

        public void Run()
        {
            Console.WriteLine($"\nДобро пожаловать, {player.Name}! Приготовьтесь к приключениям...\n");

            while (player.IsAlive)
            {
                turnCount++;
                Console.WriteLine($"\n--- Ход #{turnCount} ---");
                Thread.Sleep(200);

                if (turnCount % 10 == 0)
                {
                    Enemy boss = SpawnBoss();
                    Console.WriteLine($"!! ВНИМАНИЕ: появился БОСС: {boss.Name}! !!");
                    Fight(boss);
                }
                else
                {
                    if (rand.NextDouble() < 0.5)
                        OpenChest();
                    else
                    {
                        Enemy enemy = SpawnRandomEnemy();
                        Console.WriteLine($"Появился враг: {enemy.Name}");
                        Fight(enemy);
                    }
                }

                if (!player.IsAlive)
                    Console.WriteLine("Вы пали в бою. Игра окончена.");
            }
        }

        private Enemy SpawnRandomEnemy()
        {
            return rand.Next(3) switch
            {
                0 => new Goblin(),
                1 => new Skeleton(),
                2 => new Mage(),
                _ => new Goblin(),
            };
        }

        // Теперь возвращаем Enemy, а не Character
        private Enemy SpawnBoss()
        {
            return rand.Next(4) switch
            {
                0 => new BossVVG(),
                1 => new BossKovalskiy(),
                2 => new BossArchMage(),
                3 => new BossPestov(),
                _ => new BossVVG(),
            };
        }

        private void OpenChest()
        {
            Console.WriteLine("Вы нашли сундук!");
            Thread.Sleep(300);

            switch (rand.Next(3))
            {
                case 0:
                    Console.WriteLine("В сундуке – лечебное зелье!");
                    player.DrinkPotion();
                    break;
                case 1:
                    var w = new Weapon($"Меч+{rand.Next(1, 6)}", rand.Next(1, 6));
                    Console.WriteLine($"В сундуке – {w}. Ваша текущая экипировка: {player.Weapon}");
                    Console.Write("Взять новое оружие? (y/n): ");
                    if (Console.ReadLine()!.ToLower() == "y")
                    {
                        player.Equip(w);
                        Console.WriteLine("Вы экипировали " + w);
                    }
                    break;
                default:
                case 2:
                    var a = new Armor($"Доспех+{rand.Next(1, 6)}", rand.Next(1, 6));
                    Console.WriteLine($"В сундуке – {a}. Ваш текущий доспех: {player.Armor}");
                    Console.Write("Взять новый доспех? (y/n): ");
                    if (Console.ReadLine()!.ToLower() == "y")
                    {
                        player.Equip(a);
                        Console.WriteLine("Вы экипировали " + a);
                    }
                    break;
            }
        }

        private void Fight(Enemy enemy)
        {
            bool defending = false;

            while (player.IsAlive && enemy.IsAlive)
            {
                Console.WriteLine($"\n{player.Name}: HP {player.HP}/{player.MaxHP}  vs  {enemy.Name}: HP {enemy.HP}/{enemy.MaxHP}");

                // Ход игрока
                if (player.IsFrozen)
                {
                    Console.WriteLine("Вы заморожены и пропускаете ход!");
                    player.IsFrozen = false;
                    defending = false;
                }
                else
                {
                    Console.Write("Ваш ход! (1 – Атака, 2 – Защита): ");
                    string cmd = Console.ReadLine()!;
                    if (cmd == "2")
                    {
                        defending = true;
                        Console.WriteLine("Вы готовитесь защищаться...");
                    }
                    else
                    {
                        defending = false;
                        player.Attack(enemy);
                    }
                }

                if (!enemy.IsAlive)
                {
                    Console.WriteLine($"{enemy.Name} побеждён!");
                    break;
                }

                // Ход врага
                Console.WriteLine($"\nХод врага {enemy.Name}:");
                if (defending)
                    player.DefendAndTakeHit(enemy.AttackPower);
                else
                    enemy.Attack(player);

                defending = false;

                if (!player.IsAlive)
                {
                    Console.WriteLine("Вы пали в бою.");
                    break;
                }
            }
        }
    }

    class Program
    {
        static void Main()
        {
            Console.Title = "Text Rogue-like";
            var game = new Game();
            game.Run();
            Console.WriteLine("Нажмите любую клавишу для выхода.");
            Console.ReadKey();
        }
    }
}