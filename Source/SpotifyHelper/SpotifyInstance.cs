using System;
using System.Threading;
using System.Threading.Tasks;
using PInvoke;
using SpotifyAPI.Local;

namespace SpotifyHelper
{
    public class SpotifyInstance
    {
        private readonly SpotifyLocalAPI _sl;

        public bool IsStillAvailable => !Process.HasExited;

        public bool IsPlaying => _sl.GetStatus().Playing;

        public async Task TogglePlayState()
        {
            if (_sl.GetStatus().Playing)
                await _sl.Pause();
            else
                await _sl.Play();
        }

        public async Task Play()
        {
            await _sl.Play();
        }

        public async Task Pause()
        {
            await _sl.Pause();
        }

        public static SpotifyInstance TryFind(out SpotifyLocalAPI api)
        {
            var hwnd = User32.FindWindow("SpotifyMainWindow", null);

            if (hwnd == IntPtr.Zero)
            {
                api = null;
                return null;
            }


            if (!SpotifyLocalAPI.IsSpotifyWebHelperRunning())
            {
                SpotifyLocalAPI.RunSpotifyWebHelper();
            }

            int processId;
            User32.GetWindowThreadProcessId(hwnd, out processId);

            var proc = System.Diagnostics.Process.GetProcessById(processId);

            var sl = new SpotifyLocalAPI { ListenForEvents = true };
            Task.Run(() => sl.Connect()).GetAwaiter().GetResult();

            api = sl;
            return new SpotifyInstance(proc, hwnd, sl);
        }

        public IntPtr MainWindowHandle { get; }

        public System.Diagnostics.Process Process { get; }


        private SpotifyInstance(System.Diagnostics.Process process, IntPtr hwnd, SpotifyLocalAPI sl)
        {
            Process = process;
            MainWindowHandle = hwnd;
            _sl = sl;
        }
    }
}
