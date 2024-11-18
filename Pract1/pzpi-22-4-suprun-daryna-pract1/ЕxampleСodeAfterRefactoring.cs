namespace CleanCodeExample
{
    // 1. Використовуйте осмислені назви змінних, функцій та класів
    // У цьому прикладі ми використовуємо назви, які чітко описують роль кожного класу, методу та змінної.
    public class User
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string DiscountType { get; set; }

        public User(string name, string email, string discountType)
        {
            Name = name;
            Email = email;
            DiscountType = discountType;
        }
    }

    // 2. Дотримуйтесь чіткої структури коду. Кожен метод чи клас має відповідати лише одній задачі
    public class DiscountService
    {
        private static readonly Dictionary<string, decimal> DiscountRates = new Dictionary<string, decimal>
        {
            { "student", 0.9m },
            { "senior", 0.85m },
            { "employee", 0.8m },
            { "none", 1.0m }
        };

        // Метод обчислює знижку користувача
        public decimal CalculateDiscount(decimal price, string discountType)
        {
            var discountRate = DiscountRates.ContainsKey(discountType) ? DiscountRates[discountType] : 1.0m;
            return price * discountRate;
        }
    }

    // 3. Дотримання стилю кодування (C# Microsoft Coding Conventions)
    public class EmailService
    {
        // Асинхронний метод для надсилання електронного повідомлення
        public async Task SendEmailAsync(User user, string message)
        {
            Console.WriteLine($"Надсилання повідомлення {user.Email}...");
            await Task.Delay(2000);  // Симуляція відправки листа
            Console.WriteLine("Повідомлення надіслано.");
        }
    }

    public class UserRepository
    {
        // Метод для збереження користувача в базі даних
        public void Save(User user)
        {
            Console.WriteLine($"Користувач {user.Name} збережений у базі даних.");
        }
    }

    // 4. Уникайте дублювання коду (DRY)
    // Ми винесли загальний код для логування операцій у метод LogOperation
    public class Logger
    {
        public void LogOperation(string operation)
        {
            Console.WriteLine(operation);
        }
    }

    // Основний клас програми
    public class Store
    {
        private readonly DiscountService _discountService = new DiscountService();
        private readonly EmailService _emailService = new EmailService();
        private readonly UserRepository _userRepository = new UserRepository();
        private readonly Logger _logger = new Logger();

        // Метод для обробки покупки користувача
        public async Task ProcessPurchase(User user, decimal price)
        {
            // Обробка знижки
            var finalPrice = _discountService.CalculateDiscount(price, user.DiscountType);
            _logger.LogOperation($"Користувач {user.Name} отримав знижку. Кінцева ціна: {finalPrice}");

            // Надсилання повідомлення
            await _emailService.SendEmailAsync(user, $"Дякуємо за покупку! Ваша ціна: {finalPrice}");

            // Збереження користувача
            _userRepository.Save(user);
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // Створення користувача
            var user = new User("Дарина Супрун", "daryna.suprun@nure.ua", "student");

            // Ініціалізація магазину
            var store = new Store();

            // Обробка покупки
            await store.ProcessPurchase(user, 1000m); // Вхідна ціна товару
        }
    }
}
