using System;
using System.Collections.Generic;
using System.Reflection;
using CB.CompilerServices;


namespace CB.Dynamic.CompilerServices
{
    public class DynamicEventHandler
    {
        #region Fields
        /*private readonly Dictionary<EventInfo, Tuple<string, string>> _handlers =
            new Dictionary<EventInfo, Tuple<string, string>>();*/
        private readonly HashSet<EventHandlerInfo> _handlers = new HashSet<EventHandlerInfo>();
        #endregion


        #region Methods
        public static string CreateHandlerDefinition(EventInfo eventInfo, string handlerName,
            string eventBody)
        {
            var methodCreator = new MethodCreator(GetEventHandlerMethodInfo(eventInfo))
            {
                AccessModifier = AccessModifier.Public,
                IndentLevel = 2,
                IsStatic = true,
                VirtualState = VirtualState.None,
                MethodName = handlerName
            };

            return methodCreator.Create(eventBody);
        }

        public void AddHandler(string handlerName, EventInfo eventInfo, string eventBody)
            => _handlers.Add(new EventHandlerInfo(eventInfo, handlerName,
                CreateHandlerDefinition(eventInfo, handlerName, eventBody)));

        public void AddHandler(EventInfo eventInfo, string eventBody)
            => AddHandler(CreateHandlerName(eventInfo), eventInfo, eventBody);

        public void Apply<TTarget>(TTarget target)
        {
            var classCreator = new ClassCreator();
            classCreator.AddReferencedAssembly(typeof(TTarget));
            foreach (var eventHandlerInfo in _handlers)
            {
                classCreator.AddMethod(eventHandlerInfo.HandlerDefinition);
            }

            var type = classCreator.Compile();
            foreach (var eventHandlerInfo in _handlers)
            {
                var handlerDelegate =
                    Delegate.CreateDelegate(eventHandlerInfo.EventInfo.EventHandlerType, type,
                        eventHandlerInfo.HandlerName);
                eventHandlerInfo.EventInfo.AddEventHandler(target, handlerDelegate);
            }
        }
        #endregion


        #region Implementation
        private static string CreateHandlerName(EventInfo eventInfo) => "On" + eventInfo.Name;

        private static MethodInfo GetEventHandlerMethodInfo(EventInfo eventInfo)
            => eventInfo.EventHandlerType.GetMethod("Invoke");
        #endregion
    }
}