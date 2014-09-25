using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace JustAnExpression
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<TOut>> Pipe<TPipe, TOut>(this Expression<Func<TPipe>> expression1, Expression<Func<TPipe, TOut>> expression2)
        {
            return (Expression<Func<TOut>>)PipePrivate(expression1, expression2);
        }

        public static Expression<Func<T1, TOut>> Pipe<T1, TPipe, TOut>(this Expression<Func<T1, TPipe>> expression1, Expression<Func<TPipe, TOut>> expression2)
        {
            return (Expression<Func<T1, TOut>>)PipePrivate(expression1, expression2);
        }

        public static Expression<Func<T1, T2, TOut>> Pipe<T1, T2, TPipe, TOut>(this Expression<Func<T1, T2, TPipe>> expression1, Expression<Func<TPipe, TOut>> expression2)
        {
            return (Expression<Func<T1, T2, TOut>>)PipePrivate(expression1, expression2);
        }

        public static Expression<Func<T1, T2, T3, TOut>> Pipe<T1, T2, T3, TPipe, TOut>(this Expression<Func<T1, T2, T3, TPipe>> expression1, Expression<Func<TPipe, TOut>> expression2)
        {
            return (Expression<Func<T1, T2, T3, TOut>>)PipePrivate(expression1, expression2);
        }

        public static Expression<Func<T1, T2, T3, T4, TOut>> Pipe<T1, T2, T3, T4, TPipe, TOut>(this Expression<Func<T1, T2, T3, T4, TPipe>> expression1, Expression<Func<TPipe, TOut>> expression2)
        {
            return (Expression<Func<T1, T2, T3, T4, TOut>>)PipePrivate(expression1, expression2);
        }

        public static Expression<Func<T1, T2, T3, T4, T5, TOut>> Pipe<T1, T2, T3, T4, T5, TPipe, TOut>(this Expression<Func<T1, T2, T3, T4, T5, TPipe>> expression1, Expression<Func<TPipe, TOut>> expression2)
        {
            return (Expression<Func<T1, T2, T3, T4, T5, TOut>>)PipePrivate(expression1, expression2);
        }

        public static Expression<Func<T1, T2, T3, T4, T5, T6, TOut>> Pipe<T1, T2, T3, T4, T5, T6, TPipe, TOut>(this Expression<Func<T1, T2, T3, T4, T5, T6, TPipe>> expression1, Expression<Func<TPipe, TOut>> expression2)
        {
            return (Expression<Func<T1, T2, T3, T4, T5, T6, TOut>>)PipePrivate(expression1, expression2);
        }

        private static Expression PipePrivate(this LambdaExpression expression1, LambdaExpression expression2)
        {
            var expression2Param = expression2.Parameters.First();
            if (expression2.Parameters.Count != 1 || !expression2Param.Type.IsAssignableFrom(expression1.ReturnType))
            {
                throw new ArgumentException("expression2 must take a single parameter that is equal to the return type of expression1", "expression2");
            }

            var body = expression2.Body.Replace(expression2Param, expression1.Body);

            var typeList = expression1.Parameters.Select(v => v.Type).ToList();
            typeList.Add(expression2.ReturnType);

            var newType = MakeGenericFuncType(typeList.ToArray());

            return Expression.Lambda(newType, body, expression1.Parameters);
        }

        public static Type MakeGenericFuncType(Type[] types)
        {
            Type baseType;
            switch (types.Length)
            {
                case 1: baseType = typeof(Func<>); break;
                case 2: baseType = typeof(Func<,>); break;
                case 3: baseType = typeof(Func<,,>); break;
                case 4: baseType = typeof(Func<,,,>); break;
                case 5: baseType = typeof(Func<,,,,>); break;
                case 6: baseType = typeof(Func<,,,,,>); break;
                case 7: baseType = typeof(Func<,,,,,,>); break;
                case 8: baseType = typeof(Func<,,,,,,,>); break;
                default: throw new Exception(string.Format("Cannot create Func<{0}> type.", string.Join(",", types.Select(v => v.Name).ToArray())));
            }

            return baseType.MakeGenericType(types.ToArray());
        }

        public static Type MakeGenericActionType(Type[] types)
        {
            Type baseType;
            switch (types.Length)
            {
                case 1: baseType = typeof(Action<>); break;
                case 2: baseType = typeof(Action<,>); break;
                case 3: baseType = typeof(Action<,,>); break;
                case 4: baseType = typeof(Action<,,,>); break;
                case 5: baseType = typeof(Action<,,,,>); break;
                case 6: baseType = typeof(Action<,,,,,>); break;
                case 7: baseType = typeof(Action<,,,,,,>); break;
                case 8: baseType = typeof(Action<,,,,,,,>); break;
                default: throw new Exception(string.Format("Cannot create Action<{0}> type.", string.Join(",", types.Select(v => v.Name).ToArray())));
            }

            return baseType.MakeGenericType(types.ToArray());
        }

        public static Expression Compose<T1, TOut>(this Expression<Func<T1, TOut>> expression, Expression expression1)
        {
            return ComposePrivate(expression, expression1);
        }

        public static Expression Compose<T1, T2, TOut>(this Expression<Func<T1, T2, TOut>> expression, Expression expression1, Expression expression2)
        {
            return ComposePrivate(expression, expression1, expression2);
        }

        public static Expression Compose<T1, T2, T3, TOut>(this Expression<Func<T1, T2, T3, TOut>> expression, Expression expression1, Expression expression2, Expression expression3)
        {
            return ComposePrivate(expression, expression1, expression2, expression3);
        }

        public static Expression Compose<T1, T2, T3, T4, TOut>(this Expression<Func<T1, T2, T3, T4, TOut>> expression, Expression expression1, Expression expression2, Expression expression3, Expression expression4)
        {
            return ComposePrivate(expression, expression1, expression2, expression3, expression4);
        }

        public static Expression Compose<T1, T2, T3, T4, T5, TOut>(this Expression<Func<T1, T2, T3, T4, T5, TOut>> expression, Expression expression1, Expression expression2, Expression expression3, Expression expression4, Expression expression5)
        {
            return ComposePrivate(expression, expression1, expression2, expression3, expression4, expression5);
        }

        public static Expression Compose<T1, T2, T3, T4, T5, T6, TOut>(this Expression<Func<T1, T2, T3, T4, T5, T6, TOut>> expression, Expression expression1, Expression expression2, Expression expression3, Expression expression4, Expression expression5, Expression expression6)
        {
            return ComposePrivate(expression, expression1, expression2, expression3, expression4, expression5, expression6);
        }

        public static Expression Compose<T1, T2, T3, T4, T5, T6, T7, TOut>(this Expression<Func<T1, T2, T3, T4, T5, T6, T7, TOut>> expression, Expression expression1, Expression expression2, Expression expression3, Expression expression4, Expression expression5, Expression expression6, Expression expression7)
        {
            return ComposePrivate(expression, expression1, expression2, expression3, expression4, expression5, expression6, expression7);
        }

        private static Expression ComposePrivate(this LambdaExpression expression, params Expression[] parameters)
        {
            if (expression.Parameters.Count != parameters.Length)
            {
                throw new ArgumentException("The number of parameters passed in must match the number of parameters in the LambdaExpression", "parameters");
            }

            var result = expression.Body;
            var lambdaParameters = expression.Parameters;

            for (int i = 0; i < parameters.Length; i++)
            {
                result = result.Replace(lambdaParameters[i], parameters[i]);
            }
            return result;
        }

        /// <summary>
        /// Return a new expression where originalExpression has been replaced by replacementExpression
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expr"></param>
        /// <param name="originalExpression"></param>
        /// <param name="replacementExpression"></param>
        /// <returns></returns>
        public static T Replace<T>(this T expr, Expression originalExpression, Expression replacementExpression)
             where T : Expression
        {
            return (T)(new ReplaceVisitor(originalExpression, replacementExpression)).Visit(expr);
        }

        /// <summary>
        /// Replace a parameter in the expressions with a replacement expression
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expr"></param>
        /// <param name="numberedParameter">Numbers start with 0</param>
        /// <param name="replacementExpression"></param>
        /// <returns></returns>
        public static T Replace<T>(this T expr, int numberedParameter, Expression replacementExpression)
            where T : LambdaExpression
        {
            var originalExpression = (expr as LambdaExpression).Parameters[numberedParameter];

            return (T)(new ReplaceVisitor(originalExpression, replacementExpression)).Visit(expr);
        }
        public static T Replace<T>(this T expr, string namedParameter, Expression replacementExpression)
            where T : LambdaExpression
        {
            var originalExpression = namedParameter.ToLower() == "{body}" ? expr.Body : expr.Parameters.Single(v => v.Name == namedParameter);

            return (T)(new ReplaceVisitor(originalExpression, replacementExpression)).Visit(expr);
        }

        private class ReplaceVisitor : ExpressionVisitor
        {
            private readonly Expression _originalExpression;
            private readonly Expression _replacementExpression;

            public ReplaceVisitor(Expression originalExpression, Expression replacementExpression)
            {
                this._originalExpression = originalExpression;
                this._replacementExpression = replacementExpression;
            }

            public override Expression Visit(Expression node)
            {
                return _originalExpression == node ? _replacementExpression : base.Visit(node);
            }
        }

        public static Expression ReplaceParameters<TExpr>(this TExpr expr, params Expression[] replacements)
            where TExpr : LambdaExpression
        {
            var parameters = expr.Parameters.ToArray();
            var body = expr.Body;

            if (parameters.Length != replacements.Length)
            {
                throw new ArgumentException("You must specify a replacement for every parameter.  Use null if you do not wish to replace a parameter.", "replacments");
            }

            for (int i = 0; i < replacements.Length; i++)
            {
                var replacementExpression = replacements[i];
                if (replacementExpression != null)
                {
                    body = body.Replace(parameters[i], replacementExpression);
                }
            }
            return body;
        }
    }
}
