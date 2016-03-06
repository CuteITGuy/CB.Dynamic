namespace Test.Console.CB.Dynamic.CompilerServices
{
    internal class Program
    {
        #region Implementation
        private static void Main()
        {
            TestDynamicEventHandler.Run();
            System.Console.ReadLine();
        }
        #endregion
    }

    /*internal class Program
    {
        #region Events
        public static event EventHandler Start;
        #endregion


        #region Implementation
        private static string CreateVirtualMethodDefinition(MethodInfo method)
        {
            var methodName = method.Name;
            return
                $@"Console.WriteLine(""Before calling {methodName}"");
{SubClassCreator.BASE_CALLING}
Console.WriteLine(""After calling {methodName}"");";
        }

        private static void DoStart()
        {
            OnStart();
        }

        [STAThread]
        private static void Main()
        {
            //TestMyClassEventApply();
            TestWindowEventApply();

            //TestProgramEventApply();
        }

        private static void OnStart()
        {
            Start?.Invoke(null, EventArgs.Empty);
        }

        private static void TestMyClassEventApply()
        {
            var myClass = new MyClass();

            var dynHandler = new DynamicEventHandler();
            dynHandler.Attach(typeof(MyClass).GetEvent("Done"),
                @"Console.WriteLine(""I'm Done"");");
            dynHandler.Apply(myClass);
            myClass.Do();
            System.Console.ReadLine();
        }

        private static void TestProgramEventApply()
        {
            var dynHandler = new DynamicEventHandler();
            dynHandler.Attach(typeof(Program).GetEvent("Start"),
                @"Console.WriteLine(""Started"");");
            dynHandler.Apply<Program>(null);
            DoStart();
            System.Console.ReadLine();
        }

        private static void TestWindowEventApply()
        {
            var windowSubClassCreator = new SubClassCreator(typeof(Window));
            foreach (
                var virtualMethod in windowSubClassCreator.GetVirtualMethods().Where(m => m.Name != "OnPropertyChanged")
                )
            {
                windowSubClassCreator.OverrideMethod(virtualMethod,
                    CreateVirtualMethodDefinition(virtualMethod), BaseCallingStrategy.AutoReturn);
            }

            var windowSubClass = windowSubClassCreator.Compile();
            var window = Activator.CreateInstance(windowSubClass) as Window;

            //var window = new Window();
            if (window != null)
            {
                /*window.Content = new Button();
                window.Resources = new ResourceDictionary
                {
                    ["object"] = new object(),
                    ["string"] = "hello"
                };#1#

                var dynHandler = new DynamicEventHandler();
                foreach (var eventInfo in typeof(Window).GetEvents())
                {
                    var eventBody = $@"Console.WriteLine(""Call {eventInfo.Name}"");";
                    dynHandler.Attach(eventInfo, eventBody);
                }
                dynHandler.Apply(window);
                window.Show();
                window.Close();
            }
            System.Console.ReadLine();
        }
        #endregion
    }*/
}