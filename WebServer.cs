using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Server
{
    public class WebServer
    {
        private Hashtable ht = new Hashtable();
        private List<ServerListener> listeners = new List<ServerListener>();   //侦听器列表
        private int listenerNo; //侦听的端口数

        // 创建若干个侦听器，本机名称为ip
        public void CreateListeners(string ip)
        {
            this.listenerNo = 0;
            ServerListener.bindIP(ip);
        }

        // 增加端口侦听，侦听端口port，对应根目录root，最大连接数maxRequest
        public bool AddPort(string port, string root, int maxRequest)
        {
            this.listenerNo++;
            ServerListener lis = new ServerListener();
            // 绑定端口
            if (!lis.bindPort(port))
                return false;
            // 设置最大连接数
            if (!lis.setMaxRequest(maxRequest))
                return false;
            // 绑定根目录
            if (!lis.bindRoot(root))
                return false;
            // 加入列表
            //listeners.Add(lis);
            ht.Add(port, lis);
            // 准备侦听
            lis.prepareListener();
            return true;
        }
    }
}
