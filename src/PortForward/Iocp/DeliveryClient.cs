// https://freshflower.iteye.com/blog/2285286

using System;
using System.Net;
using System.Net.Sockets;
using Vit.Core.Module.Log;
using Vit.Core.Util.Net;

namespace Sers.CL.Socket.Iocp
{
    public class DeliveryClient
    {

        /// <summary>
        /// 缓存区大小
        /// </summary>
        public int receiveBufferSize = 8 * 1024;



        /// <summary>
        ///  服务端 host地址（默认 "127.0.0.1" ）。例如： "127.0.0.1"、"serset.com"。
        /// </summary>
        public string host;
        /// <summary>
        /// 服务端 监听端口号（默认4501）。例如： 4501。
        /// </summary>
        public int port;
        public void Connect(Action<DeliveryConnection> onConneced)
        {
            try
            {
                //Logger.Info("[CL.DeliveryClient] Socket.Iocp,connecting... host:" + host + " port:" + port);

                //(x.1) Instantiates the endpoint and socket.
                var hostEndPoint = new IPEndPoint(NetHelp.ParseToIPAddress(host), port);
                var socket = new global::System.Net.Sockets.Socket(hostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);


                DeliveryConnection _conn = new DeliveryConnection();

                _conn.Conn_OnConnected = onConneced;

                var receiveEventArgs = _conn.receiveEventArgs = new SocketAsyncEventArgs();

                receiveEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);


                receiveEventArgs.UserToken = _conn;


                _conn.Init(socket);

                //var buff= DataPool.BytesGet(receiveBufferSize);
                var buff = new byte[receiveBufferSize];
               _conn.receiveEventArgs.SetBuffer(buff, 0, buff.Length);


                //(x.2)
                SocketAsyncEventArgs connectArgs = new SocketAsyncEventArgs();
                connectArgs.RemoteEndPoint = hostEndPoint;
                connectArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnConnect);
                connectArgs.UserToken = _conn;

                socket.ConnectAsync(connectArgs);                
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

    
  
 
 
      

        // Calback for connect operation
        private void OnConnect(object sender, SocketAsyncEventArgs e)
        {
            var conn = e.UserToken as DeliveryConnection;
            if (conn == null)
            {
                throw new Exception("[iocp]Error[2021-01-17_lith_001]");
            }

            try
            {
                conn.Conn_OnConnected(conn);

                //如果连接成功,则初始化socketAsyncEventArgs
                if (e.SocketError == SocketError.Success)
                {
                    //启动接收,不管有没有,一定得启动.否则有数据来了也不知道.
                    if (!conn.socket.ReceiveAsync(conn.receiveEventArgs))
                        ProcessReceive(conn.receiveEventArgs);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            try
            {
                conn.Close();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
 
 

        void IO_Completed(object sender, SocketAsyncEventArgs e)
        { 
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    Logger.Info("[Iocp]IO_Completed Send");
                    return;

                    //ProcessSend(e);
                    //break;
                default:
                    Logger.Info("[Iocp]IO_Completed default");
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }
        }

        // This method is invoked when an asynchronous receive operation completes. 
        // If the remote host closed the connection, then the socket is closed.  
        // If data was received then the data is echoed back to the client.
        //
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            var conn = e.UserToken as DeliveryConnection;
            if (conn == null)
            {
                throw new Exception("[iocp]Error[2021-01-17_lith_002]");
            }

            try
            {

                // check if the remote host closed the connection 
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    //读取数据                  
                    conn.AppendData(new ArraySegment<byte>(e.Buffer, e.Offset, e.BytesTransferred));

                    //byte[] buffData = DataPool.BytesGet(receiveBufferSize);
                    byte[] buffData = new byte[receiveBufferSize];
                    e.SetBuffer(buffData, 0, buffData.Length);

                    // start loop
                    if (!conn.socket.ReceiveAsync(e))
                        ProcessReceive(e);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            try
            {
                conn.Close();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }



        }




 




    }
}
