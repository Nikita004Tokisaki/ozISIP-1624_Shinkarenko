using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LibraryApp
{
    // Жанры книг
    enum Genre
    {
        Fiction = 1,
        Detective,
        Science,
        Fantasy,
        Biography
    }

    // Модель книги
    class Book
    {
        private static int _nextId = 1;

        public int Id { get; }
        public string Title { get; }
        public string Author { get; }
        public Genre Genre { get; }
        public int Year { get; }
        public decimal Price { get; }

        public Book(string title, string author, Genre genre, int year, decimal price)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Название не может быть пустым.");
            if (string.IsNullOrWhiteSpace(author))
                throw new ArgumentException("Автор не может быть пустым.");
            if (!Enum.IsDefined(typeof(Genre), genre))
                throw new ArgumentException("Неверный жанр.");
            if (year <= 0)
                throw new ArgumentException("Год издания должен быть положительным.");
            if (price < 0)
                throw new ArgumentException("Цена не может быть отрицательной.");

            Id = _nextId++;
            Title = title;
            Author = author;
            Genre = genre;
            Year = year;
            Price = price;
        }

        public override string ToString()
        {
            return $"[{Id}] \"{Title}\", автор: {Author}, жанр: {Genre}, год: {Year}, цена: {Price:C}";
        }
    }

    // Сервис хранения и поиска книг
    class Library
    {
        private readonly List<Book> _books = new List<Book>();

        public Library()
        {
            // Пять тестовых записей
            _books.Add(new Book("Война и мир", "Л. Толстой", Genre.Fiction, 1869, 500m));
            _books.Add(new Book("Десять негритят", "А. Кристи", Genre.Detective, 1939, 300m));
            _books.Add(new Book("Краткая история времени", "С. Хокинг", Genre.Science, 1988, 450m));
            _books.Add(new Book("Гарри Поттер", "Дж. Роулинг", Genre.Fantasy, 1997, 600m));
            _books.Add(new Book("Автобиография", "Бенджамин Франклин", Genre.Biography, 1791, 250m));
        }

        public IReadOnlyList<Book> Books => _books;

        public void Add(Book book) => _books.Add(book);
        public bool Remove(int id) => _books.RemoveAll(b => b.Id == id) > 0;

        public IEnumerable<Book> FindByTitle(string part) =>
            _books.Where(b => b.Title.IndexOf(part ?? "", StringComparison.OrdinalIgnoreCase) >= 0);

        public IEnumerable<Book> FindByAuthor(string part) =>
            _books.Where(b => b.Author.IndexOf(part ?? "", StringComparison.OrdinalIgnoreCase) >= 0);

        public IEnumerable<Book> FindByGenre(Genre genre) =>
            _books.Where(b => b.Genre == genre);

        public IEnumerable<Book> SortByTitle() => _books.OrderBy(b => b.Title);
        public IEnumerable<Book> SortByYear() => _books.OrderBy(b => b.Year);

        public Book GetMostExpensive() => _books.OrderByDescending(b => b.Price).FirstOrDefault();
        public Book GetCheapest() => _books.OrderBy(b => b.Price).FirstOrDefault();

        public IDictionary<string, int> GroupByAuthor() =>
            _books.GroupBy(b => b.Author)
                  .ToDictionary(g => g.Key, g => g.Count());
    }

    // Точка входа и меню
    class Program
    {
        static void Main()
        {
            var library = new Library();

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("=== Меню библиотеки ===");
                Console.WriteLine("1 – Добавить книгу");
                Console.WriteLine("2 – Удалить книгу по Id");
                Console.WriteLine("3 – Найти книги");
                Console.WriteLine("4 – Отсортировать книги");
                Console.WriteLine("5 – Самая дорогая и самая дешевая");
                Console.WriteLine("6 – Группировать по авторам");
                Console.WriteLine("0 – Выход");
                Console.Write("Выберите пункт: ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1": AddBook(library); break;
                    case "2": RemoveBook(library); break;
                    case "3": FindBooks(library); break;
                    case "4": SortBooks(library); break;
                    case "5": ShowPriceExtremes(library); break;
                    case "6": ShowGroupByAuthor(library); break;
                    case "0": return;
                    default: Console.WriteLine("Неверный выбор."); break;
                }
            }
        }

        static void AddBook(Library lib)
        {
            try
            {
                Console.Write("Название: ");
                var title = Console.ReadLine();

                Console.Write("Автор: ");
                var author = Console.ReadLine();

                Console.WriteLine("Жанры:");
                foreach (var g in Enum.GetValues(typeof(Genre)))
                    Console.WriteLine($"  {(int)g} – {g}");
                Console.Write("Номер жанра: ");
                if (!int.TryParse(Console.ReadLine(), out int gi) ||
                    !Enum.IsDefined(typeof(Genre), gi))
                    throw new ArgumentException("Неверный жанр.");
                var genre = (Genre)gi;

                Console.Write("Год издания: ");
                if (!int.TryParse(Console.ReadLine(), out int year) || year <= 0)
                    throw new ArgumentException("Неверный год.");

                Console.Write("Цена: ");
                if (!decimal.TryParse(Console.ReadLine(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price) ||
                    price < 0)
                    throw new ArgumentException("Неверная цена.");

                var book = new Book(title, author, genre, year, price);
                lib.Add(book);
                Console.WriteLine("Книга добавлена: " + book);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }
        }

        static void RemoveBook(Library lib)
        {
            Console.Write("Id для удаления: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                if (lib.Remove(id))
                    Console.WriteLine("Книга удалена.");
                else
                    Console.WriteLine("Книга с таким Id не найдена.");
            }
            else
            {
                Console.WriteLine("Неверный формат Id.");
            }
        }

        static void FindBooks(Library lib)
        {
            Console.WriteLine("1–по названию, 2–по автору, 3–по жанру");
            switch (Console.ReadLine())
            {
                case "1":
                    Console.Write("Часть названия: ");
                    foreach (var b in lib.FindByTitle(Console.ReadLine()))
                        Console.WriteLine(b);
                    break;
                case "2":
                    Console.Write("Часть автора: ");
                    foreach (var b in lib.FindByAuthor(Console.ReadLine()))
                        Console.WriteLine(b);
                    break;
                case "3":
                    Console.WriteLine("Выберите жанр:");
                    foreach (var g in Enum.GetValues(typeof(Genre)))
                        Console.WriteLine($"  {(int)g} – {g}");
                    if (int.TryParse(Console.ReadLine(), out int gi) && Enum.IsDefined(typeof(Genre), gi))
                    {
                        foreach (var b in lib.FindByGenre((Genre)gi))
                            Console.WriteLine(b);
                    }
                    else
                        Console.WriteLine("Неверный жанр.");
                    break;
                default:
                    Console.WriteLine("Неверная опция.");
                    break;
            }
        }

        static void SortBooks(Library lib)
        {
            Console.WriteLine("1–по названию, 2–по году");
            switch (Console.ReadLine())
            {
                case "1":
                    foreach (var b in lib.SortByTitle())
                        Console.WriteLine(b);
                    break;
                case "2":
                    foreach (var b in lib.SortByYear())
                        Console.WriteLine(b);
                    break;
                default:
                    Console.WriteLine("Неверная опция.");
                    break;
            }
        }

        static void ShowPriceExtremes(Library lib)
        {
            var max = lib.GetMostExpensive();
            var min = lib.GetCheapest();
            Console.WriteLine("Самая дорогая:  " + (max != null ? max.ToString() : "(нет)"));
            Console.WriteLine("Самая дешевая: " + (min != null ? min.ToString() : "(нет)"));
        }

        static void ShowGroupByAuthor(Library lib)
        {
            var groups = lib.GroupByAuthor();
            Console.WriteLine("Книг на автора:");
            foreach (var kv in groups)
                Console.WriteLine($"  {kv.Key}: {kv.Value}");
        }
    }
}