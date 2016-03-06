using System;


namespace CB.CompilerServices
{
    public class ClassCreatorException: Exception
    {
        #region  Constructors & Destructors
        public ClassCreatorException() { }

        public ClassCreatorException(string message): base(message) { }

        public ClassCreatorException(string message, Exception innerException)
            : base(message, innerException) { }
        #endregion
    }
}