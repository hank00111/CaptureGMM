﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CaptureGMM.cs
{
    public static class C_Adapter
    {
        public static SynchronizationContext Dispacher { get; private set; }
        /// <summary>
        /// 請於UI執行緒呼叫此方法。
        /// </summary>
        public static void Initialize()
        {
            if (C_Adapter.Dispacher == null)
                C_Adapter.Dispacher = SynchronizationContext.Current;

        }
        /// <summary>
        /// 在 Dispatcher 關聯的執行緒上以同步方式執行指定的委派。
        /// </summary>
        public static void Invoke(SendOrPostCallback d, object state)
        {
            Dispacher.Send(d, state);
        }
        /// <summary>
        /// 在 Dispatcher 關聯的執行緒上以非同步方式執行指定的委派。
        /// </summary>
        public static void BeginInvoke(SendOrPostCallback d, object state)
        {
            Dispacher.Post(d, state);
        }

        /// <summary>
        /// 在UI執行緒執行
        /// </summary>
        /// <param name="ac"></param>
        public static void fun_UI執行緒(Action ac)
        {

            C_Adapter.Invoke(new SendOrPostCallback(obj => { // 呼叫UI執行緒
                ac();

            }), null);

        }

    }
}
