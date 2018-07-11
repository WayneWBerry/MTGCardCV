using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.ipevo.Presenter.CameraKit;
using Emgu.CV;
using Emgu.CV.Structure;

namespace CardImaging
{
    public class CameraController
    {
        // Variable to track camera device selected
        int CameraDevice = 0;

        public CameraController()
        {
        }

        /*
        /// <summary>
        /// Capture Camera Output Async
        /// </summary>
        /// <returns></returns>
        public Task<Image<Gray, Single>> CaptureAsync()
        {
            return Task.Factory.StartNew<Image<Gray, Single>>(() =>
            {
                using (Capture capture = new Capture(CameraDevice))
                {
                    Image<Gray, Single> result = new Image<Gray, Single>(1600, 1200);

                    using (ManualResetEvent resetEvent = new ManualResetEvent(initialState: false))
                    {
                        capture.ImageGrabbed += (s, e) =>
                        {
                            capture.Retrieve(result, channel: 4);
                            resetEvent.Set();
                        };

                        if (!capture.Grab())
                        {
                            return null;
                        }

                        resetEvent.WaitOne();

                        return (result);
                    }
                }
            });
        }
        */

        /// <summary>
        /// Capture Camera Output Async
        /// </summary>
        /// <returns></returns>
        public Task<Mat> CaptureAsync(double focus)
        {
            return Task.Factory.StartNew<Mat>(() =>
            {
                using (Capture capture = new Capture(CameraDevice))
                {
                    capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth, 1200);
                    capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight, 1600);
                    capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Focus, focus);
                    capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps, 29.5);
                    capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Exposure, 10.0);
                    capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.Zoom, 1.0);
                    var image = capture.QueryFrame();

                    //Image<Gray, Byte> imageInvert = new Image<Gray, Byte>(image.Bitmap);
                    return image;
                }
            });
        }
    }
}
