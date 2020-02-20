using Matrox_Camera_Example.Err;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Matrox_Camera_Example.Device
{
    public class CrevisImage : IDisposable
    {
        #region Fields
        private int m_Width;
        private int m_Height;
        private byte[] m_RawImage;
        private IntPtr m_PImage;
        private BitmapSource m_BitmapSourceImage;
        private Bitmap m_BitmapImage;
        private string m_PixelFormat;
        #endregion

        #region Properties
        /// <summary>
        /// 카메라 이미지의 넓이를 가져옵니다.
        /// </summary>
        public int Width
        {
            get { return m_Width; }
        }

        /// <summary>
        /// 카메라 이미지의 높이를 가져옵니다.
        /// </summary>
        public int Height
        {
            get { return m_Height; }
        }
        /// <summary>
        /// 카메라가 취득한 Raw Image를 가져옵니다.
        /// </summary>
        public byte[] RawImage
        {
            get { return m_RawImage; }
        }

        /// <summary>
        /// Raw Image를 IntPtr 형식 이미지로 변환하여 가져옵니다.
        /// </summary>
        public IntPtr PImage
        {
            get { return m_PImage; }
        }

        /// <summary>
        /// Raw Image를 BitmapSource 형식 이미지로 변환하여 가져옵니다.
        /// </summary>
        public BitmapSource BitmapSourceImage
        {
            get
            {
                if (m_BitmapSourceImage == null && m_PImage.ToInt64() != 0)
                {
                    ConvertImage();
                }
                return m_BitmapSourceImage;
            }
        }

        /// <summary>
        /// Raw Image를 Bitmap 형식 이미지로 변환하여 가져옵니다.
        /// </summary>
        public Bitmap BitmapImage
        {
            get
            {
                if (m_BitmapImage == null && m_PImage.ToInt64() != 0)
                {
                    ConvertImage();
                }
                return m_BitmapImage;
            }
        }
        /// <summary>
        /// Raw Image의 포맷 형태를 가져옵니다.
        /// </summary>
        public string PixelFormat
        {
            get { return m_PixelFormat; }
        }

        #endregion
        /// <summary>
        /// 이미지의 너비와 높이를 지정하여 임의의 흑백 이미지를 생성합니다.
        /// </summary>
        /// <param name="width">이미지의 너비.</param>
        /// <param name="height">이미지의 높이.</param>
        public CrevisImage(int width, int height)
        {
            this.m_Width = width;
            this.m_Height = height;
            this.m_RawImage = new byte[width * height];
            this.m_PImage = Marshal.AllocHGlobal(m_RawImage.Length);
            this.m_PixelFormat = "Mono 8";
        }
        /// <summary>
        /// Raw Image를 통해 이미지를 생성합니다.
        /// </summary>
        /// <param name="width">이미지의 너비.</param>
        /// <param name="height">이미지의 높이.</param>
        /// <param name="rawImage">생성할 이미지의 Raw Image.</param>
        /// <param name="pixelFormat">이미지 픽셀 형식.</param>
        public CrevisImage(int width, int height, byte[] rawImage, string pixelFormat = "Mono 8")
        {
            this.m_Width = width;
            this.m_Height = height;

            this.m_RawImage = rawImage.ToArray();
            this.m_PImage = Marshal.AllocHGlobal(rawImage.Length);
            Marshal.Copy(m_RawImage, 0, m_PImage, m_RawImage.Length);
            this.m_PixelFormat = pixelFormat;
        }

        /// <summary>
        /// Pointer를 통해 이미지를 생성합니다.
        /// </summary>
        /// <param name="width">이미지의 너비.</param>
        /// <param name="height">이미지의 높이.</param>
        /// <param name="pImage">생성할 이미지의 Pointer 객체.</param>
        /// <param name="pixelFormat">이미지 픽셀 형식.</param>
        public CrevisImage(int width, int height, IntPtr pImage, string pixelFormat = "Mono 8")
        {
            this.m_Width = width;
            this.m_Height = height;

            this.m_PImage = pImage;
            this.m_PixelFormat = pixelFormat;

            if (pixelFormat.Contains("Mono")) this.m_RawImage = new byte[width * height];
            else this.m_RawImage = new byte[width * height * 3];
            Marshal.Copy(pImage, m_RawImage, 0, m_RawImage.Length);
            //m_PImage = Marshal.AllocHGlobal(m_RawImage.Length);
            //Marshal.Copy(m_RawImage, 0, m_PImage, m_RawImage.Length);
        }

        ~CrevisImage()
        {
            Dispose();
        }

        public void Dispose()
        {
            m_Width = 0;
            m_Height = 0;
            if(BitmapImage != null) BitmapImage.Dispose();
            m_BitmapImage = null;
            m_BitmapSourceImage = null;
            Marshal.FreeHGlobal(PImage);
            m_PImage = IntPtr.Zero;
            m_RawImage = null;
            m_PixelFormat = null;
        }

        #region Methods
        private void ConvertImage()
        {
            try
            {
                if (PixelFormat.Contains("Mono"))
                {
                    int bpp = System.Windows.Media.PixelFormats.Gray8.BitsPerPixel;
                    int stride = (m_Width * bpp + 7) / 8;

                    var monoBmp = new Bitmap(m_Width, m_Height, stride, System.Drawing.Imaging.PixelFormat.Format8bppIndexed, m_PImage);
                    var bitmapData = monoBmp.LockBits(new Rectangle(0, 0, monoBmp.Width, monoBmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, monoBmp.PixelFormat);

                    m_BitmapSourceImage = BitmapSource.Create(
                            bitmapData.Width, bitmapData.Height,
                            monoBmp.HorizontalResolution, monoBmp.VerticalResolution,
                            System.Windows.Media.PixelFormats.Gray8, null,
                            bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

                    monoBmp.UnlockBits(bitmapData);
                    SetGrayscalePalette(monoBmp);
                    m_BitmapImage = monoBmp;
                    m_BitmapSourceImage.Freeze();
                }
                else if (PixelFormat.Contains("Bayer"))
                {
                    int bpp = System.Windows.Media.PixelFormats.Bgr24.BitsPerPixel;
                    int stride = (m_Width * bpp + 7) / 8;

                    var colorBmp = new Bitmap(m_Width, m_Height, stride, System.Drawing.Imaging.PixelFormat.Format24bppRgb, m_PImage);
                    var bitmapData = colorBmp.LockBits(new Rectangle(0, 0, colorBmp.Width, colorBmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, colorBmp.PixelFormat);

                    m_BitmapSourceImage = BitmapSource.Create(
                            bitmapData.Width, bitmapData.Height,
                            colorBmp.HorizontalResolution, colorBmp.VerticalResolution,
                            System.Windows.Media.PixelFormats.Bgr24, null,
                            bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

                    colorBmp.UnlockBits(bitmapData);
                    m_BitmapImage = colorBmp;
                    m_BitmapSourceImage.Freeze();
                }
            }
            catch(ArgumentException)
            {

            }
            catch(Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

        private void SetGrayscalePalette(System.Drawing.Bitmap bitmap)
        {
            System.Drawing.Imaging.ColorPalette GrayscalePalette = bitmap.Palette;

            for (int i = 0; i < 255; i++)
            {
                GrayscalePalette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
            }

            bitmap.Palette = GrayscalePalette;
        }
        #endregion
    }
}
