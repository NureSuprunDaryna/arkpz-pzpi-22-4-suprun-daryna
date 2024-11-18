namespace Code
{
    class User
    {
        public bool SetAge(int age)
        {
            if (age < 0)
            {
                return false;  // Використання перевірки замість винятку
            }
            // Логіка для встановлення віку
            return true;
        }
    }

}
