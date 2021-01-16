using Sers.CL.Socket.Iocp;
using System;
using Vit.Core.Module.Log;

namespace PortForward.Common
{
    public class LocalManager
    {       
        /// <summary>
        /// 
        /// </summary>
        public int inputConn_Port;

        /// <summary>
        /// dns or ip
        /// </summary>
        public string outputConn_Host = "127.0.0.1";
        public int outputConn_Port;      

        DeliveryServer server = new DeliveryServer();
        DeliveryClient client = new DeliveryClient();
        public void StartLinstening()
        {
            client.host = outputConn_Host;
            client.port = outputConn_Port;

            server.port = inputConn_Port;
            server.Conn_OnConnected = ServerOnInputConnected;

            server.Start();
        }

 


        void ServerOnInputConnected(DeliveryConnection conn)
        {         
            string RemoteEndPoint=null;
            try
            {
                RemoteEndPoint = conn.socket?.RemoteEndPoint.ToString();


                client.Connect((conn2)=> 
                {
                    conn.Bind(conn2);
                    Commond.PrintConnectionInfo("转发成功[" + RemoteEndPoint + "]");
                });           
            }
            catch (Exception ex)
            {              
                Logger.Error("转发失败[" + RemoteEndPoint + "]",ex);
            }

        }


    }
}
