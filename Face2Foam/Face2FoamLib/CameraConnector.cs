using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using EOSDigital.API;

namespace Face2FoamLib
{
    // see https://www.codeproject.com/articles/688276/canon-edsdk-tutorial-in-csharp
    public class CameraConnector : IDisposable
    {
        CanonAPI CanonAPI;
        Camera Camera;

        public bool IsConnected { get { return Camera != null && Camera.SessionOpen; } }
        public bool IsStreaming { get { return IsConnected && Camera.IsLiveViewOn; } }

        public event LiveViewUpdate NewLiveViewImageAvailable;
        public event EventHandler SomethingChanged;
        public CameraConnector()
        {
            CanonAPI = new CanonAPI(false);
        }

        //tries to open a session with the one and only connected camera
        public void Connect()
        {
            Camera?.Dispose();
            Camera = CanonAPI.GetCameraList().SingleOrDefault();
            if (Camera == null) return;
            Camera.LiveViewUpdated += HandleLiveViewUpdated;
            Camera.CameraHasShutdown += delegate { HandleSomethingChanged(); };
            Camera.DownloadReady += delegate { HandleSomethingChanged(); };
            Camera.LiveViewStopped += delegate { HandleSomethingChanged(); };
            Camera.ObjectChanged += delegate { HandleSomethingChanged(); };
            Camera.ProgressChanged += delegate { HandleSomethingChanged(); };
            Camera.PropertyChanged += delegate { HandleSomethingChanged(); };
            Camera.StateChanged += delegate { HandleSomethingChanged(); };
            Camera?.OpenSession();
        }

        public void Disconnect()
        {
            Camera?.CloseSession();
        }

        public void StartLiveCapture()
        {
            if (Camera?.IsLiveViewOn == true) return;
            Camera?.StartLiveView();
        }

        public void StopLiveCapture()
        {
            Camera?.StopLiveView();
        }

        public void HandleSomethingChanged()
        {
            SomethingChanged?.Invoke(this, EventArgs.Empty);
        }

        public void HandleLiveViewUpdated(Camera source, System.IO.Stream stream)
        {
            NewLiveViewImageAvailable?.Invoke(source, stream);
        }


        public void Dispose()
        {
            Camera?.Dispose();
            CanonAPI?.Dispose();
        }
    }
}
