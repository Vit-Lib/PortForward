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
            inputServer.Conn_OnConnected = OnInputConnected;

            inputServer.Start();


            //output
            DeliveryServer outputServer = new DeliveryServer();
            outputServer.port = outputConn_Port;
            outputServer.Conn_OnConnected = OnOutputConnected;

            outputServer.Start();     
        }

       

        void OnInputConnected(DeliveryConnection input)
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
                Commond.PrintConnectionInfo("转发成功");
                input.Bind(output);
                //OutPut_SendStartMsg
                output.SendFrameAsync(StartMsg);
            }           
        }         
         
        
        private void OnOutputConnected(DeliveryConnection output)
        {
            output.OnGetFrame = Output_OnGetFrame;          
        }


        void Output_OnGetFrame(DeliveryConnection output, ArraySegment<byte> data) 
        {
            //(x.1)读取数据
            var byteList = output.ext as List<byte>;
            if (byteList == null) byteList = new List<byte>();
            byteList.AddRange(data);

            if (byteList.Count < authTokenBytes.Length)
            {
                return;
            }

            //(x.2)匹配不通过
            if (byteList.Count != authTokenBytes.Length || !authTokenBytes.SequenceEqual(byteList))
            {         
                Commond.PrintConnectionInfo( "收到连接-失败-权限认证不通过");
                output.Close();
                return;
            }

            //(x.3)匹配通过
            Commond.PrintConnectionInfo("收到连接-成功-权限认证通过");
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
                Commond.PrintConnectionInfo("转发成功");
                input.Bind(output);
                //OutPut_SendStartMsg
                output.SendFrameAsync(StartMsg);
            }
            #endregion

        }

        byte[] StartMsg = new[] { (byte)0 };



        ConcurrentQueue<DeliveryConnection> inputQueue = new ConcurrentQueue<DeliveryConnection>();
        ConcurrentQueue<DeliveryConnection> outputQueue = new ConcurrentQueue<DeliveryConnection>();
    }
}
