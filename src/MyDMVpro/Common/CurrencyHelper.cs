using Humanizer;
using System;

namespace MyDMVpro.Common;

public class CurrencyHelper
{
    public static string ConvertToWords(decimal value, bool centsAsFraction = true, bool sentenceCasing = false, bool upperCase = true)
    {
        string[] numbers = new string[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
        string[] tens = new string[] { "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
        string[] teens = new string[] { "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
        string[] groups = new string[] { "", "Thousand", "Million", "Billion" };
        string[] parts = value.ToString("0.00").Split('.');
        string dollars = parts[0];
        string cents = parts[1];
        string[] dollarsParts = new string[(int)Math.Ceiling((double)dollars.Length / 3)];

        for (int i = dollarsParts.Length - 1; i >= 0; i--)
        {
            int length = dollars.Length - i * 3;
            if (length > 3)
                length = 3;
            dollarsParts[i] = dollars.Substring(dollars.Length - i * 3 - length, length);
        }
        string result = "";
        for (int i = dollarsParts.Length-1; i >= 0; i--)
        {
            int number = int.Parse(dollarsParts[i]);
            if (number == 0)
                continue;
            string group = groups[i];
            if (result.Length > 0)
                result += " ";
            result += ConvertGroup(number);
            if (group.Length > 0)
            {
                result += " ";
                result += group;
            }
        }
        if (result.Length == 0)
            result = numbers[0];

        if (centsAsFraction)
        {
            if (result.Length > 0)
                result += " and ";
            result += cents + "/100";
        }

        if (result.Length > 0)
            result += " Dollars";

        if (cents != "00" && !centsAsFraction)
        {
            int number = int.Parse(cents);
            if (result.Length > 0)
                result += " and ";
            result += ConvertGroup(number) + " Cents";
        }
        if (upperCase)
        {
            result = result.ToUpper();
        }
        else if (sentenceCasing)
        {
            result = result.ToLower().ApplyCase(LetterCasing.Sentence);
        }
        return result;
    }
    // static function to convert a group of numbers to words
    private static string ConvertGroup(int number)
    {
        string[] numbers = new string[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine" };
        string[] tens = new string[] { "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
        string[] teens = new string[] { "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
        string result = "";
        if (number >= 100)
        {
            result += numbers[number / 100] + " Hundred";
            number %= 100;
            if (number > 0)
                result += " ";
        }
        if (number >= 20)
        {
            result += tens[number / 10 - 1];
            number %= 10;
            if (number > 0)
                result += "-";
        }
        if (number >= 10)
        {
            result += teens[number - 10];
        }
        else if (number > 0)
        {
            result += numbers[number];
        }
        return result;
    }
    // add test cases here
#if DEBUG
    public static void Test(Decimal val)
    {
        System.Diagnostics.Debug.WriteLine($"{val:C2}: {ConvertToWords(val)}");
    }
    public static void Test()
    {
        return;
        Test(89123.12m);
        Test(10.00m); // Zero Dollars
        Test(11.00m); // Zero Dollars
        Test(12.00m); // Zero Dollars
        Test(13.00m); // Zero Dollars
        Test(14.00m); // Zero Dollars
        Test(15.00m); // Zero Dollars
        Test(16.00m); // Zero Dollars
        Test(17.00m); // Zero Dollars
        Test(18.00m); // Zero Dollars
        Test(19.00m); // Zero Dollars
        Test(20.00m); // Zero Dollars
        Test(21.00m); // Zero Dollars
        for (int i = 21; i < 100; i += 3)
        {
            Test(i);
        }
        //for (int i = 0; i < 10000; i += 100)
        //{
        //    Test(i);
        //}
        //for (int i = 0; i < 20000; i += 100)
        //{
        //    Test(i);
        //}
        for (int i = 13; i < 90000; i += 100)
        {
            Test(((decimal)i) + .25m);
        }
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(0.00m)); // Zero Dollars
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(0.01m)); // One Cent
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(0.10m)); // Ten Cents
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(0.11m)); // Eleven Cents
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(0.99m)); // Ninety Nine Cents
        //for (int i = 0; i <= 99; i++)
        //{
        //    System.Diagnostics.Debug.WriteLine(ConvertToWords(0m + (i / 100m))); // All cents values
        //}
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(1.00m)); // One Dollar
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(1.01m)); // One Dollar and One Cent
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(1.10m)); // One Dollar and Ten Cents
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(1.11m)); // One Dollar and Eleven Cents
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(1.99m)); // One Dollar and Ninety Nine Cents
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(10.00m)); // Ten Dollars
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(10.01m)); // Ten Dollars and One Cent
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(10.10m)); // Ten Dollars and Ten Cents
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(10.11m)); // Ten Dollars and Eleven Cents
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(10.99m)); // Ten Dollars and Ninety Nine Cents
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(100.00m)); // One Hundred Dollars
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(100.01m)); // One Hundred Dollars and One Cent
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(100.10m)); // One Hundred Dollars and Ten Cents
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(100.11m)); // One Hundred Dollars and Eleven Cents
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(100.99m)); // One Hundred Dollars and Ninety Nine Cents
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(1000.00m)); // One Thousand Dollars
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(1000.01m)); // One Thousand Dollars and One Cent
        //System.Diagnostics.Debug.WriteLine(ConvertToWords(1000.10m)); // One Thousand Dollars and Ten Cents

        System.Diagnostics.Debug.WriteLine(ConvertToWords(1000.99m));
        System.Diagnostics.Debug.WriteLine(ConvertToWords(12000.00m)); 
        System.Diagnostics.Debug.WriteLine(ConvertToWords(345000.00m));
    }
#endif
}
