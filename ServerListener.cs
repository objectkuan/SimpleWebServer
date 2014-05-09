using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace Server
{

    class ServerListener
    {
        private static string ip = null;   // 主机的名称，各个端口侦听器公用
        private int maxRequest = 100; // 最大连接数
        private string port = null;        // 本侦听器侦听的端口
        private string root = null;        // 本侦听器对应的根目录

        private HttpListener coreListener;    // 本侦听器的核心功能对象

        // 根目录
        public bool bindRoot(string root)
        {
            if (!Directory.Exists(root))
                return false;
            root = root.Replace('/', '\\');
            root = root.TrimEnd(new char[] { '\\' });
            this.root = root;
            return true;
        }
        public string getRoot()
        {
            return this.root;
        }

        // 主机名
        public static void bindIP(string ip)
        {
            ServerListener.ip = ip;
        }
        public static string getIP()
        {
            return ServerListener.ip;
        }

        // ServerListener侦听的端口
        public bool bindPort(string port)
        {
            int iPort = Convert.ToInt32(port);
            if (iPort < 1024 || iPort > 65535 || !isPortFree(port))
                return false;
            this.port = port;
            return true;
        }
        public string getPort()
        {
            return this.port;
        }

        // 端口是否被占用
        private bool isPortFree(string port)
        {
            string[] ipPart = ServerListener.ip.Split(new char[] { '.' });
            IPEndPoint iepClient = new IPEndPoint(
                new IPAddress(
                    new byte[] {
                        Convert.ToByte(ipPart[0]),
                        Convert.ToByte(ipPart[1]),
                        Convert.ToByte(ipPart[2]),
                        Convert.ToByte(ipPart[3])
                    }
                    ),
                Convert.ToInt32(port));
            try
            {
                //尝试绑定本机端点
                TcpClient tcpTest = new TcpClient();
                tcpTest.Client.Bind((EndPoint)iepClient);
                tcpTest.Close();
            }
            catch
            {
                return false;
            }
            return true;
        }

        // 本端口的最大连接数
        public bool setMaxRequest(int maxRequest)
        {
            if (maxRequest < 0 || maxRequest > 100)
                return false;
            this.maxRequest = maxRequest;
            return true;
        }
        public int getMaxRequest()
        {
            return this.maxRequest;
        }

        // 准备侦听
        public void prepareListener()
        {
            coreListener = new HttpListener();
            coreListener.Prefixes.Add("http://" + ip + ":" + port + "/");
            coreListener.Start();
            // maxRequest个线程侦听同一个端口
            for (int count = 0; count < maxRequest; count++)
            {
                // 侦听处理函数是静态的requestHandler
                // 传进去的参数是本侦听器
                coreListener.BeginGetContext(requestHandler, this);
            }
        }

        // 侦听处理
        private static void requestHandler(IAsyncResult result)
        {
            // 传进来的参数看看是哪个端口侦听器有消息了
            ServerListener listener = (ServerListener)(result.AsyncState);
            // 将那个端口侦听器的相关信息存在本方法的局部变量中
            string root = listener.root;
            HttpListener coreListener = listener.coreListener;
            try
            {
                #region pretreatment
                // 找正文
                HttpListenerContext context = coreListener.EndGetContext(result);
                // 正文中提取出尾部url
                string url = context.Request.RawUrl;
                // 得到服务器端对应的文件
                string wholePath = root + url.Replace("/", "\\");
                // 要是文件不存在
                if (!File.Exists(wholePath))
                {
                    StreamWriter writer = new StreamWriter(context.Response.OutputStream, Encoding.UTF8);
                    writer.WriteLine(@"<html><head><title>文件不存在</title></head><body>请求的文件不存在</body></html>");
                    writer.Flush();
                    context.Response.ContentType = "text/html";
                    context.Response.ContentEncoding = Encoding.UTF8;
                    context.Response.Close();
                    return;
                }
                #endregion

                #region GET
                if (context.Request.HttpMethod == "GET")
                {
                    Stream br = new FileStream(wholePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    Stream s = context.Response.OutputStream;
                    while (true)
                    {
                        byte[] content = new byte[1048576];//1048576B = 1MB
                        int len = br.Read(content, 0, 1048576);
                        if (len == 0) break;
                        s.Write(content, 0, len);
                    }
                    s.Flush();
                    br.Close();

                    context.Response.ContentType = getMIME(wholePath);
                    context.Response.Close();
                }
                #endregion
                else if (context.Request.HttpMethod == "HEAD")
                {
                    HttpListenerResponse hlr = context.Response;
                    //hlr.Headers.Add();
                }else if(context.Request.HttpMethod == "POST")
                {

                }
            }
            catch (ObjectDisposedException odx)
            {
                Console.WriteLine(odx.Message);
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
            finally
            {
                if (coreListener.IsListening)
                    coreListener.BeginGetContext(ServerListener.requestHandler, result.AsyncState);
            }
        }

        // 通过完整文件名来得到对应的MIME
        private static string getMIME(string path)
        {
            string name = path.Substring(path.LastIndexOf("\\") + 1);
            string postfix = name.Substring(name.LastIndexOf(".") + 1);
            return mapping(postfix);
        }

        // 后缀与MIME
        private static string mapping(string postfix)
        {
            if (postfix == "*")
                return "application/octet-stream";
            else if (postfix == "001")
                return "application/x-001";
            else if (postfix == "301")
                return "application/x-301";
            else if (postfix == "323")
                return "text/h323";
            else if (postfix == "906")
                return "application/x-906";
            else if (postfix == "907")
                return "drawing/907";
            else if (postfix == "a11")
                return "application/x-a11";
            else if (postfix == "acp")
                return "audio/x-mei-aac";
            else if (postfix == "ai")
                return "application/postscript";
            else if (postfix == "aif")
                return "audio/aiff";
            else if (postfix == "aifc")
                return "audio/aiff";
            else if (postfix == "aiff")
                return "audio/aiff";
            else if (postfix == "anv")
                return "application/x-anv";
            else if (postfix == "asa")
                return "text/asa";
            else if (postfix == "asf")
                return "video/x-ms-asf";
            else if (postfix == "asp")
                return "text/asp";
            else if (postfix == "asx")
                return "video/x-ms-asf";
            else if (postfix == "au")
                return "audio/basic";
            else if (postfix == "avi")
                return "video/avi";
            else if (postfix == "awf")
                return "application/vnd.adobe.workflow";
            else if (postfix == "biz")
                return "text/xml";
            else if (postfix == "bmp")
                return "application/x-bmp";
            else if (postfix == "bot")
                return "application/x-bot";
            else if (postfix == "c4t")
                return "application/x-c4t";
            else if (postfix == "c90")
                return "application/x-c90";
            else if (postfix == "cal")
                return "application/x-cals";
            else if (postfix == "cat")
                return "application/vnd.ms-pki.seccat";
            else if (postfix == "cdf")
                return "application/x-netcdf";
            else if (postfix == "cdr")
                return "application/x-cdr";
            else if (postfix == "cel")
                return "application/x-cel";
            else if (postfix == "cer")
                return "application/x-x509-ca-cert";
            else if (postfix == "cg4")
                return "application/x-g4";
            else if (postfix == "cgm")
                return "application/x-cgm";
            else if (postfix == "cit")
                return "application/x-cit";
            else if (postfix == "class")
                return "java/*";
            else if (postfix == "cml")
                return "text/xml";
            else if (postfix == "cmp")
                return "application/x-cmp";
            else if (postfix == "cmx")
                return "application/x-cmx";
            else if (postfix == "cot")
                return "application/x-cot";
            else if (postfix == "crl")
                return "application/pkix-crl";
            else if (postfix == "crt")
                return "application/x-x509-ca-cert";
            else if (postfix == "csi")
                return "application/x-csi";
            else if (postfix == "css")
                return "text/css";
            else if (postfix == "cut")
                return "application/x-cut";
            else if (postfix == "dbf")
                return "application/x-dbf";
            else if (postfix == "dbm")
                return "application/x-dbm";
            else if (postfix == "dbx")
                return "application/x-dbx";
            else if (postfix == "dcd")
                return "text/xml";
            else if (postfix == "dcx")
                return "application/x-dcx";
            else if (postfix == "der")
                return "application/x-x509-ca-cert";
            else if (postfix == "dgn")
                return "application/x-dgn";
            else if (postfix == "dib")
                return "application/x-dib";
            else if (postfix == "dll")
                return "application/x-msdownload";
            else if (postfix == "doc")
                return "application/msword";
            else if (postfix == "dot")
                return "application/msword";
            else if (postfix == "drw")
                return "application/x-drw";
            else if (postfix == "dtd")
                return "text/xml";
            else if (postfix == "dwf")
                return "Model/vnd.dwf";
            else if (postfix == "dwf")
                return "application/x-dwf";
            else if (postfix == "dwg")
                return "application/x-dwg";
            else if (postfix == "dxb")
                return "application/x-dxb";
            else if (postfix == "dxf")
                return "application/x-dxf";
            else if (postfix == "edn")
                return "application/vnd.adobe.edn";
            else if (postfix == "emf")
                return "application/x-emf";
            else if (postfix == "eml")
                return "message/rfc822";
            else if (postfix == "ent")
                return "text/xml";
            else if (postfix == "epi")
                return "application/x-epi";
            else if (postfix == "eps")
                return "application/x-ps";
            else if (postfix == "eps")
                return "application/postscript";
            else if (postfix == "etd")
                return "application/x-ebx";
            else if (postfix == "exe")
                return "application/x-msdownload";
            else if (postfix == "fax")
                return "image/fax";
            else if (postfix == "fdf")
                return "application/vnd.fdf";
            else if (postfix == "fif")
                return "application/fractals";
            else if (postfix == "fo")
                return "text/xml";
            else if (postfix == "frm")
                return "application/x-frm";
            else if (postfix == "g4")
                return "application/x-g4";
            else if (postfix == "gbr")
                return "application/x-gbr";
            else if (postfix == "gcd")
                return "application/x-gcd";
            else if (postfix == "gif")
                return "image/gif";
            else if (postfix == "gl2")
                return "application/x-gl2";
            else if (postfix == "gp4")
                return "application/x-gp4";
            else if (postfix == "hgl")
                return "application/x-hgl";
            else if (postfix == "hmr")
                return "application/x-hmr";
            else if (postfix == "hpg")
                return "application/x-hpgl";
            else if (postfix == "hpl")
                return "application/x-hpl";
            else if (postfix == "hqx")
                return "application/mac-binhex40";
            else if (postfix == "hrf")
                return "application/x-hrf";
            else if (postfix == "hta")
                return "application/hta";
            else if (postfix == "htc")
                return "text/x-component";
            else if (postfix == "htm")
                return "text/html";
            else if (postfix == "html")
                return "text/html";
            else if (postfix == "htt")
                return "text/webviewhtml";
            else if (postfix == "htx")
                return "text/html";
            else if (postfix == "icb")
                return "application/x-icb";
            else if (postfix == "ico")
                return "image/x-icon";
            else if (postfix == "ico")
                return "application/x-ico";
            else if (postfix == "iff")
                return "application/x-iff";
            else if (postfix == "ig4")
                return "application/x-g4";
            else if (postfix == "igs")
                return "application/x-igs";
            else if (postfix == "iii")
                return "application/x-iphone";
            else if (postfix == "img")
                return "application/x-img";
            else if (postfix == "ins")
                return "application/x-internet-signup";
            else if (postfix == "isp")
                return "application/x-internet-signup";
            else if (postfix == "ivf")
                return "video/x-ivf";
            else if (postfix == "java")
                return "java/*";
            else if (postfix == "jfif")
                return "image/jpeg";
            else if (postfix == "jpe")
                return "image/jpeg";
            else if (postfix == "jpe")
                return "application/x-jpe";
            else if (postfix == "jpeg")
                return "image/jpeg";
            else if (postfix == "jpg")
                return "image/jpeg";
            else if (postfix == "jpg")
                return "application/x-jpg";
            else if (postfix == "js")
                return "application/x-javascript";
            else if (postfix == "jsp")
                return "text/html";
            else if (postfix == "la1")
                return "audio/x-liquid-file";
            else if (postfix == "lar")
                return "application/x-laplayer-reg";
            else if (postfix == "latex")
                return "application/x-latex";
            else if (postfix == "lavs")
                return "audio/x-liquid-secure";
            else if (postfix == "lbm")
                return "application/x-lbm";
            else if (postfix == "lmsff")
                return "audio/x-la-lms";
            else if (postfix == "ls")
                return "application/x-javascript";
            else if (postfix == "ltr")
                return "application/x-ltr";
            else if (postfix == "m1v")
                return "video/x-mpeg";
            else if (postfix == "m2v")
                return "video/x-mpeg";
            else if (postfix == "m3u")
                return "audio/mpegurl";
            else if (postfix == "m4e")
                return "video/mpeg4";
            else if (postfix == "mac")
                return "application/x-mac";
            else if (postfix == "man")
                return "application/x-troff-man";
            else if (postfix == "math")
                return "text/xml";
            else if (postfix == "mdb")
                return "application/msaccess";
            else if (postfix == "mdb")
                return "application/x-mdb";
            else if (postfix == "mfp")
                return "application/x-shockwave-flash";
            else if (postfix == "mht")
                return "message/rfc822";
            else if (postfix == "mhtml")
                return "message/rfc822";
            else if (postfix == "mi")
                return "application/x-mi";
            else if (postfix == "mid")
                return "audio/mid";
            else if (postfix == "midi")
                return "audio/mid";
            else if (postfix == "mil")
                return "application/x-mil";
            else if (postfix == "mml")
                return "text/xml";
            else if (postfix == "mnd")
                return "audio/x-musicnet-download";
            else if (postfix == "mns")
                return "audio/x-musicnet-stream";
            else if (postfix == "mocha")
                return "application/x-javascript";
            else if (postfix == "movie")
                return "video/x-sgi-movie";
            else if (postfix == "mp1")
                return "audio/mp1";
            else if (postfix == "mp2")
                return "audio/mp2";
            else if (postfix == "mp2v")
                return "video/mpeg";
            else if (postfix == "mp3")
                return "audio/mp3";
            else if (postfix == "mp4")
                return "video/mpeg4";
            else if (postfix == "mpa")
                return "video/x-mpg";
            else if (postfix == "mpd")
                return "application/vnd.ms-project";
            else if (postfix == "mpe")
                return "video/x-mpeg";
            else if (postfix == "mpeg")
                return "video/mpg";
            else if (postfix == "mpg")
                return "video/mpg";
            else if (postfix == "mpga")
                return "audio/rn-mpeg";
            else if (postfix == "mpp")
                return "application/vnd.ms-project";
            else if (postfix == "mps")
                return "video/x-mpeg";
            else if (postfix == "mpt")
                return "application/vnd.ms-project";
            else if (postfix == "mpv")
                return "video/mpg";
            else if (postfix == "mpv2")
                return "video/mpeg";
            else if (postfix == "mpw")
                return "application/vnd.ms-project";
            else if (postfix == "mpx")
                return "application/vnd.ms-project";
            else if (postfix == "mtx")
                return "text/xml";
            else if (postfix == "mxp")
                return "application/x-mmxp";
            else if (postfix == "net")
                return "image/pnetvue";
            else if (postfix == "nrf")
                return "application/x-nrf";
            else if (postfix == "nws")
                return "message/rfc822";
            else if (postfix == "odc")
                return "text/x-ms-odc";
            else if (postfix == "out")
                return "application/x-out";
            else if (postfix == "p10")
                return "application/pkcs10";
            else if (postfix == "p12")
                return "application/x-pkcs12";
            else if (postfix == "p7b")
                return "application/x-pkcs7-certificates";
            else if (postfix == "p7c")
                return "application/pkcs7-mime";
            else if (postfix == "p7m")
                return "application/pkcs7-mime";
            else if (postfix == "p7r")
                return "application/x-pkcs7-certreqresp";
            else if (postfix == "p7s")
                return "application/pkcs7-signature";
            else if (postfix == "pc5")
                return "application/x-pc5";
            else if (postfix == "pci")
                return "application/x-pci";
            else if (postfix == "pcl")
                return "application/x-pcl";
            else if (postfix == "pcx")
                return "application/x-pcx";
            else if (postfix == "pdf")
                return "application/pdf";
            else if (postfix == "pdf")
                return "application/pdf";
            else if (postfix == "pdx")
                return "application/vnd.adobe.pdx";
            else if (postfix == "pfx")
                return "application/x-pkcs12";
            else if (postfix == "pgl")
                return "application/x-pgl";
            else if (postfix == "pic")
                return "application/x-pic";
            else if (postfix == "pko")
                return "application/vnd.ms-pki.pko";
            else if (postfix == "pl")
                return "application/x-perl";
            else if (postfix == "plg")
                return "text/html";
            else if (postfix == "pls")
                return "audio/scpls";
            else if (postfix == "plt")
                return "application/x-plt";
            else if (postfix == "png")
                return "image/png";
            else if (postfix == "png")
                return "application/x-png";
            else if (postfix == "pot")
                return "application/vnd.ms-powerpoint";
            else if (postfix == "ppa")
                return "application/vnd.ms-powerpoint";
            else if (postfix == "ppm")
                return "application/x-ppm";
            else if (postfix == "pps")
                return "application/vnd.ms-powerpoint";
            else if (postfix == "ppt")
                return "application/vnd.ms-powerpoint";
            else if (postfix == "ppt")
                return "application/x-ppt";
            else if (postfix == "pr")
                return "application/x-pr";
            else if (postfix == "prf")
                return "application/pics-rules";
            else if (postfix == "prn")
                return "application/x-prn";
            else if (postfix == "prt")
                return "application/x-prt";
            else if (postfix == "ps")
                return "application/x-ps";
            else if (postfix == "ps")
                return "application/postscript";
            else if (postfix == "ptn")
                return "application/x-ptn";
            else if (postfix == "pwz")
                return "application/vnd.ms-powerpoint";
            else if (postfix == "r3t")
                return "text/vnd.rn-realtext3d";
            else if (postfix == "ra")
                return "audio/vnd.rn-realaudio";
            else if (postfix == "ram")
                return "audio/x-pn-realaudio";
            else if (postfix == "ras")
                return "application/x-ras";
            else if (postfix == "rat")
                return "application/rat-file";
            else if (postfix == "rdf")
                return "text/xml";
            else if (postfix == "rec")
                return "application/vnd.rn-recording";
            else if (postfix == "red")
                return "application/x-red";
            else if (postfix == "rgb")
                return "application/x-rgb";
            else if (postfix == "rjs")
                return "application/vnd.rn-realsystem-rjs";
            else if (postfix == "rjt")
                return "application/vnd.rn-realsystem-rjt";
            else if (postfix == "rlc")
                return "application/x-rlc";
            else if (postfix == "rle")
                return "application/x-rle";
            else if (postfix == "rm")
                return "application/vnd.rn-realmedia";
            else if (postfix == "rmf")
                return "application/vnd.adobe.rmf";
            else if (postfix == "rmi")
                return "audio/mid";
            else if (postfix == "rmj")
                return "application/vnd.rn-realsystem-rmj";
            else if (postfix == "rmm")
                return "audio/x-pn-realaudio";
            else if (postfix == "rmp")
                return "application/vnd.rn-rn_music_package";
            else if (postfix == "rms")
                return "application/vnd.rn-realmedia-secure";
            else if (postfix == "rmvb")
                return "application/vnd.rn-realmedia-vbr";
            else if (postfix == "rmx")
                return "application/vnd.rn-realsystem-rmx";
            else if (postfix == "rnx")
                return "application/vnd.rn-realplayer";
            else if (postfix == "rp")
                return "image/vnd.rn-realpix";
            else if (postfix == "rpm")
                return "audio/x-pn-realaudio-plugin";
            else if (postfix == "rsml")
                return "application/vnd.rn-rsml";
            else if (postfix == "rt")
                return "text/vnd.rn-realtext";
            else if (postfix == "rtf")
                return "application/msword";
            else if (postfix == "rtf")
                return "application/x-rtf";
            else if (postfix == "rv")
                return "video/vnd.rn-realvideo";
            else if (postfix == "sam")
                return "application/x-sam";
            else if (postfix == "sat")
                return "application/x-sat";
            else if (postfix == "sdp")
                return "application/sdp";
            else if (postfix == "sdw")
                return "application/x-sdw";
            else if (postfix == "sit")
                return "application/x-stuffit";
            else if (postfix == "slb")
                return "application/x-slb";
            else if (postfix == "sld")
                return "application/x-sld";
            else if (postfix == "slk")
                return "drawing/x-slk";
            else if (postfix == "smi")
                return "application/smil";
            else if (postfix == "smil")
                return "application/smil";
            else if (postfix == "smk")
                return "application/x-smk";
            else if (postfix == "snd")
                return "audio/basic";
            else if (postfix == "sol")
                return "text/plain";
            else if (postfix == "sor")
                return "text/plain";
            else if (postfix == "spc")
                return "application/x-pkcs7-certificates";
            else if (postfix == "spl")
                return "application/futuresplash";
            else if (postfix == "spp")
                return "text/xml";
            else if (postfix == "ssm")
                return "application/streamingmedia";
            else if (postfix == "sst")
                return "application/vnd.ms-pki.certstore";
            else if (postfix == "stl")
                return "application/vnd.ms-pki.stl";
            else if (postfix == "stm")
                return "text/html";
            else if (postfix == "sty")
                return "application/x-sty";
            else if (postfix == "svg")
                return "text/xml";
            else if (postfix == "swf")
                return "application/x-shockwave-flash";
            else if (postfix == "tdf")
                return "application/x-tdf";
            else if (postfix == "tg4")
                return "application/x-tg4";
            else if (postfix == "tga")
                return "application/x-tga";
            else if (postfix == "tif")
                return "image/tiff";
            else if (postfix == "tif")
                return "application/x-tif";
            else if (postfix == "tiff")
                return "image/tiff";
            else if (postfix == "tld")
                return "text/xml";
            else if (postfix == "top")
                return "drawing/x-top";
            else if (postfix == "torrent")
                return "application/x-bittorrent";
            else if (postfix == "tsd")
                return "text/xml";
            else if (postfix == "txt")
                return "text/plain";
            else if (postfix == "uin")
                return "application/x-icq";
            else if (postfix == "uls")
                return "text/iuls";
            else if (postfix == "vcf")
                return "text/x-vcard";
            else if (postfix == "vda")
                return "application/x-vda";
            else if (postfix == "vdx")
                return "application/vnd.visio";
            else if (postfix == "vml")
                return "text/xml";
            else if (postfix == "vpg")
                return "application/x-vpeg005";
            else if (postfix == "vsd")
                return "application/vnd.visio";
            else if (postfix == "vsd")
                return "application/x-vsd";
            else if (postfix == "vss")
                return "application/vnd.visio";
            else if (postfix == "vst")
                return "application/vnd.visio";
            else if (postfix == "vst")
                return "application/x-vst";
            else if (postfix == "vsw")
                return "application/vnd.visio";
            else if (postfix == "vsx")
                return "application/vnd.visio";
            else if (postfix == "vtx")
                return "application/vnd.visio";
            else if (postfix == "vxml")
                return "text/xml";
            else if (postfix == "wav")
                return "audio/wav";
            else if (postfix == "wax")
                return "audio/x-ms-wax";
            else if (postfix == "wb1")
                return "application/x-wb1";
            else if (postfix == "wb2")
                return "application/x-wb2";
            else if (postfix == "wb3")
                return "application/x-wb3";
            else if (postfix == "wbmp")
                return "image/vnd.wap.wbmp";
            else if (postfix == "wiz")
                return "application/msword";
            else if (postfix == "wk3")
                return "application/x-wk3";
            else if (postfix == "wk4")
                return "application/x-wk4";
            else if (postfix == "wkq")
                return "application/x-wkq";
            else if (postfix == "wks")
                return "application/x-wks";
            else if (postfix == "wm")
                return "video/x-ms-wm";
            else if (postfix == "wma")
                return "audio/x-ms-wma";
            else if (postfix == "wmd")
                return "application/x-ms-wmd";
            else if (postfix == "wmf")
                return "application/x-wmf";
            else if (postfix == "wml")
                return "text/vnd.wap.wml";
            else if (postfix == "wmv")
                return "video/x-ms-wmv";
            else if (postfix == "wmx")
                return "video/x-ms-wmx";
            else if (postfix == "wmz")
                return "application/x-ms-wmz";
            else if (postfix == "wp6")
                return "application/x-wp6";
            else if (postfix == "wpd")
                return "application/x-wpd";
            else if (postfix == "wpg")
                return "application/x-wpg";
            else if (postfix == "wpl")
                return "application/vnd.ms-wpl";
            else if (postfix == "wq1")
                return "application/x-wq1";
            else if (postfix == "wr1")
                return "application/x-wr1";
            else if (postfix == "wri")
                return "application/x-wri";
            else if (postfix == "wrk")
                return "application/x-wrk";
            else if (postfix == "ws")
                return "application/x-ws";
            else if (postfix == "ws2")
                return "application/x-ws";
            else if (postfix == "wsc")
                return "text/scriptlet";
            else if (postfix == "wsdl")
                return "text/xml";
            else if (postfix == "wvx")
                return "video/x-ms-wvx";
            else if (postfix == "xdp")
                return "application/vnd.adobe.xdp";
            else if (postfix == "xdr")
                return "text/xml";
            else if (postfix == "xfd")
                return "application/vnd.adobe.xfd";
            else if (postfix == "xfdf")
                return "application/vnd.adobe.xfdf";
            else if (postfix == "xhtml")
                return "text/html";
            else if (postfix == "xls")
                return "application/vnd.ms-excel";
            else if (postfix == "xls")
                return "application/x-xls";
            else if (postfix == "xlw")
                return "application/x-xlw";
            else if (postfix == "xml")
                return "text/xml";
            else if (postfix == "xpl")
                return "audio/scpls";
            else if (postfix == "xq")
                return "text/xml";
            else if (postfix == "xql")
                return "text/xml";
            else if (postfix == "xquery")
                return "text/xml";
            else if (postfix == "xsd")
                return "text/xml";
            else if (postfix == "xsl")
                return "text/xml";
            else if (postfix == "xslt")
                return "text/xml";
            else if (postfix == "xwd")
                return "application/x-xwd";
            else if (postfix == "x_b")
                return "application/x-x_b";
            else if (postfix == "x_t")
                return "application/x-x_t";
            else
                return "text/plain";
        }
    }
}
