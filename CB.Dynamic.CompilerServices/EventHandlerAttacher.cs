using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;


namespace CB.Dynamic.CompilerServices
{
    public class EventHandlerAttacher
    {
        #region Fields
        private const string ASSEMBLY_NAME = "__MyAssembly";
        private const string FIELD_NAME = "__myDelegate";
        private const string FILE_NAME = MODULE_NAME + ".dll";
        private const string HANDLER_NAME = "__myHandler";
        private const string INVOKE_METHOD = "Invoke";
        private const string MODULE_NAME = "__MyModule";
        private const string TYPE_NAME = "__MyType";
        #endregion


        #region Methods
        public static Delegate Attach(object target, EventInfo eventInfo, Action body)
            => Attach(target, eventInfo, (Delegate)body);

        public static Delegate Attach<T>(object target, string eventName, Func<T> body)
            => Attach(target, eventName, (Delegate)body);

        public static Delegate Attach(object target, string eventName, Action body)
            => Attach(target, eventName, (Delegate)body);

        public static Delegate Attach<T>(object target, EventInfo eventInfo, Func<T> func)
            => Attach(target, eventInfo, (Delegate)func);

        public static Delegate Attach(object target, string eventName, Delegate body)
            => Attach(target, target.GetType().GetEvent(eventName), body);

        public static Delegate Attach(object target, EventInfo eventInfo, Delegate body)
        {
            var type = CreateDynamicType(eventInfo, body.GetType());
            var instance = Activator.CreateInstance(type, body);
            var handlerDelegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, instance, HANDLER_NAME);
            eventInfo.AddEventHandler(target, handlerDelegate);
            return handlerDelegate;
        }

        public static void Detach(object target, EventInfo eventInfo, Delegate handler)
            => eventInfo.RemoveEventHandler(target, handler);

        public static void Detach(object target, string eventName, Delegate handler)
            => Detach(target, target.GetType().GetEvent(eventName), handler);
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
            var objConstructor = typeof(object).GetConstructor(new Type[0]);
            Debug.Assert(objConstructor != null, "objConstructor != null");
            ctorGen.Emit(OpCodes.Call, objConstructor);
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