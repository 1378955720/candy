using System.Data.Common;
using System.Collections.Generic;
namespace Candy.SqlBuilder.ExpressionAnalysis
{
    public class ExpressionModel
    {
        public ExpressionModel(string cmdText, DbParameter[] parameters)
        {
            CmdText = cmdText;
            Parameters = parameters;
        }
        /// <summary>
        /// 转换成的sql语句 all
        /// </summary>
        public string CmdText { get; }
        /// <summary>
        /// 参数化列表 union/where
        /// </summary>
        public DbParameter[] Parameters { get; }
    }

}