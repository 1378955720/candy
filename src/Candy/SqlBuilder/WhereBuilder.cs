﻿using Candy.Common;
using Candy.DbHelper;
using Candy.DBHelper;
using Candy.Extensions;
using Candy.SqlBuilder.AnalysisExpression;
using Candy.SqlBuilder.ExpressionAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Candy.SqlBuilder
{
    public abstract class WhereBuilder<TSQL, TModel> : SqlBuilder<TSQL>
        where TSQL : class, ISqlBuilder
        where TModel : class, ICandyDbModel, new()
    {
        /// <summary>
        /// 
        /// </summary>
        private TSQL This => this as TSQL;

        /// <summary>
        /// 是否or状态
        /// </summary>
        private bool _isOrState = false;

        /// <summary>
        /// or表达式
        /// </summary>
        private readonly List<string> _orExpression = new List<string>();

        #region Constructor
        protected WhereBuilder(ICandyDbContext dbContext) : base(dbContext, typeof(TModel))
        {

        }
        protected WhereBuilder(ICandyDbExecute dbExecute) : base(dbExecute)
        {
            if (string.IsNullOrEmpty(MainTable))
                MainTable = EntityHelper.GetDbTable<TModel>().TableName;
        }
        #endregion

        /// <summary>
        /// 子模型where
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="selector"></param>
        /// <param name="isAdd">是否添加此表达式</param>
        /// <returns></returns>
        public TSQL Where<TSource>(Expression<Func<TSource, bool>> predicate, bool isAdd = true) where TSource : ICandyDbModel, new()
        {
            if (!isAdd) return This;

            var info = GetWhere(predicate);
            AddParameters(info.Parameters);
            return Where(info.CmdText);
        }

        /// <summary>
        /// 主模型重载
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="isAdd">是否添加此表达式</param>
        /// <returns></returns>
        public TSQL Where(Expression<Func<TModel, bool>> selector, bool isAdd = true)
            => Where<TModel>(selector, isAdd);

        /// <summary>
        /// 开始Or where表达式
        /// </summary>
        /// <returns></returns>
        public TSQL WhereStartOr()
        {
            _isOrState = true;
            return This;
        }

        /// <summary>
        /// 结束Or where表达式
        /// </summary>
        /// <returns></returns>
        public TSQL WhereEndOr()
        {
            _isOrState = false;
            if (_orExpression.Count > 0)
            {
                Where(string.Join(" OR ", _orExpression));
                _orExpression.Clear();
            }
            return This;
        }

        /// <summary>
        /// 字符串where语句
        /// </summary>
        /// <param name="where"></param>
        /// <returns></returns>
        public TSQL Where(string where)
        {
            if (_isOrState)
                _orExpression.Add($"({where})");
            else
                base.WhereList.Add($"({where})");
            return This;
        }

        /// <summary>
        /// any
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <exception cref="ArgumentNullException">values is null or length is zero</exception>
        /// <returns></returns>
        public TSQL WhereAny<TSource, TKey>(Expression<Func<TSource, TKey>> selector, IEnumerable<TKey> values) where TSource : ICandyDbModel, new()
            => WhereAny(GetSelector(selector), values);

        /// <summary>
        /// any方法
        /// </summary>
        /// <typeparam name="TKey">key类型</typeparam>
        /// <param name="key">字段名片</param>
        /// <param name="values">值</param>
        /// <returns></returns>
        public TSQL WhereAny<TKey>(string key, IEnumerable<TKey> values)
        {
            if (!values?.Any() ?? true)
                throw new ArgumentNullException(nameof(values));
            if (values.Count() == 1)
            {
                AddParameter(out string index1, values.ElementAt(0));
                return Where(string.Concat(key, $" = @{index1}"));
            }
            AddParameter(out string index, values.ToArray());
            return Where(string.Concat(key, $" = any(@{index})"));
        }

        /// <summary>
        /// any 方法, optional字段
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <exception cref="ArgumentNullException">values is null or length is zero</exception>
        /// <returns></returns>
        public TSQL WhereAny<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, IEnumerable<TKey> values) where TSource : ICandyDbModel, new() where TKey : struct
            => WhereAny(GetSelector(selector), values);

        /// <summary>
        /// any方法
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <exception cref="ArgumentNullException">values is null or length is zero</exception>
        /// <returns></returns>
        public TSQL WhereAny<TKey>(Expression<Func<TModel, TKey>> selector, IEnumerable<TKey> values)
            => WhereAny<TModel, TKey>(selector, values);

        /// <summary>
        /// any 方法, optional字段
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <exception cref="ArgumentNullException">values is null or length is zero</exception>
        /// <returns></returns>
        public TSQL WhereAny<TKey>(Expression<Func<TModel, TKey?>> selector, IEnumerable<TKey> values) where TKey : struct
            => WhereAny<TModel, TKey>(selector, values);

        /// <summary>
        /// not equals any 方法
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <exception cref="ArgumentNullException">values is null or length is zero</exception>
        /// <returns></returns>
        public TSQL WhereNotAny<TSource, TKey>(Expression<Func<TSource, TKey>> selector, IEnumerable<TKey> values) where TSource : ICandyDbModel, new()
            => WhereNotAny(GetSelector(selector), values);

        /// <summary>
        /// not equals any 方法, optional字段
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <exception cref="ArgumentNullException">values is null or length is zero</exception>
        /// <returns></returns>
        public TSQL WhereNotAny<TSource, TKey>(Expression<Func<TSource, TKey?>> selector, IEnumerable<TKey> values) where TSource : ICandyDbModel, new() where TKey : struct
            => WhereNotAny(GetSelector(selector), values);

        /// <summary>
        /// not equals any 方法
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="key"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public TSQL WhereNotAny<TKey>(string key, IEnumerable<TKey> values)
        {
            if (!values?.Any() ?? true)
                throw new ArgumentNullException(nameof(values));
            if (values.Count() == 1)
            {
                AddParameter(out string index1, values.ElementAt(0));
                return Where(string.Concat(key, $" <> @{index1}"));
            }
            AddParameter(out string index, values.ToArray());
            return Where(string.Concat(key, $" <> all(@{index})"));
        }

        /// <summary>
        /// where not in
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="sqlBuilder"></param>
        /// <exception cref="ArgumentNullException">sql is null or empty</exception>
        /// <returns></returns>
        public TSQL WhereNotIn<TSource>(Expression<Func<TSource, object>> selector, ISqlBuilder sqlBuilder) where TSource : ICandyDbModel, new()
        {
            if (sqlBuilder == null)
                throw new ArgumentNullException(nameof(sqlBuilder));
            AddParameters(sqlBuilder.Params);
            return Where($"{GetSelector(selector)} NOT IN ({sqlBuilder.CommandText})");
        }

        /// <summary>
        /// where in
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="sqlBuilder"></param>
        /// <exception cref="ArgumentNullException">value is null or empty</exception>
        /// <returns></returns>
        public TSQL WhereIn<TSource>(Expression<Func<TSource, object>> selector, ISqlBuilder sqlBuilder) where TSource : ICandyDbModel, new()
        {
            if (sqlBuilder == null)
                throw new ArgumentNullException(nameof(sqlBuilder));
            AddParameters(sqlBuilder.Params);
            return Where($"{GetSelector(selector)} IN ({sqlBuilder.CommandText})");
        }

        /// <summary>
        /// where not in
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="sqlBuilder"></param>
        /// <exception cref="ArgumentNullException">sql is null or empty</exception>
        /// <returns></returns>
        public TSQL WhereNotIn(Expression<Func<TModel, object>> selector, ISqlBuilder sqlBuilder)
            => WhereNotIn(selector, sqlBuilder);

        /// <summary>
        /// where in
        /// </summary>
        /// <param name="selector"></param>
        /// <param name="sqlBuilder"></param>
        /// <exception cref="ArgumentNullException">value is null or empty</exception>
        /// <returns></returns>
        public TSQL WhereIn(Expression<Func<TModel, object>> selector, ISqlBuilder sqlBuilder)
            => WhereIn<TModel>(selector, sqlBuilder);

        /// <summary>
        /// where exists 
        /// </summary>
        /// <param name="sqlBuilder"></param>
        /// <exception cref="ArgumentNullException">sqlBuilder is null</exception>
        /// <returns></returns>
        public TSQL WhereExists(ISqlBuilder sqlBuilder)
        {
            if (sqlBuilder == null)
                throw new ArgumentNullException(nameof(sqlBuilder));
            AddParameters(sqlBuilder.Params);
            sqlBuilder.Fields = "1";
            return Where($"EXISTS ({sqlBuilder.CommandText})");
        }

        /// <summary>
        /// where exists 
        /// </summary>
        /// <param name="sqlBuilderSelector"></param>
        /// <returns></returns>
        private TSQL WhereExists(Expression<Func<TModel, ISqlBuilder>> sqlBuilderSelector)
        {
            return This;
        }

        /// <summary>
        /// where not exists 
        /// </summary>
        /// <param name="sqlBuilder"></param>
        /// <exception cref="ArgumentNullException">sqlBuilder is null</exception>
        /// <returns></returns>
        public TSQL WhereNotExists(ISqlBuilder sqlBuilder)
        {
            if (sqlBuilder == null)
                throw new ArgumentNullException(nameof(sqlBuilder));
            AddParameters(sqlBuilder.Params);
            sqlBuilder.Fields = "1";
            return Where($"NOT EXISTS ({sqlBuilder.CommandText})");
        }

        /// <summary>
        /// where any 如果values 是空或长度为0 直接返回空数据(无论 or and 什么条件)
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public TSQL WhereAnyOrDefault<TSource, TKey>(Expression<Func<TSource, TKey>> selector, IEnumerable<TKey> values)
            where TSource : ICandyDbModel, new()
        {
            if (!values?.Any() ?? true) { IsReturnDefault = true; return This; }
            return WhereAny(selector, values);
        }

        /// <summary>
        /// where any 如果values 是空或长度为0 直接返回空数据(无论 or and 什么条件)
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <param name="selector"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public TSQL WhereAnyOrDefault<TKey>(Expression<Func<TModel, TKey>> selector, IEnumerable<TKey> values)
            => WhereAnyOrDefault<TModel, TKey>(selector, values);

        /// <summary>
        /// 可选添加, format写法
        /// </summary>
        /// <param name="isAdd"></param>
        /// <param name="filter"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public TSQL Where(bool isAdd, string filter, params object[] values)
            => isAdd ? Where(filter, values) : This;

        /// <summary>
        /// 可选添加添加func返回的where语句
        /// </summary>
        /// <param name="isAdd"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public TSQL Where(bool isAdd, Func<string> filter)
            => isAdd ? Where(filter.Invoke()) : This;

        /// <summary>
        /// 是否添加 添加func返回的where语句, format格式
        /// </summary>
        /// <param name="isAdd">是否添加</param>
        /// <param name="filter">返回Where(string,object) </param>
        /// <returns></returns>
        public TSQL Where(bool isAdd, Func<(string, object[])> filter)
        {
            if (!isAdd) return This;
            var (sql, ps) = filter.Invoke();
            return Where(sql, ps);
        }


        /// <summary>
        /// 双主键
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="selectorT1"></param>
        /// <param name="selectorT2"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public TSQL Where<T1, T2>(
            Expression<Func<TModel, T1>> selectorT1,
            Expression<Func<TModel, T2>> selectorT2,
            IEnumerable<(T1, T2)> values)
        {
            string t1 = GetSelector(selectorT1), t2 = GetSelector(selectorT2);
            WhereStartOr();
            foreach (var item in values)
                Where(string.Concat(t1, "={0} and ", t2, "={1}"), item.Item1, item.Item2);
            return WhereEndOr();
        }

        /// <summary>
        /// 三主键
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <param name="selectorT1"></param>
        /// <param name="selectorT2"></param>
        /// <param name="selectorT3"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public TSQL Where<T1, T2, T3>(
            Expression<Func<TModel, T1>> selectorT1,
            Expression<Func<TModel, T2>> selectorT2,
            Expression<Func<TModel, T3>> selectorT3,
            IEnumerable<(T1, T2, T3)> values)
        {

            string t1 = GetSelector(selectorT1), t2 = GetSelector(selectorT2), t3 = GetSelector(selectorT3);
            WhereStartOr();
            foreach (var item in values)
                Where(string.Concat(t1, "={0} and ", t2, "={1} and ", t3, "={2}"), item.Item1, item.Item2, item.Item3);
            return WhereEndOr();
        }

        /// <summary>
        /// 四主键
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <param name="selectorT1"></param>
        /// <param name="selectorT2"></param>
        /// <param name="selectorT3"></param>
        /// <param name="selectorT4"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public TSQL Where<T1, T2, T3, T4>(
            Expression<Func<TModel, T1>> selectorT1,
            Expression<Func<TModel, T2>> selectorT2,
            Expression<Func<TModel, T3>> selectorT3,
            Expression<Func<TModel, T4>> selectorT4,
            IEnumerable<(T1, T2, T3, T4)> values)
        {
            string t1 = GetSelector(selectorT1), t2 = GetSelector(selectorT2), t3 = GetSelector(selectorT3), t4 = GetSelector(selectorT4);
            WhereStartOr();
            foreach (var item in values)
                Where(string.Concat(t1, "={0} and ", t2, "={1} and ", t3, "={2} and ", t4, "={3}"), item.Item1, item.Item2, item.Item3, item.Item4);
            return WhereEndOr();
        }

        /// <summary>
        /// where format 写法
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public TSQL Where(string filter, params object[] values)
        {
            if (string.IsNullOrEmpty(filter))
                throw new ArgumentNullException(nameof(filter));
            if (!values?.Any() ?? true)
                return Where(SqlHelper.GetNullSql(filter, @"\{\d\}"));

            for (int i = 0; i < values.Length; i++)
            {
                var index = string.Concat("{", i, "}");
                if (filter.IndexOf(index, StringComparison.Ordinal) == -1)
                    throw new ArgumentException(nameof(filter));
                if (values[i] == null)
                    filter = SqlHelper.GetNullSql(filter, index.Replace("{", @"\{").Replace("}", @"\}"));
                else
                {
                    AddParameter(out string pIndex, values[i]);
                    filter = filter.Replace(index, "@" + pIndex);
                }
            }
            return Where(filter);
        }

        #region SqlGenerator
        protected string GetSelector<TSource>(Expression<Func<TSource, object>> selector)
           => SqlGenerator.GetSelector(selector, DbExecute.ConnectionOptions.DataBaseType);

        protected string GetSelector<TSource, TKey>(Expression<Func<TSource, TKey>> selector)
            => SqlGenerator.GetSelector(selector, DbExecute.ConnectionOptions.DataBaseType);

        protected ExpressionModel GetWhere(Expression<Func<TModel, bool>> predicate)
            => SqlGenerator.GetWhere(predicate, DbExecute.ConnectionOptions.GetDbParameter, DbExecute.ConnectionOptions.DataBaseType);

        protected ExpressionModel GetWhere<TSource>(Expression<Func<TSource, bool>> predicate)
            => SqlGenerator.GetWhere(predicate, DbExecute.ConnectionOptions.GetDbParameter, DbExecute.ConnectionOptions.DataBaseType);
        #endregion

    }
}
