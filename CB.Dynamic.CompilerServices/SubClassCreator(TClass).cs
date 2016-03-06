using System;
using System.Linq;
using System.Reflection;


namespace CB.Dynamic.CompilerServices
{
    public class SubClassCreator<TClass>: ClassCreator where TClass: class
    {
        #region  Constructors & Destructors
        public SubClassCreator()
        {
            _namespaceUsings.Add($"using {GetTClassNamespace()};");
            ClassName = GetBaseClassName() + "SubClass";
        }
        #endregion


        #region Methods
        public override string GenerateSourceCode(string classBody)
        {
            var format = GetSourceFormat();
            return string.Format(format, GetUsings(), Namespace,
                $"{ClassName}: {GetBaseClassName()}", classBody);
        }

        public virtual void OverrideMethod(string methodName, string methodBody)
        {
            var method = typeof(TClass).GetMethods()
                                       .FirstOrDefault(
                                           m => m.Name == methodName && IsInstanceVirtual(m));

            if (method == null)
            {
                throw new ArgumentException(
                    $"Cannot find an instance virtual method named {methodName}");
            }

            OverrideMethod(method, methodBody);
        }

        public virtual void OverrideMethod(MethodInfo method, string methodBody)
        {
            if (!IsInstanceVirtual(method))
            {
                throw new ArgumentException($"{nameof(method)} is not an instance virtual method.");
            }

            var methodDefinition = CreateOverrideMethodDefinition(method, methodBody);
            AddMethod(methodDefinition);
        }
        #endregion


        #region Implementation
        private static string CreateMethodParameters(MethodInfo method)
        {
            return string.Join(", ",
                method.GetParameters().Select(p => $"{p.ParameterType} {p.Name}"));
        }

        /*private static string CreateMethodSignature(MethodInfo method)
            => $"{method.ReturnType} {method.Name}({CreateMethodParameters(method)})";*/

        private static string CreateOverrideMethodDefinition(MethodInfo method, string methodBody)
        {
            var methodDefinition =
                $@"{GetMethodAccessModifier(method)} override {CreateMethodSignature(method)}
{INDENT}{INDENT}{{
{methodBody}
{INDENT}{INDENT}}}";
            return methodDefinition;
        }

        private static string GetBaseClassName()
        {
            return typeof(TClass).Name;
        }

        private static string GetMethodAccessModifier(MethodBase method)
        {
            if (method.IsPublic)
            {
                return "public";
            }
            if (method.IsFamilyOrAssembly)
            {
                return "protected internal";
            }
            if (method.IsAssembly)
            {
                return "internal";
            }
            return method.IsFamily ? "protected" : "private";
        }

        private static string GetTClassNamespace()
        {
            return typeof(TClass).Namespace;
        }

        private static bool IsInstanceVirtual(MethodInfo method)
        {
            return !method.Attributes.HasFlag(MethodAttributes.Static) &&
                   method.Attributes.HasFlag(MethodAttributes.Virtual);
        }
        #endregion
    }
}