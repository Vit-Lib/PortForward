using Sers.CL.Socket.Iocp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Vit.Core.Module.Log;

namespace PortForward.Common
{
    public class ServerManager
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
                authTokenBytes = System.Text.Encoding.ASCII.GetBytes(_authToken);
            }
        }
        private string _authToken;
        byte[] authTokenBytes;
        #endregion

        public int inputConn_Port;
        public int outputConn_Port;

 

        public void StartLinstening()
        {
            //input
            DeliveryServer inputServer = new DeliveryServer();
            inputServer.port = inputConn_Port;
            inputServer.Conn_OnConnected = Input_OnConnected;

            inputServer.Start();


            //output
            DeliveryServer outputServer = new DeliveryServer();
            outputServer.port = outputConn_Port;
            outputServer.Conn_OnConnected = Output_OnConnected;

            outputServer.Start();     
        }

       

        void Input_OnConnected(DeliveryConnection input)
        {
            DeliveryConnection output;
            #region get output
            while (true)
            {
                if (!outputQueue.TryDequeue(out output)) break;

                if (output.IsConnected)
                {
                    break;
                }              
            }            
            #endregion


            if (output == null)
            {
                inputQueue.Enqueue(input);
            }
            else 
            {        
                input.Bind(output);
                //OutPut_SendStartMsg
                output.SendFrameAsync(StartMsg);

                Logger.Info($" [{output.GetHashCode()}] 转发成功");
            }           
        }         
         
        
        private void Output_OnConnected(DeliveryConnection output)
        {
            output.OnGetFrame = Output_OnGetFrame;
            Logger.Info($" [{output.GetHashCode()}] 收到连接");
        }


        ArraySegment<byte> Output_OnGetFrame(DeliveryConnection output, ArraySegment<byte> data) 
        {
            //(x.1)读取数据
            var byteList = output.ext as List<byte>;
            if (byteList == null)
            {
                output.ext = byteList = new List<byte>();
            }
            byteList.AddRange(data);

            if (byteList.Count < authTokenBytes.Length)
            {
                return null;
            }

            //(x.2)匹配不通过
            if (byteList.Count != authTokenBytes.Length || !authTokenBytes.SequenceEqual(byteList))
            {
                Logger.Info($" [{output.GetHashCode()}] 权限认证-不通过");
                output.Close();
                return null;
            }

            //(x.3)匹配通过
            Logger.Info($" [{output.GetHashCode()}] 权限认证-通过");
            output.ext = null;
            output.OnGetFrame = null;

            //(x.4)发送验证通过标志
            output.SendFrameAsync(authTokenBytes);


            #region (x.5)进行桥接或放入连接池            
            DeliveryConnection input;

            #region get input
            while (true)
            {
                if (!inputQueue.TryDequeue(out input)) break;

                if (input.IsConnected)
                {
                    break;
                }
            }
            #endregion

            if (input == null)
            {
                outputQueue.Enqueue(output);
            }
            else
            {    
                input.Bind(output);
                //OutPut_SendStartMsg
                output.SendFrameAsync(StartMsg);

                Logger.Info($" [{output.GetHashCode()}] 转发成功");
            }
            #endregion

            return null;

        }

        byte[] StartMsg = new[] { (byte)0 };



        ConcurrentQueue<DeliveryConnection> inputQueue = new ConcurrentQueue<DeliveryConnection>();
        ConcurrentQueue<DeliveryConnection> outputQueue = new ConcurrentQueue<DeliveryConnection>();
    }
}
