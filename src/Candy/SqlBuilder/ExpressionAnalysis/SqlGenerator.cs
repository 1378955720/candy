using System.Collections.Generic;
using System.Data.Common;
using System;
using System.Linq.Expressions;
using Candy.Model;
using Candy.SqlBuilder.ExpressionAnalysis;
using Candy.DbHelper;

namespace Candy.SqlBuilder.ExpressionAnalysis
{
    /// <summary>
    /// lambda表达式转为where条件sql
    /// </summary>
    public class SqlGenerator
    {
        public static string GetWhereByLambda<T>(Expression<Func<T, bool>> predicate, DataBaseType databaseType)
        {
            ConditionBuilder conditionBuilder = new ConditionBuilder(databaseType);
            conditionBuilder.Build(predicate);

            for (int i = 0; i < conditionBuilder.Arguments.Length; i++)
            {
                object ce = conditionBuilder.Arguments[i];
                switch (ce)
                {
                    case null:
                        conditionBuilder.Arguments[i] = DBNull.Value;
                        break;
                    case string:
                    case char:
                        if (ce.ToString().ToLower().Trim().IndexOf(@"in(") == 0 ||
                            ce.ToString().ToLower().Trim().IndexOf(@"not in(") == 0 ||
                            ce.ToString().ToLower().Trim().IndexOf(@" like '") == 0 ||
                            ce.ToString().ToLower().Trim().IndexOf(@"not like") == 0)
                        {
                            conditionBuilder.Arguments[i] = string.Format(" {0} ", ce.ToString());
                        }
                        else
                            goto default;
                        //{
                        //	//****************************************
                        //	conditionBuilder.Arguments[i] = string.Format("'{0}'", ce.ToString());
                        //}
                        break;
                    case DateTime:
                        goto default;

                    case int:
                    case long:
                    case short:
                    case decimal:
                    case double:

                    case float:
                    case bool:
                    case byte:
                    case sbyte:
                    case ValueType:
                        conditionBuilder.Arguments[i] = ce.ToString();
                        break;

                    default:
                        conditionBuilder.Arguments[i] = string.Format("'{0}'", ce.ToString());
                        break;
                }

            }
            string strWhere = string.Format(conditionBuilder.Condition, conditionBuilder.Arguments);
            return strWhere;
        }
        #region Expression 转成 where
        /// <summary>
        /// Expression 转成 Where String
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate"></param>
        /// <param name="databaseType">数据类型（用于字段是否加引号）</param>
        /// <returns></returns>
        public static ExpressionModel GetWhere<TModel>(Expression<Func<TModel, bool>> predicate, Func<string, object, DbParameter> fnCreateParameter, DataBaseType dataBaseType)
        {
            ConditionBuilder conditionBuilder = new ConditionBuilder(dataBaseType);
            conditionBuilder.Build(predicate);

            var argumentsLength = conditionBuilder.Arguments.Length;

            var ps = new DbParameter[argumentsLength];

            var indexs = new string[argumentsLength];

            for (int i = 0; i < argumentsLength; i++)
            {
                var index = EntityHelper.ParamsIndex;
                ps[i] = fnCreateParameter(index, conditionBuilder.Arguments[i]);
                indexs[i] = index;
            }
            string cmdText = string.Format(conditionBuilder.Condition, indexs);
            return new ExpressionModel(cmdText, ps);
        } 
        
        public static string GetSelector<TModel>(Expression<Func<TModel, object>> selector, DataBaseType databaseType)
        {
            ConditionBuilder conditionBuilder = new ConditionBuilder(databaseType);
            conditionBuilder.Build(selector);

            return conditionBuilder.Condition;
        }public static string GetSelector<TModel,TKey>(Expression<Func<TModel, TKey>> selector, DataBaseType databaseType)
        {
            ConditionBuilder conditionBuilder = new ConditionBuilder(databaseType);
            conditionBuilder.Build(selector);

            return conditionBuilder.Condition;
        }
        // public static ExpressionModel GetSqlSelector<TModel1, TModel2>(Expression<Func<TModel1, TModel2, bool>> predicate, Func<string, object, DbParameter> fnCreateParameter, DataBaseType dataBaseType)
        // {
        //     return new ExpressionModel();
        // }
        #endregion
    }
}
