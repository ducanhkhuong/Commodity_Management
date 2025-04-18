using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;
using ZXing;
using ZXing.Common;
using ZXing.Presentation;
using ZXing.QrCode.Internal;

namespace SessionManager
{
    public partial class SessionManager : Form
    {
        private Capture capture;
        private bool captureInProgress;
        private ZXing.BarcodeReader barcodeReader;
        private string lastQrCode;
        private CancellationTokenSource cancellationTokenSource;
        
        private ConcurrentQueue<string> processHandllerQueue;
        private ConcurrentQueue<string> processUIUXQueue;
        
        private Task HandllerPushTask;
        private Task HandllerUITask;
        private Task HandllerPullTask;
        
        public SessionManager()
        {
            InitializeComponent();
            processHandllerQueue    = new ConcurrentQueue<string>();
            processUIUXQueue        = new ConcurrentQueue<string>();
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                capture = new Capture(1);
                barcodeReader = new ZXing.BarcodeReader
                {
                    AutoRotate = true,
                    Options = new DecodingOptions
                    {
                        TryHarder = true,
                        PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
                    }
                };

                HandllerPushTask = Task.Run(() => ProcessPushHandller(cancellationTokenSource.Token));
                HandllerUITask   = Task.Run(() => ProcessUI(cancellationTokenSource.Token));
                HandllerPullTask = Task.Run(() => ProcessPullHandller(cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khởi tạo camera hoặc barcodeReader: {ex.Message}");
            }
        }

        private async void ProcessPullHandller(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                //nhận dữ liệu phản hồi mỗi 0.5 giây là các byte hex , phân tích dữ liệu và lưu vào DB
                Console.WriteLine($"task pull");
                await Task.Delay(500);
            }
        }

        private async void ProcessPushHandller(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (processHandllerQueue.TryDequeue(out string qrCode))
                {
                    //xử lý mã qr , ép về byte hex , gửi dữ liệu đi 
                    Console.WriteLine($"ProcessHandller from queue: {qrCode}");
                }
                await Task.Delay(100);
            }
        }

        private async void ProcessUI(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (processUIUXQueue.TryDequeue(out string qrCode))
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        //xử lý ui ux giao diện 
                        datamock.Text = $"{qrCode}";
                        Console.WriteLine($"ProcessUIUX from queue: {qrCode}");
                    });
                    await Task.Delay(200);
                }
                await Task.Delay(100);
            }
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            try
            {
                if (capture == null)
                {
                    return;
                }

                Image<Bgr, Byte> ImageFrame = capture.QueryFrame();
                if (ImageFrame == null || ImageFrame.Width == 0 || ImageFrame.Height == 0)
                {
                    return;
                }

                CamImageBox.Image = ImageFrame;

                using (Bitmap bitmap = ImageFrame.ToBitmap())
                {
                    if (bitmap == null)
                    {
                        return;
                    }

                    if (barcodeReader == null)
                    {
                        return;
                    }

                    var result = barcodeReader.Decode(bitmap);
                    if (result != null && result.Text != lastQrCode)
                    {
                        lastQrCode = result.Text;
                        processHandllerQueue.Enqueue(result.Text);
                        processUIUXQueue.Enqueue(result.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xử lý khung hình: {ex.Message}");
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (capture == null)
            {
                return;
            }

            if (captureInProgress)
            {
                btnStart.Text = "Start!";
                Application.Idle -= ProcessFrame;
                captureInProgress = false;
            }
            else
            {
                btnStart.Text = "Stop";
                Application.Idle += ProcessFrame;
                captureInProgress = true;
            }
        }

        private void ReleaseData()
        {
            if (capture != null)
            {
                capture.Dispose();
                capture = null;
            }
            cancellationTokenSource?.Cancel();
            try
            {
                Task.WhenAll(HandllerPushTask, HandllerUITask).Wait(1000);
            }
            catch { }
            cancellationTokenSource?.Dispose();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            ReleaseData();
        }

        private void CameraCapture_Load(object sender, EventArgs e)
        {
            ;
        }

        private void CamImageBox_Click(object sender, EventArgs e)
        {
            ;
        }
    }
}