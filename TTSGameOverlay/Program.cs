namespace TTSGameOverlay
{
    // Program entry point
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Create and show the overlay form
            var overlayForm = new TtsOverlayForm();
            Application.Run(overlayForm);
        }
    }
}