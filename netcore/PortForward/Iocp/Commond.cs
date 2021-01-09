#region << 版本注释 - v2 >>
/*
 * ========================================================================
 * 版本：v2
 * 时间：190212
 * 作者：Lith   
 * Q  Q：755944120
 * 邮箱：litsoft@126.com
 * 
 * ========================================================================
*/
#endregion

using PortForward.Common;
using System;

namespace PortForward.Iocp
{
    public class Commond
    {
        public Action<string> OnWriteLine;


        private void WriteLine(string value)
        {
            OnWriteLine?.Invoke(value);
        }

        /// <summary>
        ///  configString demo:"PortForwardClient--authToken--192.168.3.162--6203--192.168.3.162--3389--2"
        /// </summary>
        /// <param name="configString"></param>
        public void Call(string configString)
        {

            var config = configString.Split(new string[] { "--" }, StringSplitOptions.RemoveEmptyEntries);
            switch (config[0])
            {
                case "PortForwardClient": RunClient(config); return;
                case "PortForwardServer": RunServer(config); return;
                case "PortForwardLocal": RunLocal(config); return;
            }
            throw new Exception("错误：配置信息不合法！！");
        }


        #region RunClient
        void RunClient(string[] config)
        {
            var mng = new ClientManager()
            {
                authToken = config[1],
                server_Host = config[2],
                outputConn_Port = int.Parse(config[3]),
                localConn_Host = config[4],
                localConn_Port = int.Parse(config[5])
            };

            int connectCount = int.Parse(config[6]);

            WriteLine($"当前为 端口桥接工具-客户端：");
            WriteLine($"authToken:{mng.authToken}");
            WriteLine($"server_Host:{mng.server_Host}");
            WriteLine($"outputConn_Port:{mng.outputConn_Port}");
            WriteLine($"localConn_Host:{mng.localConn_Host}");
            WriteLine($"localConn_Port:{mng.localConn_Port}");
            WriteLine($"connectCount:{connectCount}");

            //定义显示输出
            if (config.Length <= 7 || "NoPrint" != config[7])
                mng.ConsoleWriteLine = WriteLine;

            mng.ConsoleWriteLine("开始...");
            mng.StartConnectThread(connectCount);

        }

        #endregion

        #region RunServer
        void RunServer(string[] config)
        {
            var mng = new ServerManager()
            {
                authToken = config[1],
                inputConn_Port = int.Parse(config[2]),
                outputConn_Port = int.Parse(config[3])
            };

            WriteLine($"当前为 端口桥接工具-服务端：");
            WriteLine($"authToken:{mng.authToken}");
            WriteLine($"inputConn_Port:{mng.inputConn_Port}");
            WriteLine($"outputConn_Port:{mng.outputConn_Port}");


            //定义显示输出
            if (config.Length <= 4 || "NoPrint" != config[4])
                mng.ConsoleWriteLine = WriteLine;
            mng.ConsoleWriteLine("开始...");
            mng.StartLinstening();
        }
        #endregion

        #region RunLocal
        void RunLocal(string[] config)
        {
            var mng = new LocalManager()
            {
                inputConn_Port = int.Parse(config[1]),
                outputConn_Host = config[2],
                outputConn_Port = int.Parse(config[3])
            };


            WriteLine($"当前为 本地端口转发工具：");
            WriteLine($"inputConn_Port:{mng.inputConn_Port}");
            WriteLine($"outputConn_Host:{mng.outputConn_Host}");
            WriteLine($"outputConn_Port:{mng.outputConn_Port}");


            //定义显示输出
            if (config.Length <= 4 || "NoPrint" != config[4])
                mng.ConsoleWriteLine = WriteLine;

            mng.ConsoleWriteLine("开始...");
            mng.StartLinstening();
        }
        #endregion

        public void PrintHelp()
        {
            #region print Help
            WriteLine("version:  1.10");
            WriteLine("author:  lith");
            WriteLine("email:   sersms@163.com");
            WriteLine("----Lith端口转发----");
            WriteLine("从文件名获取配置信息,分为“本地端口转发工具”和“端口桥接工具”。");
            WriteLine("----本地端口转发工具----");
            WriteLine("     配置信息格式为：");
            WriteLine("         PortForwardLocal--inputConnPort--outputConnHost--outputConnPort--NoPrint");
            WriteLine("     Demo:");
            WriteLine("         dotnet PortForward.dll PortForwardLocal--8000--192.168.1.5--3384--NoPrint");
            WriteLine("         把本地的8000端口转发至 主机192.168.1.5的3384端口");
            WriteLine("     NoPrint:定义是否回显，若指定为NoPrint则不实时回显连接信息");
            WriteLine("");
            WriteLine("----端口桥接工具----");
            WriteLine("     客户端格式为：");
            WriteLine("         PortForwardClient--authToken--serverHost--outputConnPort-localConnHost--localConnPort--ConnectCount--NoPrint");
            WriteLine("     服务端格式为：");
            WriteLine("         PortForwardServer--authToken--inputConnPort--outputConnPort--NoPrint");
            WriteLine("     Demo:");
            WriteLine("         dotnet PortForward.dll PortForwardClient--authToken--192.168.1.100--6203--abc.com--3389--5");
            WriteLine("         dotnet PortForward.dll PortForwardServer--authToken--6202--6203");
            WriteLine("");
            WriteLine("     说明:");
            WriteLine("         把服务端（serverHost）的 inputConnPort端口转发至 客户端连接的 主机localConnHost的端口localConnPort");
            WriteLine("         服务端和客户端通过端口outputConnPort连接");
            WriteLine("     autoToken   :权限校验字段，服务端和客户端必须一致");
            WriteLine("     ConnectCount:客户端保持的空闲连接个数，推荐5");
            WriteLine("     NoPrint     :定义是否回显，若指定为NoPrint则不实时回显连接信息，可不指定");
            WriteLine("----------------");
            WriteLine("");
            #endregion
        }

    }
}
