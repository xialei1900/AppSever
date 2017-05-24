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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                case "SetExpectedDate":
                    result = SetExpectedDate();
                    break;
                case "getRemindToday":
                    result = getRemindToday();
                    break;
                //case "getData":
                //    result = getData();
                //    break;
                //case "getDataByself":
                //    result = getDataByself();
                //    break;
                //case "getFood":
                //    result = getFood();
                //    break;
                case "getFoodList":
                    result = getFoodList();
                    break;
                case "updateRemind":
                    result = updateRemind();
                    break;
                case "getItemList":
                    result = getItemList();
                    break;
                case "getCheckList":
                    result = getCheckList();
                    break;
                case "searchFoodByName":
                    result = searchFoodByName();
                    break;
                case "getQuestionList":
                    result = getQuestionList();
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

            int state = sqlHelper.ExecuteSql("insert into Users (userName,loginId,loginPwd) values ('"
                + registerUserName + "'," + registerId + ",'" + registerPwd + "')");
            if (state > 0)
                return Content("S");
            else
                return Content("E");
        }
        #endregion

        #region SetExpectedDate
        public ContentResult SetExpectedDate()
        {
            string userName = request.Form["userName"].Trim();
            string expectedDate = request.Form["expectedDate"].Trim();

            int state = sqlHelper.ExecuteSql("update Users set expectedDate='"
                + expectedDate + "' where userName='" + userName + "'");
            if (state > 0)
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
            else
            {
                day = count;
            }

            DataSet ds_1 = sqlHelper.GetDataSet("select top(1) babyPhysical,momPhysical from remindDailyData where week=" + week + " and day=" + day);
            DataSet ds_2 = sqlHelper.GetDataSet("select top(1) checkTimes,checkWeek from pregnancyCheck where checkWeek>=" + week);
            DataSet ds_3 = sqlHelper.GetDataSet("select top(1) nutritionRecipes from recipes where week=" + week + " and day=" + day);

            DataTable table = ds_1.Tables[0];
            table.Columns.Add("week");
            table.Columns.Add("day");
            table.Columns.Add("checkTimes");
            table.Columns.Add("checkWeek");
            table.Columns.Add("nutritionRecipes");

            table.Rows[0]["week"] = week;
            table.Rows[0]["day"] = day;
            table.Rows[0]["checkTimes"] = ds_2.Tables[0].Rows[0]["checkTimes"];
            table.Rows[0]["checkWeek"] = ds_2.Tables[0].Rows[0]["checkWeek"];
            table.Rows[0]["nutritionRecipes"] = ds_3.Tables[0].Rows[0]["nutritionRecipes"];

            return Content(Common.JsonHelper.ToJson(table));


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
            //HtmlNode node = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/aside/dl[1]/dd[1]");
            //babyPhysical = node.InnerText.Trim();
            //node = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/aside/dl[1]/dd[2]");
            //momPhysical = node.InnerText.Trim();
            HtmlNode node = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/aside/div[4]/dl");
            nutritionRecipes = node.InnerText.Trim();
            //保存至数据库
            //int state = sqlHelper.ExecuteSql("insert into dailyData (week,day,babyPhysical,momPhysical,nutritionRecipes) values ("+week+","+day+",'" + babyPhysical + "','" + momPhysical + "','" + nutritionRecipes + "')");

            //if (state < 1)
            //    return Content("E");//
            //else
            return Content(nutritionRecipes);
        }
        #endregion

        #region 自动爬取数据
        public ContentResult getDataByself()
        {
            string html;
            string babyPhysical = "";
            string momPhysical = "";
            string nutritionRecipes = "";
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
                //HtmlNode node = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/aside/dl[1]/dd[1]");
                //babyPhysical = node.InnerText.Trim();
                //node = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/aside/dl[1]/dd[2]");
                //momPhysical = node.InnerText.Trim();
                HtmlNode node = doc.DocumentNode.SelectSingleNode("/html/body/div[4]/div/aside/div[4]/dl");
                nutritionRecipes = node.InnerText.Trim();
                //保存至数据库
                //int state = sqlHelper.ExecuteSql("insert into remindDailyData (week,day,babyPhysical,momPhysical) values (" + week + "," + day + ",'" + babyPhysical + "','" + momPhysical + "')");
                int state = sqlHelper.ExecuteSql("insert into recipes (week,day,nutritionRecipes) values (" + week + "," + day + ",'" + nutritionRecipes + "')");
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

        #region MyRegion
        public ContentResult getFood()
        {
            //获取html
            int j = 0;
            for (int i = 558; i <= 572; i++)
            {
                HttpWebRequest rq = (HttpWebRequest)HttpWebRequest.Create("http://www.mama.cn/index.php?g=Home&a=QrqmAjax&d=safeFoodsById&id=" + i + "&age=1");    //创建一个请求示例
                rq.Method = "Get";
                HttpWebResponse response = (HttpWebResponse)rq.GetResponse();　　//获取响应，即发送请求
                Stream responseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8);
                string jsonResult = streamReader.ReadToEnd();

                jsonResult = Common.StringHelper.UnicodeDencode(jsonResult);
                JObject jobject = JObject.Parse(jsonResult);
                int state = sqlHelper.ExecuteSql("insert into foodSafe (nameId,sort,name,alias,yunma,yunma_desc,big_pic,small_pic) values (" + jobject["id"] + "," + jobject["sort"] + ",'" + jobject["name"] + "','" + jobject["alias"] + "'," + jobject["yunma"] + ",'" + jobject["yunma_desc"] + "','" + jobject["big_pic"] + "','" + jobject["small_pic"] + "')");
                j = i;
            }

            return Content("nameId:" + j);
            //return Content(jsonResult);
        }
        #endregion

        #region 获取FoodList
        public ContentResult getFoodList()
        {
            string sort = request.Form["sort"].Trim();
            DataSet ds = sqlHelper.GetDataSet("select * from foodSafe where sort=" + sort);
            return Content(Common.JsonHelper.ToJson(ds.Tables[0]));

        }
        #endregion

        #region updateRemind
        public ContentResult updateRemind()
        {
            string week = request.Form["week"].Trim();
            string day = request.Form["day"].Trim();

            DataSet ds_1 = sqlHelper.GetDataSet("select top(1) babyPhysical,momPhysical from remindDailyData where week=" + week + " and day=" + day);
            DataSet ds_2 = sqlHelper.GetDataSet("select top(1) checkTimes,checkWeek from pregnancyCheck where checkWeek>=" + week);
            DataSet ds_3 = sqlHelper.GetDataSet("select top(1) nutritionRecipes from recipes where week=" + week + " and day=" + day);

            DataTable table = ds_1.Tables[0];
            table.Columns.Add("week");
            table.Columns.Add("day");
            table.Columns.Add("checkTimes");
            table.Columns.Add("checkWeek");
            table.Columns.Add("nutritionRecipes");

            table.Rows[0]["week"] = week;
            table.Rows[0]["day"] = day;
            table.Rows[0]["checkTimes"] = ds_2.Tables[0].Rows[0]["checkTimes"];
            table.Rows[0]["checkWeek"] = ds_2.Tables[0].Rows[0]["checkWeek"];
            table.Rows[0]["nutritionRecipes"] = ds_3.Tables[0].Rows[0]["nutritionRecipes"];

            return Content(Common.JsonHelper.ToJson(table));
        }
        #endregion

        #region getItemList
        public ContentResult getItemList()
        {
            string week = request.Form["week"].Trim();

            DataSet ds_1 = sqlHelper.GetDataSet("select top(7) week,day,babyPhysical,momPhysical from remindDailyData where week=" + week);
            DataSet ds_2 = sqlHelper.GetDataSet("select top(1) checkTimes,checkWeek from pregnancyCheck where checkWeek>=" + week);
            DataSet ds_3 = sqlHelper.GetDataSet("select top(7) nutritionRecipes from recipes where week=" + week);

            DataTable table = ds_1.Tables[0];
            table.Columns.Add("checkTimes");
            table.Columns.Add("checkWeek");
            table.Columns.Add("nutritionRecipes");

            for (int i = 0; i < 7; i++)
            {
                table.Rows[i]["checkTimes"] = ds_2.Tables[0].Rows[0]["checkTimes"];
                table.Rows[i]["checkWeek"] = ds_2.Tables[0].Rows[0]["checkWeek"];
                table.Rows[i]["nutritionRecipes"] = ds_3.Tables[0].Rows[i]["nutritionRecipes"];
            }

            return Content(Common.JsonHelper.ToJson(table));
        }
        #endregion

        #region getCheckList
        public ContentResult getCheckList()
        {
            string week = request.Form["week"].Trim();
            string type = request.Form["type"].Trim();
            DataSet ds = new DataSet();
            switch (type)
            {
                case "incrase":
                    ds = sqlHelper.GetDataSet("select top(1) * from pregnancyCheck where checkWeek>=" + week);
                    break;
                case "reduce":
                    ds = sqlHelper.GetDataSet("select top(1) * from pregnancyCheck where checkWeek<=" + week + " order by checkWeek desc");
                    break;
            }
            return Content(Common.JsonHelper.ToJson(ds.Tables[0]));
        }
        #endregion

        #region searchFoodByName
        public ContentResult searchFoodByName()
        {
            string keyWord = request.Form["keyWord"].Trim();
            DataSet ds = sqlHelper.GetDataSet("select * from foodSafe where alias like '%" + keyWord + "%' or name like '%" + keyWord + "%'");
            if (ds == null)
                return Content("E");
            else
                return Content(Common.JsonHelper.ToJson(ds.Tables[0]));
        }
        #endregion

        #region getQuestionList
        public ContentResult getQuestionList()
        {
            DataSet ds = sqlHelper.GetDataSet("select * from questionList order by addTime desc");
            if (ds == null)
                return Content("E");
            else
                return Content(Common.JsonHelper.ToJson(ds.Tables[0]));
        }
        #endregion
    }
}