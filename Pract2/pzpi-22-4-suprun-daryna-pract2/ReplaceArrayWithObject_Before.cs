class Employee
{
    private string[] employeeDetails = new string[2];  // Масив для зберігання імені та посади

    public Employee(string name, string position)
    {
        employeeDetails[0] = name;        // employeeDetails[0] представляє ім'я
        employeeDetails[1] = position;    // employeeDetails[1] представляє посаду
    }

    public void PrintDetails()
    {
        Console.WriteLine("Name: " + employeeDetails[0]);
        Console.WriteLine("Position: " + employeeDetails[1]);
    }
}
