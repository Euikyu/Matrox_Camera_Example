using Matrox_Camera_Example.Err;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Matrox_Camera_Example.Xml
{
    /// <summary>
    /// 여러 설정 값을 XML 형식으로 저장/불러오기 하는 클래스입니다.
    /// </summary>
    public class XmlParser : IDisposable
    {
        #region Fields
        private readonly string m_DefaultDirPath = AppDomain.CurrentDomain.BaseDirectory + @"\Config\";
        private XmlSerializer m_XmlSerializer;
        #endregion

        #region Properties
        /// <summary>
        /// 현재 다루는 데이터의 XML 타입을 가져옵니다. 
        /// </summary>
        public EXmlType XmlType { get; }
        
        /// <summary>
        /// XML에서 불러온 데이터를 가져옵니다.
        /// </summary>
        public ParsedData ParsedData { get; private set; }
        #endregion

        /// <summary>
        /// 여러 설정 값을 XML 형식으로 저장/불러오기 하는 클래스를 생성합니다.
        /// </summary>
        /// <param name="xmlType">다룰 데이터의 XML 타입.</param>
        public XmlParser(EXmlType xmlType)
        {
            this.XmlType = xmlType;
            switch (xmlType)
            {
                case EXmlType.MatroxBoardListData:
                    m_XmlSerializer = new XmlSerializer(typeof(MatroxBoardListData));
                    ParsedData = new MatroxBoardListData();
                    break;
            }
        }

        public void Dispose()
        {
            ParsedData = null;
            m_XmlSerializer = null;
        }

        #region Methods
        /// <summary>
        /// XML로부터 데이터를 불러옵니다.
        /// </summary>
        /// <returns></returns>
        public ERR_RESULT LoadXml()
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                if(!File.Exists(m_DefaultDirPath + XmlType.ToString() + ".xml"))
                {
                    throw new CREVIS_XmlException();
                }
                using (var sr = new StreamReader(m_DefaultDirPath + XmlType.ToString() + ".xml"))
                {
                    ParsedData = m_XmlSerializer.Deserialize(sr) as ParsedData;
                }

                return m_Err;
            }
            catch(CREVIS_XmlException)
            {
                m_Err = SaveXml();
                return m_Err;
            }
            catch(Exception err)
            {
                m_Err = ErrProcess.SetErrResult(err);
                return m_Err;
            }
        }

        /// <summary>
        /// 현재 데이터를 XML로 저장합니다.
        /// </summary>
        /// <returns></returns>
        public ERR_RESULT SaveXml()
        {
            ERR_RESULT m_Err = new ERR_RESULT();
            try
            {
                Directory.CreateDirectory(m_DefaultDirPath);
                using (var sw = new StreamWriter(m_DefaultDirPath + XmlType.ToString() + ".xml"))
                {
                    m_XmlSerializer.Serialize(sw, ParsedData);
                }

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

    #region Parse Data Classes
    /// <summary>
    /// 데이터의 XML 타입
    /// </summary>
    public enum EXmlType
    {
        MatroxBoardListData,
    }

    /// <summary>
    /// XML로 저장/불러오기 할 데이터 클래스입니다.
    /// </summary>
    public class ParsedData
    {
    }

    #region MatroxBoard
    /// <summary>
    /// XML로 저장/불러오기 할 데이터 중 Matrox 보드에 관한 클래스입니다.
    /// </summary>
    public class MatroxBoardListData : ParsedData
    {
        /// <summary>
        /// 저장된 Matrox 보드 리스트를 가져오거나 설정합니다.
        /// </summary>
        public List<MatroxBoardCamData> BoardList { get; set; }
    }
    /// <summary>
    /// Matrox 보드와 카메라 데이터 클래스입니다.
    /// </summary>
    public class MatroxBoardCamData
    {
        /// <summary>
        /// 보드의 종류를 가져오거나 설정합니다.
        /// </summary>
        public string BoardType { get; set; }
        /// <summary>
        /// 카메라의 지정된 Device 번호를 가져오거나 설정합니다.
        /// </summary>
        public int HDevice { get; set; }
        /// <summary>
        /// 카메라의 사용자 지정 이름을 가져오거나 설정합니다.
        /// </summary>
        public string UserID { get; set; }
        /// <summary>
        /// .DCF 의 절대경로를 가져오거나 설정합니다.
        /// </summary>
        public string DCFPath { get; set; }
        /// <summary>
        /// 카메라의 지정 픽셀 형식을 가져오거나 설정합니다.
        /// </summary>
        public string PixelFormat { get; set; }
    }
    #endregion


    #endregion
}
