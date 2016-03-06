using System;
using System.Linq;
using System.Reflection;
using System.Text;
using CB.CompilerServices;


namespace CB.Dynamic.CompilerServices
{
    public class MethodCreator
    {
        #region Fields
        private const string ONE_INDENT = "    ";
        private int _indentLevel = 2;
        private readonly MethodInfo _methodInfo;
        private string _methodName;
        #endregion


        #region  Constructors & Destructors
        public MethodCreator(MethodInfo methodInfo)
        {
            if (methodInfo == null) throw new ArgumentNullException(nameof(methodInfo));
            _methodInfo = methodInfo;
            AccessModifier = GetAccessModifier(methodInfo);
            IsStatic = methodInfo.IsStatic;
            VirtualState = GetVirtualState(methodInfo);
            MethodName = methodInfo.Name;
        }
        #endregion


        #region  Properties & Indexers
        public AccessModifier AccessModifier { get; set; }

        public int IndentLevel
        {
            get { return _indentLevel; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _indentLevel = value;
            }
        }

        public bool IsStatic { get; set; }

        public string MethodName
        {
            get { return _methodName; }
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentException(nameof(value));
                _methodName = value;
            }
        }

        public VirtualState VirtualState { get; set; }
        #endregion


        #region Methods
        public string Create(string methodBody)
        {
            var indent = CreateIndent();
            return
$@"{indent}{CreateMethodModifier()} {CreatePrototype()}
{indent}{{
{indent}{ONE_INDENT}{methodBody}
{indent}}}";
        }

        public string CreatePrototype()
            =>
                $"{ReturnTypeToString(_methodInfo.ReturnType)} {MethodName}({CreateMethodParameters(_methodInfo)})";
        #endregion


        #region Implementation
        private string CreateIndent()
            => IndentLevel == 0 ? "" : string.Concat(Enumerable.Repeat(ONE_INDENT, IndentLevel));

        private string CreateMethodModifier()
        {
            var sb = new StringBuilder(AccessModifier.ToString());
            if (IsStatic)
            {
                sb.Append(" static");
            }
            switch (VirtualState)
            {
                case VirtualState.None:
                    break;

                case VirtualState.Virtual:
                    sb.Append(" virtual");
                    break;

                case VirtualState.Override:
                    sb.Append(" override");
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return sb.ToString();
        }

        private static string CreateMethodParameters(MethodInfo method)
            => string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType} {p.Name}"));

        private static AccessModifier GetAccessModifier(MethodBase methodInfo)
        {
            if (methodInfo.IsAssembly) return AccessModifier.Internal;
            if (methodInfo.IsFamily) return AccessModifier.Protected;
            if (methodInfo.IsFamilyOrAssembly) return AccessModifier.ProtectedInternal;
            if (methodInfo.IsPrivate) return AccessModifier.Private;
            if (methodInfo.IsPublic) return AccessModifier.Public;
            throw new NotImplementedException();
        }

        private static VirtualState GetVirtualState(MethodBase methodInfo)
        {
            if (!methodInfo.IsVirtual) return VirtualState.None;
            return methodInfo.Attributes.HasFlag(MethodAttributes.NewSlot)
                       ? VirtualState.Virtual : VirtualState.Override;
        }

        private static string ReturnTypeToString(Type returnType)
            => returnType == typeof(void) ? "void" : returnType.ToString();
        #endregion
    }
}