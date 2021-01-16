using System;
using System.Collections.Generic;
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

            this.Flush();
            conn2.Flush();
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
        public Func<DeliveryConnection, ArraySegment<byte>, ArraySegment<byte>> OnGetFrame { private get; set; }


        public Action<DeliveryConnection> Conn_OnDisconnected { get; set; }

        //readonly BlockingCollection<ArraySegment<byte> > msgFrameToSend = new BlockingCollection<ArraySegment<byte>>();

        public void SendFrameAsync(ArraySegment<byte> data)
        {
            if (data == null || socket == null) return;
            try
            {
                //msgFrameToSend.Add(data);
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

        List<ArraySegment<byte>> buff = null;

        void Flush() 
        {
            if (buff != null && connector!=null)
            {
                foreach (var d in buff)
                {
                    connector.SendFrameAsync(d);
                }
                buff = null;
            }
        }

        public void AppendData(ArraySegment<byte> data)
        {
            try
            {
                if (data.Count == 0) return;

                if (OnGetFrame != null)
                {
                    data = OnGetFrame(this, data);
                    if (data.Count == 0) return;
                }
                          

                if (connector == null)
                {
                    if (buff == null) buff = new List<ArraySegment<byte>>();
                    buff.Add(data);
                    return;
                }


                connector.SendFrameAsync(data);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


    }
}
