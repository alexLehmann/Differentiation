using System;
using System.Linq.Expressions;

namespace Reflection.Differentiation
{
    public class ParameterModifier : ExpressionVisitor
    {
        private ParameterExpression parameter;
        public Expression Modify(ParameterExpression parameter, Expression expression)
        {
            this.parameter = parameter;
            return Visit(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            bool change = false;
            foreach (var item in node.Arguments)
            {
                if (item.NodeType.Equals(ExpressionType.Parameter)) change = true;
            }
            if (change)
                return Expression.Call(node.Method, Expression.Add(parameter, Expression.Constant(1e-7)));

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            bool change = false;

            Expression left = this.Visit(b.Left);
            Expression right = this.Visit(b.Right);
            if (left.NodeType == ExpressionType.Parameter)
            {
                change = true;
                left = Expression.Add(parameter, Expression.Constant(1e-7));
            }
            if (right.NodeType == ExpressionType.Parameter)
            {
                change = true;
                right = Expression.Add(parameter, Expression.Constant(1e-7));
            }
            if (change)
                return Expression.MakeBinary(b.NodeType, left, right);

            return base.VisitBinary(b);
        }
    }

    public static class Algebra
    {
        public static Expression<Func<double, double>> Differentiate(Expression<Func<double, double>> function)
        {
            var constant = Expression.Constant(1e-7);
            var body = function.Body;
            var parameter = function.Parameters[0];

            ParameterModifier treeModifier = new ParameterModifier();
            Expression modifiedExpr = body.NodeType.Equals(ExpressionType.Parameter)
                ? Expression.Add(parameter,constant)
                : treeModifier.Modify(parameter, (Expression)body);

            var expression = Expression.Lambda<Func<double, double>>(
                Expression.Divide(
                    Expression.Subtract(
                        modifiedExpr, body
                        ),
                    constant),
                parameter);
            return expression;
        }
    }
}
