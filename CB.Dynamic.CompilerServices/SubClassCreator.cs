using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CB.CompilerServices;


namespace CB.Dynamic.CompilerServices
{
    public class SubClassCreator: ClassCreator
    {
        public delegate string MethodInfoCallback(MethodInfo methodInfo);


        #region Fields
        public const string BASE_CALLING = "$base$";
        private readonly Type _baseType;
        #endregion


        #region  Constructors & Destructors
        public SubClassCreator(Type baseType)
        {
            CheckValidBaseType(baseType);

            _baseType = baseType;
            _namespaceUsings.Add($"using {GetBaseTypeNamespace()};");
            ClassName = baseType.Name + "SubClass";
            AddReferencedAssembly(baseType);
        }
        #endregion


        #region Methods
        public override string GenerateSourceCode(string classBody)
        {
            var format = GetSourceFormat();
            return string.Format(format, GetUsings(), Namespace,
                $"{ClassName}: {GetBaseTypeName()}", classBody);
        }

        public virtual IEnumerable<MethodInfo> GetVirtualMethods()
            => _baseType.GetMethods(BindingFlags.Instance | BindingFlags.Public |BindingFlags.NonPublic).Where(IsValidVirtualMethod);

        public virtual void OverrideMethod(string methodName, MethodInfoCallback callback,
            BaseCallingStrategy baseCallingStrategy)
            =>
                OverrideMethod(methodName, callback(GetVirtualMethod(methodName)),
                    baseCallingStrategy);

        public virtual void OverrideMethod(string methodName, string methodBody,
            BaseCallingStrategy baseCallingStrategy)
            => OverrideMethod(GetVirtualMethod(methodName), methodBody, baseCallingStrategy);

        public virtual void OverrideMethod(MethodInfo method, string methodBody,
            BaseCallingStrategy baseCallingStrategy)
        {
            if (!IsValidVirtualMethod(method))
            {
                throw new ClassCreatorException(
                    $"{nameof(method)} is not an instance virtual method.");
            }

            var methodDefinition = CreateOverrideMethodDefinition(method, methodBody,
                baseCallingStrategy);
            AddMethod(methodDefinition);
        }
        #endregion


        #region Implementation
        private static void CheckValidBaseType(Type baseType)
        {
            if (baseType == null) throw new ClassCreatorException($"{nameof(baseType)} is null.");
            if (!baseType.IsClass)
                throw new ClassCreatorException($"{nameof(baseType)} is not class type.");
            if (baseType.IsSealed)
                throw new ClassCreatorException($"{nameof(baseType)} is sealed.");
            if (!baseType.IsPublic)
                throw new ClassCreatorException($"{nameof(baseType)} is not public.");
        }

        private static string CreateOverrideMethodDefinition(MethodInfo method, string methodBody,
            BaseCallingStrategy baseCallingStrategy)
        {
            var result =
                $@"{GetVirtualMethodAccessModifier(method)} override {CreateMethodSignature(method)}
{INDENT}{INDENT}{{
{
                    ProcessMethodBody(method, methodBody, baseCallingStrategy)}
{INDENT}{INDENT}}}";
            return result;
        }

        private static string GetBaseCalling(MethodBase method)
            => $"base.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.Name))});";

        private string GetBaseTypeName() => _baseType.Name;

        private string GetBaseTypeNamespace() => _baseType.Namespace;

        private MethodInfo GetVirtualMethod(string methodName)
        {
            var method = _baseType.GetMethods()
                                  .FirstOrDefault(
                                      m => m.Name == methodName && IsValidVirtualMethod(m));
            if (method == null)
            {
                throw new ArgumentException(
                    $"Cannot find an instance virtual method named {methodName}");
            }

            return method;
        }

        private static string GetVirtualMethodAccessModifier(MethodBase method)
            => method.IsPublic ? "public"
                   : method.IsFamilyOrAssembly || method.IsFamily ? "protected"
                         : method.IsAssembly ? "internal"
                               : "private";

        private static string InsertBaseCalling(MethodBase method, string methodBody)
        {
            return methodBody.Replace(BASE_CALLING, GetBaseCalling(method));
        }

        private static string InsertBaseCallingAndReturn(MethodInfo method, string methodBody,
            bool returnsBaseValue = true)
        {
            var baseCallingInserted = InsertBaseCalling(method, methodBody);

            if (method.ReturnType == typeof(void))
            {
                return baseCallingInserted;
            }

            if (!returnsBaseValue)
            {
                return baseCallingInserted + $@"{INDENT}{INDENT}{INDENT}return default({method.ReturnType});";
            }

            const string RETURN_NAME = "__returnValue";
            var baseCalling = $"var {RETURN_NAME} = {GetBaseCalling(method)}";

            return
$@"{methodBody.Replace(BASE_CALLING, baseCalling)}
{INDENT}{INDENT}{INDENT}return {RETURN_NAME};";
        }

        private static bool IsValidVirtualMethod(MethodInfo method)
        {
            var attr = method.Attributes;
            var isVirtual = (attr & MethodAttributes.Virtual) == MethodAttributes.Virtual;
            var isInstance = (attr & MethodAttributes.Static) == 0;
            var isAccessible = (attr & MethodAttributes.Public) == MethodAttributes.Public ||
                               (attr & MethodAttributes.Family) == MethodAttributes.Family ||
                               (attr & MethodAttributes.FamORAssem) == MethodAttributes.FamORAssem;
            var isSealed = (attr & MethodAttributes.Final) == MethodAttributes.Final;
            var isSpecialName = (attr & MethodAttributes.SpecialName) ==
                                MethodAttributes.SpecialName;
            var isFinalize = method.Name == "Finalize" && method.GetParameters().Length == 0;

            return isVirtual && isInstance && isAccessible && !isSealed && !isSpecialName &&
                   !isFinalize;
        }

        private static string ProcessMethodBody(MethodInfo method, string methodBody,
            BaseCallingStrategy baseCallingStrategy)
        {
            switch (baseCallingStrategy)
            {
                case BaseCallingStrategy.None:
                    return methodBody;

                case BaseCallingStrategy.ReturnDefault:
                    return InsertBaseCallingAndReturn(method, methodBody, false);

                case BaseCallingStrategy.AutoReturn:
                    return InsertBaseCallingAndReturn(method, methodBody);

                case BaseCallingStrategy.NotReturn:
                    return InsertBaseCalling(method, methodBody);

                default:
                    throw new NotImplementedException();
            }
        }
        #endregion
    }
}