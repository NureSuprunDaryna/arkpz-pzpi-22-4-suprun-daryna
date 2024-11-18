namespace Code
{
    class FileReader
    {
        public const int SUCCESS = 0;
        public const int FILE_NOT_FOUND = 1;

        public int ReadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return FILE_NOT_FOUND; // Код помилки
            }
            // Логіка читання файлу
            return SUCCESS;
        }
    }

}
