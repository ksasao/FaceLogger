using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceLogger
{
    public class Logger : IDisposable
    {
        string defaultPath = @".\Logs";
        string oldPath = "";
        StreamWriter writer = null;

        public string LogPath { get; private set; }

        public Logger()
        {
            Initialize(defaultPath);
        }
        public Logger(string path)
        {
            Initialize(path);
        }

        private void Initialize(string path)
        {
            this.LogPath = Path.GetFullPath(path) + @"\";
            if (!Directory.Exists(this.LogPath))
            {
                Directory.CreateDirectory(this.LogPath);
            }
        }

        public void Add(string message)
        {
            DateTime now = DateTime.Now;

            // 月が替わるごとにファイル名を変更
            string name = this.LogPath + now.ToString("yyyyMM") + ".csv";
            if (oldPath != name || writer == null)
            {
                if(writer != null){
                    writer.Close();
                    writer.Dispose();
                }
                oldPath = name;
                writer = new StreamWriter(name, true);
            }

            // 書き込み内容
            string timestamp = now.ToString("yyyy/MM/dd HH:mm:ss");
            string data = string.Format("{0},{1}", timestamp, message);

            writer.WriteLine(data);
           // writer.Flush();
        }

        public void Dispose()
        {
            if (writer != null)
            {
                writer.Close();
                writer.Dispose();
            }
        }
    }
}
