namespace Serilog.Sinks.RollingFile.Extension.Aspects
{
    using System;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    using Polly;
    using PostSharp.Aspects;
    using Serilog;
    using Debugging;

    [Serializable]
    public class RetryOnExceptionAttribute : MethodInterceptionAspect
    {
        public Type ExceptionHandlingStrategyType { get; set; }
        private IDictionary<int, int> retryCounter = new Dictionary<int, int>();
        protected readonly int waitInMillisecond;

        public RetryOnExceptionAttribute(int waitInMillisecond = 0)
        {
            this.MaxRetries = AsyncOptions.MaxRetries;
            this.waitInMillisecond = waitInMillisecond != 0 ? waitInMillisecond : AsyncOptions.RetryWaitInMillisecond;
        }

        public int MaxRetries { get; set; }

        public bool Forever { get; set; }

        public override void OnInvoke(MethodInterceptionArgs args)
        {
            Func<int, TimeSpan> sleepFunc =
                retryNumber => TimeSpan.FromMilliseconds(this.waitInMillisecond * retryNumber);

            if (!retryCounter.ContainsKey(args.Instance.GetHashCode()))
                retryCounter.Add(args.Instance.GetHashCode(), 0);

            var polly = Policy.Handle<Exception>();

            if (Forever)
                polly.RetryForever(
                    (exception) => Log.Error("Error executing callback {@Exception}", exception))
                    .Execute(() =>
                    {
                        if (retryCounter[args.Instance.GetHashCode()] > 0)
                            Thread.Sleep(waitInMillisecond);

                        retryCounter[args.Instance.GetHashCode()]++;

                        base.OnInvoke(args);
                    });
            else
                polly.WaitAndRetry(this.MaxRetries, sleepFunc,
                       (exception, timeSpan) => Log.Error("Error executing callback {@Exception}", exception))
                       .Execute(() => base.OnInvoke(args));

        }

        [ExcludeFromCodeCoverage]
        private void OnRetry(Exception exception, TimeSpan timeSpan, MethodInterceptionArgs args)
        {
            if (this.Forever)
            {
                Log.Error(
                    "Exception during attempt of calling method {className}.{methodName}: {@Exception}",
                    args.Method.DeclaringType,
                    args.Method.Name,
                    exception);
            }
            else
            {
                Log.Warning(
                    "Exception during attempt of calling method {className}.{methodName}: {@Exception}",
                    args.Method.DeclaringType,
                    args.Method.Name,
                    exception);
            }
        }

        [ExcludeFromCodeCoverage]
        public override bool CompileTimeValidate(MethodBase method)
        {
            var methodInfo = method as MethodInfo;
            if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
            {
                var message = string.Format("[{0}] cannot be applied to a method with Task return type {1}.{2}", GetType().Name, methodInfo.DeclaringType, methodInfo.Name);
                throw new Exception(message);
            }

            return base.CompileTimeValidate(method);
        }
    }
}
