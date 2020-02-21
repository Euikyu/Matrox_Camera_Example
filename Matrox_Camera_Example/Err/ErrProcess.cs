using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Matrox_Camera_Example.Err
{
    public delegate void ErrProcessEventHandler(ERR_RESULT result);
    public struct ERR_RESULT
    {
        public string FuncName;
        public short ErrCode;
        public short? InnerErrCode;
        public string Message;
        public string ErrTrace;
    };
    public class ErrProcess : Exception
    {
        #region Fields
        private short m_ErrCode;

        public const int ERR_SUCCESS = 0;
        public const int SYSTEM_ERR = -10;
        public const int CLASS_DISPOSED_ERR = -20;

        public const int MIL_ERR = -100;
        public const int NOT_EXIST_DCF_ERR = -101;
        public const int CAM_ALREADY_OPEN_ERR = -102;
        public const int CAM_NOT_OPEN_ERR = -103;
        public const int CAM_NOT_START_ERR = -104;
        public const int CAM_NOT_SUPPORT_PIXEL_FORMAT = -105;
        public const int CAM_OUT_OF_INDEX = -106;

        public const int XML_WRONG_PARSE_DATA = -201;
        public const int XML_NOT_EXIST_DATA = -202;
        #endregion

        #region Properties
        public short ErrCode
        {
            get { return m_ErrCode; }
        }
        #endregion

        #region Delegates
        public event ErrProcessEventHandler ActionCallback;
        #endregion

        public ErrProcess()
        {
            ActionCallback = null;
            m_ErrCode = -1;
        }

        public ErrProcess(short errCode)
        {
            ActionCallback = null;
            m_ErrCode = errCode;
        }

        #region StaticMethods
        public static ERR_RESULT SetErrResult(Exception err, short? innerErrCode = null, string userMsg = null)
        {
            ERR_RESULT result = new ERR_RESULT();
            string[] errTrace = new string[255];

            ExtractErrTrace(err.StackTrace, ref errTrace);
            int position = errTrace.Length;

            //에러코드와 메세지 설정
            if (!(err is ErrProcess ep)) // 예상 외의 에러 (미리 선언하지 않은 에러)
            {
                if (innerErrCode != null && userMsg != null) ep = new ErrProcess((short)innerErrCode);
                else ep = new ErrProcess(-10); // 재정의
                result.FuncName = errTrace[position - 2];
                result.ErrTrace = errTrace[position - 1];
                if (userMsg != null) result.Message = ep.GetErrMessage() + userMsg;
                else result.Message = ep.GetErrMessage() + err.Message;
                result.InnerErrCode = innerErrCode;
                result.ErrCode = ep.ErrCode;
            }
            else // 미리 정의한 에러
            {
                result.FuncName = errTrace[position - 2];
                result.ErrTrace = errTrace[position - 1];
                if (userMsg != null) result.Message = ep.GetErrMessage() + userMsg;
                else result.Message = ep.GetErrMessage();
                result.InnerErrCode = innerErrCode;
                result.ErrCode = ep.ErrCode;
            }
            return result;
        }

        private static void ExtractErrTrace(string errTrace, ref string[] extractTrace)
        {
            string[] token = new string[1] { Environment.NewLine };
            extractTrace = errTrace.Split(token, StringSplitOptions.RemoveEmptyEntries);
            int position = extractTrace.Length;
            string tmpStr = extractTrace[position - 1];
            token = new string[2] { " 위치: ", "파일 " };
            extractTrace = tmpStr.Split(token, StringSplitOptions.RemoveEmptyEntries);
        }
        #endregion

        #region Methods
        public void SetErrCall(ERR_RESULT err)
        {
            if (ActionCallback == null) return;
            ActionCallback(err);
        }
        public void ResetErr()
        {
            m_ErrCode = -1;
        }
        public string GetErrMessage()
        {
            string errMessage = string.Empty;
            switch (m_ErrCode)
            {
                case ERR_SUCCESS:
                    errMessage = "Success.";
                    break;
                // System Exception (Normal Exception)
                case SYSTEM_ERR:
                    errMessage = "System Err - ";
                    break;


                case CLASS_DISPOSED_ERR:
                    errMessage = "Common Err - This class is disposed.";
                    break;


                //Cam Exception
                case MIL_ERR:
                    errMessage = "CAM - MIL error : ";
                    break;
                case NOT_EXIST_DCF_ERR:
                    errMessage = "CAM - Not exists the DCF file.";
                    break;
                case CAM_ALREADY_OPEN_ERR:
                    errMessage = "CAM - Cam is already opened.";
                    break;
                case CAM_NOT_OPEN_ERR:
                    errMessage = "CAM - Cam wasn't opened.";
                    break;
                case CAM_NOT_START_ERR:
                    errMessage = "CAM - Cam didn't start.";
                    break;
                case CAM_NOT_SUPPORT_PIXEL_FORMAT:
                    errMessage = "CAM - Cam doesn't support this pixel format.";
                    break;
                case CAM_OUT_OF_INDEX:
                    errMessage = "CAM - Camera index (or user id) Not exists.";
                    break;


                //Xml Exception
                case XML_WRONG_PARSE_DATA:
                    errMessage = "XML - Wrong parsed data.";
                    break;
                case XML_NOT_EXIST_DATA:
                    errMessage = "XML - Device number (or board type) not exists.";
                    break;




                //정의하지 않거나 에러 초기화한 뒤 호출할 때
                default:
                    errMessage = "Not Defined Err.";
                    break;
            }
            return errMessage;
        }
        #endregion

    }
    #region Inherit ErrProcess
    public class CREVIS_CameraException : ErrProcess
    {
        public CREVIS_CameraException() : base()
        { }
        public CREVIS_CameraException(short errCode) : base(errCode)
        { }
    }
    public class CREVIS_XmlException : ErrProcess
    {
        public CREVIS_XmlException() : base()
        { }
        public CREVIS_XmlException(short errCode) : base(errCode)
        { }
    }
    #endregion
}
