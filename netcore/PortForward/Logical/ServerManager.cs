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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace PortForward.Common
{
    public class ServerManager
    {
        public Action<string> ConsoleWriteLine = (msg) => { };//Console.WriteLine;//


        #region authToken authTokenBytes        
        /// <summary>
        /// 只可为字母数字
        /// </summary>
        public string authToken
        {
            get { return _authToken; }
            set
            {
                _authToken = value;
                if (string.IsNullOrEmpty(_authToken)) _authToken = " ";
                authTokenBytes = System.Text.Encoding.ASCII.GetBytes(_authToken);
            }
        }
        private string _authToken;
        byte[] authTokenBytes;
        #endregion

        public int inputConn_Port;
        public int outputConn_Port;

        public int auth_ReadTimeout = 500;

        public void StartLinstening()
        {
            //input
            TcpHelp.Listening(inputConn_Port, OnInputConnected);

            //output
            TcpHelp.Listening(outputConn_Port, OnOutputConnected);
        }

        /// <summary>
        /// 获取连接中的output
        /// 不抛异常
        /// 若获取不到则返回null
        /// </summary>
        /// <returns></returns>
        private TcpClient GetConnectedOutputFromQueue()
        {
            TcpClient outputClient;
            while (true)
            {
                if (!outputQueue.TryDequeue(out outputClient)) return null;

                if (TcpHelp.TcpClientIsConnected(outputClient, 10))
                {
                    return outputClient;
                }
                try
                {
                    outputClient?.Close();
                }
                catch { }
            }
        }

        private void OnInputConnected(TcpClient inputClient)
        {
            TcpClient outputClient = GetConnectedOutputFromQueue();
            if (null == outputClient)
            {
                inputQueue.Enqueue(inputClient);
                return;
            }

            Bridge(inputClient, outputClient);
        }

        private void Bridge(TcpClient inputClient, TcpClient outputClient)
        {

            try
            {
                OutPut_SendStartMsg(outputClient);
            }
            catch (Exception)
            {
                inputClient.Close();
                outputClient.Close();
                throw;
            }

            if (TcpHelp.Bridge(outputClient, inputClient))
            {
                ConsoleWriteLine(DateTime.Now.ToString("[HH:mm:ss.fff]") + "转发成功");
            }
        }


        bool OutPut_CheckAuth(TcpClient outputClient)
        {
            try
            {
                var stream = outputClient.GetStream();

                byte[] receivedBuff = new byte[authTokenBytes.Length];

                //read
                var ReadTimeout = stream.ReadTimeout;
                stream.ReadTimeout = auth_ReadTimeout;
                stream.Read(receivedBuff, 0, receivedBuff.Length);
                stream.ReadTimeout = ReadTimeout;

                //比较
                if (authTokenBytes.SequenceEqual(receivedBuff))
                {
                    //write
                    stream.Write(authTokenBytes, 0, authTokenBytes.Length);
                    stream.Flush();
                    return true;
                }
            }
            catch { }
            return false;
        }
        void OutPut_SendStartMsg(TcpClient outputClient)
        {
            outputClient.GetStream().WriteByte(0);
        }
        private void OnOutputConnected(TcpClient outputClient)
        {
            //权限校验
            if (!OutPut_CheckAuth(outputClient))
            {
                outputClient.Close();
                ConsoleWriteLine(DateTime.Now.ToString("[HH:mm:ss.fff]") + "收到连接-失败-权限认证不通过");
                return;
            }
            ConsoleWriteLine(DateTime.Now.ToString("[HH:mm:ss.fff]") + "收到连接-成功-权限认证通过");


            if (inputQueue.TryDequeue(out TcpClient inputClient) && null != inputClient)
            {
                Bridge(inputClient, outputClient);
            }
            else
            {
                outputQueue.Enqueue(outputClient);
            }
        }

        ConcurrentQueue<TcpClient> inputQueue = new ConcurrentQueue<TcpClient>();
        ConcurrentQueue<TcpClient> outputQueue = new ConcurrentQueue<TcpClient>();
    }
}
