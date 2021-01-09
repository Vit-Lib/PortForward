using System;
using System.Threading;

namespace PortForward
{
    class Program
    {

        static void Main(string[] args)
        {
            var cmd = new Commond { OnWriteLine = Console.WriteLine };

            cmd.PrintHelp();
           

            try
            {
                //通过文件名获取配置
                string configString = args[0];
                //configString = "PortForwardClient--authToken--192.168.3.162--6203--192.168.3.162--3389--2";

                cmd.Call(configString);

                new AutoResetEvent(false).WaitOne();                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"出错：" + ex.GetBaseException().Message);
                return;
            }
            
        }



    }
}
