//
// 静止画キャプチャ
//
// 2006/6/11 ささお
// http://tmp.junkbox.info/
//
// (謝辞)
// DirectX関連の大部分のソースコードは、
// 塚田浩二氏による 「USBカメラをC#で使おう」
// http://mobiquitous.com/programming/usbcamera.html
// を利用させていただいています。
//
using System;
using DShowNET;
using DShowNET.Device;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

namespace JUNKBOX.IO
{
    public class CaptureDevice
    {
        private string name_;       // キャプチャデバイス名
        private DsDevice device_;   // DsDevice名

        /// <summary>
        /// カメラ画像の幅(ピクセル)
        /// </summary>
        public int DeviceWidth { get; private set; }   // キャプチャデバイスで設定されている幅
        /// <summary>
        /// カメラ画像の高さ(ピクセル)
        /// </summary>
        public int DeviceHeight { get; private set; } // キャプチャデバイスで設定されている高さ
        private int width_;         // リサイズ後の幅
        private int height_;        // リサイズ後の高さ

        private int oldBufferSize;
        private byte[] buffer;
        /// <summary>
        /// キャプチャデバイス名
        /// </summary>
        public string Name
        {
            get { return name_; }
        }

        /// <summary>
        /// 各々のキャプチャデバイスを利用するためのクラス。
        /// 幅、高さはデフォルト値が設定されます。
        /// </summary>
        /// <param name="device">キャプチャデバイス</param>
        public CaptureDevice(DsDevice device)
        {
            device_ = device;
            name_ = device.Name;
        }

        /// <summary>
        /// キャプチャデバイスとして有効化する
        /// </summary>
        /// <param name="previewPanel">プレビューに利用するPanel</param>
        public void Activate()
        {
            InitVideo(device_.Mon);

            //フレームデータのサイズを設定
            width_ = DeviceWidth = videoInfoHeader_.BmiHeader.Width;
            height_ = DeviceHeight = videoInfoHeader_.BmiHeader.Height;
        }


        /// <summary>
        /// キャプチャデバイスとして有効化する
        /// </summary>
        /// <param name="previewPanel">プレビューに利用するPanel</param>
        /// <param name="width">指定した幅(pixel)</param>
        /// <param name="height">指定した高さ(pixel)</param>
        public void Activate(int width, int height)
        {
            InitVideo(device_.Mon);

            //フレームデータのサイズを設定
            DeviceWidth = videoInfoHeader_.BmiHeader.Width;
            DeviceHeight = videoInfoHeader_.BmiHeader.Height;

            // キャプチャ時のサイズを設定
            width_ = width;
            height_ = height;
        }

        /// <summary>
        /// 静止画をキャプチャする
        /// </summary>
        /// <returns>キャプチャしたBitmap(24bitRGB)</returns>
        public Bitmap Capture(Bitmap target)
        {
            if (sampleGrabber_ == null) return null;

            Bitmap bitmap;
            //フレームデータのサイズを取得
            int width = videoInfoHeader_.BmiHeader.Width;
            int height = videoInfoHeader_.BmiHeader.Height;

            //widthが4の倍数でない場合＆widthとheightの値が適正でない場合は終了．
            if (((width & 0x03) != 0) || (width < 32) || (width > 4096) || (height < 32) || (height > 4096))
                return null;

            // 画像バッファサイズを取得するため、IntPtr.Zeroを指定して空読み
            int bufferSize = 0;
            sampleGrabber_.GetCurrentBuffer(ref bufferSize, IntPtr.Zero);

            if (oldBufferSize != bufferSize)
            {
                buffer = new byte[bufferSize];
                oldBufferSize = bufferSize;
            }
            // buffer のアドレスを，メモリ空間内で固定してデータを取得
            GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            int addr = (int)gcHandle.AddrOfPinnedObject();
            sampleGrabber_.GetCurrentBuffer(ref bufferSize, (IntPtr)addr);

            // 画像を上下反転してBitmapへ取得するための調整
            //  stride(1ライン分のデータサイズ(byte)=width* 3(RGB))を設定．
            int stride = width * 3;
            addr += stride * (height - 1);

            // buffer に取得した画像データを，ビットマップデータに．
            bitmap = new Bitmap(width, height, -stride, PixelFormat.Format24bppRgb, (IntPtr)addr);
            gcHandle.Free();

            // リサイズしてコピー
            //            bitmap = new Bitmap(bitmap, width_, height_);
            using (Graphics g = Graphics.FromImage(target))
            {
                g.DrawImageUnscaled(bitmap, 0, 0);
            }
            //bitmap = new Bitmap(bitmap);
            return bitmap;
        }

        /// <summary>
        /// 画像キャプチャ用のバッファサイズを返します
        /// </summary>
        /// <returns>byte配列換算のバッファサイズ</returns>
        public int GetBufferSize()
        {
            if (sampleGrabber_ == null) return 0;
            int bufferSize = 0;
            sampleGrabber_.GetCurrentBuffer(ref bufferSize, IntPtr.Zero);
            return bufferSize;
        }
        public void CaptureToByteArray(byte[] buffer)
        {
            if (sampleGrabber_ == null) return;

            //フレームデータのサイズを取得
            int width = videoInfoHeader_.BmiHeader.Width;
            int height = videoInfoHeader_.BmiHeader.Height;

            // buffer のアドレスを，メモリ空間内で固定してデータを取得
            GCHandle gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            int addr = (int)gcHandle.AddrOfPinnedObject();
            int bufferSize = buffer.Length;
            sampleGrabber_.GetCurrentBuffer(ref bufferSize, (IntPtr)addr);

            gcHandle.Free();
        }

        #region DirectShow のための メンバ変数
        //ソースフィルタ
        private IBaseFilter captureFilter_;

        // GetInterfaces()メソッドで初期化されるメンバ変数
        // SetupGraph()メソッドで初期化されるメンバ変数
        private IGraphBuilder graphBuilder_; //基本的なフィルタグラフマネージャ
        private ICaptureGraphBuilder2 captureGraphBuilder_;//ビデオキャプチャ＆編集用のメソッドを備えたキャプチャグラフビルダ
        private IVideoWindow videoWindow_; //オーナーウィンドウの位置やサイズなどの設定用のインタフェース．
        private IMediaControl mediaControl_;//データのストリーミングの移動、ポーズ、停止などの処理用のインタフェース．
        private IMediaEventEx mediaEvent_; //DirectShowイベント処理用のインタフェース

        private ISampleGrabber sampleGrabber_; //フィルタグラフ内を通る個々のデータ取得用のインタフェース． 
        private IBaseFilter grabFilter_; //Grabber Filterのインタフェース．
        private VideoInfoHeader videoInfoHeader_;//ビデオイメージのフォーマットを記述する構造体

        // SetupViewWindow()メソッドで使用される定数
        private const int WS_CHILD = 0x40000000;		//VideoWindowの属性．
        private const int WS_CLIPCHILDREN = 0x02000000; //（Win32APIのCreateWindowExと同様のものを利用可能．）
        #endregion

        #region 各種デバイス設定

        /// <summary>
        /// DirectXの初期化
        /// </summary>
        /// <param name="moniker">モニカ</param>
        /// <param name="panel">表示画面用のパネル</param>
        /// <returns>成功した場合はtrue</returns>
        private bool InitVideo(System.Runtime.InteropServices.ComTypes.IMoniker moniker)
        {
            int result;

            try
            {
                //ビデオキャプチャデバイスをフィルタのインスタンスに対応させる．
                if (!CreateCaptureDevice(moniker)) return false;

                ////各種COM コンポーネントを呼び出し，インタフェースを取得する．
                if (!GetInterfaces()) return false;

                ////各種設定をする．
                if (!SetupGraph()) return false;

                ////キャプチャ開始する．
                result = mediaControl_.Run();

                if (result < 0) Marshal.ThrowExceptionForHR(result);

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("ビデオストリームを正常に読み込めません．\r\n" + e.ToString());
                throw;
            }
        }

        //ビデオキャプチャ用デバイスをフィルタのインスタンスに対応させる．
        private bool CreateCaptureDevice(System.Runtime.InteropServices.ComTypes.IMoniker moniker)
        {
            object captureObject = null;

            try
            {
                //キャプチャデバイス(device)とソースフィルタ(captureFilter)を対応付ける．
                Guid guidBF = typeof(IBaseFilter).GUID;
                moniker.BindToObject(null, null, ref guidBF, out captureObject);

                captureFilter_ = (IBaseFilter)captureObject;
                captureObject = null;
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("キャプチャデバイスを作成できません．\r\n" + e.ToString());
                return false;
            }
            finally
            {
                if (captureObject != null)
                {
                    Marshal.ReleaseComObject(captureObject);
                    captureObject = null;
                }
            }
        }

        //各種のCOM コンポーネントを作成し，インタフェースを取得する．
        private bool GetInterfaces()
        {
            Type comType = null;
            object comObject = null;

            try
            {
                //graphBuilderを作成．
                comType = Type.GetTypeFromCLSID(Clsid.FilterGraph);
                if (comType == null)
                    throw new NotImplementedException("DirectShowのFiterGraphオブジェクトが作成できません．");
                comObject = Activator.CreateInstance(comType);
                graphBuilder_ = (IGraphBuilder)comObject;
                comObject = null;


                //キャプチャグラフビルダ(captureGraphBuilder)を作成．
                comType = Type.GetTypeFromCLSID(Clsid.CaptureGraphBuilder2);
                comObject = Activator.CreateInstance(comType);
                captureGraphBuilder_ = (ICaptureGraphBuilder2)comObject;
                comObject = null;


                //サンプルグラバ(sampleGrabber)を作成
                comType = Type.GetTypeFromCLSID(Clsid.SampleGrabber);
                if (comType == null)
                    throw new NotImplementedException("DirectShowのSampleGrabberオブジェクトが作成できません．");
                comObject = Activator.CreateInstance(comType);
                sampleGrabber_ = (ISampleGrabber)comObject;
                comObject = null;

                //フィルタと関連付ける．
                grabFilter_ = (IBaseFilter)sampleGrabber_;

                //graphBuilderから，各種インタフェースを取得．
                mediaControl_ = (IMediaControl)graphBuilder_;
                videoWindow_ = (IVideoWindow)graphBuilder_;
                mediaEvent_ = (IMediaEventEx)graphBuilder_;

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("インタフェースの取得に失敗しました．\r\n" + e.ToString());
                return false;
            }

            finally
            {
                //COMリソースを解放
                if (comObject != null)
                {
                    Marshal.ReleaseComObject(comObject);
                    comObject = null;
                }
            }
        }

        //画像のプレビューや動画キャプチャ用の設定を行う．
        private bool SetupGraph()
        {
            int result;

            try
            {
                //captureGraphBuilder（キャプチャグラフビルダ）をgraphBuilder（フィルタグラフマネージャ）に追加．
                result = captureGraphBuilder_.SetFiltergraph(graphBuilder_);
                if (result < 0) Marshal.ThrowExceptionForHR(result);

                //captureFilter(ソースフィルタ)をgraphBuilder（フィルタグラフマネージャ）に追加．
                result = graphBuilder_.AddFilter(captureFilter_, "Video Capture Device");
                if (result < 0) Marshal.ThrowExceptionForHR(result);

                // キャプチャサイズ設定ダイアログの表示
                DsUtils.ShowCapPinDialog(captureGraphBuilder_, captureFilter_, IntPtr.Zero);

                //キャプチャするビデオデータのフォーマットを設定．
                AMMediaType amMediaType = new AMMediaType();
                amMediaType.majorType = MediaType.Video;
                amMediaType.subType = MediaSubType.RGB24;
                amMediaType.formatType = FormatType.VideoInfo;
                result = sampleGrabber_.SetMediaType(amMediaType);
                if (result < 0) Marshal.ThrowExceptionForHR(result);


                //grabFilter(変換フィルタ)をgraphBuilder（フィルタグラフマネージャ）に追加．
                result = graphBuilder_.AddFilter(grabFilter_, "Frame Grab Filter");
                if (result < 0) Marshal.ThrowExceptionForHR(result);


                // キャプチャフィルタをサンプルグラバーフィルタに接続する．
                // (画像処理用)
                Guid pinCategory;
                Guid mediaType;

                pinCategory = PinCategory.Capture;
                mediaType = MediaType.Video;
                result = captureGraphBuilder_.RenderStream(ref pinCategory, ref mediaType,
                    captureFilter_, null, grabFilter_);
                if (result < 0) Marshal.ThrowExceptionForHR(result);

                //フレームキャプチャの設定が完了したかを確認する．
                amMediaType = new AMMediaType();
                result = sampleGrabber_.GetConnectedMediaType(amMediaType);
                if (result < 0) Marshal.ThrowExceptionForHR(result);
                if ((amMediaType.formatType != FormatType.VideoInfo) || (amMediaType.formatPtr == IntPtr.Zero))
                    throw new NotSupportedException("キャプチャ(Grab)できないメディアフォーマットです．");

                //キャプチャするビデオデータのフォーマットから，videoInfoHeaderを作成する．
                videoInfoHeader_ =
                    (VideoInfoHeader)Marshal.PtrToStructure(amMediaType.formatPtr, typeof(VideoInfoHeader));
                Marshal.FreeCoTaskMem(amMediaType.formatPtr);
                amMediaType.formatPtr = IntPtr.Zero;

                //フィルタ内を通るサンプルをバッファにコピーするように指定する．
                result = sampleGrabber_.SetBufferSamples(true);

                //サンプルを一つ（1フレーム）受け取ったらフィルタを停止するように指定する．
                if (result == 0) result = sampleGrabber_.SetOneShot(false);

                //コールバック関数の利用を停止する．
                if (result == 0) result = sampleGrabber_.SetCallback(null, 0);
                if (result < 0) Marshal.ThrowExceptionForHR(result);

            }
            catch (Exception e)
            {
                MessageBox.Show("フィルターグラフの設定に失敗しました．" + e.ToString());
                return false;
            }

            return true;
        }

        #endregion

    }
}
