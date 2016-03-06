using System;


namespace Test.Console.CB.Dynamic.CompilerServices
{
    public class MyClass
    {
        #region Fields
        private readonly string _message;
        #endregion


        #region  Constructors & Destructor
        public MyClass(string message)
        {
            _message = message;
        }
        #endregion


        #region Events
        public event EventHandler Done;
        #endregion


        #region Methods
        public void Do()
        {
            System.Console.WriteLine("Doing");
            OnDone();
        }

        public void Print()
        {
            System.Console.WriteLine(_message);
        }
        #endregion


        #region Implementation
        protected virtual void OnDone()
        {
            Done?.Invoke(this, EventArgs.Empty);
        }
        #endregion
    }
}