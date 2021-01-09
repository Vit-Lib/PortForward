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

using Framework.Util.Socket;
using System;
using System.Net;
using System.Net.Sockets;

namespace PortForward.Common
{
    public class LocalManager
    {
        public Action<string> ConsoleWriteLine =  (msg) => {     };//Console.WriteLine;//



 
        /// <summary>
        /// 
        /// </summary>
        public int inputConn_Port;

        /// <summary>
        /// dns  or ip
        /// </summary>
        public string outputConn_Host = "127.0.0.1";
        public int outputConn_Port;

        IPAddress outputConn_IPAddress
        {
            get
            {
                return TcpHelp.ParseToIPAddress(outputConn_Host);
            }
        }


        public void StartLinstening()
        {       
            //input
            TcpHelp.Listening(inputConn_Port, OnInputConnected);         
        }

 


        private void OnInputConnected(TcpClient inputClient)
        {
            //Bridge
            string RemoteEndPoint=null;
            try
            {
                RemoteEndPoint = inputClient.Client.RemoteEndPoint.ToString();
                var outputClient = new TcpClient();
                outputClient.Connect(outputConn_IPAddress, outputConn_Port);

                if (TcpHelp.Bridge(inputClient, outputClient))
                {           
                    ConsoleWriteLine(DateTime.Now.ToString("[HH:mm:ss.fff]") + "转发成功["+ RemoteEndPoint+"]");
                }
            }
            catch (Exception ex)
            {
                ConsoleWriteLine(DateTime.Now.ToString("[HH:mm:ss.fff]") + "转发失败[" + RemoteEndPoint + "]:" + ex.GetBaseException().Message);
            }

        }


    }
}
