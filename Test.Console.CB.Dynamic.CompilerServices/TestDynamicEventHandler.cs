using System;
using CB.Dynamic.CompilerServices;


namespace Test.Console.CB.Dynamic.CompilerServices
{
    public class TestDynamicEventHandler
    {
        #region Methods
        public static void Run()
        {
            var test = new MyTestClass();
            var random = new Random(DateTime.Now.Millisecond);

            AddEvent(test, nameof(MyTestClass.EventInt), (Func<int>)(() =>
            {
                System.Console.WriteLine($"Call EventInt {random.NextDouble()}");
                return 101;
            }));

            AddEvent(test, nameof(MyTestClass.EventVoid),
                (Action)(() => System.Console.WriteLine($"Call EventVoid {random.NextDouble()}")));

            AddEvent(test, nameof(MyTestClass.EventComplex),
                (Action)(() => System.Console.WriteLine($"Call EventComplex {random.NextDouble()}")));

            AddEvent(test, nameof(MyTestClass.EventString),
                (Action)(() => System.Console.WriteLine($"Call EventString {random.NextDouble()}")));

            test.CallEventVoid();
            test.CallEventInt(42);
            test.CallEventComplex(9, "Wow", true);
            test.CallEventString("a string");
        }
        #endregion


        #region Implementation
        private static void AddEvent(object target, string eventName, Delegate body)
        {
            EventHandlerAttacher.Attach(target.GetType().GetEvent(eventName), target, body);
        }
        #endregion


        public class MyTestClass
        {
            public delegate void TestDelegateComplex(int number, string text, bool flag);

            public delegate int TestDelegateInt(int parameter);

            public delegate void TestDelegateString(string parameter);

            public delegate void TestDelegateVoid();


            #region Events
            public event TestDelegateComplex EventComplex;
            public event TestDelegateInt EventInt;
            public event TestDelegateString EventString;
            public event TestDelegateVoid EventVoid;
            #endregion


            #region Methods
            public void CallEventComplex(int number, string text, bool flag) => OnEventComplex(number, text, flag);

            public int CallEventInt(int parameter) => OnEventInt(parameter);

            public void CallEventString(string parameter) => OnEventString(parameter);

            public void CallEventVoid() => OnEventVoid();
            #endregion


            #region Implementation
            protected virtual void OnEventComplex(int number, string text, bool flag)
                => EventComplex?.Invoke(number, text, flag);

            protected virtual int OnEventInt(int parameter) => EventInt?.Invoke(parameter) ?? 0;

            protected virtual void OnEventString(string parameter) => EventString?.Invoke(parameter);

            protected virtual void OnEventVoid() => EventVoid?.Invoke();
            #endregion
        }
    }
}