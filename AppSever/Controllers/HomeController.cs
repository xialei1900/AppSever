using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Common;
using System.Data.SqlClient;
using System.Data.Common;
using System.Data;
using System.Net;
using System.IO;
using System.Text;
using HtmlAgilityPack;

namespace AppSever.Controllers
{
    public class HomeController : Controller
    {

        HttpRequest request = System.Web.HttpContext.Current.Request;
        Common.SqlHelper sqlHelper = new SqlHelper();
        //
        // GET: /Home/
        public ActionResult Index()
        {
            //return View("/Views/Home/Index.cshtml");

            string action = "";
            ContentResult result = null;

            action = request.Form["action"].Trim();
            switch (action)
            {
                case "Login":
                    result = Login();
                    break;
                case "Register":
                    result = Register();
                    break;
                case "getRemindToday":
                    result = getRemindToday();
                    break;
                case "getData":
                    result = getData();
                    break;
                case "getDataByself":
                    result = getDataByself();
                    break;
            }
            return result;
        }

        #region 登录
        public ContentResult Login()
        {
            string loginId = request.Form["loginId"].Trim();
            string loginPwd = request.Form["loginPwd"].Trim();

            if (loginId == "" || loginPwd == "")
                return Content("获取表单数据失败！");
            else
            {
                Common.SqlHelper sqlHelper = new SqlHelper();
                DataSet ds = sqlHelper.GetDataSet("select * from Users where loginId='" + loginId + "' and loginPwd='" + loginPwd + "'");
                //DataSet ds = sqlHelper.GetDataSet("select * from Users");
                if (ds != null)
                {
                    return Content(Common.JsonHelper.ToJson(ds.Tables[0]));
                }
                else
                {
                    return Content("用户名或密码错误，请重试！");
                }
            }
        }
        #endregion

        #region 注册
        public ContentResult Register()
        {
            string registerId = request.Form["registerId"].Trim();
            string registerPwd = request.Form["registerPwd"].Trim();
            string registerUserName = request.Form["registerUserName"].Trim();


            int state = sqlHelper.ExecuteSql("insert into Users (userName,loginId,loginPwd) values ('" + registerUserName + "'," + registerId + ",'" + registerPwd + "')");
            if (state > 0)
                //return Content("Id:" + registerId + " 密码:" + registerPwd + " 名称:" + registerUserName);
                return Content("S");
            else
                return Content("E");
        }
        #endregion

        #region 获取每日提醒数据更新
        public ContentResult getRemindToday()
        {
            string expectedDate = request.Form["expectedDate"].Trim();

            DateTime eDate = Common.DateHelper.CStrToDate(expectedDate);
            double betweendays = Common.DateHelper.DateDiff("day", DateTime.Now, eDate);
            int count = 280 - (int)betweendays;   //怀孕天数
            int week = 1;
            int day = 1;
            if (count >= 7)
            {
                week = count / 7;       //当前怀孕周数
                day = count % 7 + 1;    //该周怀孕天数
            }
            else {
                day = count;
            }

            DataSet ds = sqlHelper.GetDataSet("select babyPhysical,momPhysical from remindDailyData where week="+week+" and day="+day+"");
            return Content(Common.JsonHelper.ToJson(ds.Tables[0]));
            
           
        }
        #endregion

        #region html解析
        public ContentResult getData()
        {
            string html;
            string babyPhysical;
            string momPhysical;
            string nutritionRecipes;

            string week = request.Form["week"].Trim();
            string day = request.Form["day"].Trim();

            //获取html
            HttpWebRequest rq = (HttpWebRequest)HttpWebRequest.Create("http://www.mama.cn/qrqm/" + week + "/?day=" + day);    //创建一个请求示例
            HttpWebResponse response = (HttpWebResponse)rq.GetResponse();　　//获取响应，即发送请求
            Stream responseStream = response.GetResponseStream();
            StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
            html = streamReader.ReadToEnd();
            //解析html
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);
            HtmlNode node = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/aside/dl[1]/dd[1]");
            babyPhysical = node.InnerText.Trim();
            node = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/aside/dl[1]/dd[2]");
            momPhysical = node.InnerText.Trim();
            node = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/aside/div[4]/dl");
            nutritionRecipes = node.InnerText.Trim();
            //保存至数据库
            int state = sqlHelper.ExecuteSql("insert into dailyData (week,day,babyPhysical,momPhysical,nutritionRecipes) values ("+week+","+day+",'" + babyPhysical + "','" + momPhysical + "','" + nutritionRecipes + "')");

            if (state < 1)
                return Content("E");
            else
                return Content("S");
        }
        #endregion

        #region 自动爬取数据
        public ContentResult getDataByself()
        {
            string html;
            string babyPhysical ="";
            string momPhysical = "";
            bool flag = true;
            string week = request.Form["week"].Trim();

            HtmlDocument doc = new HtmlDocument();
            for (int day = 1; day <= 7; day++)
            {
                //获取html
                HttpWebRequest rq = (HttpWebRequest)HttpWebRequest.Create("http://www.mama.cn/qrqm/" + week + "/?day=" + day);    //创建一个请求示例
                HttpWebResponse response = (HttpWebResponse)rq.GetResponse();　　//获取响应，即发送请求
                Stream responseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
                html = streamReader.ReadToEnd();
                //解析html
                doc.LoadHtml(html);
                HtmlNode node = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/aside/dl[1]/dd[1]");
                babyPhysical = node.InnerText.Trim();
                node = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/aside/dl[1]/dd[2]");
                momPhysical = node.InnerText.Trim();
                //node = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/aside/div[4]/dl");
                //nutritionRecipes = node.InnerText.Trim();
                //保存至数据库
                int state = sqlHelper.ExecuteSql("insert into remindDailyData (week,day,babyPhysical,momPhysical) values (" + week + "," + day + ",'" + babyPhysical + "','" + momPhysical + "')");
                //w = week;
                //d = day;
                if (state < 1)
                    flag = false;
            }

            if (flag)
                return Content("S");
            else
                return Content("E");
        }
        #endregion
    }
}