using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Common
{
    class StringHelper
    {
        #region unicode 字符转义
        /// <summary>  
        ///  unicode 解码 
        /// </summary>  
        /// <param name="str"></param>  
        /// <returns></returns>  
        public static string UnicodeDencode(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;
            return Regex.Unescape(str);
        }
        /// <summary>  
        /// 将字符串进行 unicode 编码  
        /// </summary>  
        /// <param name="str"></param>  
        /// <returns></returns>  
        public static string UnicodeEncode(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return str;
            StringBuilder strResult = new StringBuilder();
            if (!string.IsNullOrEmpty(str))
            {
                for (int i = 0; i < str.Length; i++)
                {
                    strResult.Append("\\u");
                    strResult.Append(((int)str[i]).ToString("x4"));
                }
            }
            return strResult.ToString();
        }
        #endregion
    }
}