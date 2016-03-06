using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;


namespace CB.Dynamic.CompilerServices
{
    public class EventHandlerAttacher
    {
        #region Fields
        private const string ASSEMBLY_NAME = "MyAssembly";
        private const string FIELD_NAME = "myDelegate";
        private const string FILE_NAME = MODULE_NAME + ".dll";
        private const string HANDLER_NAME = "myHandler";
        private const string INVOKE_METHOD = "Invoke";
        private const string MODULE_NAME = "MyModule";
        private const string TYPE_NAME = "myType";
        #endregion


        #region Methods
        public static void Attach(EventInfo eventInfo, object target, Action action)
        {
            Attach(eventInfo, target, (Delegate)action);
        }

        public static void Attach<T>(EventInfo eventInfo, object target, Func<T> func)
        {
            Attach(eventInfo, target, (Delegate)func);
        }

        public static void Attach(EventInfo eventInfo, object target, Delegate @delegate)
        {
            var myType = CreateDynamicType(eventInfo, @delegate.GetType());
            var myObject = Activator.CreateInstance(myType, @delegate);
            var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, myObject, HANDLER_NAME);
            eventInfo.AddEventHandler(target, handler);
        }
        #endregion


        #region Implementation
        private static Type CreateDynamicType(EventInfo eventInfo, Type fieldType)
        {
            var asmName = new AssemblyName(ASSEMBLY_NAME);
            var asmBuild = DefineAssembly(asmName);
            var modBuild = DefineModule(asmBuild, asmName);
            var typBuild = DefineType(modBuild);
            var fldBuild = DefineField(fieldType, typBuild);
            DefineConstructor(typBuild, fldBuild);
            DefineMethod(eventInfo, typBuild, fldBuild);
            var myType = typBuild.CreateType();

#if DEBUG
            asmBuild.Save(FILE_NAME);
#endif
            return myType;
        }

        private static AssemblyBuilder DefineAssembly(AssemblyName assemblyName)
        {
#if DEBUG
            return AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
#else
            return AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#endif
        }

        private static void DefineConstructor(TypeBuilder typeBuilder, FieldInfo fieldInfo)
        {
            var ctorBuild = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard,
                new[] { fieldInfo.FieldType });
            var ctorGen = ctorBuild.GetILGenerator();
            ctorGen.Emit(OpCodes.Ldarg_0);
            ctorGen.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));
            ctorGen.Emit(OpCodes.Ldarg_0);
            ctorGen.Emit(OpCodes.Ldarg_1);
            ctorGen.Emit(OpCodes.Stfld, fieldInfo);
            ctorGen.Emit(OpCodes.Ret);
        }

        private static FieldBuilder DefineField(Type fieldType, TypeBuilder typeBuilder)
        {
            return typeBuilder.DefineField(FIELD_NAME, fieldType, FieldAttributes.Private);
        }

        private static void DefineMethod(EventInfo eventInfo, TypeBuilder typeBuilder, FieldInfo fieldInfo)
        {
            var handlerInfo = eventInfo.EventHandlerType.GetMethod(INVOKE_METHOD);
            var handlerParamTypes = handlerInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            var metBuild = typeBuilder.DefineMethod(HANDLER_NAME, MethodAttributes.Public,
                handlerInfo.ReturnType, handlerParamTypes);

            var ilGen = metBuild.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldfld, fieldInfo);
            ilGen.Emit(OpCodes.Callvirt, fieldInfo.FieldType.GetMethod(INVOKE_METHOD));
            ilGen.Emit(OpCodes.Ret);
        }

        private static ModuleBuilder DefineModule(AssemblyBuilder assemblyBuilder, AssemblyName assemblyName)
        {
#if DEBUG
            return assemblyBuilder.DefineDynamicModule(assemblyName.Name, FILE_NAME);
#else
            return assemblyBuilder.DefineDynamicModule(assemblyName.Name);
#endif
        }

        private static TypeBuilder DefineType(ModuleBuilder moduleBuilder)
        {
            return moduleBuilder.DefineType(TYPE_NAME, TypeAttributes.Public | TypeAttributes.Class);
        }
        #endregion
    }
}