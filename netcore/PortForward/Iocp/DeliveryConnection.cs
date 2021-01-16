using System;
using System.Net.Sockets;
 
using Vit.Core.Module.Log;

namespace Sers.CL.Socket.Iocp
{
    public class DeliveryConnection  
    {
        public void Bind(DeliveryConnection conn2)
        {
            this.connector = conn2;
            conn2.connector = this;
        }

        DeliveryConnection connector;

        public object ext;


        public Action<DeliveryConnection> Conn_OnConnected { get; set; }


        public SocketAsyncEventArgs receiveEventArgs;



        public bool IsConnected
        {
            get
            {
                return socket!=null;
            }
        }




        /// <summary>
        /// 请勿处理耗时操作，需立即返回。接收到客户端的数据事件
        /// </summary>
        public Action<DeliveryConnection, ArraySegment<byte>> OnGetFrame { private get; set; }


        public Action<DeliveryConnection> Conn_OnDisconnected { get; set; }

        

        public void SendFrameAsync(ArraySegment<byte> data)
        {
            //if (data == null || socket == null) return;
            try
            {          
                socket.SendAsync(data, SocketFlags.None);          
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Close();
            }           
        }
     

        public void Close()
        {
            if (socket == null) return;

            Logger.Info("conn 断开");
          

            var socket_ = socket;
            socket = null;

          

            try
            {
                if (socket_.Connected)
                {
                    socket_.Close();
                    socket_.Dispose();
                }
                //socket_.Shutdown(SocketShutdown.Both);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            try
            {
                connector?.Close();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            try
            {
                Conn_OnDisconnected?.Invoke(this);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

        }
        public void Init(global::System.Net.Sockets.Socket socket)
        {
            this.socket = socket;
          
        }
 
        /// <summary>
        /// 通信SOCKET
        /// </summary>
        public global::System.Net.Sockets.Socket socket { get;private set; } 

        public void AppendData(ArraySegment<byte> data)
        {
            OnGetFrame?.Invoke(this, data);
            connector?.SendFrameAsync(data);
        }


    }
}
