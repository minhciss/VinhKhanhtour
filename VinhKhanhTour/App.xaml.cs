namespace VinhKhanhTour
{
    public partial class App : Application
    {
        private readonly HeartbeatService _heartbeat;

        public App(HeartbeatService heartbeat)
        {
            InitializeComponent();
            _heartbeat = heartbeat;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            // ── Lifecycle hooks để điều khiển heartbeat ──
            window.Activated   += (_, _) => _heartbeat.Start();   // App vào foreground
            window.Deactivated += (_, _) => _heartbeat.Stop();    // App vào background / minimize
            window.Destroying  += (_, _) => _heartbeat.Stop();    // App bị tắt hoàn toàn

            return window;
        }
    }
}