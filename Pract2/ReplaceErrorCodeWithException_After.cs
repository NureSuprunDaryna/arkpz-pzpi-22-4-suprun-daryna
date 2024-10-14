namespace Code
{
    class FileReader
    {
        public void ReadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The file was not found: " + filePath); // Викидання винятку
            }
            // Логіка читання файлу
        }
    }

}
