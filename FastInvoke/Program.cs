using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FastInvoke
{
    class Program
    {
        static void Main(string[] args)
        {
            Type instType = typeof(Test);
            var methodInfo = instType.GetMethod("Add", new[] {typeof(int), typeof(int)});
            var compiler = Complier2(instType, methodInfo);

            var expression = Expression.New(instType.GetConstructor(new Type[0]));
            var t = Expression.Lambda<Func<Test>>(expression).Compile()();

            int times = 5000000;
            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 1; i <= times; i++)
            {
                compiler(t, new object[] {i, i * 10});
            }
            watch.Stop();
            Console.WriteLine($"Expression用时：{watch.Elapsed.Seconds}");

            watch.Reset();
            watch.Start();
            for (int i = 1; i <= times; i++)
            {
                methodInfo.Invoke(t, new object[] {i, i * 10});
            }
            watch.Stop();

            Console.WriteLine($"Reflection用时：{watch.Elapsed.Seconds}");
            Console.ReadLine();
        }

        private static void Complier()
        {
            var target = Expression.Label();
            var varExpr = Expression.Variable(typeof(int), "result");
            var block = Expression.Block(
                Expression.Loop(
                    Expression.Block(
                        Expression.AddAssign(varExpr, Expression.Constant(1, typeof(int))),
                        Expression.IfThen(Expression.Equal(varExpr, Expression.Constant(100)), Expression.Break(target))
                    )
                ),
                Expression.Label(target),
                Expression.Call(typeof(Console).GetMethod("WriteLine", new[] {typeof(object)}), Expression.Convert(varExpr, typeof(object)))
            );

            var lambda = Expression.Lambda<Action<int,int>>(block, varExpr);
            lambda.Compile();
        }

        private static Func<object, object[], object> Complier2(Type instType, MethodInfo methodInfo)
        {
            ParameterExpression instExpr = Expression.Parameter(typeof(object));
            ParameterExpression argsExper = Expression.Parameter(typeof(object[]), "args");

            var methodArgs = methodInfo.GetParameters();
            var indexerInfo = typeof(IList).GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public);

            List<Expression> callArgs = new List<Expression>(methodArgs.Length);

            for (int i = 0; i < methodArgs.Length; i++)
            {
                callArgs.Add(Expression.Convert(Expression.Call(argsExper, indexerInfo, Expression.Constant(i)), methodArgs[i].ParameterType));
            }

            var callExpr = Expression.Convert(methodInfo.IsStatic ? Expression.Call(methodInfo, callArgs) : Expression.Call(Expression.Convert(instExpr, instType), methodInfo, callArgs), typeof(object));

            var lambda = Expression.Lambda<Func<object, object[], object>>(callExpr, instExpr, argsExper);
            return lambda.Compile();
        }
    }
}
