using System.Diagnostics;
using Process.NET.Native.Types;
using Process.NET.Windows.Keyboard;

namespace SpotifyHelper
{
    public class MediaKeyModule : ISimpleModule
    {
        private readonly KeyboardHook _kh;

        public MediaKeyModule(SpotifyInstance instance)
        {
            _kh = new KeyboardHook("98");
            _kh.KeyDownEvent += args =>
            {
                switch (args.Key)
                {
                    case Keys.MediaPlayPause:
                        //Spotify already handles Pause...but it still might be useful in case a rogue app is blocking the keys
                        //instance.TogglePlayState().Wait();

                        break;
                    case Keys.MediaNextTrack:
                    case Keys.MediaPreviousTrack:
                        // The underlying API uses SendKeys to implement this so we can't exactly use that in our KeyDownEvent!
                        break;
                    case Keys.MediaStop:
                        instance.Pause().Wait();
                        break;
                }
            };

            //To ease development, don't bother enable this if started with the debugger attached.  Otherwise, it seems like using keyboard
            //   shortcuts to step around code confuse things
            if (!Debugger.IsAttached)
            {
                _kh.Enable();
            }

        }
        public void Dispose()
        {
            _kh.Dispose();
        }
    }
}
