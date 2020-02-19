using Matrox_Camera_Example.Err;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Matrox_Camera_Example.Device
{
    /// <summary>
    /// 크래비스에서 제공하는 카메라의 장치 제어 및 이미지를 취득할 수 있는 인터페이스입니다.
    /// </summary>
    public interface ICamDevice : IDisposable
    {
        #region Properties
        /// <summary>
        /// 카메라의 사용자 지정 이름을 가져오거나 설정합니다.
        /// </summary>
        string UserID { get; set; }

        /// <summary>
        /// 카메라 H코드를 가져옵니다.
        /// </summary>
        int HDevice { get; }

        /// <summary>
        /// 카메라 활성화 여부를 가져옵니다.
        /// </summary>
        bool IsOpened { get; }

        /// <summary>
        /// 카메라 취득 준비 여부를 가져옵니다.
        /// </summary>
        bool IsAcqStart { get; }

        /// <summary>
        /// 클래스의 메모리 반환 여부를 가져옵니다.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// 카메라의 Image 묶음 개체를 가져옵니다.
        /// </summary>
        CrevisImage CrevisImage { get; }

        /// <summary>
        /// 이미지의 가로 값을 가져옵니다.
        /// </summary>
        int Width { get; }

        /// <summary>
        /// 이미지의 세로 값을 가져옵니다.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Raw Image의 포맷 형태를 가져옵니다.
        /// </summary>
        string PixelFormat { get; }


        #endregion

        #region Methods
        /// <summary>
        /// 연결된 카메라를 활성화합니다.
        /// </summary>
        /// <returns></returns>
        ERR_RESULT Open();
        
        /// <summary>
        /// 활성화된 카메라를 비활성화합니다.
        /// </summary>
        /// <returns></returns>
        ERR_RESULT Close();
        
        /// <summary>
        /// 카메라 취득 준비를 시작합니다.
        /// </summary>
        /// <returns></returns>
        ERR_RESULT AcqStart();
        
        /// <summary>
        /// 취득 준비 중인 카메라를 정지합니다.
        /// </summary>
        /// <returns></returns>
        ERR_RESULT AcqStop();
        
        /// <summary>
        /// 이미지를 취득합니다.
        /// </summary>
        /// <param name="option">이미지를 취득할 트리거 옵션.</param>
        /// <returns></returns>
        ERR_RESULT Grab(ETriggerOption option);

        #endregion
    }
}
