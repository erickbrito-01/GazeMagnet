namespace GazeMagnet;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var instance = new Mutex(true, @"Local\GazeMagnet", out bool first);
        if (!first)
        {
            MessageBox.Show("O GazeMagnet já está aberto.", "GazeMagnet");
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
