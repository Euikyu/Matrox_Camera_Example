using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Matrox_Camera_Example.Err;
using Matrox.MatroxImagingLibrary;
using System.Runtime.InteropServices;
using System.Threading;

namespace Matrox_Camera_Example.Device
{
    /// <summary>
    /// Matrox 보드 종류
    /// </summary>
    public enum EMatroxBoardType
    {
        M_SYSTEM_1394,
        M_SYSTEM_CRONOSPLUS,
        M_SYSTEM_DEFAULT,
        M_SYSTEM_GIGE_VISION,
        M_SYSTEM_GPU,
        M_SYSTEM_HOST,
        M_SYSTEM_IRIS_GT,
        M_SYSTEM_MORPHIS,
        M_SYSTEM_MORPHISQXT,
        M_SYSTEM_ORION_HD,
        M_SYSTEM_RADIENT,
        M_SYSTEM_RADIENTCLHS,
        M_SYSTEM_RADIENTCXP,
        M_SYSTEM_RADIENTEVCL,
        M_SYSTEM_RADIENTPRO,
        M_SYSTEM_SOLIOS,
        M_SYSTEM_USB3_VISION,
        M_SYSTEM_VIO,
        Other
    }

    /// <summary>
    /// Matrox 보드에 연결된 Camera Link 타입 카메라의 장치 제어 및 이미지를 취득하는 클래스입니다.
    /// </summary>
    public class MatroxCLCamDevice : ICamDevice
    {
        #region Fields
        private CrevisImage m_CrevisImage;
        private int m_HDevice;
        private int m_Width;
        private int m_Height;
        private int m_OriginX;
        private int m_OriginY;
        private MIL_ID m_System;
        private MIL_ID m_Digitizer;
        private MIL_ID m_MilImageBuffer;
        private bool m_IsGrabCancel;
        #endregion

        #region Properties
        /// <summary>
        /// 카메라 이미지의 원점 X좌표를 가져옵니다.
        /// </summary>
        public int OriginX
        {
            get { return m_OriginX; }
        }

        /// <summary>
        /// 카메라 이미지의 원점 Y좌표를 가져옵니다.
        /// </summary>
        public int OriginY
        {
            get { return m_OriginY; }
        }

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
        /// 카메라의 Image 묶음 개체를 가져옵니다.
        /// </summary>
        public CrevisImage CrevisImage
        {
            get { return m_CrevisImage; }
        }
        
        /// <summary>
        /// .DCF(Digitizer Camera File) 의 존재 여부를 가져옵니다.
        /// </summary>
        public bool ExistDCF
        {
            get
            {
                if (CamFilePath != null) return File.Exists(CamFilePath);
                else return false;
            }
        }

        /// <summary>
        /// 카메라의 지정된 Device 번호를 가져옵니다.
        /// </summary>
        public int HDevice
        {
            get { return m_HDevice; }
        }

        /// <summary>
        /// 카메라의 사용자 지정 이름을 가져오거나 설정합니다.
        /// </summary>
        public string UserID { get; set; }

        /// <summary>
        /// 카메라 보드의 종류를 가져옵니다.
        /// </summary>
        public EMatroxBoardType BoardType { get; }

        /// <summary>
        /// Raw Image의 포맷 형태를 가져옵니다.
        /// </summary>
        public string PixelFormat { get; }


        /// <summary>
        /// 카메라 활성화 여부를 가져옵니다.
        /// </summary>
        public bool IsOpened { get; private set; }

        /// <summary>
        /// 카메라 취득 준비 여부를 가져옵니다.
        /// </summary>
        public bool IsAcqStart { get; private set; }

        /// <summary>
        /// 클래스의 메모리 반환 여부를 가져옵니다.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// .DCF(Digitizer Camera File) 의 절대경로를 가져옵니다.
        /// </summary>
        public string CamFilePath { get; private set; }

        #endregion

        /// <summary>
        /// Matrox 보드에 연결된 Camera Link 타입 카메라의 장치 제어 및 이미지를 취득하는 클래스를 생성합니다.
        /// </summary>
        /// <param name="systemId">등록된 Matrox 보드 식별 번호.</param>
        /// <param name="digNum">보드에 연결된 카메라 번호.</param>
        /// <param name="boardType">카메라 보드의 종류.</param>
        /// <param name="camfilePath">.DCF 절대경로.</param>
        /// <param name="pixelFormat">Raw Image 포맷형태.</param>
        public MatroxCLCamDevice(MIL_ID systemId, int digNum, EMatroxBoardType boardType, string camfilePath, string pixelFormat = "Mono 8")
        {
            this.m_System = systemId;
            this.m_HDevice = digNum;
            this.BoardType = boardType;
            this.CamFilePath = camfilePath;
            this.PixelFormat = pixelFormat;
        }

        public void Dispose()
        {
            if(IsOpened) Close();
            CrevisImage.Dispose();

            m_HDevice = -1;
            m_Width = -1;
            m_Height = -1;

            UserID = null;
            CamFilePath = null;

            IsDisposed = true;
        }

        #region Methods
        /// <summary>
        /// 연결된 카메라를 활성화합니다. 
        /// </summary>
        /// <returns></returns>
        public ERR_RESULT Open()
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                if (IsDisposed) throw new CREVIS_CameraException(ErrProcess.CLASS_DISPOSED_ERR);
                if (!ExistDCF) throw new CREVIS_CameraException(ErrProcess.NOT_EXIST_DCF_ERR);
                if (IsOpened) throw new CREVIS_CameraException(ErrProcess.CAM_ALREADY_OPEN_ERR);

                //카메라 alloc
                MIL.MdigAlloc(m_System, m_HDevice, CamFilePath, MIL.M_DEFAULT, ref m_Digitizer);
                
                //카메라 정보 가져오기
                MIL.MdigInquire(m_Digitizer, MIL.M_SIZE_X, ref m_Width);
                MIL.MdigInquire(m_Digitizer, MIL.M_SIZE_Y, ref m_Height);
                MIL.MdigInquire(m_Digitizer, MIL.M_SOURCE_OFFSET_X, ref m_OriginX);
                MIL.MdigInquire(m_Digitizer, MIL.M_SOURCE_OFFSET_Y, ref m_OriginY);

                IsOpened = true;

                return m_Err;
            }
            catch(MILException err)
            {
                m_Err = ErrProcess.SetErrResult(err, ErrProcess.MIL_ERR, err.Message);
                return m_Err;
            }
            catch (CREVIS_CameraException err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
            catch (Exception err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
        }

        /// <summary>
        /// 활성화된 카메라를 비활성화합니다.
        /// </summary>
        /// <returns></returns>
        public ERR_RESULT Close()
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                if (IsDisposed) throw new CREVIS_CameraException(ErrProcess.CLASS_DISPOSED_ERR);
                if (!IsOpened) return m_Err;
                
                if (IsAcqStart)
                {
                    m_Err = AcqStop();

                    //AcqStop에서 에러 발생 시
                    if (m_Err.ErrCode != ErrProcess.ERR_SUCCESS) return m_Err;
                }

                //카메라가 등록되어있는 경우
                if (m_Digitizer != MIL.M_NULL)
                {
                    MIL.MdigFree(m_Digitizer);
                    m_Digitizer = MIL.M_NULL;
                }

                //보드가 등록되어있는 경우
                if (m_System != MIL.M_NULL)
                {
                    MIL.MsysFree(m_System);
                    m_System = MIL.M_NULL;
                }

                IsOpened = false;

                return m_Err;
            }
            catch (MILException err)
            {
                m_Err = ErrProcess.SetErrResult(err, ErrProcess.MIL_ERR, err.Message);
                return m_Err;
            }
            catch (CREVIS_CameraException err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
            catch (Exception err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
        }

        /// <summary>
        /// 카메라 취득 준비를 시작합니다.
        /// </summary>
        /// <returns></returns>
        public ERR_RESULT AcqStart()
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                if (IsDisposed) throw new CREVIS_CameraException(ErrProcess.CLASS_DISPOSED_ERR);
                if (!IsOpened) throw new CREVIS_CameraException(ErrProcess.CAM_NOT_OPEN_ERR);

                int tmpChannel = 0;
                //픽셀 형식에 따라 채널 설정
                if (PixelFormat.Contains("Mono"))
                {
                    tmpChannel = 1;
                }
                else if (PixelFormat.Contains("Bayer"))
                {
                    tmpChannel = 3;
                }
                else
                {
                    throw new CREVIS_CameraException(ErrProcess.CAM_NOT_SUPPORT_PIXEL_FORMAT);
                }
                //이미지 버퍼 등록
                MIL.MbufAllocColor(m_System, tmpChannel, Width, Height, 8, MIL.M_IMAGE + MIL.M_GRAB + MIL.M_PROC, ref m_MilImageBuffer);
                ////카메라 그랩 변수 초기화
                m_IsGrabCancel = false;

                IsAcqStart = true;

                return m_Err;
            }
            catch (MILException err)
            {
                m_Err = ErrProcess.SetErrResult(err, ErrProcess.MIL_ERR, err.Message);
                return m_Err;
            }
            catch (CREVIS_CameraException err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
            catch (Exception err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
        }

        /// <summary>
        /// 취득 준비 중인 카메라를 정지합니다.
        /// </summary>
        /// <returns></returns>
        public ERR_RESULT AcqStop()
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                if (IsDisposed) throw new CREVIS_CameraException(ErrProcess.CLASS_DISPOSED_ERR);
                if (!IsAcqStart) return m_Err;

                //그랩 중이면 취소
                m_IsGrabCancel = true;
                MIL.MdigControl(m_Digitizer, MIL.M_GRAB_ABORT, MIL.M_DEFAULT);
                MIL.MdigHalt(m_Digitizer);

                //이미지 버퍼 해제
                if (m_MilImageBuffer != MIL.M_NULL)
                {
                    MIL.MbufFree(m_MilImageBuffer);
                    m_MilImageBuffer = MIL.M_NULL;
                }

                IsAcqStart = false;

                return m_Err;
            }
            catch (MILException err)
            {
                m_Err = ErrProcess.SetErrResult(err, ErrProcess.MIL_ERR, err.Message);
                return m_Err;
            }
            catch (CREVIS_CameraException err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
            catch (Exception err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
        }

        /// <summary>
        /// 이미지를 취득합니다.
        /// </summary>
        /// <param name="option">이미지를 취득할 트리거 옵션.</param>
        /// <returns></returns>
        public ERR_RESULT Grab(ETriggerOption option)
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                if (IsDisposed) throw new CREVIS_CameraException(ErrProcess.CLASS_DISPOSED_ERR);
                if (!IsAcqStart) throw new CREVIS_CameraException(ErrProcess.CAM_NOT_START_ERR);
                
                MIL.MdigGrab(m_Digitizer, m_MilImageBuffer);

                if (m_IsGrabCancel) return m_Err;
                m_Err = CaptureCrevisImage();

                return m_Err;
            }
            catch (MILException err)
            {
                m_Err = ErrProcess.SetErrResult(err, ErrProcess.MIL_ERR, err.Message);
                return m_Err;
            }
            catch (CREVIS_CameraException err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
            catch (Exception err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
        }

        private ERR_RESULT CaptureCrevisImage()
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                byte[] rawImage = null;
                if (PixelFormat.Contains("Mono"))
                {
                    rawImage = new byte[Width * Height * 1];
                    //이미지 버퍼에 복사
                    MIL.MbufGet2d(m_MilImageBuffer, m_OriginX, m_OriginY, m_Width, m_Height, rawImage);
                }
                else if (PixelFormat.Contains("Bayer RG"))
                {
                    rawImage = new byte[Width * Height * 3];
                    //이미지 버퍼의 포맷 형식을 Bayer RG 로 변경 
                    MIL.MbufBayer(m_MilImageBuffer, m_MilImageBuffer, MIL.M_DEFAULT, MIL.M_BAYER_RG);

                    //이미지 버퍼에 복사
                    MIL.MbufGetColor2d(m_MilImageBuffer, MIL.M_PACKED + MIL.M_BGR24, MIL.M_ALL_BANDS, 0, 0, m_Width, m_Height, rawImage);
                }
                else if (PixelFormat.Contains("Bayer GR"))
                {
                    rawImage = new byte[Width * Height * 3];
                    //이미지 버퍼의 포맷 형식을 Bayer GR 로 변경 
                    MIL.MbufBayer(m_MilImageBuffer, m_MilImageBuffer, MIL.M_DEFAULT, MIL.M_BAYER_GR);

                    //이미지 버퍼에 복사
                    MIL.MbufGetColor2d(m_MilImageBuffer, MIL.M_PACKED + MIL.M_BGR24, MIL.M_ALL_BANDS, 0, 0, m_Width, m_Height, rawImage);
                }
                else if (PixelFormat.Contains("Bayer GB"))
                {
                    rawImage = new byte[Width * Height * 3];
                    //이미지 버퍼의 포맷 형식을 Bayer GB 로 변경 
                    MIL.MbufBayer(m_MilImageBuffer, m_MilImageBuffer, MIL.M_DEFAULT, MIL.M_BAYER_GB);

                    //이미지 버퍼에 복사
                    MIL.MbufGetColor2d(m_MilImageBuffer, MIL.M_PACKED + MIL.M_BGR24, MIL.M_ALL_BANDS, 0, 0, m_Width, m_Height, rawImage);
                }
                else if (PixelFormat.Contains("Bayer BG"))
                {
                    rawImage = new byte[Width * Height * 3];
                    //이미지 버퍼의 포맷 형식을 Bayer BG 로 변경 
                    MIL.MbufBayer(m_MilImageBuffer, m_MilImageBuffer, MIL.M_DEFAULT, MIL.M_BAYER_BG);

                    //이미지 버퍼에 복사
                    MIL.MbufGetColor2d(m_MilImageBuffer, MIL.M_PACKED + MIL.M_BGR24, MIL.M_ALL_BANDS, 0, 0, m_Width, m_Height, rawImage);
                }
                else
                {
                    throw new CREVIS_CameraException(ErrProcess.CAM_NOT_SUPPORT_PIXEL_FORMAT);
                }
                
                //크래비스 이미지 묶음 생성
                m_CrevisImage = new CrevisImage(m_Width, m_Height, rawImage, PixelFormat);

                return m_Err;
            }
            catch (MILException err)
            {
                m_Err = ErrProcess.SetErrResult(err, ErrProcess.MIL_ERR, err.Message);
                return m_Err;
            }
            catch (CREVIS_CameraException err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
            catch (Exception err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
        }

        public ERR_RESULT SoftwareTrigger()
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                MIL.MdigControl(m_Digitizer, MIL.M_TIMER_TRIGGER_SOFTWARE, MIL.M_ACTIVATE);
                MIL.MdigControl(m_Digitizer, MIL.M_GRAB_TRIGGER_SOFTWARE, MIL.M_ACTIVATE);

                return m_Err;
            }
            catch (MILException err)
            {
                m_Err = ErrProcess.SetErrResult(err, ErrProcess.MIL_ERR, err.Message);
                return m_Err;
            }
            catch (Exception err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
        }
        #endregion
    }
}
