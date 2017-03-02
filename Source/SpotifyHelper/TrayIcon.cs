using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using SpotifyAPI.Local;
using SpotifyAPI.Local.Models;

namespace SpotifyHelper
{
    public class TrayIcon : ILongRunningModule, ITrackChanged, IPlayStateChanged
    {
        public TrayIcon()
        {
            var exit = new MenuItem("E&xit", (sender, args) =>
            {
                Application.Exit();
            });

            //TODO: Cleanup this logic - better icons!

            var sp = System.Diagnostics.Process.GetProcessesByName("Spotify").FirstOrDefault();
            var icon = sp == null ? SystemIcons.Shield : Icon.ExtractAssociatedIcon(sp.MainModule.FileName);

            var bm = icon.ToBitmap();

            using (var g = Graphics.FromImage(bm))
            {
                g.FillEllipse(Brushes.Black, 16, 16, 16, 16);
                g.FillEllipse(Brushes.PaleVioletRed, 18, 18, 14, 14);
                _paused = Icon.FromHandle(bm.GetHicon());
            }

            bm = icon.ToBitmap();
            using (var g = Graphics.FromImage(bm))
            {
                g.FillEllipse(new SolidBrush(Color.FromArgb(48, 255, 255, 255)), 0, 0, 32, 32);
                _playing = Icon.FromHandle(bm.GetHicon());
            }

            bm = icon.ToBitmap();

            using (var g = Graphics.FromImage(bm))
            {
                g.FillEllipse(Brushes.Black, 16, 16, 16, 16);
                g.FillEllipse(Brushes.Red, 18, 18, 14, 14);
                _notRunning = Icon.FromHandle(bm.GetHicon());
            }

            _ni = new NotifyIcon
            {
                Icon = icon,
                Visible = true,
                ContextMenu = new ContextMenu(new[] { exit })
            };
            _ni.MouseClick += (sender, args) =>
            {
                if (args.Button == MouseButtons.Left && _instance?.IsStillAvailable == true)
                {
                    _instance.TogglePlayState().Wait();
                }
            };
        }

        private static void SetNotifyIconText(NotifyIcon ni, string text)
        {
            if (text.Length >= 128)
            {
                throw new ArgumentOutOfRangeException(nameof(text), "Text limited to 127 characters");
            }
            var t = typeof(NotifyIcon);
            const BindingFlags hidden = BindingFlags.NonPublic | BindingFlags.Instance;
            t.GetField("text", hidden).SetValue(ni, text);
            if ((bool) t.GetField("added", hidden).GetValue(ni))
            {
                t.GetMethod("UpdateIcon", hidden).Invoke(ni, new object[] { true });
            }
        }

        public void OnTrackChanged(Track newTrack)
        {
            if (newTrack == null)
                _ni.Text = "No current track";
            else if (newTrack.IsAd())
            {
                _ni.Text = "An ad is playing...";
            }
            else
            {
                var length = TimeSpan.FromSeconds(newTrack.Length);

                //TODO: Limit to 128 characters or else get ArgumentException
                SetNotifyIconText(_ni,
                    $"{newTrack.TrackResource.Name} by {newTrack.ArtistResource.Name} on {newTrack.AlbumResource.Name} ({Math.Floor(length.TotalMinutes)}:{length.Seconds})");
                //_ni.Text += _ni.Text + _ni.Text;
            }
        }

        private readonly NotifyIcon _ni;
        private readonly Icon _paused;
        private readonly Icon _playing;
        private SpotifyInstance _instance;
        private readonly Icon _notRunning;

        public void Dispose()
        {
            _ni.Visible = false;
            _ni.Dispose();
        }

        public void SpotifyAvailable(SpotifyInstance instance)
        {
            _instance = instance;
        }

        public void OnPlayStateChanged(PlayStateEventArgs args)
        {
            _ni.Icon = args.Playing ? _playing : _paused;
        }

        public void InstanceClosed(SpotifyInstance oldInstance)
        {
            _ni.Icon = _notRunning;
            _instance = null;
            _ni.Text = "Spotify not running";
        }
    }
}
