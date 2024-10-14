namespace Code
{
    class User
    {
        public void SetAge(int age)
        {
            if (age < 0)
            {
                throw new ArgumentException("Age cannot be negative"); // Кидання винятку для поганих даних
            }
            // Логіка для встановлення віку
        }
    }

}
