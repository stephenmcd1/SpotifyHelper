using Microsoft.Win32;

namespace SpotifyHelper
{
    public class LockDetector : ISimpleModule
    {
        private bool _shouldUnPause;
        private readonly SpotifyInstance _instance;

        public LockDetector(SpotifyInstance instance)
        {
            _instance = instance;
            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.SessionLock:
                    if (_instance.IsPlaying)
                    {
                        _shouldUnPause = true;
                        _instance.Pause().Wait();
                    }
                    break;
                case SessionSwitchReason.SessionUnlock:
                    if (_shouldUnPause)
                    {
                        _shouldUnPause = false;
                        _instance.Play().Wait();
                    }
                    break;
            }
        }
        public void Dispose()
        {
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
        }
    }
}
