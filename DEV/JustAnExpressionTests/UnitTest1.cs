using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using JustAnExpression;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JustAnExpressionTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void ExpressionHelperTestMethod()
        {
            var exp = Just.BeginExpression<Func<int, string>>();
            exp.CreateVariable<DateTime>("dt");

            exp.Body.Add(
                Expression.Assign(exp.Variable("dt"), Expression.Constant(new DateTime(2012, 5, 5))),
                Expression.Assign(exp.ReturnVariable, exp.Flatten(
                    exp.Lambda((DateTime d, int i) => d.ToString() + "1234:" + i.ToString()),
                    exp.Variable("dt"),
                    exp.Parameter(0)
                    )
                ),
                exp.ReturnVariable
                );

            var y = exp.Build();

            var z = y.Compile()(56);

            Assert.AreEqual("5/5/2012 12:00:00 AM1234:56", z);
        }

        [TestMethod]
        public void ExpressionHelperTestMethod2()
        {
            var exp = Just.BeginExpression<Func<DateTime>>();
            exp.CreateVariable<DateTime>("dt");

            exp.Body.Add(Expression.Constant(new DateTime(2012, 5, 5)));

            var y = exp.Build();

            var z = y.Compile()();

            Assert.AreEqual(new DateTime(2012, 5, 5), z);
        }

        class ActionAssertHelper
        {
            public DateTime BirthDate { get; set; }
        }

        [TestMethod]
        public void ExpressionHelperTestMethodAction()
        {
            var helper = new ActionAssertHelper();

            var exp = Just.BeginExpression<Action<DateTime>>();
            exp.CreateVariable<DateTime>("dt");

            exp.Body.Add(
                Expression.Assign(
                    exp.Flatten(exp.Lambda((ActionAssertHelper v) => v.BirthDate), Expression.Constant(helper)),
                    exp.Parameter(0)
                ),
                Expression.Throw(Expression.New(typeof(System.Exception).GetConstructor(new Type[] { typeof(string) }), Expression.Constant("Test Error")))
            );

            var y = exp.Build();

            var func = y.Compile();

            func(new DateTime(2012, 5, 5));

            Assert.AreEqual(new DateTime(2012, 5, 5), helper.BirthDate);
        }

        [TestMethod]
        public void ComposeTestMethod()
        {
            var param1 = Expression.Parameter(typeof(string));
            var body = Expression.Constant(false);
            var myexpr1 = Expression.Lambda<Func<string, bool>>(body, param1);

            Expression<Func<string, string, string, bool>> myexpr2 = (x, y, z) => string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) || string.IsNullOrEmpty(z);

            var xConst = Expression.Constant("1234");
            var yConst = Expression.Constant("5678");

            var result = myexpr2.Compose(param1, xConst, yConst);

            var newExpr = myexpr1.Replace(body, result);

            var func = newExpr.Compile();

            Assert.IsTrue(func(""));
            Assert.IsFalse(func("not empty"));
        }

        [TestMethod]
        public void PipeTestMethod()
        {
            Expression<Func<int, int, int, string>> expr1 = (x, y, z) => (x + y + z).ToString();
            Expression<Func<string, string>> expr2 = v => string.Format("Result=({0})", v);

            var newExpr = expr1.Pipe(expr2);

            var func = newExpr.Compile();

            var output = func(1, 2, 3);

            Assert.AreEqual("Result=(6)", output);
        }

        [TestMethod]
        public void PipeTest2Method()
        {
            Expression<Func<TestPerson, IQueryable<TestPerson>>> getPeopleExpr = t => new List<TestPerson>
            {
                t,
                new TestPerson{ FirstName="Jon", LastName="Jovi"},
                new TestPerson{ FirstName="Jonas", LastName="Jameson"}
            }.AsQueryable();

            Expression<Func<IQueryable<TestPerson>, IQueryable<TestPerson>>> filterExpr = v => v.Where(z => z.LastName == "Jovi");
            Expression<Func<IQueryable<TestPerson>, List<TestPerson>>> toListExpr = v => v.ToList();

            var newExpr = getPeopleExpr
                .Pipe(filterExpr)
                .Pipe(toListExpr);

            var func = newExpr.Compile();
            var funcResult = func(new TestPerson { LastName = "Jovi", FirstName = "JonBon" });

            Assert.AreEqual(2, funcResult.Count());
            Assert.IsTrue(funcResult.All(v => v.LastName == "Jovi"));

        }

        class TestPerson
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }

            public override string ToString()
            {
                return (FirstName ?? "") + " " + (LastName ?? "");
            }
        }

        [TestMethod]
        public void AllOfLastNameTest()
        {
            Expression<Func<TestPerson, IQueryable<TestPerson>>> getPeopleExpr = t => new List<TestPerson>
            {
                t,
                new TestPerson{ FirstName="Jon", LastName="Jameson"},
                new TestPerson{ FirstName="Jonas", LastName="Jameson"},
                new TestPerson{ FirstName="Jon", LastName="Jameson"},
                new TestPerson{ FirstName="Jonas", LastName="Jameson"},
                new TestPerson{ FirstName="Jon", LastName="Jameson"},
                new TestPerson{ FirstName="Jonas", LastName="Jameson"}
            }.AsQueryable();

            var funcList = new List<Expression<Func<TestPerson, bool>>>();
            funcList.Add(x => x.FirstName.StartsWith("J"));
            funcList.Add(d => d.LastName == "Jameson");



            var whereBody = ((Expression<Func<TestPerson, bool>>)(w => true));

            funcList = funcList.Select(v => v.Replace(0, whereBody.Parameters.First())).ToList();

            whereBody = whereBody
                .Replace("{body}", Just.AllOf(funcList.Select(v => v.Body)));


            Expression<Func<IQueryable<TestPerson>, Expression<Func<TestPerson, bool>>, IQueryable<TestPerson>>> filterExpr = (entityCollection, filter) => entityCollection.Where(filter);
            var getResultsExpr = filterExpr.Body.Replace(filterExpr.Parameters.First(), getPeopleExpr.Body);
            getResultsExpr = getResultsExpr.Replace(filterExpr.Parameters[1], whereBody);

            var finalExpression = Expression.Lambda<Func<TestPerson, IQueryable<TestPerson>>>(getResultsExpr, getPeopleExpr.Parameters);

            var results1 = finalExpression.Compile()(new TestPerson { FirstName = "Bill", LastName = "Bixby" });
            var results2 = finalExpression.Compile()(new TestPerson { FirstName = "Jill", LastName = "Jameson" });

        }

        public static void MakeFilteredQueryable<T>(Expression<Func<IQueryable<T>>> queryable)
        {

        }

        [TestMethod]
        public void MakeGenericTypeTest()
        {
            Assert.AreEqual(typeof(Func<int, int, int, int, int>), ExpressionExtensions.MakeGenericFuncType(typeof(int).Repeat(5).ToArray()));
            Assert.AreEqual(typeof(Action<int, int, int, int, int>), ExpressionExtensions.MakeGenericActionType(typeof(int).Repeat(5).ToArray()));
        }

        [TestMethod]
        public void BuildExpressionBlockTest()
        {
            Expression<Func<string, int, DateTime, string>> mainExpression = (name, age, birthdate) => null;
            var parms = mainExpression.Parameters.ToDictionary(v => v.Name);

            Expression<Func<string, int, string, string>> concatExpr = (a, b, c) => a + '\n' + b.ToString() + '\n' + c;

            Expression<Func<string, string>> nameFormatter = t => string.Format("My Name is {0}", t);
            Expression<Func<int, int>> ageConvertToDogYears = t => t / 7;
            Expression<Func<DateTime, string>> birthDateFormat = t => t.ToShortDateString();

            var newBody = concatExpr.Compose(
                    nameFormatter.Compose(parms["name"]),
                    ageConvertToDogYears.Compose(parms["age"]),
                    birthDateFormat.Compose(parms["birthdate"])
                    );

            var finalExpression = Expression.Lambda<Func<string, int, DateTime, string>>(newBody, mainExpression.Parameters);

            var func = finalExpression.Compile();

            var result = func("David", 42, new DateTime(1972, 9, 2));
            Assert.AreEqual("My Name is David\n6\n9/2/1972", result);
        }

        [TestMethod]
        public void AllExpressionTest()
        {
            var trueExpr = Expression.Constant(true);
            var parm = Expression.Parameter(typeof(bool));

            var allExpr = Just.AllOf(trueExpr, trueExpr, trueExpr, parm);

            var finalExpression = Expression.Lambda<Func<bool, bool>>(allExpr, parm);
            var func = finalExpression.Compile();

            Assert.IsTrue(func(true));
            Assert.IsFalse(func(false));
        }


        [TestMethod]
        public void AnyExpressionTest()
        {
            var falseExpr = Expression.Constant(false);
            var parm = Expression.Parameter(typeof(bool));

            var anyExpr = Just.AllOf(falseExpr, falseExpr, falseExpr, parm);

            var finalExpression = Expression.Lambda<Func<bool, bool>>(anyExpr, parm);
            var func = finalExpression.Compile();

            Assert.IsTrue(func(true));
            Assert.IsFalse(func(false));

        }

        [TestMethod]
        public void AggregrateExpressionTest()
        {
            var zeroExpr = Expression.Constant(0);
            var sevenExpr = Expression.Constant(7);
            var nineExpr = Expression.Constant(9);
            var parm = Expression.Parameter(typeof(int));

            var anyExpr = Just.AggregrateBinary(Expression.LessThanOrEqual(Expression.Constant(0), Expression.Constant(0)), zeroExpr, sevenExpr, nineExpr, parm);

            var finalExpression = Expression.Lambda<Func<int, bool>>(anyExpr, parm);
            var func = finalExpression.Compile();

            Assert.IsTrue(func(11));
            Assert.IsFalse(func(3));
        }

        [TestMethod]
        public void AggregrateExpression2Test()
        {
            var zeroExpr = Expression.Constant(0);
            var sevenExpr = Expression.Constant(7);
            var nineExpr = Expression.Constant(9);
            var parm = Expression.Parameter(typeof(int));

            var anyExpr = Just.AggregrateBinary(ExpressionType.LessThanOrEqual, zeroExpr, sevenExpr, nineExpr, parm);

            var finalExpression = Expression.Lambda<Func<int, bool>>(anyExpr, parm);
            var func = finalExpression.Compile();

            Assert.IsTrue(func(11));
            Assert.IsFalse(func(3));

        }

    }
}
