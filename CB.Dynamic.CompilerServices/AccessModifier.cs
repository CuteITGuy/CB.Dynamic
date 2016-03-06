using System.Diagnostics.CodeAnalysis;


namespace CB.Dynamic.CompilerServices
{
    [SuppressMessage("ReSharper", "ConvertToAutoProperty")]
    public class AccessModifier
    {
        #region Fields
        private static readonly AccessModifier _internal = new AccessModifier("internal");
        private static readonly AccessModifier _private = new AccessModifier("private");
        private static readonly AccessModifier _protected = new AccessModifier("protected");

        private static readonly AccessModifier _protectedInternal =
            new AccessModifier("protected internal");

        private static readonly AccessModifier _public = new AccessModifier("public");
        #endregion


        #region  Constructors & Destructor
        private AccessModifier(string modifier)
        {
            Modifier = modifier;
        }
        #endregion


        #region  Properties & Indexers
        public static AccessModifier Internal => _internal;

        public static AccessModifier Private => _private;

        public static AccessModifier Protected => _protected;

        public static AccessModifier ProtectedInternal => _protectedInternal;

        public static AccessModifier Public => _public;

        public string Modifier { get; }
        #endregion


        #region Override
        public override string ToString()
        {
            return Modifier;
        }
        #endregion
    }
}