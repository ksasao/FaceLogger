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
using System.Collections.Generic;
using System.Text;
using DShowNET;
using DShowNET.Device;
using System.Collections;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;

namespace JUNKBOX.IO
{
    /// <summary>
    /// ビデオキャプチャ可能なDirectXデバイスを扱うクラス
    /// </summary>
    public class VideoCapture
    {
        List<CaptureDevice> devices_;

        /// <summary>
        /// 列挙されたキャプチャ可能デバイス
        /// </summary>
        public List<CaptureDevice> Devices
        {
            get { return devices_; }
        }

        /// <summary>
        /// キャプチャに利用可能なデバイスを検索する
        /// </summary>
        public VideoCapture()
        {
            devices_ = FindDevices();
        }

        /// <summary>
        /// キャプチャデバイスの列挙
        /// </summary>
        private List<CaptureDevice> FindDevices()
        {
            //DirectX ver 8.1以上が正常にインストールされているか？
            if (!DsUtils.IsCorrectDirectXVersion())
            {
                MessageBox.Show("アプリケーションの動作にはDirectX ver 8.1以上が必要です．");
                return null;
            }

            //PCに接続されているキャプチャデバイス（USBカメラなど)のリストを取得．
            ArrayList captureDevices;
            if (!DsDev.GetDevicesOfCat(FilterCategory.VideoInputDevice, out captureDevices))
            {
                MessageBox.Show("ビデオキャプチャ可能なデバイスが見つかりません");
                return null;
            }

            List<CaptureDevice> devices = new List<CaptureDevice>();

            // デバイスを列挙
            foreach (DsDevice device in captureDevices)
            {
                if (device != null)
                {
                    CaptureDevice cap = new CaptureDevice(device);
                    devices.Add(cap);
                }
            }
            if (devices.Count == 0) return null;

            return devices;
        }

        /// <summary>
        /// Bitmapをbyte[]に変換する
        /// </summary>
        /// <param name="bitmap">変換元のBitmap</param>
        /// <returns>1 pixel = 4 byte (+3:A, +2:R, +1:G, +0:B) に変換したbyte配列</returns>
        public static byte[] BitmapToByteArray(Bitmap bmp)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            // Bitmapの先頭アドレスを取得
            IntPtr ptr = bmpData.Scan0;

            // 32bppArgbフォーマットで値を格納
            int bytes = bmp.Width * bmp.Height * 4;
            byte[] rgbValues = new byte[bytes];

            // Bitmapをbyte[]へコピー
            Marshal.Copy(ptr, rgbValues, 0, bytes);

            bmp.UnlockBits(bmpData);
            return rgbValues;
        }

        /// <summary>
        /// byte[]をBitmapに変換する
        /// </summary>
        /// <param name="byteArray">1 pixel = 4 byte (+3:A, +2:R, +1:G, +0:B) に変換したbyte配列</param>
        /// <param name="bmp">変換先のBitmap</param>
        /// <returns>変換先のBitmap</returns>
        public static Bitmap ByteArrayToBitmap(byte[] rgbValues, Bitmap bmp)
        {
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            // Bitmapの先頭アドレスを取得
            IntPtr ptr = bmpData.Scan0;

            // Bitmapへコピー
            Marshal.Copy(rgbValues, 0, ptr, rgbValues.Length);

            bmp.UnlockBits(bmpData);

            return bmp;
        }
    }
}
