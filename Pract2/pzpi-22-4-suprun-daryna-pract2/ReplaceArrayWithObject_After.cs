namespace Code
{
    class Employee
    {
        private string name;        // Поле для імені
        private string position;    // Поле для посади

        public Employee(string name, string position)
        {
            this.name = name;            // Пряме присвоєння
            this.position = position;
        }

        public void PrintDetails()
        {
            Console.WriteLine("Name: " + name);
            Console.WriteLine("Position: " + position);
        }
    }

}
