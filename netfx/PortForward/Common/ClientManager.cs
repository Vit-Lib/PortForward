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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PortForward.Common
{
    public class ClientManager
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
                authTokenBytes= System.Text.Encoding.ASCII.GetBytes(_authToken);
            }
        }
        private string _authToken;
        byte[] authTokenBytes;
        #endregion



        /// <summary>
        /// dns  or ip
        /// </summary>
        public string server_Host ;
        /// <summary>
        /// server_Port
        /// </summary>
        public int outputConn_Port ;

        /// <summary>
        /// dns  or ip
        /// </summary>
        public string localConn_Host = "127.0.0.1";
        public int localConn_Port;

        /// <summary>
        /// 权限验证等待回传超时时间(毫秒)
        /// </summary>
        public int auth_ReadTimeout = 10000;

        IPAddress server_IPAddress {
            get
            {
                return TcpHelp.ParseToIPAddress(server_Host);
            }
        }

        IPAddress localConn_IPAddress
        {
            get
            {
                return TcpHelp.ParseToIPAddress(localConn_Host);
            }
        }





        void Bridge(TcpClient clientToServer)
        {
            new Task(()=> {

                try
                {                   

                    var clientToLocal = new TcpClient();
                    clientToLocal.Connect(TcpHelp.ParseToIPAddress(localConn_Host), localConn_Port);
                 
                    if (TcpHelp.Bridge(clientToServer, clientToLocal))
                    {
                        ConsoleWriteLine(DateTime.Now.ToString("[HH:mm:ss.fff]") + "转发成功");
                    }  
                }
                catch (Exception ex)
                {
                    ConsoleWriteLine(DateTime.Now.ToString("[HH:mm:ss.fff]") + "转发失败:" + ex.GetBaseException().Message);
                }
            }).Start();             
        }

        public void StartConnectThread(int threadCount)
        {
            while ((--threadCount) >= 0)
                StartConnectThread();
        }

        /// <summary>
        /// 调用一次开启一个线程，且不会终止
        /// </summary>
        public void StartConnectThread()
        {
            new Task(() =>
            {
                byte[] receivedBuff = new byte[authTokenBytes.Length];
                while (true)
                {
                    try
                    {
                       
                        var outputClient = new TcpClient();
                        outputClient.Connect(server_IPAddress, outputConn_Port);

                        if (!OutPut_SendAuth(outputClient, receivedBuff))
                        {
                            outputClient.Close();
                            ConsoleWriteLine(DateTime.Now.ToString("[HH:mm:ss.fff]") + "发起连接--失败-权限不正确");
                            Thread.Sleep(2000);
                            continue;
                        }
                        ConsoleWriteLine(DateTime.Now.ToString("[HH:mm:ss.fff]") + "发起连接--成功-权限认证通过");
                        try
                        {                            
                            OutPut_ReceiveStartMsg(outputClient);
                        }
                        catch (Exception)
                        {
                            outputClient.Close();
                            continue;
                        }
                       
                        Bridge(outputClient);
                    }
                    catch { }
                }

            }).Start();
        }
        
        /// <summary>
        /// 不会抛异常，不会关闭outputClient
        /// </summary>
        /// <param name="outputClient"></param>
        /// <param name="receivedBuff"></param>
        /// <returns></returns>
        bool OutPut_SendAuth(TcpClient outputClient, byte[] receivedBuff)
        {
            try
            {
                var stream = outputClient.GetStream();

                //write
                stream.Write(authTokenBytes, 0, authTokenBytes.Length);
                stream.Flush();

                //read
                var ReadTimeout = stream.ReadTimeout;
                stream.ReadTimeout = auth_ReadTimeout;
                stream.Read(receivedBuff, 0, receivedBuff.Length);
                stream.ReadTimeout = ReadTimeout;

                //比较
                if (authTokenBytes.SequenceEqual(receivedBuff))
                {
                    return true;
                }
            }
            catch { }
            return false;
        }
        /// <summary>
        /// 失败则抛异常，不会关闭outputClient
        /// </summary>
        /// <param name="outputClient"></param>
        void OutPut_ReceiveStartMsg(TcpClient outputClient)
        {
            var stream = outputClient.GetStream();
            int ReadTimeout = stream.ReadTimeout;
            //5分钟自动重新连接
            stream.ReadTimeout=300000;
            outputClient.GetStream().ReadByte();
            stream.ReadTimeout = ReadTimeout;
        }

    }
}
