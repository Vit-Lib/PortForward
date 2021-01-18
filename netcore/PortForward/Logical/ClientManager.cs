using Sers.CL.Socket.Iocp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vit.Core.Module.Log;

namespace PortForward.Common
{
    public class ClientManager
    {
 


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

     




   
        DeliveryClient outputClient = new DeliveryClient();
        DeliveryClient localClient = new DeliveryClient();
        public void StartConnectThread(int threadCount)
        {
            outputClient.host = server_Host;
            outputClient.port = outputConn_Port;

            localClient.host = localConn_Host;
            localClient.port = localConn_Port;

            while (--threadCount>=0) 
            {
                StartNewOutput();
            }
        }

        void StartNewOutput() 
        {
            outputClient.Connect(Output_OnConnected);
        }


        void Output_OnConnected(DeliveryConnection output) 
        {

            output.Conn_OnDisconnected = (conn) => 
            {
                StartNewOutput();
            };

            output.OnGetFrame = Output_WaitForAuth;
 
            //(x.2)send token 
            output.SendFrameAsync(authTokenBytes);
      
        }

        ArraySegment<byte> Output_WaitForAuth(DeliveryConnection output, ArraySegment<byte> data)
        {
            //(x.1)读取数据
            var byteList = output.ext as List<byte>;
            if(byteList==null)
            {
                output.ext = byteList = new List<byte>();
            }
            byteList.AddRange(data);

            if (byteList.Count < authTokenBytes.Length)
            {
                return null;
            }

            #region (x.2)收到数据的长度 等于 token长度  
            if (byteList.Count == authTokenBytes.Length)
            {
                if (authTokenBytes.SequenceEqual(byteList))
                {
                    //权限认证通过
                    Logger.Info("发起连接--成功-权限认证通过");

                    output.ext = null;

                    output.OnGetFrame = Output_WaitForStartMsg;
                    return null;
                }
                else 
                {
                    //权限认证不通过
                    Logger.Info("收到连接-失败-权限认证不通过");

                    output.ext = null;
                    output.OnGetFrame = null;
       
                    output.Close();
                    return null;
                }               
            }
            #endregion

            #region (x.3)收到数据的长度 大于 token长度            
            if (authTokenBytes.SequenceEqual(byteList.Take(authTokenBytes.Length)))
            {
                //权限认证通过 且已经接受到开始标志
                Logger.Info("发起连接--成功-权限认证通过");

                output.ext = null;
                output.OnGetFrame = null;
   
                Output_OnReceiveStartMsg(output, byteList.Skip(authTokenBytes.Length+1).ToArray());
                return null;
            }
            else
            {
                //权限认证不通过
                Logger.Info("收到连接-失败-权限认证不通过");

                output.ext = null;
                output.OnGetFrame = null;
            
                output.Close();
                return null;
            }
            #endregion
        }

        ArraySegment<byte> Output_WaitForStartMsg(DeliveryConnection output, ArraySegment<byte> data)
        {
            output.OnGetFrame = null;

            Output_OnReceiveStartMsg(output, data.Slice(1));
            return null;
        }


        void Output_OnReceiveStartMsg(DeliveryConnection output, ArraySegment<byte> data)
        {
            StartNewOutput(); 

            localClient.Connect((local) =>
            {
                try
                {
                    if (data.Count != 0)
                    {
                        local.SendFrameAsync(data);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }                
                local.Bind(output);
            });
        }
           
    }
}
