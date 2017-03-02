using System;
using SpotifyAPI.Local;
using SpotifyAPI.Local.Models;

namespace SpotifyHelper
{
    public interface ISimpleModule : IDisposable
    {
        //TODO: Friendly description of the module to allow settings to enable/disable it
    }

    public interface ILongRunningModule : ISimpleModule
    {
        void SpotifyAvailable(SpotifyInstance newInstance);
        void InstanceClosed(SpotifyInstance oldInstance);
    }

    public interface IPlayStateChanged : ISimpleModule
    {
        void OnPlayStateChanged(PlayStateEventArgs args);
    }

    public interface ITrackChanged : ISimpleModule
    {
        void OnTrackChanged(Track newTrack);
    }
}
