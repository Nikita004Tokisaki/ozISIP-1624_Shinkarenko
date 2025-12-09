using System;
using System.Collections.Generic;
using System.Linq;

namespace UniversityManagement
{
    // Абстрактный класс Person — общие свойства для студентов и преподавателей
    abstract class Person
    {
        private static int _nextId = 1;

        public int Id { get; }
        public string Name { get; }
        public int Age { get; }
        public string Email { get; }

        protected Person(string name, int age, string email)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Имя не может быть пустым.");
            if (age <= 0)
                throw new ArgumentException("Возраст должен быть положительным.");
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
                throw new ArgumentException("Некорректный e-mail.");

            Id = _nextId++;
            Name = name;
            Age = age;
            Email = email;
        }

        // Полиморфный метод для отображения информации о человеке
        public abstract string GetInfo();

        public override string ToString() => GetInfo();
    }

    // Класс Student наследует Person и хранит список курсов
    class Student : Person
    {
        private readonly List<Course> _courses = new List<Course>();
        public IReadOnlyList<Course> Courses => _courses;

        public Student(string name, int age, string email)
            : base(name, age, email)
        {
        }

        // Записать студента на курс
        public void Enroll(Course course)
        {
            if (course == null)
                throw new ArgumentNullException(nameof(course));
            if (!_courses.Contains(course))
            {
                _courses.Add(course);
                course.AddStudent(this);
            }
        }

        public override string GetInfo()
        {
            return $"[Студент #{Id}] {Name}, {Age} лет, {Email}. Записан на курсов: {_courses.Count}";
        }
    }

    // Класс Instructor наследует Person и хранит список курсов, которые ведёт
    class Instructor : Person
    {
        private readonly List<Course> _courses = new List<Course>();
        public IReadOnlyList<Course> Courses => _courses;

        public Instructor(string name, int age, string email)
            : base(name, age, email)
        {
        }

        // Назначить преподавателя на курс
        public void AssignCourse(Course course)
        {
            if (course == null)
                throw new ArgumentNullException(nameof(course));
            if (!_courses.Contains(course))
            {
                _courses.Add(course);
                course.AssignInstructor(this);
            }
        }

        public override string GetInfo()
        {
            return $"[Преподаватель #{Id}] {Name}, {Age} лет, {Email}. Преподаёт курсов: {_courses.Count}";
        }
    }

    // Класс Course хранит информацию о названии, преподавателе и списке студентов
    class Course
    {
        private static int _nextId = 1;

        public int Id { get; }
        public string Title { get; }
        public Instructor? Instructor { get; private set; }

        private readonly List<Student> _students = new List<Student>();
        public IReadOnlyList<Student> Students => _students;

        public Course(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Название курса не может быть пустым.");
            Id = _nextId++;
            Title = title;
        }

        // Внутренний метод — добавляет студента в список
        internal void AddStudent(Student student)
        {
            if (!_students.Contains(student))
                _students.Add(student);
        }

        // Внутренний метод — назначает преподавателя
        internal void AssignInstructor(Instructor instructor)
        {
            Instructor = instructor;
        }

        public string GetInfo()
        {
            var instrInfo = Instructor != null
                ? $"Преподаватель: {Instructor.Name}"
                : "Преподаватель не назначен";
            return $"[Курс #{Id}] {Title}. {instrInfo}. Студентов: {_students.Count}";
        }

        public override string ToString() => GetInfo();
    }

    // Класс University — точка хранения всех студентов, преподавателей и курсов
    class University
    {
        private readonly List<Student> _students = new List<Student>();
        private readonly List<Instructor> _instructors = new List<Instructor>();
        private readonly List<Course> _courses = new List<Course>();

        // Добавить студента
        public Student AddStudent(string name, int age, string email)
        {
            var s = new Student(name, age, email);
            _students.Add(s);
            return s;
        }

        // Получить студента по Id
        public Student? GetStudent(int id) => _students.FirstOrDefault(s => s.Id == id);

        public IEnumerable<Student> GetAllStudents() => _students;

        // Добавить преподавателя
        public Instructor AddInstructor(string name, int age, string email)
        {
            var i = new Instructor(name, age, email);
            _instructors.Add(i);
            return i;
        }

        public Instructor? GetInstructor(int id) => _instructors.FirstOrDefault(i => i.Id == id);

        public IEnumerable<Instructor> GetAllInstructors() => _instructors;

        // Добавить курс
        public Course AddCourse(string title)
        {
            var c = new Course(title);
            _courses.Add(c);
            return c;
        }

        public Course? GetCourse(int id) => _courses.FirstOrDefault(c => c.Id == id);

        public IEnumerable<Course> GetAllCourses() => _courses;
    }

    // Консольное меню
    class Program
    {
        static void Main()
        {
            var university = new University();

            while (true)
            {
                Console.WriteLine("\n===== Меню управления университетом =====");
                Console.WriteLine("1  — Добавить студента");
                Console.WriteLine("2  — Показать всех студентов");
                Console.WriteLine("3  — Просмотреть данные студента");
                Console.WriteLine("4  — Добавить преподавателя");
                Console.WriteLine("5  — Показать всех преподавателей");
                Console.WriteLine("6  — Просмотреть данные преподавателя");
                Console.WriteLine("7  — Создать курс");
                Console.WriteLine("8  — Показать все курсы");
                Console.WriteLine("9  — Просмотреть данные курса");
                Console.WriteLine("10 — Записать студента на курс");
                Console.WriteLine("11 — Назначить преподавателя на курс");
                Console.WriteLine("12 — Список курсов студента");
                Console.WriteLine("13 — Список студентов курса");
                Console.WriteLine("0  — Выход");
                Console.Write("Выберите команду: ");

                var choice = Console.ReadLine();
                Console.WriteLine();

                try
                {
                    switch (choice)
                    {
                        case "1": AddStudent(university); break;
                        case "2": ListStudents(university); break;
                        case "3": ShowStudentInfo(university); break;
                        case "4": AddInstructor(university); break;
                        case "5": ListInstructors(university); break;
                        case "6": ShowInstructorInfo(university); break;
                        case "7": AddCourse(university); break;
                        case "8": ListCourses(university); break;
                        case "9": ShowCourseInfo(university); break;
                        case "10": EnrollStudent(university); break;
                        case "11": AssignInstructor(university); break;
                        case "12": ListStudentCourses(university); break;
                        case "13": ListCourseStudents(university); break;
                        case "0": return;
                        default:
                            Console.WriteLine("Неверная команда");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка: " + ex.Message);
                }
            }
        }

        static void AddStudent(University uni)
        {
            Console.Write("Имя: ");
            var name = Console.ReadLine()!;
            Console.Write("Возраст: ");
            var age = int.Parse(Console.ReadLine()!);
            Console.Write("Email: ");
            var email = Console.ReadLine()!;

            var s = uni.AddStudent(name, age, email);
            Console.WriteLine("Добавлен " + s);
        }

        static void ListStudents(University uni)
        {
            foreach (var s in uni.GetAllStudents())
                Console.WriteLine(s);
        }

        static void ShowStudentInfo(University uni)
        {
            Console.Write("ID студента: ");
            var id = int.Parse(Console.ReadLine()!);
            var s = uni.GetStudent(id);
            if (s == null)
                Console.WriteLine("Студент не найден");
            else
            {
                Console.WriteLine(s);
                Console.WriteLine("Курсы студента:");
                foreach (var c in s.Courses)
                    Console.WriteLine("  " + c);
            }
        }

        static void AddInstructor(University uni)
        {
            Console.Write("Имя: ");
            var name = Console.ReadLine()!;
            Console.Write("Возраст: ");
            var age = int.Parse(Console.ReadLine()!);
            Console.Write("Email: ");
            var email = Console.ReadLine()!;

            var i = uni.AddInstructor(name, age, email);
            Console.WriteLine("Добавлен " + i);
        }

        static void ListInstructors(University uni)
        {
            foreach (var i in uni.GetAllInstructors())
                Console.WriteLine(i);
        }

        static void ShowInstructorInfo(University uni)
        {
            Console.Write("ID преподавателя: ");
            var id = int.Parse(Console.ReadLine()!);
            var i = uni.GetInstructor(id);
            if (i == null)
                Console.WriteLine("Преподаватель не найден");
            else
            {
                Console.WriteLine(i);
                Console.WriteLine("Курсы преподавателя:");
                foreach (var c in i.Courses)
                    Console.WriteLine("  " + c);
            }
        }

        static void AddCourse(University uni)
        {
            Console.Write("Название курса: ");
            var title = Console.ReadLine()!;
            var c = uni.AddCourse(title);
            Console.WriteLine("Создан " + c);
        }

        static void ListCourses(University uni)
        {
            foreach (var c in uni.GetAllCourses())
                Console.WriteLine(c);
        }

        static void ShowCourseInfo(University uni)
        {
            Console.Write("ID курса: ");
            var id = int.Parse(Console.ReadLine()!);
            var c = uni.GetCourse(id);
            if (c == null)
                Console.WriteLine("Курс не найден");
            else
            {
                Console.WriteLine(c);
                Console.WriteLine("Студенты на курсе:");
                foreach (var s in c.Students)
                    Console.WriteLine("  " + s);
            }
        }

        static void EnrollStudent(University uni)
        {
            Console.Write("ID студента: ");
            var sid = int.Parse(Console.ReadLine()!);
            Console.Write("ID курса: ");
            var cid = int.Parse(Console.ReadLine()!);
            var s = uni.GetStudent(sid);
            var c = uni.GetCourse(cid);
            if (s == null || c == null)
                Console.WriteLine("Студент или курс не найдены");
            else
            {
                s.Enroll(c);
                Console.WriteLine($"Студент {s.Name} записан на курс {c.Title}");
            }
        }

        static void AssignInstructor(University uni)
        {
            Console.Write("ID преподавателя: ");
            var iid = int.Parse(Console.ReadLine()!);
            Console.Write("ID курса: ");
            var cid = int.Parse(Console.ReadLine()!);
            var i = uni.GetInstructor(iid);
            var c = uni.GetCourse(cid);
            if (i == null || c == null)
                Console.WriteLine("Преподаватель или курс не найдены");
            else
            {
                i.AssignCourse(c);
                Console.WriteLine($"Преподаватель {i.Name} назначен на курс {c.Title}");
            }
        }

        static void ListStudentCourses(University uni)
        {
            Console.Write("ID студента: ");
            var sid = int.Parse(Console.ReadLine()!);
            var s = uni.GetStudent(sid);
            if (s == null)
                Console.WriteLine("Студент не найден");
            else
            {
                Console.WriteLine($"Курсы студента {s.Name}:");
                foreach (var c in s.Courses)
                    Console.WriteLine("  " + c);
            }
        }

        static void ListCourseStudents(University uni)
        {
            Console.Write("ID курса: ");
            var cid = int.Parse(Console.ReadLine()!);
            var c = uni.GetCourse(cid);
            if (c == null)
                Console.WriteLine("Курс не найден");
            else
            {
                Console.WriteLine($"Студенты на курсе {c.Title}:");
                foreach (var s in c.Students)
                    Console.WriteLine("  " + s);
            }
        }
    }
}