using System;
using System.IO;
using System.Windows.Forms;

namespace PortForward
{
    class Program
    {

        static void Main(string[] args)
        {
            var cmd = new Commond { OnWriteLine = Console.WriteLine };

            cmd.PrintHelp();



            //通过文件名获取配置
            string configString = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
            //configString = "PortForwardClient--authToken--192.168.3.162--6203--192.168.3.162--3389--2";

            try
            {
                cmd.Call(configString);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"出错：" + ex.GetBaseException().Message);
            }

            
          
            
            Console.ReadLine();
        }



    }
}
