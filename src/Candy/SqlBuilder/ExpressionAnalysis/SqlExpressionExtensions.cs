using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Candy.Model;
namespace Candy.SqlBuilder.ExpressionAnalysis
{
    internal static class SqlExpressionExtensions
    {
        private static readonly IReadOnlyDictionary<ExpressionType, string> ExpressionOperator = new Dictionary<ExpressionType, string>
        {
            [ExpressionType.And] = "AND",
            [ExpressionType.AndAlso] = "AND",
            [ExpressionType.Equal] = "=",
            [ExpressionType.GreaterThan] = ">",
            [ExpressionType.GreaterThanOrEqual] = ">=",
            [ExpressionType.LessThan] = "<",
            [ExpressionType.LessThanOrEqual] = "<=",
            [ExpressionType.NotEqual] = "<>",
            [ExpressionType.Or] = "OR",
            [ExpressionType.OrElse] = "OR",
            [ExpressionType.Add] = "+",
            [ExpressionType.AddChecked] = "+",
            [ExpressionType.Subtract] = "-",
            [ExpressionType.SubtractChecked] = "-",
            [ExpressionType.Divide] = "/",
            [ExpressionType.Multiply] = "*",
            [ExpressionType.MultiplyChecked] = "*",

        };

        /// <summary>
        /// 成员表达式改成数据库字段 a.Xxx-> a."xxx"
        /// </summary>
        /// <param name="mb"></param>
        /// <returns></returns>
        public static string ToDatebaseField(this MemberExpression mb)
        {
            return string.Concat(mb.ToString().ToLower().Replace(".", ".\""), '"');
        }

        /// <summary>
		/// 递归member表达式, 针对optional字段, 从 a.xxx.Value->a.xxx
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static MemberExpression GetOriginalExpression(this MemberExpression node)
        {
            if (node.NodeType == ExpressionType.MemberAccess && node.Expression is MemberExpression me)
                return GetOriginalExpression(me);
            return node;
        }

        /// <summary>
        /// 获取是否字段加双引号
        /// </summary>
        /// <param name="databaseType"></param>
        /// <returns></returns>
        public static bool GetWithQuotationMarks(this DataBaseType databaseType) => databaseType switch
        {
            DataBaseType.PostgreSql or DataBaseType.Oracle => true,
            _ => false,
        };

        /// <summary>
        /// 运算符转换
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string ExpressionTypeCast(this ExpressionType type)
            => ExpressionOperator.TryGetValue(type, out var value)
            ? value
            : throw new NotSupportedException(type + "is not supported.");

        /// <summary>
        /// 空与非空运算符转换
        /// </summary>
        /// <param name="opr"></param>
        /// <returns></returns>
        public static string NullableOperatorCast(this ExpressionType type)
            => type == ExpressionType.Equal
            ? "IS NULL"
            : (type == ExpressionType.NotEqual ? "IS NOT NULL" : type.ExpressionTypeCast());

        /// <summary>
        /// 空与非空运算符转换
        /// </summary>
        /// <param name="opr"></param>
        /// <returns></returns>
        public static bool IsBracketsExpressionType(this ExpressionType type)
            => type == ExpressionType.AndAlso || type == ExpressionType.OrElse;

        /// <summary>
        /// 获得like语句链接符
        /// </summary>
        /// <param name="databaseType"></param>
        /// <returns></returns>
        public static string GetLikeConnectorWords(this DataBaseType databaseType)
            => databaseType switch
            {
                DataBaseType.PostgreSql or DataBaseType.Oracle or DataBaseType.MySql or DataBaseType.Sqlite => "||",
                _ => "+",
            };
    }
}