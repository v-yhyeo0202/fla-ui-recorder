using Minerals.StringCases;
using System.Diagnostics;
using System.Windows;

namespace RecorderUI.Core;

public class Utility
{
    public string ToCustomSnakeCase(string text)
    {
        string[] arrayText = text.Split("_");

        for (int i = 0; i < arrayText.Length; i++)
        {
            arrayText[i] = arrayText[i].ToSnakeCase();
        }

        string snakeCaseText = string.Join("__", arrayText);

        return snakeCaseText;
    }

    public void ShowExceptionMessage(Exception e, string functionName)
    {
        string exceptionMessage = $"Exception caught in {functionName} {e.TargetSite}\n{e.Message}\nPlease report this issue to developer";
        Trace.WriteLine(exceptionMessage);
        MessageBox.Show(exceptionMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        return;
    }

    public void ShowSuccessMessage()
    {
        MessageBox.Show("Operation succeeds", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

        return;
    }

    public void ShowErrorMessage(string errorMessage)
    {
        MessageBox.Show($"{errorMessage}\nPlease report this issue to developer", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        return;
    }
}