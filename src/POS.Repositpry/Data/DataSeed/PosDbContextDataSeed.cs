namespace POS.Repository.Data.DataSeed
{
    public class PosDbContextDataSeed
    {
        private static readonly List<string> potentialFilePaths =
        [
            Path.Combine("Data", "DataSeed","JsonFiles"),
            Path.Combine("..", "DesignsAndBuildWebsite.Repository", "Data", "DataSeed","JsonFiles"),
        ];

        public async static Task SeedAsync(AppDbContext _dbContext)
        {
        }

        private static string FindValidFilePath(List<string> paths, string fileName)
        {
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
            return string.Empty;
        }

        public static async Task<List<T>> GetDataFromJsonFile<T>(string fileName)
        {
            var filePath = FindValidFilePath(potentialFilePaths, fileName);
            if (string.IsNullOrEmpty(filePath))
                return new List<T>();

            var data = await File.ReadAllTextAsync(filePath);
            var result = JsonSerializer.Deserialize<List<T>>(data);
            return result ?? new List<T>();
        }
    }
}
