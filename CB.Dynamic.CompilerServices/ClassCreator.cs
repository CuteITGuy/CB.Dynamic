using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CB.CompilerServices;
using CB.Model.Common;
using Microsoft.CSharp;


namespace CB.Dynamic.CompilerServices
{
    public class ClassCreator
    {
        #region Fields
        private const string COMPILER_OPTIONS = "/optimize";
        private const string EVENT_NAME_PATTERN = @"(\w+)\s*;";
        private const string FIELD_NAME_PATTERN = @"(\w+)(?=\s*[=;])";
        protected const int INDENT_COUNT = 4;
        private const string METHOD_NAME_PATTERN = @"(\w+)\s*\(";
        private const string METHOD_PARAMS_PATTERN = @"\((.*?)\)";
        private const string PROPERTY_NAME_PATTERN = @"(\w+)\s+{";
        private const string TYPE_NAME_PATTERN = TYPE_PATTERN + @"\s+\b(\w+)\b";
        private const string TYPE_PATTERN = @"class|delegate|enum|struct";
        private const string TYPE_TYPE_PATTERN = @"\b(" + TYPE_PATTERN + @")\b";
        public static readonly string INDENT = new string(' ', INDENT_COUNT);

        private readonly CompilerParameters _compilerParameters = new CompilerParameters
        {
            CompilerOptions = COMPILER_OPTIONS,
            GenerateExecutable = false,
            GenerateInMemory = true,
            TreatWarningsAsErrors = false,
            WarningLevel = 3
        };

        private readonly SortedDictionary<string, string> _constructors =
            new SortedDictionary<string, string>();

        private readonly SortedDictionary<string, string> _events =
            new SortedDictionary<string, string>();

        private readonly SortedDictionary<string, string> _fields =
            new SortedDictionary<string, string>();

        private readonly SortedDictionary<Key<string, string>, string> _methods =
            new SortedDictionary<Key<string, string>, string>();

        protected readonly SortedSet<string> _namespaceUsings;

        private readonly SortedDictionary<string, string> _properties =
            new SortedDictionary<string, string>();

        private readonly SortedDictionary<Key<string, string>, string> _types =
            new SortedDictionary<Key<string, string>, string>();
        #endregion


        #region  Constructors & Destructor
        public ClassCreator()
        {
            _namespaceUsings = CreateDefaultUsings();
            ClassName = "MyClass";
            Namespace = "MyNamespace";
            AddDefaultReferencedAssemblies();
        }
        #endregion


        #region  Properties & Indexers
        public string ClassName { get; set; }
        public string Namespace { get; set; }
        #endregion


        #region Methods
        public static string CreateMethodDefintion(MethodInfo method, string methodBody,
            AccessModifier accessModifier, ClassScope classScope)
        {
            return "";
        }

        public static string CreateMethodSignature(MethodInfo method)
            => $"{ReturnTypeToString(method.ReturnType)} {method.Name}({CreateMethodParameters(method)})";

        public virtual void AddConstructor(string definition)
            => _constructors.Add(GetMethodParams(definition), definition);

        public void AddEvent(string declaration)
            => _events.Add(GetEventName(declaration), declaration);

        public virtual void AddField(string declaration)
            => _fields.Add(GetFieldName(declaration), declaration);

        public virtual void AddMethod(string definition) => _methods.Add(
            new Key<string, string>(GetMethodName(definition), GetMethodParams(definition)),
            definition);

        public virtual void AddNestedType(string definition)
            => _types.Add(new Key<string, string>(GetTypeType(definition), GetTypeName(definition)),
                definition);

        public virtual void AddProperty(string definition)
            => _properties.Add(GetPropertyName(definition), definition);

        public void AddReferencedAssembly(Assembly referencedAssembly)
        {
            if (referencedAssembly == null) return;

            AddReferencedAssemblyLocation(referencedAssembly.Location);

            foreach (var assembly in referencedAssembly.GetReferencedAssemblies()
                                                       .Select(Assembly.Load))
            {
                AddReferencedAssemblyLocation(assembly.Location);
            }
        }

        public void AddReferencedAssembly(Type type)
        {
            if (type != null)
            {
                AddReferencedAssembly(type.Assembly);
            }
        }

        public virtual Type Compile() => Compile(GenerateClassBody());

        public virtual Type Compile(string classBody)
        {
            using (var compiler = new CSharpCodeProvider())
            {
                var results = compiler.CompileAssemblyFromSource(_compilerParameters,
                    GenerateSourceCode(classBody));

                if (results.Errors.Count > 0)
                {
                    throw new Exception();
                }

                var asm = results.CompiledAssembly;
                return asm.ExportedTypes.FirstOrDefault(t => t.Name == ClassName);
            }
        }

        public virtual string GenerateSourceCode() => GenerateSourceCode(GenerateClassBody());

        public virtual string GenerateSourceCode(string classBody)
            => string.Format(GetSourceFormat(), GetUsings(), Namespace, ClassName, classBody);
        #endregion


        #region Implementation
        private void AddDefaultReferencedAssemblies()
        {
            var defaultReferencedAssemblies = new[]
            {
                "Microsoft.CSharp.dll",
                "System.dll",
                "System.Core.dll",
                "System.Data.dll",
                "System.Data.DataSetExtensions.dll",
                "System.Net.Http.dll",
                "System.Xml.dll",
                "System.Xml.Linq.dll"
            };
            _compilerParameters.ReferencedAssemblies.AddRange(defaultReferencedAssemblies);
        }

        private void AddReferencedAssemblyLocation(string referencedAssembly)
        {
            if (!ReferencedAssemblyExisted(referencedAssembly))
            {
                _compilerParameters.ReferencedAssemblies.Add(referencedAssembly);
            }
        }

        private static SortedSet<string> CreateDefaultUsings()
        {
            var defaultUsings = new SortedSet<string>
            {
                "using System;",
                "using System.Collections.Generic;",
                "using System.Linq;",
                "using System.Text;",
                "using System.Threading.Tasks;"
            };
            return defaultUsings;
        }

        private static string CreateMethodParameters(MethodInfo method)
            => string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType} {p.Name}"));

        private static string CreateRegion<TKey>(string regionName,
            IDictionary<TKey, string> members,
            bool paddingLine = true, int lineBreaks = 2)
        {
            if (members.Count == 0)
            {
                return "";
            }

            var padding = paddingLine
                              ? string.Concat(Enumerable.Repeat(Environment.NewLine, 3))
                              : "";

            var line = string.Concat(Enumerable.Repeat(Environment.NewLine, lineBreaks));
            return
                $@"{padding}{INDENT}{INDENT}#region {regionName}
{INDENT}{INDENT}{string.Join(line + INDENT + INDENT, members.Values)}
{INDENT}{INDENT}#endregion";
        }

        private static string ExtractString(string input, string pattern)
            => Regex.Match(input.Replace(Environment.NewLine, ""), pattern).Groups[1].Value;

        private string GenerateClassBody()
        {
            var sb = new StringBuilder();
            sb.Append(CreateRegion("Nested Types", _types, false));
            sb.Append(CreateRegion("Fields", _fields, sb.Length > 0, 1));
            sb.Append(CreateRegion("Constructors", _constructors, sb.Length > 0));
            sb.Append(CreateRegion("Properties", _properties, sb.Length > 0));
            sb.Append(CreateRegion("Events", _events, sb.Length > 0, 1));
            sb.Append(CreateRegion("Methods", _methods, sb.Length > 0));
            return sb.ToString();
        }

        protected static string GetEventName(string declaration) => ExtractString(declaration, EVENT_NAME_PATTERN);

        protected static string GetFieldName(string declaration) => ExtractString(declaration, FIELD_NAME_PATTERN);

        protected static string GetMethodName(string definition) => ExtractString(definition, METHOD_NAME_PATTERN);

        protected static string GetMethodParams(string definition) => ExtractString(definition, METHOD_PARAMS_PATTERN);

        protected static string GetPropertyName(string definition) => ExtractString(definition, PROPERTY_NAME_PATTERN);

        protected static string GetSourceFormat() => File.ReadAllText("source.txt");

        private static string GetTypeName(string definition)
            => ExtractString(definition, TYPE_NAME_PATTERN);

        private static string GetTypeType(string definition)
            => ExtractString(definition, TYPE_TYPE_PATTERN);

        protected virtual string GetUsings() => string.Join(Environment.NewLine, _namespaceUsings);

        private bool ReferencedAssemblyExisted(string referencedAssembly)
            => _compilerParameters.ReferencedAssemblies.OfType<string>().Any(
                asm =>
                {
                    var fileName = Path.GetFileName(asm);
                    return fileName != null && fileName.Equals(Path.GetFileName(referencedAssembly),
                        StringComparison.InvariantCultureIgnoreCase);
                });

        private static string ReturnTypeToString(Type returnType)
            => returnType == typeof(void) ? "void" : returnType.ToString();
        #endregion
    }
}