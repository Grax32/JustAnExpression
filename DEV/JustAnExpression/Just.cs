using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace JustAnExpression
{
    public static class Just
    {
        /// <summary>
        /// Return a new boolean "or" expression that will return true if any of the boolean expressions passed to it are true
        /// </summary>
        /// <param name="expressions">Any number of boolean expressions</param>
        /// <returns></returns>
        public static Expression AnyOf(Expression expression1, params Expression[] expressions)
        {
            if (expressions.Any(v => v.Type != typeof(bool)))
            {
                throw new ArgumentException("All expressions must return type bool", "expressions");
            }
            return AggregrateBinary(Expression.OrElse(expression1, expression1), expression1, expressions);
        }

        /// <summary>
        /// Return a new boolean "and" expression that will return true if all of the boolean expressions passed to it are true
        /// </summary>
        /// <param name="expressions"></param>
        /// <returns></returns>
        public static Expression AllOf(Expression expression1, params Expression[] expressions)
        {
            if (expressions.Any(v => v.Type != typeof(bool)))
            {
                throw new ArgumentException("All expressions must return type bool", "expressions");
            }
            return AggregrateBinary(Expression.AndAlso(expression1, expression1), expression1, expressions);
        }

        public static Expression AllOf(IEnumerable<Expression> expressions)
        {
            return AllOf(expressions.First(), expressions.Skip(1).ToArray());
        }

        public static Expression AggregrateBinary(ExpressionType expressionType, Expression expression1, params Expression[] expressions)
        {
            var type = expression1.Type;
            var typeDefault = GetDefaultValue(type);
            return AggregrateBinary(Expression.MakeBinary(expressionType, Expression.Constant(typeDefault, type), Expression.Constant(typeDefault, type)), expression1, expressions);
        }

        private static object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static Expression AggregrateBinary(BinaryExpression binaryExpression, Expression expression1, params Expression[] expressions)
        {
            var aggregationType = GetAggregationType(binaryExpression);
            Expression result;

            switch (aggregationType)
            {
                case AggregationType.Anded:
                    var prev = expression1;
                    var binaries = new List<BinaryExpression>();
                    foreach (var expr in expressions)
                    {
                        binaries.Add(ReplaceBinaryOperands(binaryExpression, prev, expr));
                        prev = expr;
                    }

                    result = binaries.Aggregate((aggregate, current) => aggregate == null ? current : Expression.AndAlso(aggregate, current));
                    break;
                case AggregationType.SameType:
                    result = expressions.Aggregate(expression1, (aggregrate, current) => ReplaceBinaryOperands(binaryExpression, aggregrate, current));
                    break;
                default:
                    throw new Exception("Unable to process aggregation.");
            }
            return result;
        }

        //0 > 1 && 1 > 2 && 2 > 3

        private static AggregationType GetAggregationType(BinaryExpression expression)
        {
            var types = new[] { expression.Type, expression.Left.Type, expression.Right.Type };
            AggregationType returnValue = AggregationType.None;

            if (types.All(v => v == types[0]))
            {
                returnValue = AggregationType.SameType;
            }
            else if (types[0] == typeof(bool))
            {
                returnValue = AggregationType.Anded;
            }

            return returnValue;
        }

        private enum AggregationType
        {
            None,
            SameType,
            Anded
        }

        private static BinaryExpression ReplaceBinaryOperands(BinaryExpression target, Expression left, Expression right)
        {
            return target
                .Replace(target.Left, left)
                .Replace(target.Right, right);
        }

        public static void Add(this List<Expression> expressionList, params Expression[] expressions)
        {
            expressionList.AddRange(expressions);
        }

        private static List<Expression> GetAllLevelsFromExpression(Expression expression)
        {
            var returnValue = new List<Expression>();
            Expression parentExpression = null;

            if (expression is MemberExpression)
            {
                var memberExpression = expression as MemberExpression;
                parentExpression = memberExpression.Expression;
            }
            else if (expression is MethodCallExpression)
            {
                var methodCallExpression = expression as MethodCallExpression;
                parentExpression = methodCallExpression.Object;
            }

            if (parentExpression != null)
            {
                returnValue.AddRange(GetAllLevelsFromExpression(parentExpression));
            }

            returnValue.Add(expression);
            return returnValue;
        }

        public static LambdaExpression NullSafeIfFy(LambdaExpression expression, Expression defaultValue)
        {
            if (defaultValue == null)
            {
                defaultValue = Expression.Default(expression.Body.Type);
            }

            if (!expression.Body.Type.IsAssignableFrom(defaultValue.Type))
            {
                throw new ArgumentException("The default value must return the same type as the expression", "defaultValue");
            }

            var allLevels = GetAllLevelsFromExpression(expression.Body);
            var levelExpressions = new List<Expression>();
            var parameters = new List<ParameterExpression>();

            var outputParm = Expression.Variable(expression.Body.Type, "out");
            parameters.Add(outputParm);

            var previousParm = (ParameterExpression)allLevels.First();
            Expression previousExpr = previousParm;

            var level = 0;

            foreach (var currentExpr in allLevels)
            {
                var levelValueExpression = currentExpr.Replace(previousExpr, previousParm);
                var parm = Expression.Variable(currentExpr.Type, "L" + level.ToString());

                if (currentExpr == allLevels.Last())
                {
                    levelExpressions.Add(levelValueExpression);
                }
                else
                {
                    Expression nullConstant = Expression.Constant(null, parm.Type);
                    parameters.Add(parm);
                    var nullCheck = Expression.NotEqual(nullConstant, Expression.Assign(parm, levelValueExpression));
                    levelExpressions.Add(nullCheck);
                }
                level++;
                previousParm = parm;

                previousExpr = currentExpr;
            }

            var last = levelExpressions.Last();
            levelExpressions.Remove(last);

            var resultExpr = levelExpressions.Aggregate<Expression>((acc, expr) => Expression.AndAlso(acc, expr));
            resultExpr = Expression.IfThen(resultExpr, Expression.Assign(outputParm, last));
            resultExpr = Expression.Block(parameters,
                Expression.Assign(outputParm, defaultValue),
                resultExpr,
                outputParm);

            return Expression.Lambda(resultExpr, expression.Parameters);
        }

        public static ExpressionBuilder<T> BeginExpression<T>()
        {
            return new ExpressionBuilder<T>();
        }

        public class ExpressionBuilder<T>
        {
            static Type typeofT = typeof(T);
            const string _returnValueName = "returnValue";
            const string _parmPrefix = "parm";

            public ExpressionBuilder()
            {
                // typeofT == i.e. Func<int,string> or Action<int>
                if (typeofT.IsGenericType)
                {
                    var parameterTypes = typeofT.GetGenericArguments();
                    var typeName = typeofT.Name.Split('`')[0];
                    var parameterLength = parameterTypes.Length;
                    bool isFunc = typeName == "Func";

                    if (isFunc)
                    {
                        parameterLength--;
                    }

                    // skip the last generic argument as it is the return type
                    for (int i = 0; i < parameterLength; i++)
                    {
                        CreateParameter(parameterTypes[i], _parmPrefix + i.ToString());
                    }

                    if (isFunc)
                    {
                        CreateVariable(parameterTypes.Last(), _returnValueName);
                    }
                }
            }

            readonly Dictionary<string, ParameterExpression> _variables = new Dictionary<string, ParameterExpression>();
            readonly List<ParameterExpression> _parameters = new List<ParameterExpression>();
            readonly List<Expression> _bodyExpressions = new List<Expression>();

            IEnumerable<ParameterExpression> _nonParameterVariables()
            {
                return _variables.Values.Except(_parameters);
            }

            ParameterExpression CreateParameter(Type parameterType, string key)
            {
                var parm = CreateVariable(parameterType, key);
                _parameters.Add(parm);
                return parm;
            }

            public ParameterExpression CreateVariable<TVar>(string key)
            {
                return CreateVariable(typeof(TVar), key);
            }

            public ParameterExpression CreateVariable(Type type, string key)
            {
                var returnValue = Expression.Variable(type, key);
                _variables[key] = returnValue;
                return returnValue;
            }

            public ParameterExpression Variable(string key)
            {
                return _variables[key];
            }

            public ParameterExpression ReturnVariable
            {
                get { return Variable(_returnValueName); }
            }

            public List<Expression> Body { get { return _bodyExpressions; } }

            public Expression Flatten(LambdaExpression expression, params Expression[] arguments)
            {
                return expression.ReplaceParameters(arguments);
            }

            public Expression<T> Build()
            {
                if (!Body.Any())
                {
                    throw new Exception("No Body expressions have been specified");
                }

                var _body = Body.Count > 1 ? Expression.Block(_nonParameterVariables(), Body) : Body.First();

                return Expression.Lambda<T>(_body, _parameters.ToArray());
            }

            public Expression Parameter(int parameterPosition)
            {
                return Variable(_parmPrefix + parameterPosition.ToString());
            }

            public LambdaExpression Lambda<T1, T2, T3, T4, T5, T6, T7, T8>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1, T2, T3, T4, T5, T6, T7>(Expression<Func<T1, T2, T3, T4, T5, T6, T7>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1, T2, T3, T4, T5, T6>(Expression<Func<T1, T2, T3, T4, T5, T6>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1, T2, T3, T4, T5>(Expression<Func<T1, T2, T3, T4, T5>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1, T2, T3, T4>(Expression<Func<T1, T2, T3, T4>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1, T2, T3>(Expression<Func<T1, T2, T3>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1, T2>(Expression<Func<T1, T2>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1>(Expression<Func<T1>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1, T2, T3, T4, T5, T6, T7, T8>(Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1, T2, T3, T4, T5, T6, T7>(Expression<Action<T1, T2, T3, T4, T5, T6, T7>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1, T2, T3, T4, T5, T6>(Expression<Action<T1, T2, T3, T4, T5, T6>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1, T2, T3, T4, T5>(Expression<Action<T1, T2, T3, T4, T5>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1, T2, T3>(Expression<Action<T1, T2, T3>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1, T2>(Expression<Action<T1, T2>> expr)
            {
                return expr;
            }

            public LambdaExpression Lambda<T1>(Expression<Action<T1>> expr)
            {
                return expr;
            }
        }
    }
}
