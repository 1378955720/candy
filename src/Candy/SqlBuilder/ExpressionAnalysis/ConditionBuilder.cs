using System.Collections.ObjectModel;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Candy.Model;

namespace Candy.SqlBuilder.ExpressionAnalysis
{
    internal class ConditionBuilder : ExpressionVisitor
    {
        /// <summary>
        /// 数据库类型
        /// </summary>
        private readonly DataBaseType _dataBaseType;
        /// <summary>
        /// 字段是否加引号
        /// </summary>
        private readonly bool _ifWithQuotationMarks = false;

        private readonly List<object> _arguments = new List<object>();
        private readonly Stack<string> _conditionParts = new Stack<string>();

        public ConditionBuilder(DataBaseType dataBaseType)
        {
            _dataBaseType = dataBaseType;
            _ifWithQuotationMarks = dataBaseType.GetWithQuotationMarks();
        }

        public string Condition { get; private set; }

        public object[] Arguments { get; private set; }


        #region 加双引号
        /// <summary>
        /// 加双引号
        /// </summary>
        /// <param name="str">字串</param>
        /// <returns></returns>
        public static string AddQuotationMarks(string str)
            => string.IsNullOrEmpty(str) ? string.Empty : string.Concat('"', str.Trim(), '"');


        #endregion

        public void Build(Expression expression)
        {
            var evaluator = new PartialEvaluator();
            var evaluatedExpression = evaluator.Eval(expression);

            Visit(evaluatedExpression);

            Arguments = _arguments.ToArray();
            Condition = _conditionParts.Count > 0 ? _conditionParts.Pop() : null;
        }

        protected override Expression VisitBinary(BinaryExpression expression)
        {
            if (expression == null) return expression;

            Visit(expression.Left);
            Visit(expression.Right);

            var right = _conditionParts.Pop();
            var left = _conditionParts.Pop();

            var opr = expression.NodeType.NullableOperatorCast();

            if (left == null)
                _conditionParts.Push(string.Format("{0} {1}", right.Trim(), opr));

            else if (right == null)
                _conditionParts.Push(string.Format("{0} {1}", left.Trim(), opr));

            else
            {
                var cond = string.Format("{0} {1} {2}", left.Trim(), opr, right.Trim());

                if (expression.NodeType.IsBracketsExpressionType())
                    cond = string.Concat('(', cond, ')');

                _conditionParts.Push(cond);
            }
            return expression;
        }

        protected override Expression VisitConstant(ConstantExpression expression)
        {
            if (expression == null) return expression;


            if (expression.Value == null)
                _conditionParts.Push(null);

            else
            {
                _arguments.Add(expression.Value);
                _conditionParts.Push(string.Format("{{{0}}}", _arguments.Count - 1));
            }
            return expression;
        }

        protected override Expression VisitMember(MemberExpression expression)
        {
            if (expression == null) return expression;

            var propertyInfo = expression.Member as PropertyInfo;
            if (propertyInfo == null) return expression;

            //是否添加引号
            if (_ifWithQuotationMarks)
            {
                _conditionParts.Push(string.Format("{0}", expression.GetOriginalExpression().ToDatebaseField()));
            }
            else
            {
                _conditionParts.Push(string.Format("{0}", expression.GetOriginalExpression()));
            }

            return expression;
        }
        #region 其他
        private static string BinarExpressionProvider(Expression left, Expression right, ExpressionType type)
        {
            string sb = "(";
            //先处理左边
            sb += ExpressionRouter(left);

            sb += type.ExpressionTypeCast();

            //再处理右边
            var tmpStr = ExpressionRouter(right);

            if (tmpStr == "null")
            {
                if (sb.EndsWith(" ="))
                    sb = sb[0..^1] + " IS NULL";
                else if (sb.EndsWith("<>"))
                    sb = sb[0..^2] + " IS NOT NULL";
            }
            else
                sb += tmpStr;
            return sb += ")";
        }

        private static string ExpressionRouter(Expression expression)
        {
            switch (expression)
            {
                case BinaryExpression be:
                    return BinarExpressionProvider(be.Left, be.Right, be.NodeType);

                case MemberExpression me:
                    return me.Member.Name;

                case NewArrayExpression ae:
                    StringBuilder tmpstr = new();
                    foreach (var ex in ae.Expressions)
                    {
                        tmpstr.Append(ExpressionRouter(ex));
                        tmpstr.Append(',');
                    }
                    return tmpstr.ToString(0, tmpstr.Length - 1);

                case MethodCallExpression mce:
                    switch (mce.Method.Name)
                    {
                        case "Like": return string.Format("({0} like {1})", ExpressionRouter(mce.Arguments[0]), ExpressionRouter(mce.Arguments[1]));

                        case "NotLike": return string.Format("({0} Not like {1})", ExpressionRouter(mce.Arguments[0]), ExpressionRouter(mce.Arguments[1]));

                        case "In": return string.Format("{0} In ({1})", ExpressionRouter(mce.Arguments[0]), ExpressionRouter(mce.Arguments[1]));

                        case "NotIn": return string.Format("{0} Not In ({1})", ExpressionRouter(mce.Arguments[0]), ExpressionRouter(mce.Arguments[1]));

                        case "StartWith": return string.Format("{0} like '{1}%'", ExpressionRouter(mce.Arguments[0]), ExpressionRouter(mce.Arguments[1]));
                    }
                    break;

                case ConstantExpression ce:

                    if (ce.Value == null)
                        return "null";

                    else if (ce.Value is ValueType)
                        return ce.Value.ToString();

                    else if (ce.Value is string || ce.Value is DateTime || ce.Value is char)
                        return string.Format("'{0}'", ce.Value.ToString());
                    break;

                case UnaryExpression ue:
                    return ExpressionRouter(ue.Operand);
            }
            return null;
        }

        
        #endregion


        /// <summary>
        /// ConditionBuilder 并不支持生成Like操作，如 字符串的 StartsWith，Contains，EndsWith 并不能生成这样的SQL： Like ‘xxx%’, Like ‘%xxx%’ , Like ‘%xxx’ . 只要override VisitMethodCall 这个方法即可实现上述功能。
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            string connectorWords = _dataBaseType.GetLikeConnectorWords(); //获取like链接符

            if (expression == null) return expression;

            string format = expression.Method.Name switch
            {
                "StartsWith" => string.Concat("{0} LIKE ''", connectorWords, "{1}", connectorWords, "'%'"),
                "Contains" => string.Concat("{0} LIKE '%'", connectorWords, "{1}", connectorWords, "'%'"),
                "EndsWith" => string.Concat("{0} LIKE '%'", connectorWords, "{1}", connectorWords, "''"),
                "Equals" => "({0} {1})",// not in 或者  in 或 not like
                _ => throw new NotSupportedException(expression.NodeType + " is not supported!"),
            };

            Visit(expression.Object);
            Visit(expression.Arguments[0]);

            string right = _conditionParts.Pop();
            string left = _conditionParts.Pop();

            _conditionParts.Push(string.Format(format, left, right));
            return expression;
        }

       

    }
}
