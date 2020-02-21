using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrox.MatroxImagingLibrary;
using Matrox_Camera_Example.Err;
using Matrox_Camera_Example.Xml;

namespace Matrox_Camera_Example.Device
{
    /// <summary>
    /// Matrox 보드에 연결된 Camera Link 타입 카메라를 다룰 수 있는 클래스입니다.
    /// </summary>
    public class MatroxCLCamera : ICrevisCamera
    {
        #region Fields
        private MIL_ID m_MilApplication;
        private List<MatroxCLCamDevice> m_CameraList;
        private XmlParser m_XmlParser;
        #endregion

        #region Properties
        /// <summary>
        /// 현재 활성화된 카메라 리스트를 가져옵니다.
        /// </summary>
        public IReadOnlyList<ICamDevice> CameraList
        {
            get { return m_CameraList; }            
        }
        #endregion

        /// <summary>
        /// Matrox 보드에 연결된 Camera Link 타입의 카메라를 다룰 클래스를 생성합니다.
        /// </summary>
        public MatroxCLCamera()
        {
            m_MilApplication = MIL.M_NULL;
            m_CameraList = new List<MatroxCLCamDevice>();
            m_XmlParser = new XmlParser(EXmlType.MatroxBoardListData);
        }
        
        public void Dispose()
        {
            Close();
            m_CameraList = null;
        }

        #region Methods
        /// <summary>
        /// 연결된 모든 카메라를 활성화합니다.
        /// </summary>
        /// <returns></returns>
        public ERR_RESULT Open()
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                m_Err = m_XmlParser.LoadXml();
                if (m_Err.ErrCode != ErrProcess.ERR_SUCCESS) return m_Err;

                //이미 열려있는 카메라는 닫고 다시 열기
                if (m_CameraList.Count > 0) Close();
                
                //MIL 오픈
                MIL.MappAlloc(MIL.M_NULL, MIL.M_DEFAULT, ref m_MilApplication);
                MIL.MappControl(m_MilApplication, MIL.M_ERROR, MIL.M_THROW_EXCEPTION);

                //설치된 보드 수 가져오기
                var insSysCount = MIL.MappInquire(MIL.M_INSTALLED_SYSTEM_COUNT);
                for (int i = 0; i < insSysCount; i++)
                {
                    MIL_ID tmpSystem = MIL.M_NULL;
                    StringBuilder sb = new StringBuilder();

                    //보드 종류 문자열로 뽑아내기
                    MIL.MappInquire(MIL.M_INSTALLED_SYSTEM_DESCRIPTOR + i, sb);

                    var devCount = 0;
                    //같은 종류의 보드 몇 개까지 존재하는지 확인
                    while (sb.ToString() != EMatroxBoardType.M_SYSTEM_HOST.ToString())
                    {
                        MIL_ID systemId = MIL.M_NULL;
                        try
                        {
                            //보드 alloc
                            MIL.MsysAlloc(sb.ToString(), devCount, MIL.M_DEFAULT, ref systemId);
                        }
                        catch
                        {
                            break;
                        }
                        var digCount = MIL.MsysInquire(systemId, MIL.M_DIGITIZER_NUM);
                        //Digitizer 몇개 존재하는지 확인
                        for (int ii = 0; ii < digCount; ii++)
                        {
                            MatroxCLCamDevice cam = null;
                            MatroxBoardCamData board = null;

                            //xml에서 정보 가져오기
                            if (m_XmlParser.ParsedData is MatroxBoardListData boardListData)
                            {
                                //xml에 해당 정보 있는지 확인
                                if (boardListData.BoardList.Any(item => item.BoardType.Equals(sb.ToString()) && item.HDevice.Equals(ii)))
                                {
                                    //있으면 그 중 하나 선택
                                    var tmpBoard = boardListData.BoardList.Where(item => item.BoardType.Equals(sb.ToString()) && item.HDevice.Equals(ii));
                                    if (tmpBoard.Count() != 1)
                                    {
                                        //중복된 것 있으면 그 중 하나만 남기고 다 지우기
                                        boardListData.BoardList.RemoveAll(item => item.BoardType.Equals(sb.ToString()) && item.HDevice.Equals(ii));
                                        boardListData.BoardList.Add(tmpBoard.First());
                                        m_XmlParser.SaveXml();
                                    }
                                    board = tmpBoard.First();
                                }
                                else
                                {
                                    //없으면 데이터 새로 생성하여 추가
                                    board = new MatroxBoardCamData
                                    {
                                        BoardType = sb.ToString(),
                                        HDevice = ii,
                                        //tnwjdtnwjd119 : 여기는 다시 수정할 것, default 캠파일 불러오는 것으로
                                        DCFPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Matrox_MC-A500x-163_8TAP_8bit_CC1_HWTRIG.dcf",
                                        PixelFormat = "Mono 8",
                                        UserID = Guid.NewGuid().ToString()
                                    };
                                    boardListData.BoardList.Add(board);
                                    m_XmlParser.SaveXml();
                                }
                                //string dcfPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Matrox_MC-A500x-163_8TAP_8bit_CC1.dcf";
                                //string dcfPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Matrox_MC-A500x-163_8TAP_8bit_CC1_HWTRIG.dcf";
                                //string dcfPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Matrox_MC-A500x-163_8TAP_8bit_CC1_CON.dcf";

                                //cam = new MatroxCLCamDevice(systemId, ii, (EMatroxBoardType)Enum.Parse(typeof(EMatroxBoardType), sb.ToString()), dcfPath, "Mono 8");
                                
                                //카메라 오픈 및 활성 카메라 리스트에 추가
                                cam = new MatroxCLCamDevice(systemId, board.HDevice, (EMatroxBoardType)Enum.Parse(typeof(EMatroxBoardType), board.BoardType), board.DCFPath, board.PixelFormat);
                                cam.UserID = board.UserID;
                                m_Err = cam.Open();
                                if (m_Err.ErrCode != ErrProcess.ERR_SUCCESS) return m_Err;
                                m_CameraList.Add(cam);
                            }
                            else
                            {
                                throw new CREVIS_XmlException(ErrProcess.XML_WRONG_PARSE_DATA);
                            }
                        }
                        devCount++;
                    }
                }

                m_Err = m_XmlParser.SaveXml();
                if (m_Err.ErrCode != ErrProcess.ERR_SUCCESS) return m_Err;

                return m_Err;
            }
            catch (MILException err)
            {
                m_Err = ErrProcess.SetErrResult(err, ErrProcess.MIL_ERR, err.Message);
                return m_Err;
            }
            catch(CREVIS_XmlException err)
            {
                m_Err = ErrProcess.SetErrResult(err);
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
        /// 활성화된 카메라를 모두 비활성화합니다.
        /// </summary>
        /// <returns></returns>  
        public ERR_RESULT Close()
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                if (m_CameraList.Count == 0) return m_Err;
                foreach(var cam in m_CameraList)
                {
                    m_Err = cam.Close();
                    if (m_Err.ErrCode != ErrProcess.ERR_SUCCESS) return m_Err;
                }
                m_CameraList.Clear();

                // Free the application.
                if (m_MilApplication != MIL.M_NULL)
                {
                    MIL.MappFree(m_MilApplication);
                    m_MilApplication = MIL.M_NULL;
                }

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
        /// 활성화된 모든 카메라에 이미지를 취득할 준비를 시킵니다.
        /// </summary>
        /// <returns></returns>
        public ERR_RESULT AcqStart()
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                foreach(var cam in m_CameraList)
                {
                    m_Err = cam.AcqStart();
                    if (m_Err.ErrCode != ErrProcess.ERR_SUCCESS) return m_Err;
                }
                return m_Err;
            }
            catch (Exception err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
        }
        /// <summary>
        /// 특정 인덱스에 해당하는 활성화된 카메라에 이미지를 취득할 준비를 시킵니다.
        /// </summary>
        /// <param name="idx">취득준비시킬 카메라의 인덱스.</param>
        /// <returns></returns>
        public ERR_RESULT AcqStart(int idx)
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                if (idx >= m_CameraList.Count) throw new CREVIS_CameraException(ErrProcess.CAM_OUT_OF_INDEX);
                var cam = m_CameraList[idx];

                m_Err = cam.AcqStart();

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
        /// 특정 사용자 지정 이름에 해당하는 활성화된 카메라에 이미지를 취득할 준비를 시킵니다.
        /// </summary>
        /// <param name="userID">취득준비시킬 카메라의 사용자 지정 이름.</param>
        /// <returns></returns>
        public ERR_RESULT AcqStart(string userID)
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                var tmpCamList = m_CameraList.Where(c => c.UserID.Equals(userID));
                if (tmpCamList.Count() != 1) throw new CREVIS_CameraException(ErrProcess.CAM_OUT_OF_INDEX);
                var cam = tmpCamList.First();

                m_Err = cam.AcqStart();

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
        /// 이미지 취득 준비 중인 모든 카메라를 정지시킵니다.
        /// </summary>
        /// <returns></returns>
        public ERR_RESULT AcqStop()
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                foreach (var cam in m_CameraList)
                {
                    m_Err = cam.AcqStop();
                    if (m_Err.ErrCode != ErrProcess.ERR_SUCCESS) return m_Err;
                }
                return m_Err;
            }
            catch (Exception err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
        }
        /// <summary>
        /// 특정 인덱스에 해당하는 이미지 취득 준비 중인 카메라를 정지시킵니다.
        /// </summary>
        /// <param name="idx">정지할 카메라의 인덱스.</param>
        /// <returns></returns>
        public ERR_RESULT AcqStop(int idx)
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                if (idx >= m_CameraList.Count) throw new CREVIS_CameraException(ErrProcess.CAM_OUT_OF_INDEX);
                var cam = m_CameraList[idx];

                m_Err = cam.AcqStop();

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
        /// 특정 사용자 지정 이름에 해당하는 이미지 취득 준비 중인 카메라를 정지시킵니다.
        /// </summary>
        /// <param name="userID">정지할 카메라의 사용자 지정 이름.</param>
        /// <returns></returns>
        public ERR_RESULT AcqStop(string userID)
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                var tmpCamList = m_CameraList.Where(c => c.UserID.Equals(userID));
                if (tmpCamList.Count() != 1) throw new CREVIS_CameraException(ErrProcess.CAM_OUT_OF_INDEX);
                var cam = tmpCamList.First();

                m_Err = cam.AcqStop();

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
        /// 모든 카메라를 재연결합니다.
        /// </summary>
        /// <returns></returns>
        public ERR_RESULT Refresh()
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                var lastRunCamList = new List<MatroxCLCamDevice>(m_CameraList);
                Close();
                m_Err = Open();
                if (lastRunCamList.Count == m_CameraList.Count)
                {
                    for (int i = 0; i < lastRunCamList.Count; i++)
                    {
                        if (lastRunCamList[i].IsAcqStart) AcqStart(i);
                    }
                }

                return m_Err;
            }
            catch (Exception err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
        }

        /// <summary>
        /// 모든 카메라에서 이미지를 취득합니다.
        /// </summary>
        /// <param name="option">이미지를 취득할 트리거 옵션.</param>
        /// <returns></returns>
        public ERR_RESULT Grab(ETriggerOption option)
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                foreach (var cam in m_CameraList)
                {
                    m_Err = cam.Grab(option);
                    if (m_Err.ErrCode != ErrProcess.ERR_SUCCESS) return m_Err;
                }
                return m_Err;
            }
            catch (Exception err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
        }
        /// <summary>
        /// 특정 인덱스에 해당하는 카메라에서 이미지를 취득합니다.
        /// </summary>
        /// <param name="idx">그랩할 카메라의 인덱스.</param>
        /// <param name="option">이미지를 취득할 트리거 옵션.</param>
        /// <returns></returns>
        public ERR_RESULT Grab(int idx, ETriggerOption option)
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                if (idx >= m_CameraList.Count) throw new CREVIS_CameraException(ErrProcess.CAM_OUT_OF_INDEX);
                var cam = m_CameraList[idx];

                m_Err = cam.Grab(option);

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
        /// 특정 사용자 지정 이름에 해당하는 카메라에서 이미지를 취득합니다.
        /// </summary>
        /// <param name="userID">그랩할 카메라의 사용자 지정 이름.</param>
        /// <param name="option">이미지를 취득할 트리거 옵션.</param>
        /// <returns></returns>
        public ERR_RESULT Grab(string userID, ETriggerOption option)
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                var tmpCamList = m_CameraList.Where(c => c.UserID.Equals(userID));
                if (tmpCamList.Count() != 1) throw new CREVIS_CameraException(ErrProcess.CAM_OUT_OF_INDEX);
                var cam = tmpCamList.First();

                m_Err = cam.Grab(option);

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
        /// 보드 종류와 카메라 지정 번호에 해당하는 카메라를 찾아 .dcf 파일 경로를 교체합니다.
        /// </summary>
        /// <param name="boardType">Matrox 보드 종류.</param>
        /// <param name="hDevice">카메라 지정 device 번호.</param>
        /// <param name="dcfPath">.dcf 파일 경로.</param>
        /// <returns></returns>
        public ERR_RESULT Replace(string boardType, int hDevice, string dcfPath)
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                if (!File.Exists(dcfPath)) throw new CREVIS_CameraException(ErrProcess.NOT_EXIST_DCF_ERR);
                if(m_XmlParser.ParsedData is MatroxBoardListData boardListData)
                {
                    //xml에 해당 정보 있는지 확인
                    if (boardListData.BoardList.Any(item => item.BoardType.Equals(boardType) && item.HDevice.Equals(hDevice)))
                    {
                        //있으면 그 중 하나 선택
                        var tmpBoard = boardListData.BoardList.Where(item => item.BoardType.Equals(boardType) && item.HDevice.Equals(hDevice));
                        if (tmpBoard.Count() != 1)
                        {
                            //중복된 것 있으면 그 중 하나만 남기고 다 지우기
                            boardListData.BoardList.RemoveAll(item => item.BoardType.Equals(boardType) && item.HDevice.Equals(hDevice));
                            boardListData.BoardList.Add(tmpBoard.First());
                        }
                        //dcf 경로 수정
                        boardListData.BoardList.Single(item => item.BoardType.Equals(boardType) && item.HDevice.Equals(hDevice)).DCFPath = dcfPath;
                        m_XmlParser.SaveXml();
                    }
                    else
                    {
                        throw new CREVIS_XmlException(ErrProcess.XML_NOT_EXIST_DATA);
                    }
                }
                else
                {
                    throw new CREVIS_XmlException(ErrProcess.XML_WRONG_PARSE_DATA);
                }
                m_Err = Refresh();

                return m_Err;
            }
            catch(CREVIS_CameraException err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
            catch(CREVIS_XmlException err)
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
        #endregion
    }
}
