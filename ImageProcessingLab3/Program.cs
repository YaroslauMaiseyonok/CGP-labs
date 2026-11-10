using System;
using System.Windows.Forms;
using ImageProcessingLab3.UI;

namespace ImageProcessingLab3;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}