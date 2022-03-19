using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public CameraConnector()
        {
            CanonAPI = new CanonAPI(false);
        }

        //tries to open a session with the one and only connected camera
        public void Connect()
        {
            Camera?.Dispose();
            Camera = CanonAPI.GetCameraList().SingleOrDefault();
            Camera.LiveViewUpdated += HandleLiveViewUpdated;
            Camera.PropertyChanged += HandlePropertyChanged;
            Camera.StateChanged += HandleStateChanged;
            Camera?.OpenSession();
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

        public void HandlePropertyChanged(Camera source, EOSDigital.SDK.PropertyEventID eventID, EOSDigital.SDK.PropertyID propertyID ,int parameter)
        {
            
        }
        public void HandleStateChanged(Camera source, EOSDigital.SDK.StateEventID eventID, int parameter)
        {
            
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
