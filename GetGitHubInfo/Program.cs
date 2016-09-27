using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace GetGitHubInfo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("当前I开始发送请求...，请稍候!:" + 1);
            Random r = new Random();
            string strPageSize = Save("1","android", true);
            if (strPageSize != "")
            {
                int pageSize = Convert.ToInt32(strPageSize);
                int ran = r.Next(2, 8);  //产生随机请求时间间隔
                int sleepTime = 1000 * 60 * ran;
                for (int i = 2; i <= pageSize; i++)
                {
                    Console.WriteLine("当前I睡眠中...，请稍候!:" + i);
                    
                    StopWatch sw = new StopWatch(Console.CursorLeft, Console.CursorTop, sleepTime);
                    sw.Start();
                    Thread.Sleep(sleepTime);
                    sw.finsh();
                    Console.WriteLine();
                    Console.WriteLine("当前I开始发送请求...，请稍候!:" + i);
                    Save(i + "", "android");
                    Console.WriteLine("当前I请求完成!:" + i);
                }
                Console.ReadLine();
            }
        }

        /// <summary>
        /// 获取数据并保存
        /// </summary>
        /// <param name="index">当前第几页</param>
        /// <param name="name">搜索的关键定</param>
        /// <param name="isOne">是否是第一次请求，主要用于返回总页数</param>
        /// <returns></returns>
        public static string Save(string index, string name, bool isOne = false)
        {
            //第一步声明HtmlAgilityPack.HtmlDocument实例
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            Dictionary<string, string> result = HttpGET(index, name);
            string html = result["result"];  //获取返回的html 标签
            //第二步加载html文档
            doc.LoadHtml(html);
            //获取总页数
            string strPageSize = "";
            if (isOne)
            {
                HtmlAgilityPack.HtmlNodeCollection htmlnode = doc.DocumentNode.SelectNodes("//div[@class='paginate-container']/div[@class='pagination']/a");
                strPageSize = htmlnode[htmlnode.Count - 2].InnerText;
                Console.WriteLine("总页数是：" + strPageSize);
                Console.WriteLine();
                Console.WriteLine();
                htmlnode = null;
            }
            //获取对应ul下的所有li
            HtmlAgilityPack.HtmlNodeCollection collection = doc.DocumentNode.SelectNodes("//ul[@class='repo-list js-repo-list']/li");
            if (collection == null)
            {
                return "";
            }
            foreach (HtmlAgilityPack.HtmlNode item in collection)
            {
                string strURL = "";
                HtmlAgilityPack.HtmlNode mItem;
                mItem = item.SelectSingleNode("//h3[@class='repo-list-name']/a");
                strURL = "https://github.com" + mItem.Attributes["href"].Value;

                if (RedisCacheHelper.Exists(strURL))
                {
                    Dictionary<string, string> mResult = RedisCacheHelper.Get<Dictionary<string, string>>(strURL);
                    Console.WriteLine("重新取出的数据...");
                    Console.WriteLine("名字是：" + mResult["name"]);
                    Console.WriteLine("地址是：" + mResult["url"]);
                    Console.WriteLine("说明是：" + mResult["detailed"]);
                    Console.WriteLine("更新时间是：" + mResult["updateTime"]);
                    Console.WriteLine("Stargazers是：" + mResult["stargazers"]);
                    Console.WriteLine("Forks是：" + mResult["forks"]);
                    Console.WriteLine();
                    item.RemoveAll();
                    strURL = "";
                    //同一路径存在表示当前项目已爬过，跳过即可
                    continue;
                }
                Dictionary<string, string> mDic = new Dictionary<string, string>();
                mDic.Add("name", mItem.InnerText.Replace("/n", "").Trim());
                mDic.Add("url", strURL);
                Console.WriteLine("名字是：" + mItem.InnerText.Replace("/n", "").Trim());
                Console.WriteLine("地址是：" + strURL);
                mItem.RemoveAll();
                mItem = item.SelectSingleNode("//p[@class='repo-list-description']");
                mDic.Add("detailed", mItem.InnerText == null ? "" : mItem.InnerText.Replace("/n", "").Trim());
                Console.WriteLine("说明是：" + mItem.InnerText == null ? "" : mItem.InnerText.Replace("/n", "").Trim());
                mItem.RemoveAll();
                mItem = item.SelectSingleNode("//p[@class='repo-list-meta']");
                mDic.Add("updateTime", mItem.InnerText == null ? "" : mItem.InnerText.Replace("/n", "").Trim());
                Console.WriteLine("更新时间是：" + mItem.InnerText.Replace("/n", "").Trim());
                mItem = item.SelectSingleNode("//div[@class='repo-list-stats']/a[@aria-label='Stargazers']");
                mDic.Add("stargazers", mItem.InnerText == null ? "" : mItem.InnerText.Replace("/n", "").Trim());
                Console.WriteLine("Stargazers是：" + mItem.InnerText.Replace("/n", "").Trim());
                mItem = item.SelectSingleNode("//div[@class='repo-list-stats']/a[@aria-label='Forks']");
                mDic.Add("forks", mItem.InnerText == null ? "" : mItem.InnerText.Replace("/n", "").Trim());
                Console.WriteLine("Forks是：" + mItem.InnerText.Replace("/n", "").Trim());
                Console.WriteLine();
                RedisCacheHelper.Add<Dictionary<string, string>>(strURL, mDic);
                item.RemoveAll();
                mDic.Clear();
                strURL = "";
            }
            //Console.ReadLine();
            doc = null;
            result = null;
            collection = null;
            return strPageSize;
        }
        #region GET请求
        /// <summary>
        /// 发送请求 GET
        /// </summary>
        /// <param name="Url">请求URL</param>
        /// <param name="ContentType">内容类型  可以为null</param>
        /// <param name="Headers">头文件 可以为null </param>
        /// <param name="PostData">post参数内容 可以为null </param>
        /// <returns></returns>
        public static Dictionary<string, string> HttpGET(string index, string name)
        {
            string str = "https://github.com/search?o=desc&p=" + index + "&q=" + name + "&ref=searchresults&s=stars&type=Repositories&utf8=%E2%9C%93";
            return SendRequest(str, "GET", null, null, null);
        }
        #endregion
        #region 公共方法
        /// <summary>
        /// 发送请求
        /// </summary>
        /// <param name="Url">请求URL</param>
        /// <param name="ContentType">内容类型  可以为null</param>
        /// <param name="Headers">头文件 可以为null </param>
        /// <param name="PostData">post参数内容 可以为null </param>
        /// <returns>code and result</returns>
        public static Dictionary<string, string> SendRequest(string Url, string Method, string ContentType, Dictionary<string, string> Headers, string PostData)
        {
            Dictionary<string, string> responseResult = new Dictionary<string, string>();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            //设置请求模式
            request.Method = Method;

            //设置内容类型
            if (ContentType != null)
            {
                request.ContentType = ContentType;
            }

            //设置Header
            if (Headers != null)
            {
                foreach (KeyValuePair<string, string> para in Headers)
                {
                    request.Headers.Add(para.Key, para.Value);
                }
            }

            //只有参数不为空的时候才发送
            if (PostData != null)
            {
                //将字符串转换为字节
                byte[] postdata = Encoding.UTF8.GetBytes(PostData);
                request.ContentLength = postdata.Length;

                using (Stream reqStream = request.GetRequestStream())
                {
                    reqStream.Write(postdata, 0, postdata.Length);
                    reqStream.Close();
                }
            }

            string result = "";
            int statusCode = 200;
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream myResponseStream = response.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                statusCode = (int)response.StatusCode;
                result = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
            }
            catch (WebException ex)
            {
                HttpWebResponse res = ex.Response as HttpWebResponse;
                Stream myResponseStream = res.GetResponseStream();
                StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
                statusCode = (int)res.StatusCode;
                result = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
            }
            responseResult.Add("code", statusCode + "");
            responseResult.Add("result", result);
            return responseResult;
        }
        #endregion
        #region 倒计时线程
        class StopWatch
        {
            private int Interval = 1000;              //时间间隔，单位毫秒
            private int Time = 0;                        //所显示的时间
            private int left = 0;
            private int top = 0;
            private Thread timer;

            public StopWatch() { }
            public StopWatch(int left, int top, int time)
            {
                this.left = left;
                this.top = top;
                Time = time;
            }

            public void Start()
            {
                timer = new Thread(new ThreadStart(Timer));  //新建一个线程，该线程调用Timer()
                timer.Start();                               //启动线程
                Console.CursorVisible = false;   //隐藏光标

            }
            private void Timer()
            {
                while (true)
                {
                    Display();                               //显示秒表计数
                    Thread.Sleep(Interval);         //等待1秒后再执行Timer()刷新计数
                    Time = Time - 1000;                                 //秒数加1
                }
            }
            private void Display()
            {
                Console.SetCursorPosition(left, top);
                Console.Write("剩余:[" + Time / 1000 + "]秒");
            }
            public void finsh()
            {
                if (timer != null)
                    timer.Abort();                              //终止线程,用于停止秒表
                Console.SetCursorPosition(left, top);
                Console.Write("剩余:[0]秒");
            }
        }
        #endregion

    }
}
