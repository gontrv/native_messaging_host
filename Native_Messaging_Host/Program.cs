using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ExplorerByChrome;
using Newtonsoft;
using Newtonsoft.Json;
using System.Net;
using System.Diagnostics;
using System.Threading;

namespace Native_Messaging_Host
{
    class Program
    {
        static string inputData = null;
        static List<Macros> _macrosList = null;
        static int _macrosCount;
        static List<Point> _endPointList = new List<Point>();
        const string PATH_TO_FILE_STATISTIC = @".\statistic.txt";

        static void Main(string[] args)
        {
            MacrosStorage storage = new MacrosStorage();
            
            if (storage.ReadStorage())
            {
                _macrosList = storage.MacrosList;
                _macrosCount = storage.MacrosList.Count;
                foreach (Macros m in _macrosList)
                {
                    Output(MacrosWrapper(m));
                    _endPointList.Add(m.Points[m.Points.Count - 1]);
                }
            }
            else
            {
                throw new Exception("Can't read macros storage, or macros storage file is empty");
            }

            ListenInput();
        }

        private static void ListenInput()
        {
            while ((inputData = Input()) != null)
            {
                ChromeData chromeData = ConvertToChromeData(inputData);
                CheckMacrosExecution(chromeData);
            }
        }

        static string Input()
        {
            int length = 0;
            string input = "";
            byte[] bytes = new byte[4];

            using (Stream stream = Console.OpenStandardInput())
            {
                stream.Read(bytes, 0, 4);
                length = System.BitConverter.ToInt32(bytes, 0);

                for (int i = 0; i < length; i++)
                {
                    input += (char)stream.ReadByte();
                }
            }
            return input;
        }

        static void Output(string jsonStr)
        {
            int dataLength = jsonStr.Length;

            using (Stream stream = Console.OpenStandardOutput())
            {
                stream.WriteByte((byte)((dataLength >> 0) & 0xFF));
                stream.WriteByte((byte)((dataLength >> 8) & 0xFF));
                stream.WriteByte((byte)((dataLength >> 16) & 0xFF));
                stream.WriteByte((byte)((dataLength >> 24) & 0xFF));

                Console.Write(jsonStr);
            }
        }

        #region Additional Methods

        private static ChromeData ConvertToChromeData(string inputData)
        {
            return JsonConvert.DeserializeObject<ChromeData>(inputData);
        }

        private static int GetPointPosition(ChromeData pointStatus)
        {
            return GetMacrosOfPoint(pointStatus).Points.IndexOf(pointStatus.Point);
        }

        private static Macros GetMacrosOfPoint(ChromeData pointStatus)
        {
            return _macrosList.Find(m => m.Name == pointStatus.Point.Name.Substring(0, pointStatus.Point.Name.IndexOf(".")));
        }

        private static string GetMacrosName(ChromeData pointStatus)
        {
            return GetMacrosOfPoint(pointStatus).Name;
        }

        private static string GetProxyServerName()
        {
            return "127.0.0.1";
        }

        private static string MacrosWrapper(Macros macros)
        {
            return new MacrosWrapper(macros).ToString();
        }

        private static void CheckMacrosExecution(ChromeData pointStatus)
        {
            try
            {
                bool isEndPointInMacros = false;
                foreach (var p in _endPointList)
                    isEndPointInMacros = p.Name == pointStatus.Point.Name;

                if (isEndPointInMacros || !pointStatus.Complete) _macrosCount--;
                if (_macrosCount == 0)
                {

                    Process[] chromeProcesses = Process.GetProcessesByName("chrome");
                    Thread.Sleep(12000);
                    foreach (Process p in chromeProcesses)
                        p.Kill();
                }
            }
            catch (Exception)
            {
                Process.GetCurrentProcess().Kill();
            }
        }

        /*
                private bool SendStatisctic(string macrosName, ChromeData data)
                {
                    WebRequest request = WebRequest.Create("data-publishing.in.ua/savestatistic");
                }
         */
        #endregion
    }

    class MacrosWrapper
    {
        public string message { get; set; }
        public Macros macros { get; set; }

        public MacrosWrapper(Macros macros)
        {
            this.message = "macros";
            this.macros = macros;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
