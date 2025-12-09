using System;
using System.Collections.Generic;
using System.Text;

namespace TextAnalysisApp
{
    // Хранит статистику по одному тексту
    class TextStatistics
    {
        public string Text { get; set; }
        public int WordCount { get; set; }
        public string ShortestWord { get; set; }
        public string LongestWord { get; set; }
        public int SentenceCount { get; set; }
        public int VowelCount { get; set; }
        public int ConsonantCount { get; set; }
        public Dictionary<char, int> LetterFrequency { get; set; }

        // Вывод статистики в консоль
        public void Print()
        {
            Console.WriteLine("----- Статистика текста -----");
            Console.WriteLine("Исходный текст:");
            Console.WriteLine(Text);
            Console.WriteLine("Количество слов (без союзов и чисел): {0}", WordCount);
            Console.WriteLine("Самое короткое слово: {0}", ShortestWord ?? "(нет)");
            Console.WriteLine("Самое длинное слово: {0}", LongestWord ?? "(нет)");
            Console.WriteLine("Количество предложений: {0}", SentenceCount);
            Console.WriteLine("Гласных букв: {0}", VowelCount);
            Console.WriteLine("Согласных букв: {0}", ConsonantCount);
            Console.WriteLine("Частота встречаемости букв:");
            List<char> keys = new List<char>(LetterFrequency.Keys);
            keys.Sort();
            foreach (char c in keys)
                Console.WriteLine("  {0} : {1}", c, LetterFrequency[c]);
            Console.WriteLine("-----------------------------");
        }

        // Основной метод расчёта статистики по тексту
        public static TextStatistics Compute(string text, string[] conjunctions)
        {
            TextStatistics stat = new TextStatistics
            {
                Text = text,
                LetterFrequency = new Dictionary<char, int>()
            };

            // 1. Разбиение на слова
            List<string> allWords = new List<string>();
            StringBuilder current = new StringBuilder();
            foreach (char ch in text)
            {
                if (char.IsLetter(ch) || char.IsDigit(ch))
                {
                    current.Append(ch);
                }
                else if (current.Length > 0)
                {
                    allWords.Add(current.ToString());
                    current.Clear();
                }
            }
            if (current.Length > 0)
                allWords.Add(current.ToString());

            // 2. Фильтрация: убираем союзы и числа
            List<string> filteredWords = new List<string>();
            foreach (string w in allWords)
            {
                string lw = w.ToLower();
                bool isNumber = true;
                foreach (char c in lw)
                    if (!char.IsDigit(c))
                    {
                        isNumber = false;
                        break;
                    }
                if (isNumber) continue;

                bool isConj = false;
                foreach (string conj in conjunctions)
                    if (lw == conj)
                    {
                        isConj = true;
                        break;
                    }
                if (isConj) continue;

                filteredWords.Add(w);
            }
            stat.WordCount = filteredWords.Count;

            // 3. Короткое/длинное слово
            if (filteredWords.Count > 0)
            {
                stat.ShortestWord = filteredWords[0];
                stat.LongestWord = filteredWords[0];
                foreach (string w in filteredWords)
                {
                    if (w.Length < stat.ShortestWord.Length)
                        stat.ShortestWord = w;
                    if (w.Length > stat.LongestWord.Length)
                        stat.LongestWord = w;
                }
            }

            // 4. Подсчёт предложений
            int sentences = 0;
            foreach (char ch in text)
                if (ch == '.' || ch == '!' || ch == '?')
                    sentences++;
            stat.SentenceCount = sentences;

            // 5. Гласные/согласные/частота
            char[] vowels = new char[] { 'а', 'е', 'ё', 'и', 'о', 'у', 'ы', 'э', 'ю', 'я' };
            foreach (char ch in text)
            {
                if (char.IsLetter(ch))
                {
                    char lower = char.ToLower(ch);
                    if (!stat.LetterFrequency.ContainsKey(lower))
                        stat.LetterFrequency[lower] = 0;
                    stat.LetterFrequency[lower]++;

                    bool isVowel = false;
                    foreach (char v in vowels)
                        if (lower == v)
                        {
                            isVowel = true;
                            break;
                        }

                    if (isVowel) stat.VowelCount++;
                    else stat.ConsonantCount++;
                }
            }

            return stat;
        }
    }

    class Program
    {
        static readonly string[] Conjunctions = new string[]
        {
            "и", "а", "но", "как", "или", "либо", "да", "что",
            "чтобы", "ведь", "когда", "пока", "то", "же"
        };

        static void Main(string[] args)
        {
            List<TextStatistics> history = new List<TextStatistics>();

            while (true)
            {
                Console.WriteLine("Меню:");
                Console.WriteLine("1. Ввести новый текст");
                Console.WriteLine("2. Показать статистику по всем текстам");
                Console.WriteLine("3. Выход");
                Console.Write("Выберите пункт: ");
                string choice = Console.ReadLine();

                if (choice == "1")
                {
                    string text;
                    do
                    {
                        Console.WriteLine("Введите текст (не менее 100 символов):");
                        text = Console.ReadLine();
                    }
                    while (text == null || text.Length < 100);

                    var stat = TextStatistics.Compute(text, Conjunctions);
                    stat.Print();
                    history.Add(stat);

                    Console.Write("Хотите удалить из текста какие-то буквы? (д/н): ");
                    string yn = Console.ReadLine().ToLower();
                    if (yn == "д" || yn == "y" || yn == "yes")
                    {
                        Console.WriteLine("Введите через пробел буквы, которые нужно удалить (любого регистра):");
                        string toRemoveLine = Console.ReadLine();

                        // тут именно string[], а не char[]
                        string[] tokens = toRemoveLine
                            .Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

                        HashSet<char> remSet = new HashSet<char>();
                        foreach (string tok in tokens)
                        {
                            // берём первый символ каждого токена
                            if (tok.Length > 0)
                                remSet.Add(char.ToLower(tok[0]));
                        }

                        // строим новый текст без этих букв
                        StringBuilder sb = new StringBuilder();
                        foreach (char c in text)
                        {
                            if (char.IsLetter(c) && remSet.Contains(char.ToLower(c)))
                                continue;
                            sb.Append(c);
                        }
                        string newText = sb.ToString();

                        var stat2 = TextStatistics.Compute(newText, Conjunctions);
                        Console.WriteLine("Статистика после удаления букв:");
                        stat2.Print();
                        history.Add(stat2);
                    }
                }
                else if (choice == "2")
                {
                    if (history.Count == 0)
                    {
                        Console.WriteLine("Статистики ещё нет.");
                    }
                    else
                    {
                        for (int i = 0; i < history.Count; i++)
                        {
                            Console.WriteLine("---- Текст #{0} ----", i + 1);
                            history[i].Print();
                        }
                    }
                }
                else if (choice == "3")
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Неверный выбор. Попробуйте ещё раз.");
                }
            }
        }
    }
}