namespace SoftUni_Platform_Certificate_Exchange.Services;

using System.Security.Cryptography;


public static class TemporaryPasswordGenerator
{
    // Групи от допустими символи
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%*-_";

    // Генерира криптографски случайна парола
    public static string Generate()
    {
        const int length = 20;

        var password = new char[length];
        var all = Upper + Lower + Digits + Symbols;

        // Гарантираме наличието на четирите типа символи
        password[0] = Pick(Upper);
        password[1] = Pick(Lower);
        password[2] = Pick(Digits);
        password[3] = Pick(Symbols);

        // Попълваме останалите символи
        for (int i = 4; i < length; i++)
        {
            password[i] = Pick(all);
        }

        // Разбъркваме символите на случайни позиции
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }

    // Избира случаен символ с криптографски генератор
    private static char Pick(string characters)
    {
        int index = RandomNumberGenerator.GetInt32(characters.Length);
        return characters[index];
    }
}
