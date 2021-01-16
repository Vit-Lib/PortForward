using System;
using System.Threading;
using Vit.Core.Module.Log;

namespace PortForward
{
    class Program
    {

        static void Main(string[] args)
        {
            var cmd = new Commond ();

            cmd.PrintHelp();

            //args = new[] { "PortForwardLocal--4572--127.0.0.1--4570" };
            //args =new []{ "PortForwardServer--authToken--5000--5001" };

            //args = new[] { "PortForwardLocal--5002--192.168.0.153--80" };
            //args = new[] { "PortForwardClient--authToken--127.0.0.1--5001--192.168.0.153--80--5" };


            try
            {
                //通过文件名获取配置
                string configString = args[0];
                //configString = "PortForwardLocal--4572--127.0.0.1--4570";

                cmd.Call(configString);

                new AutoResetEvent(false).WaitOne();                
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return;
            }
            
        }



    }
}
