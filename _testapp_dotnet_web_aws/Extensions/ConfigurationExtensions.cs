using Microsoft.Extensions.Configuration;

namespace _testapp_dotnet_web_aws.Extensions
{
    public static class ConfigurationExtensions
    {
        public static bool GetBool(this IConfiguration configuration, string key)
        {
            if (bool.TryParse(configuration[key], out bool value))
            {
                return value;
            }

            return false;
        }

        public static string GetConnectionString(this IConfiguration configuration)
        {
            string server = configuration["DB_SERVER"];
            string port = configuration["DB_SERVER_PORT"];
            string dbName = configuration["DB_NAME"];
            string user = configuration["DB_USER"];
            string password = configuration["DB_PASSWORD"];

            string connectionString = $@"Host={server};Port={port};Database={dbName};User Id={user};Password={password};Pooling=True;";
            if (!dbName.Contains("-prod")) connectionString += "Include Error Detail=true;";

            return connectionString;
        }
    }
}