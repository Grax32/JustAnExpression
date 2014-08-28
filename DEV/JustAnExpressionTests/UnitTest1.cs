using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JustAnExpression;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JustAnExpressionTests
{
    [TestClass]
    public class UnitTest1
    {
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
        }

        [TestMethod]
        public void MakeGenericTypeTest()
        {
            Assert.AreEqual(typeof(Func<int, int, int, int, int>), ExpressionExtensions.MakeGenericFuncType(typeof(int).Repeat(5).ToArray()));
            Assert.AreEqual(typeof(Action<int, int, int, int, int>), ExpressionExtensions.MakeGenericActionType(typeof(int).Repeat(5).ToArray()));
        }

        [TestMethod]
        public void BuildExpressionBlock()
        {
            Expression<Func<string, int, DateTime, string>> mainExpression = (name, age, birthdate) => null;
            var nameExpr = mainExpression.Parameters.GetNamedParameterExpression("name");
            var ageExpr = mainExpression.Parameters.GetNamedParameterExpression("age");
            var birthDateExpr = mainExpression.Parameters.GetNamedParameterExpression("birthdate");

            Expression<Func<string, int, string, string>> concatExpr = (a, b, c) => a + '\n' + b.ToString() + '\n' + c;

            Expression<Func<string, string>> nameFormatter = t => string.Format("My Name is {0}", t);
            Expression<Func<int, int>> ageConvertToDogYears = t => t / 7;
            Expression<Func<DateTime, string>> birthDateFormat = t => t.ToShortDateString();

            var newBody = concatExpr.Compose(
                    nameFormatter.Compose(nameExpr),
                    ageConvertToDogYears.Compose(ageExpr),
                    birthDateFormat.Compose(birthDateExpr)
                    );

            var finalExpression = Expression.Lambda<Func<string, int, DateTime, string>>(newBody, mainExpression.Parameters);

            var func = finalExpression.Compile();

            var result = func("David", 42, new DateTime(1972, 9, 2));

        }
    }
}
