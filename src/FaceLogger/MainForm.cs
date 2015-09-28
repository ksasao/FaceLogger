using JUNKBOX.IO;
using OpenCv;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FaceLogger
{
    public partial class MainForm : Form
    {
        CaptureDevice camera;
        Bitmap bmp = new Bitmap(640,480);
        const string classifier = @"haarcascade_frontalface_default.xml";
        CascadeClassifier detector = new CascadeClassifier(classifier);
        Logger log = new Logger(@".\Logs");


        public MainForm()
        {
            InitializeComponent();
            InitializeCamera();
            timer1.Start();
        }

        private void InitializeCamera()
        {
            // すべてのキャプチャデバイスを取得
            VideoCapture capture = new VideoCapture();
            List<CaptureDevice> devices = capture.Devices;

            if (devices != null)
            {
                int selected = 0;
                if (devices.Count > 1)
                {
                    var names = from c in devices
                                select c.Name;
                    var form = new CameraSelectorForm(names.ToArray());
                    form.ShowDialog();
                    selected = form.SelectedIndex; 
                }
                camera = devices[selected];
                camera.Activate();
                this.Text = camera.Name;
            }
            else
            {
                MessageBox.Show("キャプチャデバイスが見つかりませんでした", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }
        }

        private int FaceDetector(Bitmap bmp)
        {
            int count = 0;
            if (detector.Loaded)
            {
                List<Rectangle> rects = detector.DetectMultiScale(bmp, 1.1, 3, new Size(100, 100), new Size(300, 300));
                count = rects.Count;
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    foreach (Rectangle r in rects)
                    {
                        g.DrawRectangle(Pens.Red, r);
                    }
                }
            }
            return count;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.timer1.Stop();

            camera.Capture(bmp);
            int count = FaceDetector(bmp);
            this.pictureBox1.Image = bmp;

            log.Add(count.ToString());

            this.timer1.Start();
        }
    }
}
