using Matrox_Camera_Example.Err;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrox_Camera_Example.Device
{
    /// <summary>
    /// 카메라 트리거 옵션 종류
    /// </summary>
    public enum ETriggerOption
    {
        Continuous,
        Software,
        Hardware
    }

    /// <summary>
    /// 크래비스에서 제공하는 카메라를 다룰 수 있는 인터페이스입니다.
    /// </summary>
    public interface ICrevisCamera : IDisposable
    {
        #region Properties
        /// <summary>
        /// 현재 활성화된 카메라 리스트를 가져옵니다.
        /// </summary>
        IReadOnlyList<ICamDevice> CameraList { get; }
        #endregion

        #region Methods
        /// <summary>
        /// 연결된 모든 카메라를 활성화합니다.
        /// </summary>
        /// <returns></returns>
        ERR_RESULT Open();

        /// <summary>
        /// 활성화된 카메라를 모두 비활성화합니다.
        /// </summary>
        /// <returns></returns>        
        ERR_RESULT Close();     
        
        /// <summary>
        /// 활성화된 모든 카메라에 이미지를 취득할 준비를 시킵니다.
        /// </summary>
        /// <returns></returns>
        ERR_RESULT AcqStart();        
        /// <summary>
        /// 특정 인덱스에 해당하는 활성화된 카메라에 이미지를 취득할 준비를 시킵니다.
        /// </summary>
        /// <param name="idx">취득준비시킬 카메라의 인덱스.</param>
        /// <returns></returns>
        ERR_RESULT AcqStart(int idx);
        /// <summary>
        /// 특정 사용자 지정 이름에 해당하는 활성화된 카메라에 이미지를 취득할 준비를 시킵니다.
        /// </summary>
        /// <param name="userID">취득준비시킬 카메라의 사용자 지정 이름.</param>
        /// <returns></returns>
        ERR_RESULT AcqStart(string userID);
        
        /// <summary>
        /// 이미지 취득 준비 중인 모든 카메라를 정지시킵니다.
        /// </summary>
        /// <returns></returns>
        ERR_RESULT AcqStop();        
        /// <summary>
        /// 특정 인덱스에 해당하는 이미지 취득 준비 중인 카메라를 정지시킵니다.
        /// </summary>
        /// <param name="idx">정지할 카메라의 인덱스.</param>
        /// <returns></returns>
        ERR_RESULT AcqStop(int idx);
        /// <summary>
        /// 특정 사용자 지정 이름에 해당하는 이미지 취득 준비 중인 카메라를 정지시킵니다.
        /// </summary>
        /// <param name="userID">정지할 카메라의 사용자 지정 이름.</param>
        /// <returns></returns>
        ERR_RESULT AcqStop(string userID);
        
        /// <summary>
        /// 모든 카메라를 재연결합니다.
        /// </summary>
        /// <returns></returns>
        ERR_RESULT Refresh();
        
        /// <summary>
        /// 모든 카메라에서 이미지를 취득합니다.
        /// </summary>
        /// <param name="option">이미지를 취득할 트리거 옵션.</param>
        /// <returns></returns>
        ERR_RESULT Grab(ETriggerOption option);
        /// <summary>
        /// 특정 인덱스에 해당하는 카메라에서 이미지를 취득합니다.
        /// </summary>
        /// <param name="idx">그랩할 카메라의 인덱스.</param>
        /// <param name="option">이미지를 취득할 트리거 옵션.</param>
        /// <returns></returns>
        ERR_RESULT Grab(int idx, ETriggerOption option);
        /// <summary>
        /// 특정 사용자 지정 이름에 해당하는 카메라에서 이미지를 취득합니다.
        /// </summary>
        /// <param name="userID">그랩할 카메라의 사용자 지정 이름.</param>
        /// <param name="option">이미지를 취득할 트리거 옵션.</param>
        /// <returns></returns>
        ERR_RESULT Grab(string userID, ETriggerOption option);
        #endregion
    }
}
