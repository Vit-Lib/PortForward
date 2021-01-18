using System;

namespace Vit.Core.Module.Log
{
    /// <summary>
    /// FATAL > ERROR > WARN > INFO > DEBUG 
    /// </summary>
    public static class Logger
    {

        /// <summary>
        ///  例如    (level, msg)=> { Console.WriteLine("[" + level + "]" + DateTime.Now.ToString("[HH:mm:ss.ffff]") + msg);   };
        /// </summary>
        public static Action<Level, string> OnLog= 
            (level, msg) => {
                if (level == Level.INFO) 
                {
                    Console.WriteLine(DateTime.Now.ToString("[HH:mm:ss.ffff]") + msg);
                    return;
                }
                Console.WriteLine("[" + level + "]" + DateTime.Now.ToString("[HH:mm:ss.ffff]") + msg);
            };

        /// <summary>
        /// DEBUG （调试信息）：记录系统用于调试的一切信息，内容或者是一些关键数据内容的输出
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            OnLog?.Invoke(Level.DEBUG, message);
        }

        #region Info

        /// <summary>
        /// INFO（一般信息）：记录系统运行中应该让用户知道的基本信息。例如，服务开始运行，功能已经开户等。
        /// </summary>
        /// <param name="message"></param>
        public static void Info(string message)
        {
            OnLog?.Invoke(Level.INFO, message);
        }
     
        #endregion



        #region Error

        /// <summary>
        /// ERROR（一般错误）：记录系统中出现的导致系统不稳定，部分功能出现混乱或部分功能失效一类的错误。例如，数据字段为空，数据操作不可完成，操作出现异常等。
        /// </summary>
        /// <param name="message"></param>
        public static void Error(string message)
        {
            OnLog?.Invoke(Level.ERROR, message);
        }

        /// <summary>
        /// ERROR（一般错误）：记录系统中出现的导致系统不稳定，部分功能出现混乱或部分功能失效一类的错误。例如，数据字段为空，数据操作不可完成，操作出现异常等。
        /// </summary>
        /// <param name="ex"></param>
        public static void Error(Exception ex)
        {
            Error(null, ex);
        }

        /// <summary>
        /// ERROR（一般错误）：记录系统中出现的导致系统不稳定，部分功能出现混乱或部分功能失效一类的错误。例如，数据字段为空，数据操作不可完成，操作出现异常等。
        /// </summary> 
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public static void Error(string message, Exception ex)
        {
            var strMsg = "";
            if (!string.IsNullOrWhiteSpace(message)) strMsg += " message:" + message;
            if (null != ex)
            {
                ex = ex.GetBaseException();
                strMsg += Environment.NewLine + " Message:" + ex.Message;
                strMsg += Environment.NewLine + " StackTrace:" + ex.StackTrace;
            }
            Error(strMsg);
        }         
        #endregion








    }
}
