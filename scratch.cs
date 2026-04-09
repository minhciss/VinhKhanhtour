using System;

class Program
{
    static void Main()
    {
        string url = "postgresql://vinhkhanh:Xm9PqWz0AOP@dpg-cqq1q7j0q-a.oregon-postgres.render.com/vinhkhanh";
        Console.WriteLine(ConvertPostgresUrl(url));
    }
    
    static string ConvertPostgresUrl(string url)
    {
        var uri = new Uri(url);
        var host = uri.Host;
        var portNum = uri.Port > 0 ? uri.Port : 5432;
        var db = uri.AbsolutePath.TrimStart('/');
        var userInfo = uri.UserInfo.Split(':');
        var user = Uri.UnescapeDataString(userInfo[0]);
        var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        return $"Host={host};Port={portNum};Database={db};Username={user};Password={pass};" +
               "SSL Mode=Require;Trust Server Certificate=true";
    }
}
